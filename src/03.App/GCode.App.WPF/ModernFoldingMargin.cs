using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Editing;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Rendering;

namespace GCode.App.WPF;

/// <summary>
/// VS Code 스타일의 꺽쇠(Chevron) 아이콘을 사용하는 현대적인 폴딩 마진
/// </summary>
public class ModernFoldingMargin : FoldingMargin
{
    protected override void OnMouseDown(System.Windows.Input.MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Handled || TextView == null) return;

        var manager = TextView.Services.GetService(typeof(FoldingManager)) as FoldingManager;
        if (manager == null) return;

        var pos = e.GetPosition(this);
        var visualLine = TextView.GetVisualLineFromVisualTop(pos.Y + TextView.VerticalOffset);
        if (visualLine == null) return;

        int offset = visualLine.FirstDocumentLine.Offset;
        int endOffset = visualLine.FirstDocumentLine.EndOffset;
        var startSection = manager.GetNextFolding(offset);
        
        if (startSection != null && (startSection.StartOffset < offset || startSection.StartOffset > endOffset))
        {
            startSection = null;
        }

        if (startSection != null)
        {
            startSection.IsFolded = !startSection.IsFolded;
            e.Handled = true;
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        if (TextView == null || !TextView.VisualLinesValid)
            return;

        var manager = TextView.Services.GetService(typeof(FoldingManager)) as FoldingManager;
        if (manager == null) return;

        var markerBrush = new SolidColorBrush(Color.FromRgb(160, 160, 160)); 
        markerBrush.Freeze();
        var guidePen = new Pen(new SolidColorBrush(Color.FromRgb(60, 60, 60)), 1);
        guidePen.Freeze();

        double centerX = RenderSize.Width / 2;

        foreach (var visualLine in TextView.VisualLines)
        {
            int offset = visualLine.FirstDocumentLine.Offset;
            double y = visualLine.VisualTop - TextView.VerticalOffset;
            double centerY = y + TextView.DefaultLineHeight / 2;

            // 1. 가이드 라인 (세로선) 그리기
            bool isInSection = manager.AllFoldings.Any(s => !s.IsFolded && s.StartOffset < offset && s.EndOffset > offset);
            if (isInSection)
            {
                drawingContext.DrawLine(guidePen, new Point(centerX, y), new Point(centerX, y + visualLine.Height));
            }

            // 2. 폴딩 시작점 아이콘 (Chevron) 그리기
            var startSection = manager.GetNextFolding(offset);
            int lineEndOffset = visualLine.FirstDocumentLine.EndOffset;

            if (startSection != null && (startSection.StartOffset < offset || startSection.StartOffset > lineEndOffset))
            {
                startSection = null;
            }

            if (startSection != null)
            {
                bool isCollapsed = startSection.IsFolded;
                var geometry = new StreamGeometry();
                using (var ctx = geometry.Open())
                {
                    if (isCollapsed)
                    {
                        ctx.BeginFigure(new Point(centerX - 2, centerY - 4), false, false);
                        ctx.LineTo(new Point(centerX + 2.5, centerY), true, false);
                        ctx.LineTo(new Point(centerX - 2, centerY + 4), true, false);
                    }
                    else
                    {
                        ctx.BeginFigure(new Point(centerX - 4, centerY - 2), false, false);
                        ctx.LineTo(new Point(centerX, centerY + 2.5), true, false);
                        ctx.LineTo(new Point(centerX + 4, centerY - 2), true, false);
                    }
                }
                geometry.Freeze();

                var pen = new Pen(markerBrush, 1.8);
                pen.StartLineCap = PenLineCap.Round;
                pen.EndLineCap = PenLineCap.Round;
                pen.LineJoin = PenLineJoin.Round;
                pen.Freeze();

                drawingContext.DrawRectangle(new SolidColorBrush(Color.FromRgb(30, 30, 30)), null, new Rect(centerX - 6, centerY - 6, 12, 12));
                drawingContext.DrawGeometry(null, pen, geometry);
            }
        }
    }
}
