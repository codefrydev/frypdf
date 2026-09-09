using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views;

public partial class NewDocumentPageView : UserControl
{
    private bool _previewsRasterized;
    private IDisposable? _visibilitySubscription;

    /// <summary>
    /// Parked in the render host whenever it is not rendering a specific template.
    /// </summary>
    /// <remarks>
    /// The host lives inside this page, so it inherits this page's DataContext — a
    /// <see cref="HomeViewModel"/> — unless one is set explicitly. TemplatePagePreview declares
    /// <c>x:DataType="vm:PageViewModel"</c>, so its compiled bindings would try to cast a
    /// HomeViewModel to a PageViewModel and throw. Parking an empty page here keeps the type
    /// correct at all times; its zero size means nothing is drawn.
    /// </remarks>
    private readonly PageViewModel _idlePreviewContext = new();

    public NewDocumentPageView()
    {
        InitializeComponent();

        // Assign before the first layout pass, so the inherited DataContext never reaches
        // the preview's compiled bindings.
        var host = this.FindControl<Controls.TemplatePagePreview>("PreviewRasterHost");
        if (host != null) host.DataContext = _idlePreviewContext;
    }

    /// <summary>
    /// Rasterizes the template thumbnails the first time this page is shown.
    /// </summary>
    /// <remarks>
    /// This lives in the view rather than the view model because it needs the attached
    /// <c>PreviewRasterHost</c> — RenderTargetBitmap on a detached control yields an empty
    /// bitmap. Work is spread one template per dispatcher frame: Avalonia rendering is
    /// UI-thread-only, so the alternative is a single block that freezes the UI for the whole
    /// batch. Cards show a placeholder until their own bitmap arrives.
    /// </remarks>
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        // Deliberately not rasterizing here. This page is mounted while the window is still
        // being constructed, so it is attached but has no TopLevel yet — RenderTargetBitmap
        // needs one. Waiting for the page to actually become visible also means the work is
        // never done for a page the user does not open.
        _visibilitySubscription ??= this.GetObservable(IsVisibleProperty)
            .Subscribe(new AnonymousObserver<bool>(isVisible =>
            {
                if (isVisible) QueuePreviewRasterization();
            }));
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _visibilitySubscription?.Dispose();
        _visibilitySubscription = null;
        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        // A new HomeViewModel brings a fresh set of template cards.
        _previewsRasterized = false;
        if (IsVisible) QueuePreviewRasterization();
    }

    /// <summary>Minimal observer so the page can react to its own visibility without Rx.</summary>
    private sealed class AnonymousObserver<T> : IObserver<T>
    {
        private readonly Action<T> _onNext;
        public AnonymousObserver(Action<T> onNext) => _onNext = onNext;
        public void OnCompleted() { }
        public void OnError(Exception error) { }
        public void OnNext(T value) => _onNext(value);
    }

    private void QueuePreviewRasterization()
    {
        if (_previewsRasterized) return;
        if (DataContext is not HomeViewModel home) return;
        if (VisualRoot == null) return;

        var host = this.FindControl<Controls.TemplatePagePreview>("PreviewRasterHost");
        var surface = this.FindControl<Canvas>("PreviewRenderSurface");
        if (host == null || surface == null) return;

        var pending = new Queue<TemplateCardViewModel>(
            home.AllTemplates.Where(t => !t.IsBlank && t.PreviewImage == null));
        if (pending.Count == 0) return;

        _previewsRasterized = true;

        var sw = Stopwatch.StartNew();
        int rendered = 0;
        int failed = 0;

        void RenderNext()
        {
            if (pending.Count == 0)
            {
                // Park an empty page rather than null: null would fall back to inheriting this
                // page's HomeViewModel and break the preview's compiled bindings again.
                host.DataContext = _idlePreviewContext;
                AppLogService.Instance.LogDuration(
                    "Templates",
                    $"Rasterized {rendered} template preview(s)" +
                    (failed > 0 ? $" ({failed} failed)" : string.Empty) +
                    $" in {sw.ElapsedMilliseconds}ms.",
                    sw.ElapsedMilliseconds,
                    warnAboveMs: 4000);
                return;
            }

            var card = pending.Dequeue();
            var bitmap = TemplatePreviewRasterizer.Render(card.PagePreview, host, surface);

            if (bitmap != null)
            {
                card.PreviewImage = bitmap;
                rendered++;
            }
            else
            {
                failed++;
                if (failed == 1)
                {
                    AppLogService.Instance.Log(AppLogLevel.Warning, "Templates",
                        $"Template preview rasterization failed: {TemplatePreviewRasterizer.LastFailureReason ?? "unknown"}.");
                }
            }

            Dispatcher.UIThread.Post(RenderNext, DispatcherPriority.Background);
        }

        Dispatcher.UIThread.Post(RenderNext, DispatcherPriority.Background);
    }
}
