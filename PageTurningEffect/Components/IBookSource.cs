using System.Windows;
using System.Windows.Media;

namespace PageTurningEffect.Components
{
    public interface IBookSource
    {
        public int PageCount { get; }
        public void RenderPage(DrawingContext drawingContext, Size pageSize, int pageIndex);
    }
}
