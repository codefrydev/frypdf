# STRICT PERFORMANCE MANDATE: Zero-Lag, 60+ FPS & Responsive Architecture

**APPLIES TO ALL AI AGENTS AND DEVELOPERS**:
FryPDF is an interactive, real-time desktop document studio. **UI lag, frame drops, blocking operations, stuttering transitions, and high memory retention are strictly forbidden.**
Whenever you write or modify code in FryPDF, you **MUST ALWAYS THINK IN TERMS OF PERFORMANCE OPTIMIZATION**.

---

## 1. Zero UI Thread Blocking
- **Never Run Heavy Operations on the UI Thread**:
  - PDF deconstruction, parsing, Skia rendering, QuestPDF generation, OCR execution, AI inference, and disk/network I/O **must always be offloaded** via `Task.Run` or asynchronous commands.
- **Never Block Asynchronously**:
  - Calling `.Result`, `.Wait()`, or `.GetAwaiter().GetResult()` on the Avalonia UI thread is strictly forbidden.
- **Always Pass `CancellationToken`**:
  - Accept and propagate cancellation tokens throughout long-running operations so users can instantly cancel actions or switch pages without background worker orphan threads.

---

## 2. Instant Navigation & View Caching (0ms Latency)
- **Cache Contributed Plugin Views**:
  - In navigation controllers (such as `HomeViewModel`), always cache dynamically created views in an instance dictionary (`_dynamicViewCache`). Never destroy and re-instantiate heavy visual trees when navigating between tabs.
- **Pre-Mounted Static Views**:
  - Built-in primary pages (Overview Dashboard, Tools Studio, PDF Reader Landing) should have pre-mounted XAML elements in `HomeView.axaml` to guarantee 0ms instantaneous switching without layout compilation overhead.

---

## 3. Virtualization & Recycling
- **Always Virtualize Dynamic Collections**:
  - Always use `ItemsRepeater` or `VirtualizingStackPanel` with `ScrollUnit="Pixel"` for lists with variable or large items (document thumbnails, tool card grids, audit logs, tabular data).
  - Never use an unvirtualized `StackPanel` inside a `ScrollViewer` for collections larger than 10 items.

---

## 4. Continuous Input Throttling & Debouncing
- **Pinch-to-Zoom Math**:
  - Calculate trackpad pinch gestures multiplicatively and clamp strictly:
    `Math.Clamp(Math.Round(currentZoom * (1.0 + delta), 3), 0.1, 5.0)` to eliminate exponential explosion.
- **Debounced Filters**:
  - Debounce search queries and text filters (150–250ms) to avoid executing filter passes on every keystroke.
- **Throttled Sliders**:
  - Do not trigger expensive Skia repaints or document recompilations on every slider micro-tick; throttle updates during drag.

---

## 5. Memory Management & Unmanaged Resource Disposal
- **Large Object Heap (LOH) Avoidance**:
  - Any byte array $\ge 85\text{ KB}$ is allocated on the LOH and causes GC fragmentation. Use `ArrayPool<byte>.Shared` and `MemoryStream` buffers.
  - Never store Base64 strings in memory for image or vector assets; store raw `byte[] ImageData` directly and compute Base64 only lazily for JSON export.
- **Dispose Unmanaged Skia Objects Immediately**:
  - `SKBitmap`, `SKImage`, `SKSurface`, `SKData`, and Avalonia `WriteableBitmap` hold native unmanaged pointers.
  - Always wrap temporary Skia objects in `using` blocks.
  - Always dispose old bitmaps before assigning replacement preview bitmaps.
- **Weak Subscriptions**:
  - Use `WeakReferenceMessenger` for pub/sub messaging across ViewModels to avoid memory retention cycles that prevent garbage collection.

---

## 6. Rendering & Frame Budgets
- **Frame Budgets**:
  - Maintain 16ms per frame (60 FPS) and 8ms per frame (120 FPS).
- **Favor `RenderTransform` Over Layout Mutations**:
  - Use GPU-composited `RenderTransform` rather than mutating layout properties (`Margin`, `Width`, `Height`) during animations or gesture interactions to avoid triggering costly synchronous layout reflows.
- **Keep Visual Trees Shallow**:
  - Avoid deeply nested `Border` and `Grid` hierarchies.

---

## 7. Real-Time Plugin Work (Audio & Deadline Threads)
Sections 1-6 are all about UI responsiveness and frame rate. They are not sufficient, because
**FryPDF loads plugins into its own process**, and a plugin may own a thread with a hard
deadline that the host knows nothing about.

The motivating case: the Music Player plugin plays audio through SoundFlow, whose playback
callback is a reverse P/Invoke from miniaudio's real-time thread **executing managed code**.
That makes it a CLR-attached thread — so it is suspended by every garbage collection, it can
block on any lock the UI thread also takes, and it competes for the same disk. A UI-thread stall
of one second against ~30ms of audio buffer is not a dropped frame; it is a guaranteed, audible
dropout.

### Rules for plugin authors
- **Never do I/O or decoding on a deadline thread.** Prefer a background-buffered data source
  (SoundFlow: `ChunkedDataProvider` or `AssetDataProvider`, never `StreamDataProvider`) so the
  callback never touches a `FileStream`.
- **Buffer for at least 200ms.** Never accept a backend's low-latency default when latency does
  not matter. Music playback is not synchronized to anything — trade latency for resilience.
- **Never allocate per callback.** Every allocation raises the collection rate that suspends
  your own thread.
- **Never take a lock a deadline thread also takes** from the UI thread, and never hold one
  across expensive work. .NET locks do not inherit priority, so this inverts.
- **`Dispatcher.UIThread.Post`, never `Invoke`**, from a deadline thread. `Invoke` blocks on the
  UI thread, which is the thing most likely to be stalled.
- **Declare the work** via `IRealtimeWorkCoordinator.BeginRealtimeWork(...)` (resolved from the
  `IServiceProvider` passed to your view factory), and dispose the handle when the work stops.
  The host raises the GC to `SustainedLowLatency` while any declaration is outstanding. This is
  a mitigation, not a licence to skip the rules above: it suppresses blocking gen-2 collections
  but not gen-0/1.

### Rules for all plugin authors, real-time or not
- **Set `DispatcherPriority` explicitly on every timer.** The parameterless `DispatcherTimer`
  constructor is `Background` — *below* `Input` — so host interaction starves it. Decoration
  belongs at or below `Background`; never animate at `Render`, which outranks the host's own
  input processing.
- **Implement `IDisposable`.** The host disposes an overlay's content and its `DataContext` when
  the overlay is unregistered. Release native handles, audio devices and timers there — a
  running `DispatcherTimer` roots your view model and prevents collection.

### Rules for the host
- Any code path a plugin's deadline thread can reach must be non-blocking. Concretely: keep
  `TraceListener.IsThreadSafe` true so `Debug.WriteLine` does not serialize process-wide, and
  keep the plugin settings store off the synchronous-write path.
- Treat editor allocation churn as an audio-quality issue, not only a frame-rate issue.

---

## 8. Verification
- Run navigation and gesture performance tests:
  ```bash
  dotnet test --filter "FullyQualifiedName~GestureAndNavigationTests"
  ```
- Verify zero warnings (`TreatWarningsAsErrors=true`).
- Check the diagnostic log for `UiStall` entries. `UiThreadWatchdog` reports at two tiers:
  Warning at 300ms (a visible freeze) and Debug at 50ms (already an audible dropout for a
  real-time plugin). Each entry carries the gen-0/1/2 collection delta, which separates a
  GC-driven stall from long synchronous work or lock contention.
