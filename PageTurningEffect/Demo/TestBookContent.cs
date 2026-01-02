using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Media;
using PageTurningEffect.Components;

namespace PageTurningEffect.Demo
{
    internal class TestBookContent : IBookContent
    {
        public int GetPageCount(Size pageSize) => 10;

        public void RenderPage(DrawingContext drawingContext, Size pageSize, int pageIndex)
        {
            // Draw background
            drawingContext.DrawRectangle(Brushes.White, null, new Rect(0, 0, pageSize.Width, pageSize.Height));

            // Draw grid, 10px per cell
            var gridPen = new Pen(Brushes.LightGray, 0.5);
            gridPen.Freeze();

            // Draw vertical lines
            for (double x = 0; x <= pageSize.Width; x += 10)
            {
                drawingContext.DrawLine(gridPen, new Point(x, 0), new Point(x, pageSize.Height));
            }

            // Draw horizontal lines
            for (double y = 0; y <= pageSize.Height; y += 10)
            {
                drawingContext.DrawLine(gridPen, new Point(0, y), new Point(pageSize.Width, y));
            }

            // Draw page index
            var pageText = $"Page {pageIndex + 1}";
            var typeface = new Typeface("Arial");
            var formattedText = new FormattedText(
                pageText,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                32,
                Brushes.Black,
                VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

            var textPosition = new Point(
                (pageSize.Width - formattedText.Width) / 2,
                (pageSize.Height - formattedText.Height) / 2);

            drawingContext.DrawText(formattedText, textPosition);
        }
    }
}
