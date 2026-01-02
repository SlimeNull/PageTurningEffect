using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace PageTurningEffect.Components
{
    public class BookPageRenderContext
    {
        private readonly Action<UIElement> _elementRegisterImpl;

        internal BookPageRenderContext(DrawingContext drawingContext, Action<UIElement> elementRegisterImpl)
        {
            _elementRegisterImpl = elementRegisterImpl;
            DrawingContext = drawingContext ?? throw new ArgumentNullException(nameof(drawingContext));
        }

        public DrawingContext DrawingContext { get; }

        public void RegisterElement(UIElement uiElement)
        {
            _elementRegisterImpl?.Invoke(uiElement);
        }
    }
}
