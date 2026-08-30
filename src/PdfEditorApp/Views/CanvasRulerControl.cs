using System;
using System.Collections.Concurrent;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using PdfEditorApp.Models;

namespace PdfEditorApp.Views;

public class CanvasRulerControl : Control
{
    public static readonly StyledProperty<Orientation> OrientationProperty =
        AvaloniaProperty.Register<CanvasRulerControl, Orientation>(nameof(Orientation), Orientation.Horizontal);

    public static readonly StyledProperty<RulerUnit> UnitProperty =
        AvaloniaProperty.Register<CanvasRulerControl, RulerUnit>(nameof(Unit), RulerUnit.Points);

    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<CanvasRulerControl, double>(nameof(ZoomLevel), 1.0);

    public static readonly StyledProperty<double> CursorPositionProperty =
        AvaloniaProperty.Register<CanvasRulerControl, double>(nameof(CursorPosition), -1);

    public static readonly StyledProperty<double> PageOffsetProperty =
        AvaloniaProperty.Register<CanvasRulerControl, double>(nameof(PageOffset), 0.0);

    public static readonly StyledProperty<double> PageDimensionProperty =
        AvaloniaProperty.Register<CanvasRulerControl, double>(nameof(PageDimension), 800.0);

    public Orientation Orientation
    {
        get => GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    public RulerUnit Unit
    {
        get => GetValue(UnitProperty);
        set => SetValue(UnitProperty, value);
    }

    public double ZoomLevel
    {
        get => GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, value);
    }

    public double CursorPosition
    {
        get => GetValue(CursorPositionProperty);
        set => SetValue(CursorPositionProperty, value);
    }

    public double PageOffset
    {
        get => GetValue(PageOffsetProperty);
        set => SetValue(PageOffsetProperty, value);
    }

    public double PageDimension
    {
        get => GetValue(PageDimensionProperty);
        set => SetValue(PageDimensionProperty, value);
    }

    // Static cached drawing resources (Zero GC allocations per mouse move)
    private static readonly IBrush s_bgBrush = new SolidColorBrush(Color.Parse("#F8FAFC"));
    private static readonly IPen s_borderPen = new Pen(new SolidColorBrush(Color.Parse("#E2E8F0")), 1);
    private static readonly IPen s_tickPen = new Pen(new SolidColorBrush(Color.Parse("#94A3B8")), 1);
    private static readonly IPen s_majorTickPen = new Pen(new SolidColorBrush(Color.Parse("#64748B")), 1);
    private static readonly IPen s_pageBorderPen = new Pen(new SolidColorBrush(Color.Parse("#CBD5E1")), 1.5);
    private static readonly IPen s_cursorPen = new Pen(new SolidColorBrush(Color.Parse("#0F6CBD")), 1.5);
    private static readonly IBrush s_pageShadeBrush = new SolidColorBrush(Color.Parse("#FFFFFF"));
    private static readonly IBrush s_labelBrush = new SolidColorBrush(Color.Parse("#64748B"));
    private static readonly Typeface s_typeface = new("Segoe UI, -apple-system, sans-serif");

