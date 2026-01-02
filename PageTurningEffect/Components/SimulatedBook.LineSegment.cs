using System.Windows;

namespace PageTurningEffect.Components
{
    public partial class SimulatedBook
    {
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
    }
}
