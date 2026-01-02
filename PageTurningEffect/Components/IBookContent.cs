using System.Windows;
using System.Windows.Media;

namespace PageTurningEffect.Components
{
    public interface IBookContent
    {
        public int GetPageCount(Size pageSize);
        public void RenderPage(DrawingContext drawingContext, Size pageSize, int pageIndex);
    }
}
