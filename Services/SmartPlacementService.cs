using System;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Services;

public class SmartPlacementService : ISmartPlacementService
{
    private const double DefaultMargin = 28.0;
    private const double CascadeOffset = 22.0;

    private double _viewportCenterX = 400.0;
    private double _viewportCenterY = 300.0;
    private double _visibleWidth = 800.0;
    private double _visibleHeight = 600.0;
    private bool _hasViewport;

    private (double X, double Y)? _pendingContextMenuPoint;
    private double _lastBaseX = double.NaN;
    private double _lastBaseY = double.NaN;
    private int _cascadeCount;

    public void UpdateViewport(double pageCenterX, double pageCenterY, double visibleWidth, double visibleHeight)
    {
        _viewportCenterX = pageCenterX;
        _viewportCenterY = pageCenterY;
        _visibleWidth = visibleWidth;
        _visibleHeight = visibleHeight;
        _hasViewport = true;
    }

    public void SetContextMenuPointer(double pageX, double pageY)
    {
        _pendingContextMenuPoint = (pageX, pageY);
    }

    public void ClearContextMenuPointer()
    {
        _pendingContextMenuPoint = null;
    }

    public void ResetCascade()
    {
        _cascadeCount = 0;
        _lastBaseX = double.NaN;
        _lastBaseY = double.NaN;
    }

    public (double X, double Y) GetPlacementPosition(
        PageViewModel page,
        double elementWidth,
        double elementHeight,
        bool isContextMenuTriggered = false)
    {
        double targetX;
        double targetY;

        double pageWidth = page.Width > 0 ? page.Width : 800.0;
        double pageHeight = page.Height > 0 ? page.Height : 1131.0;

        if (_pendingContextMenuPoint.HasValue)
        {
            var pt = _pendingContextMenuPoint.Value;
            _pendingContextMenuPoint = null;

            // When placed via context menu right-click:
            // Place top-left near cursor with subtle offset so pointer doesn't obstruct content
            targetX = pt.X - (elementWidth > 120 ? 30.0 : elementWidth / 2.0);
            targetY = pt.Y - 10.0;
        }
        else if (_hasViewport)
        {
            // Viewport-center placement (Industry Standard: Figma / Canva / Miro)
            targetX = _viewportCenterX - (elementWidth / 2.0);
            targetY = _viewportCenterY - (elementHeight / 2.0);
        }
        else
        {
            // Fallback default center-top
            targetX = Math.Max(DefaultMargin, (pageWidth - elementWidth) / 2.0);
            targetY = 180.0;
        }

        double baseTargetX = targetX;
        double baseTargetY = targetY;

        // Sequential cascading offset when inserting consecutive elements at same base position
        if (!double.IsNaN(_lastBaseX) && !double.IsNaN(_lastBaseY) &&
            Math.Abs(baseTargetX - _lastBaseX) < 2.0 && Math.Abs(baseTargetY - _lastBaseY) < 2.0)
        {
            _cascadeCount = (_cascadeCount + 1) % 6;
            targetX += _cascadeCount * CascadeOffset;
            targetY += _cascadeCount * CascadeOffset;
        }
        else
        {
            _cascadeCount = 0;
        }

        _lastBaseX = baseTargetX;
        _lastBaseY = baseTargetY;

        // Boundary and margin clamping
        double maxX = Math.Max(DefaultMargin, pageWidth - elementWidth - DefaultMargin);
        targetX = Math.Clamp(targetX, DefaultMargin, maxX);

        double maxY = Math.Max(DefaultMargin, pageHeight - elementHeight - DefaultMargin);
        targetY = Math.Clamp(targetY, DefaultMargin, maxY);

        return (Math.Round(targetX, 1), Math.Round(targetY, 1));
    }
}
