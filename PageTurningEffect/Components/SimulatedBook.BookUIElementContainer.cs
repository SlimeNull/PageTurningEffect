using System.Windows;
using System.Windows.Controls;

namespace PageTurningEffect.Components
{
    public partial class SimulatedBook
    {
        private class BookUIElementContainer : Panel
        {
            private readonly SimulatedBook _owner;

            public BookUIElementContainer(SimulatedBook owner)
            {
                _owner = owner;
            }

            protected override Size MeasureOverride(Size availableSize)
            {
                return _owner.PageSize;
            }

            protected override Size ArrangeOverride(Size finalSize)
            {
                foreach (UIElement child in InternalChildren)
                {
                    child.Arrange(new Rect(default(Point), finalSize));
                }

                return finalSize;
            }
        }
    }
}