    static CanvasRulerControl()
    {
        AffectsRender<CanvasRulerControl>(
            OrientationProperty,
            UnitProperty,
            ZoomLevelProperty,
            CursorPositionProperty,
            PageOffsetProperty,
            PageDimensionProperty);
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;

        bool isHorizontal = Orientation == Orientation.Horizontal;

        // Draw ruler background
        context.FillRectangle(s_bgBrush, new Rect(0, 0, bounds.Width, bounds.Height));

        if (isHorizontal)
        {
            // Bottom edge border
            context.DrawLine(s_borderPen, new Point(0, bounds.Height), new Point(bounds.Width, bounds.Height));

            double zoom = Math.Max(0.1, ZoomLevel);
            double stepPts = Unit == RulerUnit.Inches ? 72.0 : (Unit == RulerUnit.Millimeters ? 72.0 / 2.54 : 50.0);
            double minorStepPts = stepPts / 5.0;

            double startX = PageOffset;
            double endX = startX + (PageDimension * zoom);

            // Draw page range shade
            context.FillRectangle(s_pageShadeBrush, new Rect(Math.Max(0, startX), 0, Math.Max(0, endX - Math.Max(0, startX)), bounds.Height));
            context.DrawLine(s_pageBorderPen, new Point(startX, 0), new Point(startX, bounds.Height));
            context.DrawLine(s_pageBorderPen, new Point(endX, 0), new Point(endX, bounds.Height));

            // Draw ticks across canvas
            double maxPts = PageDimension + 200;
            for (double pt = 0; pt <= maxPts; pt += minorStepPts)
            {
                double x = startX + (pt * zoom);
                if (x < 0 || x > bounds.Width) continue;

                bool isMajor = Math.Abs(pt % stepPts) < 0.01;
                double tickH = isMajor ? bounds.Height * 0.55 : bounds.Height * 0.25;

                context.DrawLine(isMajor ? s_majorTickPen : s_tickPen, new Point(x, bounds.Height - tickH), new Point(x, bounds.Height));

                if (isMajor)
                {
                    string label = Unit switch
                    {
                        RulerUnit.Inches => $"{pt / 72.0:0}\"",
                        RulerUnit.Millimeters => $"{pt * 25.4 / 72.0:0}",
                        _ => $"{pt:0}"
                    };

                    var formattedText = new FormattedText(
                        label,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        s_typeface,
                        8.5,
                        s_labelBrush
                    );

                    context.DrawText(formattedText, new Point(x + 2, 2));
                }
            }

            // Draw cursor tracker
            if (CursorPosition >= 0)
            {
                double cursorX = startX + (CursorPosition * zoom);
                if (cursorX >= 0 && cursorX <= bounds.Width)
                {
                    context.DrawLine(s_cursorPen, new Point(cursorX, 0), new Point(cursorX, bounds.Height));
                }
            }
        }
        else
        {
            // Right edge border
            context.DrawLine(s_borderPen, new Point(bounds.Width, 0), new Point(bounds.Width, bounds.Height));

            double zoom = Math.Max(0.1, ZoomLevel);
            double stepPts = Unit == RulerUnit.Inches ? 72.0 : (Unit == RulerUnit.Millimeters ? 72.0 / 2.54 : 50.0);
            double minorStepPts = stepPts / 5.0;

            double startY = PageOffset;
            double endY = startY + (PageDimension * zoom);

            // Draw page range shade
            context.FillRectangle(s_pageShadeBrush, new Rect(0, Math.Max(0, startY), bounds.Width, Math.Max(0, endY - Math.Max(0, startY))));
            context.DrawLine(s_pageBorderPen, new Point(0, startY), new Point(bounds.Width, startY));
            context.DrawLine(s_pageBorderPen, new Point(0, endY), new Point(bounds.Width, endY));

            // Draw ticks across canvas
            double maxPts = PageDimension + 200;
            for (double pt = 0; pt <= maxPts; pt += minorStepPts)
            {
                double y = startY + (pt * zoom);
                if (y < 0 || y > bounds.Height) continue;

                bool isMajor = Math.Abs(pt % stepPts) < 0.01;
                double tickW = isMajor ? bounds.Width * 0.55 : bounds.Width * 0.25;

                context.DrawLine(isMajor ? s_majorTickPen : s_tickPen, new Point(bounds.Width - tickW, y), new Point(bounds.Width, y));

                if (isMajor)
                {
                    string label = Unit switch
                    {
                        RulerUnit.Inches => $"{pt / 72.0:0}\"",
                        RulerUnit.Millimeters => $"{pt * 25.4 / 72.0:0}",
                        _ => $"{pt:0}"
                    };

                    var formattedText = new FormattedText(
                        label,
                        CultureInfo.InvariantCulture,
                        FlowDirection.LeftToRight,
                        s_typeface,
                        8.0,
                        s_labelBrush
                    );

                    context.DrawText(formattedText, new Point(2, y + 2));
                }
            }

            // Draw cursor tracker
            if (CursorPosition >= 0)
            {
                double cursorY = startY + (CursorPosition * zoom);
                if (cursorY >= 0 && cursorY <= bounds.Height)
                {
                    context.DrawLine(s_cursorPen, new Point(0, cursorY), new Point(bounds.Width, cursorY));
                }
            }
        }
    }
}
