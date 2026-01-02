using System.Windows;
using System.Windows.Media;

namespace PageTurningEffect.Components
{
    public interface IBookContent
    {
        public int GetPageCount(Size pageSize);
        public void RenderPage(BookPageRenderContext context, Size pageSize, int pageIndex);
    }
}
