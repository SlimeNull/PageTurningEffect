using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PageTurningEffect.Components
{
    public class SimulatedBook : FrameworkElement
    {
        private readonly List<Point> _newPageMaskPoints1 = new List<Point>();
        private readonly List<Point> _newPageMaskPoints2 = new List<Point>();

        // page tunning
        public bool _isDragging;
        public Point _dragStart;
        public Point _dragCurrent;


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

        public double SpineShadowSize
        {
            get { return (double)GetValue(SpineShadowSizeProperty); }
            set { SetValue(SpineShadowSizeProperty, value); }
        }

        public double TunningShadowSize
        {
            get { return (double)GetValue(TunningShadowSizeProperty); }
            set { SetValue(TunningShadowSizeProperty, value); }
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

        public static readonly DependencyProperty BackgroundProperty =
            Panel.BackgroundProperty.AddOwner(typeof(SimulatedBook));

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source), typeof(IBookSource), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SpineShadowSizeProperty =
            DependencyProperty.Register(nameof(SpineShadowSize), typeof(double), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TunningShadowSizeProperty =
            DependencyProperty.Register(nameof(TunningShadowSize), typeof(double), typeof(SimulatedBook),
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

        private enum PageTunningMode
        {
            None,
            Prev,
            Next,
        }

        private record struct LineSegment(Point Start, Point End)
        {
            public StraightLine GetPerpendicularLine()
            {
                var midpoint = new Point(
                    (Start.X + End.X) / 2,
                    (Start.Y + End.Y) / 2);

                var direction = End - Start;
                var perpendicularDirection = new Vector(-direction.Y, direction.X);

                return new StraightLine(midpoint, perpendicularDirection);
            }

            public void ExpandToRect(double rectThickness, out Point rectP1, out Point rectP2, out Point rectP3, out Point rectP4)
            {
                var direction = End - Start;
                var perpendicularDirection = new Vector(-direction.Y, direction.X);
                perpendicularDirection.Normalize();

                var offset = perpendicularDirection * (rectThickness / 2);

                rectP1 = Start + offset;
                rectP2 = End + offset;
                rectP3 = End - offset;
                rectP4 = Start - offset;
            }
        }

        private record struct StraightLine(Point Origin, Vector Direction)
        {
            public bool IsLeft(Point pointToTest)
            {
                var toPoint = pointToTest - Origin;
                var crossProduct = Direction.X * toPoint.Y - Direction.Y * toPoint.X;
                return crossProduct < 0;
            }

            public bool IsRight(Point pointToTest)
                => !IsLeft(pointToTest);

            public Point Flip(Point pointToPerform)
            {
                var toPoint = pointToPerform - Origin;
                var directionNormalized = Direction;
                directionNormalized.Normalize();

                var projection = (toPoint.X * directionNormalized.X + toPoint.Y * directionNormalized.Y);
                var projectionPoint = new Vector(projection * directionNormalized.X, projection * directionNormalized.Y);

                var perpendicular = toPoint - projectionPoint;
                var flipped = projectionPoint - perpendicular;

                return Origin + flipped;
            }

            public Point GetIntersection(StraightLine otherLine)
            {
                var d1 = Direction;
                var d2 = otherLine.Direction;

                var denominator = d1.X * d2.Y - d1.Y * d2.X;

                if (Math.Abs(denominator) < 1e-10)
                {
                    return new Point(double.NaN, double.NaN);
                }

                var originDiff = otherLine.Origin - Origin;
                var t = (originDiff.X * d2.Y - originDiff.Y * d2.X) / denominator;

                return new Point(Origin.X + t * d1.X, Origin.Y + t * d1.Y);
            }
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (CaptureMouse())
            {
                _dragStart = e.GetPosition(this);
                e.Handled = true;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                _dragCurrent = e.GetPosition(this);
                _isDragging = true;
                e.Handled = true;
                InvalidateVisual();
            }

            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                ReleaseMouseCapture();
                e.Handled = true;
                InvalidateVisual();
            }

            base.OnMouseUp(e);
        }

        private static void FlipPolygon(IList<Point> vertices, StraightLine line, IList<Point> newVerticesStorage)
        {
            foreach (var vertex in vertices)
            {
                newVerticesStorage.Add(line.Flip(vertex));
            }
        }

        private static void HitRect(StraightLine line, Rect rect, out Point hit1, out Point hit2)
        {
            Point[] outputPoints = new Point[2];
            int currentPointIndex = 0;

            if (line.GetIntersection(new StraightLine(rect.TopLeft, new Vector(1, 0))) is { } hitUp &&
                hitUp.X >= rect.Left && hitUp.X <= rect.Right)
            {
                outputPoints[currentPointIndex++] = hitUp;
            }

            if (line.GetIntersection(new StraightLine(rect.BottomLeft, new Vector(1, 0))) is { } hitBottom &&
                hitBottom.X >= rect.Left && hitBottom.X <= rect.Right)
            {
                outputPoints[currentPointIndex++] = hitBottom;
            }

            if (line.GetIntersection(new StraightLine(rect.TopLeft, new Vector(0, 1))) is { } hitLeft &&
                hitLeft.Y >= rect.Top && hitLeft.Y <= rect.Bottom)
            {
                outputPoints[currentPointIndex++] = hitLeft;
            }

            if (line.GetIntersection(new StraightLine(rect.TopRight, new Vector(0, 1))) is { } hitRight &&
                hitRight.Y >= rect.Top && hitRight.Y <= rect.Bottom)
            {
                outputPoints[currentPointIndex++] = hitRight;
            }

            hit1 = outputPoints[0];
            hit2 = outputPoints[1];
        }

        private static void SplitPolygon(IList<Point> vertices, StraightLine line, Func<StraightLine, Point, bool> pointSelector, IList<Point> pointsStorage)
        {
            bool startSide = pointSelector.Invoke(line, vertices[0]);
            bool lastSide = startSide;
            if (startSide)
            {
                pointsStorage.Add(vertices[0]);
            }

            // skip first point
            for (int i = 1; i < vertices.Count; i++)
            {
                var currentSide = pointSelector.Invoke(line, vertices[i]);

                // changing side
                if (currentSide != lastSide)
                {
                    var lineBetweenTwoPoint = new StraightLine(vertices[i - 1], vertices[i] - vertices[i - 1]);
                    pointsStorage.Add(line.GetIntersection(lineBetweenTwoPoint));
                }

                if (currentSide)
                {
                    pointsStorage.Add(vertices[i]);
                }

                lastSide = currentSide;
            }

            if (lastSide != startSide)
            {
                var lineBetweenTwoPoint = new StraightLine(vertices[3], vertices[3] - vertices[0]);
                pointsStorage.Add(line.GetIntersection(lineBetweenTwoPoint));
            }
        }

        private static void SplitRect(Rect rect, StraightLine line, IList<Point>? leftPointsStorage, IList<Point>? rightPointsStorage)
        {
            var corners = new Point[]
            {
                new Point(rect.Left, rect.Top),
                new Point(rect.Right, rect.Top),
                new Point(rect.Right, rect.Bottom),
                new Point(rect.Left, rect.Bottom)
            };

            if (leftPointsStorage is { })
            {
                SplitPolygon(corners, line, (l, p) => l.IsLeft(p), leftPointsStorage);
            }

            if (rightPointsStorage is { })
            {
                SplitPolygon(corners, line, (l, p) => l.IsRight(p), rightPointsStorage);
            }
        }

        private static Geometry BuildPolygon(IEnumerable<Point> points)
        {
            var pathFigure = new PathFigure { IsClosed = true };
            var first = true;

            foreach (var point in points)
            {
                if (first)
                {
                    pathFigure.StartPoint = point;
                    first = false;
                }
                else
                {
                    var segment = new System.Windows.Media.LineSegment { Point = point };
                    pathFigure.Segments.Add(segment);
                }
            }

            return new PathGeometry { Figures = { pathFigure } };
        }

        private static bool CalculateDoubleSidePageTunning(Size bookSize, Point dragStart, Point dragCurrent, out PageTunningMode pageTunningMode, out StraightLine splitLine)
        {
            static StraightLine CorrectDoubleSidePageTunningSplitLine(Size bookSize, PageTunningMode pageTunningMode, StraightLine splitLine)
            {
                var lineTop = new StraightLine(new Point(0, 0), new Vector(1, 0));
                var lineBottom = new StraightLine(new Point(0, bookSize.Height), new Vector(1, 0));

                var hitPoint1 = splitLine.GetIntersection(lineTop);
                var hitPoint2 = splitLine.GetIntersection(lineBottom);

                if (pageTunningMode == PageTunningMode.Next)
                {
                    hitPoint1 = new Point(Math.Max(bookSize.Width / 2, hitPoint1.X), hitPoint1.Y);
                    hitPoint2 = new Point(Math.Max(bookSize.Width / 2, hitPoint2.X), hitPoint2.Y);
                    return new StraightLine(hitPoint2, hitPoint1 - hitPoint2);
                }
                else if (pageTunningMode == PageTunningMode.Prev)
                {
                    hitPoint1 = new Point(Math.Min(bookSize.Width / 2, hitPoint1.X), hitPoint1.Y);
                    hitPoint2 = new Point(Math.Min(bookSize.Width / 2, hitPoint2.X), hitPoint2.Y);
                    return new StraightLine(hitPoint1, hitPoint2 - hitPoint1);
                }

                throw new ArgumentException();
            }

            if (dragStart.X < bookSize.Width / 2 &&
                dragCurrent.X > dragStart.X)
            {
                pageTunningMode = PageTunningMode.Prev;
                splitLine = new LineSegment(new Point(0, dragStart.Y), dragCurrent).GetPerpendicularLine();
                splitLine = CorrectDoubleSidePageTunningSplitLine(bookSize, pageTunningMode, splitLine);
                return true;
            }
            else if (dragStart.X > bookSize.Width / 2 &&
                dragCurrent.X < dragStart.X)
            {
                pageTunningMode = PageTunningMode.Next;
                splitLine = new LineSegment(new Point(bookSize.Width - 1, dragStart.Y), dragCurrent).GetPerpendicularLine();
                splitLine = CorrectDoubleSidePageTunningSplitLine(bookSize, pageTunningMode, splitLine);
                return true;
            }

            splitLine = default;
            pageTunningMode = PageTunningMode.None;
            return false;
        }

        private static Matrix ConstructBasicMatrix(Point origin, Vector iHat, Vector jHat)
        {
            return new Matrix(iHat.X, iHat.Y, jHat.X, jHat.Y, origin.X, origin.Y);
        }

        private static MatrixTransform GetTunningPageRenderTransform(Size bookSize, Thickness padding, StraightLine splitLine, PageTunningMode pageTunningMode)
        {
            (var origin, var iHat, var jHat) = pageTunningMode switch
            {
                PageTunningMode.Next => (new Point(bookSize.Width - padding.Left, padding.Top), new Vector(-1, 0), new Vector(0, 1)),
                PageTunningMode.Prev => (new Point(bookSize.Width / 2 - padding.Left, padding.Top), new Vector(-1, 0), new Vector(0, 1)),
                _ => throw new ArgumentException(),
            };

            var absIHat = origin + iHat;
            var absJHat = origin + jHat;

            origin = splitLine.Flip(origin);
            absIHat = splitLine.Flip(absIHat);
            absJHat = splitLine.Flip(absJHat);
            iHat = absIHat - origin;
            jHat = absJHat - origin;

            return new MatrixTransform(ConstructBasicMatrix(origin, iHat, jHat));
        }

        private void CoreRenderDoubleSide(DrawingContext drawingContext)
        {
            // properties
            var background = Background;
            var source = Source;

            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;

            var spineShadowSize = SpineShadowSize;
            var tunningShadowSize = TunningShadowSize;
            var padding = Padding;
            var shadowOpacity = ShadowOpacity;
            var currentPage = CurrentPage;

            var isDragging = _isDragging;
            var bookSize = new Size(actualWidth, actualHeight);
            var bookRect = new Rect(0, 0, actualWidth, actualHeight);

            var pageSize = new Size(
                    actualWidth / 2 - spineShadowSize - padding.Left - padding.Right,
                    actualHeight - padding.Top - padding.Bottom);

            var leftPageOrigin = new Point(padding.Left, padding.Top);
            var rightPageOrigin = new Point(actualWidth / 2 + spineShadowSize + padding.Left, padding.Top);

            var leftPageRenderTransform = new TranslateTransform(leftPageOrigin.X, leftPageOrigin.Y);
            var rightPageRenderTransform = new TranslateTransform(rightPageOrigin.X, rightPageOrigin.Y);

            // background
            drawingContext.DrawRectangle(background, null, new Rect(0, 0, ActualWidth, ActualHeight));

            // draw base content
            if (source is { })
            {
                // left content
                if (currentPage < source.PageCount)
                {
                    drawingContext.PushTransform(leftPageRenderTransform);
                    source.RenderPage(drawingContext, pageSize, currentPage);
                    drawingContext.Pop();
                }

                // right content
                if (currentPage + 1 < source.PageCount)
                {
                    drawingContext.PushTransform(rightPageRenderTransform);
                    source.RenderPage(drawingContext, pageSize, currentPage + 1);
                    drawingContext.Pop();
                }
            }

            // draw spine shadow
            var spineRect = new Rect(
                actualWidth / 2 - spineShadowSize,
                0,
                spineShadowSize * 2,
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
            if (isDragging &&
                CalculateDoubleSidePageTunning(bookSize, _dragStart, _dragCurrent, out var pageTunningMode, out var splitLine))
            {

#if Gizmos
                drawingContext.DrawEllipse(Brushes.Pink, null, splitLine.Origin, 2, 2);
                drawingContext.DrawLine(new Pen(Brushes.Pink, 1), splitLine.Origin, splitLine.Origin + splitLine.Direction);
#endif
                var lineTop = new StraightLine(new Point(0, 0), new Vector(1, 0));
                var lienBottom = new StraightLine(new Point(0, actualHeight), new Vector(1, 0));
                var lineLeft = new StraightLine(new Point(0, 0), new Vector(0, 1));
                var lineRight = new StraightLine(new Point(actualWidth, 0), new Vector(0, 1));
                var lineMiddle = new StraightLine(new Point(actualWidth / 2, 0), new Vector(0, 1));

                _newPageMaskPoints1.Clear();
                _newPageMaskPoints2.Clear();

                if (pageTunningMode == PageTunningMode.Next)
                {
                    SplitRect(new Rect(actualWidth / 2, 0, actualWidth / 2, actualHeight), splitLine, null, _newPageMaskPoints1);
                }
                else
                {
                    SplitRect(new Rect(0, 0, actualWidth / 2, actualHeight), splitLine, null, _newPageMaskPoints1);
                }

                FlipPolygon(_newPageMaskPoints1, splitLine, _newPageMaskPoints2);

                var mask1 = BuildPolygon(_newPageMaskPoints1);
                var mask2 = BuildPolygon(_newPageMaskPoints2);

                if (source is { })
                {
                    drawingContext.PushClip(mask1);
                    drawingContext.DrawGeometry(background, null, mask1);
                    if (pageTunningMode == PageTunningMode.Next)
                    {
                        drawingContext.PushTransform(rightPageRenderTransform);
                        source.RenderPage(drawingContext, pageSize, currentPage + 3);
                        drawingContext.Pop();
                    }
                    else
                    {
                        drawingContext.PushTransform(leftPageRenderTransform);
                        source.RenderPage(drawingContext, pageSize, currentPage - 2);
                        drawingContext.Pop();
                    }

                    drawingContext.Pop();

                    drawingContext.PushClip(mask2);
                    drawingContext.DrawGeometry(background, null, mask2);
                    var tunningPageRenderTransform = GetTunningPageRenderTransform(bookSize, padding, splitLine, pageTunningMode);
                    if (pageTunningMode == PageTunningMode.Next)
                    {
                        drawingContext.PushTransform(tunningPageRenderTransform);
                        source.RenderPage(drawingContext, pageSize, currentPage + 2);
                        drawingContext.Pop();
                    }
                    else
                    {
                        drawingContext.PushTransform(tunningPageRenderTransform);
                        source.RenderPage(drawingContext, pageSize, currentPage - 1);
                        drawingContext.Pop();
                    }

                    drawingContext.Pop();
                }

                var rotationOfSplitLine = Math.Atan2(splitLine.Direction.Y, splitLine.Direction.X);
                var angleOfSplitLine = rotationOfSplitLine / Math.PI * 180;

                HitRect(splitLine, bookRect, out var hit1, out var hit2);
                new LineSegment(hit1, hit2).ExpandToRect(tunningShadowSize * 2,
                    out var tunningShadowRectP1,
                    out var tunningShadowRectP2,
                    out var tunningShadowRectP3,
                    out var tunningShadowRectP4);

                var tunningShadowGeometry = BuildPolygon([tunningShadowRectP1, tunningShadowRectP2, tunningShadowRectP3, tunningShadowRectP4]);
                var tunningShadowBrush = new LinearGradientBrush(
                    new GradientStopCollection()
                    {
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.0),
                        new GradientStop(Color.FromArgb((byte)(255 * shadowOpacity), 0, 0, 0), 0.5),
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0),
                    }, angleOfSplitLine)
                {
                    StartPoint = tunningShadowRectP2,
                    EndPoint = tunningShadowRectP3,
                    MappingMode = BrushMappingMode.Absolute,
                };

                var tunningShadowClip = new CombinedGeometry(mask1, mask2);

                drawingContext.PushClip(tunningShadowClip);
                drawingContext.DrawGeometry(tunningShadowBrush, null, tunningShadowGeometry);
                drawingContext.Pop();

#if Gizmos
                drawingContext.PushOpacity(0.5);
                drawingContext.DrawGeometry(Brushes.Pink, null, mask1);
                drawingContext.DrawGeometry(Brushes.Purple, null, mask2);
                drawingContext.Pop();
#endif
            }
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            CoreRenderDoubleSide(drawingContext);
        }
    }
}
