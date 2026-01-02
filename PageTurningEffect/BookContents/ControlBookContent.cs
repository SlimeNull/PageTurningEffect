using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using PageTurningEffect.Components;

namespace PageTurningEffect.BookContents
{
    [ContentProperty(nameof(Pages))]
    public class ControlBookContent : DependencyObject, IBookContent, IAddChild
    {
        public PageCollection Pages { get; }

        public ControlBookContent()
        {
            Pages = new PageCollection();
        }

        public int GetPageCount(Size pageSize) => Pages.Count;
        public void RenderPage(BookPageRenderContext context, Size pageSize, int pageIndex)
        {
            if (pageIndex < 0 || pageIndex >= Pages.Count)
                throw new ArgumentOutOfRangeException(nameof(pageIndex));

            context.RegisterElement(Pages[pageIndex]);
        }

        public void AddChild(object value)
        {
            if (value is ControlBookPage page)
            {
                Pages.Add(page);
            }
            else
            {
                throw new ArgumentException("Value must be of type ControlBookPage.", nameof(value));
            }
        }

        public void AddText(string text)
        {
            throw new NotSupportedException();
        }

        public class PageCollection : Collection<ControlBookPage>
        {

        }
    }
}
