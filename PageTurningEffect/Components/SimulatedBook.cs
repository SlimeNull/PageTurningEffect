using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace PageTurningEffect.Components
{
    [ContentProperty(nameof(Content))]
    public partial class SimulatedBook : FrameworkElement
    {
        private readonly List<Point> _newPageMaskPoints1 = new List<Point>();
        private readonly List<Point> _newPageMaskPoints2 = new List<Point>();

        // animating
        private IEasingFunction _easingFunction = new CubicEase() { EasingMode = EasingMode.EaseOut };
        private IEasingFunction _easingFunctionOutForY = new QuadraticEase() { EasingMode = EasingMode.EaseOut };
        private DateTime _dragStartTime;
        private DateTime _dragEndTime;

        private TimeSpan _dragStartEasingSpan = TimeSpan.FromMilliseconds(250);
        private TimeSpan _dragEndEasingSpan = TimeSpan.FromMilliseconds(500);

        // page turning
        private bool _isDragging;
        private Point _dragStart;
        private Point _dragCurrent;
        private Point _dragEnd;        // from calculation
        private int? _targetPage;

        // caching
        private StraightLine _lastCalculatedStraightLine;

        // ui elements
        private BookUIElementContainer _elementContainer1;  // left
        private BookUIElementContainer _elementContainer2;  // right
        private BookUIElementContainer _elementContainer3;  // tunning

        public BookMode Mode
        {
            get { return (BookMode)GetValue(ModeProperty); }
            set { SetValue(ModeProperty, value); }
        }

        public Brush Background
        {
            get { return (Brush)GetValue(BackgroundProperty); }
            set { SetValue(BackgroundProperty, value); }
        }

        public IBookContent Content
        {
            get { return (IBookContent)GetValue(SourceProperty); }
            set { SetValue(SourceProperty, value); }
        }

        public double SpineShadowSize
        {
            get { return (double)GetValue(SpineShadowSizeProperty); }
            set { SetValue(SpineShadowSizeProperty, value); }
        }

        public double TurningShadowSize
        {
            get { return (double)GetValue(TurningShadowSizeProperty); }
            set { SetValue(TurningShadowSizeProperty, value); }
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

        public Size PageSize => CalculatePageSize(new Size(ActualWidth, ActualHeight));

        public int CurrentPage
        {
            get { return (int)GetValue(CurrentPageProperty); }
            set { SetValue(CurrentPageProperty, value); }
        }

        protected override int VisualChildrenCount => 3;

        protected override IEnumerator LogicalChildren
        {
            get
            {
                if (Content is { } content)
                {
                    yield return content;
                }
            }
        }

        public SimulatedBook()
        {
            _elementContainer1 = new BookUIElementContainer(this);
            _elementContainer2 = new BookUIElementContainer(this);
            _elementContainer3 = new BookUIElementContainer(this);

            AddVisualChild(_elementContainer1);
            AddVisualChild(_elementContainer2);
            AddVisualChild(_elementContainer3);
        }

        protected override Visual GetVisualChild(int index)
        {
            return index switch
            {
                0 => _elementContainer1,
                1 => _elementContainer2,
                2 => _elementContainer3,
                _ => throw new ArgumentOutOfRangeException(nameof(index)),
            };
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            base.ArrangeOverride(finalSize);

            var pageSize = CalculatePageSize(finalSize);
            var containerRect = new Rect(default(Point), pageSize);
            _elementContainer1.Arrange(containerRect);
            _elementContainer2.Arrange(containerRect);
            _elementContainer3.Arrange(containerRect);

            return finalSize;
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (CaptureMouse())
            {
                _dragStart = e.GetPosition(this);
                if (Mode == BookMode.TwoSide)
                {
                    if (_dragStart.X > ActualWidth / 2)
                    {
                        _dragStart.X = ActualWidth - 1;
                    }
                    else if (_dragStart.X < ActualWidth / 2)
                    {
                        _dragStart.X = 0;
                    }
                }

                e.Handled = true;
            }

            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (IsMouseCaptured)
            {
                if (_targetPage is { } targetPage)
                {
                    CurrentPage = targetPage;
                }

                _dragCurrent = e.GetPosition(this);
                if (!_isDragging)
                {
                    _dragStartTime = DateTime.Now;
                    EnsureAnimationRunning();
                }

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
                var source = Content;

                var actualWidth = ActualWidth;
                var actualHeight = ActualHeight;

                var spineShadowSize = SpineShadowSize;
                var padding = Padding;
                var currentPage = CurrentPage;

                var pageSize = new Size(
                    actualWidth / 2 - spineShadowSize - padding.Left - padding.Right,
                    actualHeight - padding.Top - padding.Bottom);
                var pageCount = source?.GetPageCount(pageSize) ?? 0;

                if (Mode == BookMode.TwoSide)
                {
                    if (currentPage + 2 < pageCount &&
                        _dragStart.X > ActualWidth / 2 &&
                        _dragCurrent.X < ActualWidth / 2)
                    {
                        _dragEnd = new Point(1, _dragStart.Y);
                        _targetPage = currentPage + 2;
                    }
                    else if (
                        currentPage - 2 >= 0 &&
                        _dragStart.X < ActualWidth / 2 &&
                        _dragCurrent.X > ActualWidth / 2)
                    {
                        _dragEnd = new Point(ActualWidth - 1, _dragStart.Y);
                        _targetPage = currentPage - 2;
                    }
                    else
                    {
                        _dragEnd = _dragStart;
                    }
                }

                _isDragging = false;
                _dragEndTime = DateTime.Now;
                EnsureAnimationRunning();
                ReleaseMouseCapture();
                e.Handled = true;
                InvalidateVisual();
            }

            base.OnMouseUp(e);
        }

        private Size CalculatePageSize(Size thisSize)
        {
            var actualWidth = thisSize.Width;
            var actualHeight = thisSize.Height;

            var spineShadowSize = SpineShadowSize;
            var padding = Padding;
            var currentPage = CurrentPage;

            if (actualWidth <= 0 || actualHeight <= 0)
            {
                return new Size(0, 0);
            }

            var pageSize = new Size(
                    actualWidth / 2 - spineShadowSize - padding.Left - padding.Right,
                    actualHeight - padding.Top - padding.Bottom);

            return pageSize;
        }

        private void EnsureAnimationRunning()
        {
            CompositionTarget.Rendering -= CompositionTargetRendering;
            CompositionTarget.Rendering += CompositionTargetRendering;
        }

        private void CompositionTargetRendering(object? sender, EventArgs e)
        {
            var now = DateTime.Now;
            InvalidateVisual();

            if ((_isDragging && (now - _dragStartTime) >= _dragStartEasingSpan) ||
                (!_isDragging && (now - _dragEndTime) >= _dragEndEasingSpan))
            {
                if (_targetPage is { } currentPage)
                {
                    CurrentPage = currentPage;
                    _targetPage = null;

                    InvalidateVisual();
                }

                CompositionTarget.Rendering -= CompositionTargetRendering;
            }
        }

        private StraightLine CalculateSplitLineWithCache(Point p1, Point p2)
        {
            var calculated = new LineSegment(p1, p2).GetPerpendicularLine();
            if ((p1 - p2).LengthSquared < 0.5 &&
                _lastCalculatedStraightLine.Direction.LengthSquared > 0.5)
            {
                calculated = new StraightLine(calculated.Origin, _lastCalculatedStraightLine.Direction);
            }

            _lastCalculatedStraightLine = calculated;
            return calculated;
        }

        private bool CalculateDoubleSidePageTurning(Size bookSize, Point dragStart, Point dragCurrent, out PageTurningMode pageTurningMode, out StraightLine splitLine)
        {
            static StraightLine CorrectDoubleSidePageTurningSplitLine(Size bookSize, PageTurningMode pageTurningMode, StraightLine splitLine)
            {
                var lineTop = new StraightLine(new Point(0, 0), new Vector(1, 0));
                var lineBottom = new StraightLine(new Point(0, bookSize.Height), new Vector(1, 0));

                var hitPoint1 = splitLine.GetIntersection(lineTop);
                var hitPoint2 = splitLine.GetIntersection(lineBottom);

                if (pageTurningMode == PageTurningMode.Next)
                {
                    hitPoint1 = new Point(Math.Max(bookSize.Width / 2, hitPoint1.X), hitPoint1.Y);
                    hitPoint2 = new Point(Math.Max(bookSize.Width / 2, hitPoint2.X), hitPoint2.Y);
                    return new StraightLine(hitPoint2, hitPoint1 - hitPoint2);
                }
                else if (pageTurningMode == PageTurningMode.Prev)
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
                pageTurningMode = PageTurningMode.Prev;
                splitLine = CalculateSplitLineWithCache(new Point(0, dragStart.Y), dragCurrent);
                splitLine = CorrectDoubleSidePageTurningSplitLine(bookSize, pageTurningMode, splitLine);
                return true;
            }
            else if (dragStart.X > bookSize.Width / 2 &&
                dragCurrent.X < dragStart.X)
            {
                pageTurningMode = PageTurningMode.Next;
                splitLine = CalculateSplitLineWithCache(new Point(bookSize.Width - 1, dragStart.Y), dragCurrent);
                splitLine = CorrectDoubleSidePageTurningSplitLine(bookSize, pageTurningMode, splitLine);
                return true;
            }

            splitLine = default;
            pageTurningMode = PageTurningMode.None;
            return false;
        }

        private static void OnContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not SimulatedBook book)
            {
                return;
            }

            if (e.OldValue is { } oldContent)
            {
                book.RemoveLogicalChild(oldContent);
            }

            if (e.NewValue is { } newContent)
            {
                book.AddLogicalChild(newContent);
            }
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

        private static Matrix ConstructBasicMatrix(Point origin, Vector iHat, Vector jHat)
        {
            return new Matrix(iHat.X, iHat.Y, jHat.X, jHat.Y, origin.X, origin.Y);
        }

        private static MatrixTransform GetTurningPageRenderTransform(Size bookSize, Thickness padding, double spineShadowSize, StraightLine splitLine, PageTurningMode pageTurningMode)
        {
            (var origin, var iHat, var jHat) = pageTurningMode switch
            {
                PageTurningMode.Next => (new Point(bookSize.Width - padding.Left, padding.Top), new Vector(-1, 0), new Vector(0, 1)),
                PageTurningMode.Prev => (new Point(bookSize.Width / 2 - padding.Left - spineShadowSize, padding.Top), new Vector(-1, 0), new Vector(0, 1)),
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
            var source = Content;

            var actualWidth = ActualWidth;
            var actualHeight = ActualHeight;

            var spineShadowSize = SpineShadowSize;
            var turningShadowSize = TurningShadowSize;
            var padding = Padding;
            var shadowOpacity = ShadowOpacity;
            var currentPage = CurrentPage;

            var isDragging = _isDragging;
            var bookSize = new Size(actualWidth, actualHeight);
            var bookRect = new Rect(0, 0, actualWidth, actualHeight);

            var pageSize = new Size(
                actualWidth / 2 - spineShadowSize - padding.Left - padding.Right,
                actualHeight - padding.Top - padding.Bottom);
            var pageCount = source?.GetPageCount(pageSize) ?? 0;

            var leftPageOrigin = new Point(padding.Left, padding.Top);
            var rightPageOrigin = new Point(actualWidth / 2 + spineShadowSize + padding.Left, padding.Top);

            var leftPageRenderTransform = new TranslateTransform(leftPageOrigin.X, leftPageOrigin.Y);
            var rightPageRenderTransform = new TranslateTransform(rightPageOrigin.X, rightPageOrigin.Y);

            // animating
            var now = DateTime.Now;

            // dragging
            var dragStart = _dragStart;
            var dragCurrent = _dragCurrent;
            if (now - _dragStartTime < _dragStartEasingSpan)
            {
                var t = (now - _dragStartTime) / _dragStartEasingSpan;
                var easedT = _easingFunction.Ease(t);

                dragCurrent = new Point(
                    _dragStart.X + (_dragCurrent.X - _dragStart.X) * easedT,
                    _dragStart.Y + (_dragCurrent.Y - _dragStart.Y) * easedT);
            }

            if (!isDragging &&
                now - _dragEndTime < _dragEndEasingSpan)
            {
                var t = (now - _dragEndTime) / _dragEndEasingSpan;
                var easedT = _easingFunction.Ease(t);
                var easedTForY = _easingFunctionOutForY.Ease(easedT);

                isDragging = true;
                dragCurrent = new Point(
                    _dragCurrent.X + (_dragEnd.X - _dragCurrent.X) * easedT,
                    _dragCurrent.Y + (_dragEnd.Y - _dragCurrent.Y) * easedTForY);
            }

            // reset element children state
            _elementContainer1.Children.Clear();
            _elementContainer2.Children.Clear();
            _elementContainer3.Children.Clear();
            _elementContainer1.RenderTransform = leftPageRenderTransform;
            _elementContainer2.RenderTransform = rightPageRenderTransform;

            // background
            drawingContext.DrawRectangle(background, null, new Rect(0, 0, ActualWidth, ActualHeight));

            // draw base content
            if (source is { })
            {
                // left content
                if (currentPage >= 0 &&
                    currentPage < pageCount)
                {
                    // page render contexts
                    var leftPageRenderContext = new BookPageRenderContext(drawingContext, element => _elementContainer1.Children.Add(element));

                    drawingContext.PushTransform(leftPageRenderTransform);
                    source.RenderPage(leftPageRenderContext, pageSize, currentPage);
                    drawingContext.Pop();
                }

                // right content
                if (currentPage >= 0 &&
                    currentPage + 1 < pageCount)
                {
                    // page render contexts
                    var rightPageRenderContext = new BookPageRenderContext(drawingContext, element => _elementContainer2.Children.Add(element));

                    drawingContext.PushTransform(rightPageRenderTransform);
                    source.RenderPage(rightPageRenderContext, pageSize, currentPage + 1);
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
                CalculateDoubleSidePageTurning(bookSize, dragStart, dragCurrent, out var pageTurningMode, out var splitLine))
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

                if (pageTurningMode == PageTurningMode.Next)
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
                    if (pageTurningMode == PageTurningMode.Next &&
                        currentPage + 3 >= 0 && currentPage + 3 < pageCount)
                    {
                        // page render contexts
                        var leftPageRenderContext = new BookPageRenderContext(drawingContext, element => _elementContainer1.Children.Add(element));

                        drawingContext.PushTransform(rightPageRenderTransform);
                        source.RenderPage(leftPageRenderContext, pageSize, currentPage + 3);
                        drawingContext.Pop();
                    }
                    else if (pageTurningMode == PageTurningMode.Prev &&
                        currentPage - 2 >= 0 && currentPage - 2 < pageCount)
                    {
                        // page render contexts
                        var rightPageRenderContext = new BookPageRenderContext(drawingContext, element => _elementContainer2.Children.Add(element));

                        drawingContext.PushTransform(leftPageRenderTransform);
                        source.RenderPage(rightPageRenderContext, pageSize, currentPage - 2);
                        drawingContext.Pop();
                    }

                    drawingContext.Pop();

                    drawingContext.PushClip(mask2);
                    drawingContext.DrawGeometry(background, null, mask2);
                    var turningPageRenderTransform = GetTurningPageRenderTransform(bookSize, padding, spineShadowSize, splitLine, pageTurningMode);
                    var turningPageRenderContext = new BookPageRenderContext(drawingContext, element => _elementContainer3.Children.Add(element));
                    _elementContainer3.RenderTransform = turningPageRenderTransform;

                    if (pageTurningMode == PageTurningMode.Next &&
                        currentPage + 2 >= 0 && currentPage + 2 < pageCount)
                    {
                        drawingContext.PushTransform(turningPageRenderTransform);
                        source.RenderPage(turningPageRenderContext, pageSize, currentPage + 2);
                        drawingContext.Pop();
                    }
                    else if (pageTurningMode == PageTurningMode.Prev &&
                        currentPage - 1 >= 0 && currentPage - 1 < pageCount)
                    {
                        drawingContext.PushTransform(turningPageRenderTransform);
                        source.RenderPage(turningPageRenderContext, pageSize, currentPage - 1);
                        drawingContext.Pop();
                    }

                    drawingContext.Pop();
                }

                var rotationOfSplitLine = Math.Atan2(splitLine.Direction.Y, splitLine.Direction.X);
                var angleOfSplitLine = rotationOfSplitLine / Math.PI * 180;

                HitRect(splitLine, bookRect, out var hit1, out var hit2);
                new LineSegment(hit1, hit2).ExpandToRect(turningShadowSize * 2,
                    out var turningShadowRectP1,
                    out var turningShadowRectP2,
                    out var turningShadowRectP3,
                    out var turningShadowRectP4);

                var turningShadowBrush = new LinearGradientBrush(
                    new GradientStopCollection()
                    {
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 0.0),
                        new GradientStop(Color.FromArgb((byte)(255 * shadowOpacity), 0, 0, 0), 0.5),
                        new GradientStop(Color.FromArgb(0, 0, 0, 0), 1.0),
                    }, angleOfSplitLine)
                {
                    StartPoint = turningShadowRectP2,
                    EndPoint = turningShadowRectP3,
                    MappingMode = BrushMappingMode.Absolute,
                };

                var turningShadowClip = new CombinedGeometry(mask1, mask2);

                drawingContext.DrawGeometry(turningShadowBrush, null, turningShadowClip);

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
            if (Mode == BookMode.TwoSide)
            {
                CoreRenderDoubleSide(drawingContext);
            }
        }


        public static readonly DependencyProperty BackgroundProperty =
            Panel.BackgroundProperty.AddOwner(typeof(SimulatedBook));

        public static readonly DependencyProperty ModeProperty =
            DependencyProperty.Register(nameof(Mode), typeof(BookMode), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(BookMode.TwoSide, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Content), typeof(IBookContent), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, propertyChangedCallback: OnContentChanged));

        public static readonly DependencyProperty SpineShadowSizeProperty =
            DependencyProperty.Register(nameof(SpineShadowSize), typeof(double), typeof(SimulatedBook),
                new FrameworkPropertyMetadata(5.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TurningShadowSizeProperty =
            DependencyProperty.Register(nameof(TurningShadowSize), typeof(double), typeof(SimulatedBook),
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
    }
}
