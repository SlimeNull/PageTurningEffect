using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PageTurningEffect.Components
{
    public class SimulatedBook : FrameworkElement
    {
        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public IBookSource Source
        {
            get { return (IBookSource)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public double SpineSize
        {
            get { return (double)GetValue(SpineSizeProperty); }
            set { SetValue(SpineSizeProperty, value); }
        }

        public Thickness Padding
        {
            get { return (Thickness)GetValue(PaddingProperty); }
            set { SetValue(PaddingProperty, value); }
        }

        public double ShadowOpacity
        {
            get { return (double)GetValue(ShadowOpacityProperty); }
            set { SetValue(ShadowOpacityProperty, value); }
        }

        public int CurrentPage
        {
            get { return (int)GetValue(CurrentPageProperty); }
            set { SetValue(CurrentPageProperty, value); }
        }

        public bool IsDragging
        {
            get { return (bool)GetValue(IsDraggingProperty); }
            set { SetValue(IsDraggingProperty, value); }
        }

        public Point DragStart
        {
            get { return (Point)GetValue(DragStartProperty); }
            set { SetValue(DragStartProperty, value); }
        }

        public Point DragCurrent
        {
            get { return (Point)GetValue(DragCurrentProperty); }
            set { SetValue(DragCurrentProperty, value); }
        }

        public static readonly DependencyProperty BackgroundProperty =
            Panel.BackgroundProperty.AddOwner(typeof(SimulatedBook));

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(IBookSource), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SpineSizeProperty =
            DependencyProperty.Register(nameof(SpineSize), typeof(double), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty PaddingProperty =
            DependencyProperty.Register(nameof(Padding), typeof(Thickness), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(default(Thickness), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ShadowOpacityProperty =
            DependencyProperty.Register(nameof(ShadowOpacity), typeof(double), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(0.5, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty CurrentPageProperty =
            DependencyProperty.Register(nameof(CurrentPage), typeof(int), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsDraggingProperty =
            DependencyProperty.Register(nameof(IsDragging), typeof(bool), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DragStartProperty =
            DependencyProperty.Register(nameof(DragStart), typeof(Point), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty DragCurrentProperty =
            DependencyProperty.Register(nameof(DragCurrent), typeof(Point), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(default(Point), FrameworkPropertyMetadataOptions.AffectsRender));

        private void CoreRenderDoubleSide(DrawingContext drawingContext)
        {
            // properties
            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;

            var spineSize = SpineSize;
            var padding = Padding;
            var shadowOpacity = ShadowOpacity;
            var currentPage = CurrentPage;

            var isDragging = IsDragging;

            // background
            drawingContext.DrawRectangle(Background, null, new Rect(0, 0, ActualWidth, ActualHeight));

            // draw base content
            if (Source is { } source)
            {
                var pageSize = new Size(
                    actualWidth / 2 - spineSize - padding.Left - padding.Right,
                    actualHeight - padding.Top - padding.Bottom);

                var leftPageOrigin = new Point(padding.Left, padding.Top);
                var rightPageOrigin = new Point(actualWidth / 2 + spineSize + padding.Left, padding.Top);

                // left content
                if (currentPage < source.PageCount)
                {
                    drawingContext.PushTransform(new TranslateTransform(leftPageOrigin.X, leftPageOrigin.Y));
                    source.RenderPage(drawingContext, pageSize, currentPage);
                    drawingContext.Pop();
                }

                // right content
                if (currentPage + 1 < source.PageCount)
                {
                    drawingContext.PushTransform(new TranslateTransform(rightPageOrigin.X, rightPageOrigin.Y));
                    source.RenderPage(drawingContext, pageSize, currentPage + 1);
                    drawingContext.Pop();
                }
            }

            // draw spine shadow
            var spineRect = new Rect(
                actualWidth / 2 - spineSize,
                0,
                spineSize * 2,
                actualHeight);

            var spineBrush = new LinearGradientBrush(
                new GradientStopCollection()
                {
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.0),
                    new GradientStop(Color.FromArgb((byte)(255 * shadowOpacity), 0, 0, 0), 0.5),
                    new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0),
                }, 0);

            drawingContext.DrawRectangle(spineBrush, null, spineRect);

            // draw dragging content
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            CoreRenderDoubleSide(drawingContext);
        }
    }
}
