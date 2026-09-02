# Bug: PDF Viewer Continuous Scrolling Page Rendering & Active Page Synchronization

- **ID**: `BUG-001`
- **Component**: `PdfEditorApp` -> `PdfViewerView` / `PdfViewerViewModel`
- **Status**: Open / Postponed for later resolution
- **Reported Date**: 2026-09-02
- **Related Files**:
  - [`src/PdfEditorApp/Views/PdfViewerView.axaml`](../src/PdfEditorApp/Views/PdfViewerView.axaml)
  - [`src/PdfEditorApp/Views/PdfViewerView.axaml.cs`](../src/PdfEditorApp/Views/PdfViewerView.axaml.cs)
  - [`src/PdfEditorApp/ViewModels/PdfViewerViewModel.cs`](../src/PdfEditorApp/ViewModels/PdfViewerViewModel.cs)
  - [`src/PdfEditorApp/Views/Controls/PdfTextOverlayControl.cs`](../src/PdfEditorApp/Views/Controls/PdfTextOverlayControl.cs)

---

## 1. Description & Symptoms

When viewing a multi-page PDF document in Continuous Scroll Mode:
1. **Scrolling Does Not Render Scrolled-To Pages**:
   - As the user scrolls down through the document (e.g. from Page 1 to Page 18), the scrolled-to page displays as a blank card showing only the placeholder icon and the label (`"Page 18"`).
   - The high-resolution rendered Skia bitmap for the page (`Bitmap`) is not loaded/displayed on the screen while scrolling.
2. **Left Thumbnail Rail Does Not Synchronize**:
   - The thumbnail rail on the left continues to highlight Page 1 (blue border) and does not update to track the currently visible page in the viewport.
3. **Clicking Immediately Fixes It**:
   - Clicking directly anywhere on the page (e.g. clicking on Page 18) immediately causes the page to render and display its contents, and synchronizes the active page state.

---

## 2. Steps to Reproduce

1. Launch the desktop application:
   ```bash
   dotnet run --project src/PdfEditorApp/PdfEditorApp.csproj
   ```
2. Open any multi-page PDF document (e.g. 20+ pages).
3. Use the trackpad, mouse wheel, or scrollbar thumb to scroll down to Page 15–20.
4. **Observe**:
   - The page in the viewport displays the document icon placeholder and page number, but no document content.
   - The thumbnail rail remains on Page 1.
5. Click on the page:
   - **Observe**: The document content renders immediately upon clicking.

---

## 3. Technical Investigation & Findings So Far

### What We Know
- **Why clicking works**:
  - In `PdfTextOverlayControl.OnPointerPressed`:
    - The control captures the click directly on the bound page item (`Page = page`).
    - It invokes `vm.SelectPage(page)`.
    - `SelectPage` sets `CurrentPageNumber = page.PageNumber`.
    - `OnCurrentPageNumberChanged` triggers `EnsurePageRendered(pageNumber)`, which rasterizes the page via Skia and sets `page.Bitmap = bmp`.
- **What happens during scrolling**:
  - `ContinuousScrollViewer` scrolls the content on screen.
  - Visible-page calculation is handled by `ResolveVisiblePages` and `OnContinuousScrollOffsetChanged`.
  - Possible issues to investigate when resuming:
    1. **Event Delivery**: Verify whether `ScrollChanged` or `OffsetProperty` events are firing during user gestures on macOS.
    2. **Coordinate Offset**: In `ResolveVisiblePages`, verify whether `viewer.Offset.Y` accurately reflects the scroll offset of the items in Avalonia across various DPI scales.
    3. **Render Lock Contention**: `StartBackgroundWorker` in `PdfViewerViewModel` loops through pages extracting thumbnails and text geometry under `_renderLock`. Check if `EnsurePageRendered` is waiting on `_renderLock` or if `_pendingForegroundRenders` is being blocked.
    4. **Virtualization vs Plain StackPanel**: `ContinuousItemsControl` uses a standard `StackPanel`. Check if an `ItemsRepeater` or `VirtualizingStackPanel` with an `ILogicalScrollable` scroll provider would be more robust for large documents.

---

## 4. Next Steps When Resuming

1. Add temporary console/debug logging inside `OnContinuousViewerScrollChanged` and `OnContinuousScrollOffsetChanged` to inspect `viewer.Offset.Y`, `detectedPageNum`, and `firstVisiblePage`.
2. Inspect if `RenderPageAtScale` is called and whether the resulting `Bitmap` is assigned to `page.Bitmap` on the UI thread.
3. Test alternative viewport intersection detection (e.g. checking control visual tree positions directly or subscribing to item layout updates).
