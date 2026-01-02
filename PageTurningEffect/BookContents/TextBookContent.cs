using System;
using System.Collections.Generic;
using System.Text;

namespace PageTurningEffect.BookContents
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Windows;
    using System.Windows.Media;
    using PageTurningEffect.Components;

    public class TextBookContent : IBookContent
    {
        private readonly string _text;
        private readonly double _fontSize;
        private readonly FontFamily _fontFamily;
        private readonly Brush _foreground;
        private List<string>? _pages;
        private Size _currengPageSize;

        public TextBookContent(string text)
            : this(text, 14, new FontFamily("Arial"), Brushes.Black)
        {
        }

        public TextBookContent(
            string text,
            double fontSize = 14,
            FontFamily? fontFamily = null,
            Brush? foreground = null)
        {
            _text = text ?? string.Empty;
            _fontSize = fontSize;
            _fontFamily = fontFamily ?? new FontFamily("Arial");
            _foreground = foreground ?? Brushes.Black;
        }

        /// <summary>
        /// 根据页面尺寸计算分页
        /// </summary>
        [MemberNotNull(nameof(_pages))]
        public void CalculatePages(Size pageSize)
        {
            _pages = new List<string>();

            try
            {
                if (string.IsNullOrEmpty(_text))
                {
                    _pages.Add(string.Empty);
                    return;
                }

                var typeface = new Typeface(_fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

                // 计算可用区域
                double availableWidth = pageSize.Width;
                double availableHeight = pageSize.Height;

                // 创建格式化文本以测量行高
                var sampleText = new FormattedText(
                "Sample",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                _fontSize,
                _foreground,
                VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

                sampleText.MaxTextWidth = pageSize.Width;

                double lineHeight = sampleText.Height * 1.2; // 1.2 为行距系数
                int maxLinesPerPage = (int)(availableHeight / lineHeight);

                if (maxLinesPerPage <= 0)
                {
                    _pages.Add(_text);
                    return;
                }

                // 将文本按段落分割
                string[] paragraphs = _text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                List<string> currentPageLines = new List<string>();
                int currentLineCount = 0;

                foreach (string paragraph in paragraphs)
                {
                    if (string.IsNullOrEmpty(paragraph))
                    {
                        // 空行
                        if (currentLineCount >= maxLinesPerPage)
                        {
                            _pages.Add(string.Join(Environment.NewLine, currentPageLines));
                            currentPageLines.Clear();
                            currentLineCount = 0;
                        }
                        currentPageLines.Add(string.Empty);
                        currentLineCount++;
                        continue;
                    }

                    // 将段落按可用宽度分行
                    List<string> wrappedLines = WrapText(paragraph, availableWidth, typeface);

                    foreach (string line in wrappedLines)
                    {
                        if (currentLineCount >= maxLinesPerPage)
                        {
                            _pages.Add(string.Join(Environment.NewLine, currentPageLines));
                            currentPageLines.Clear();
                            currentLineCount = 0;
                        }

                        currentPageLines.Add(line);
                        currentLineCount++;
                    }
                }

                // 添加最后一页
                if (currentPageLines.Count > 0)
                {
                    _pages.Add(string.Join(Environment.NewLine, currentPageLines));
                }

                // 确保至少有一页
                if (_pages.Count == 0)
                {
                    _pages.Add(string.Empty);
                }
            }
            finally
            {
                _currengPageSize = pageSize;
            }
        }

        /// <summary>
        /// 将文本按指定宽度换行
        /// </summary>
        private List<string> WrapText(string text, double maxWidth, Typeface typeface)
        {
            List<string> lines = new List<string>();
            string[] words = text.Split(' ');

            System.Text.StringBuilder currentLine = new System.Text.StringBuilder();

            foreach (string word in words)
            {
                string testLine = currentLine.Length == 0 ? word : currentLine + " " + word;

                var formattedText = new FormattedText(
                testLine,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                _fontSize,
                _foreground,
                VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

                if (formattedText.Width > maxWidth && currentLine.Length > 0)
                {
                    lines.Add(currentLine.ToString());
                    currentLine.Clear();
                    currentLine.Append(word);
                }
                else
                {
                    if (currentLine.Length > 0)
                        currentLine.Append(" ");
                    currentLine.Append(word);
                }
            }

            if (currentLine.Length > 0)
            {
                lines.Add(currentLine.ToString());
            }

            return lines.Count > 0 ? lines : new List<string> { text };
        }


        public int GetPageCount(Size pageSize)
        {
            // 如果还没有计算分页，先计算
            if (_pages is null ||
                _currengPageSize != pageSize)
            {
                CalculatePages(pageSize);
            }

            return _pages.Count;
        }

        public void RenderPage(DrawingContext drawingContext, Size pageSize, int pageIndex)
        {
            // 如果还没有计算分页，先计算
            if (_pages is null ||
                _currengPageSize != pageSize)
            {
                CalculatePages(pageSize);
            }

            if (pageIndex < 0 || pageIndex >= _pages.Count)
                return;

            string pageText = _pages[pageIndex];

            if (string.IsNullOrEmpty(pageText))
                return;

            var typeface = new Typeface(_fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            var formattedText = new FormattedText(
                pageText,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                _fontSize,
                _foreground,
                VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

            formattedText.MaxTextWidth = pageSize.Width;

            // 绘制文本
            drawingContext.DrawText(formattedText, new Point(0, 0));

            // 可选：绘制页码
            DrawPageNumber(drawingContext, pageSize, pageIndex, _pages.Count);
        }

        private void DrawPageNumber(DrawingContext drawingContext, Size pageSize, int pageIndex, int pageCount)
        {
            string pageNumber = $"{pageIndex + 1} / {pageCount}";

            var typeface = new Typeface(_fontFamily, FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);

            var formattedText = new FormattedText(
                pageNumber,
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                typeface,
                10,
                Brushes.Gray,
            VisualTreeHelper.GetDpi(Application.Current.MainWindow).PixelsPerDip);

            double x = (pageSize.Width - formattedText.Width) / 2;
            double y = pageSize.Height - formattedText.Height / 2;

            drawingContext.DrawText(formattedText, new Point(x, y));
        }
    }

}
