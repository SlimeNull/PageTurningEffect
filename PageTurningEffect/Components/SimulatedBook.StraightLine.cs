using System.Windows;

namespace PageTurningEffect.Components
{
    public partial class SimulatedBook
    {
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
    }
}
