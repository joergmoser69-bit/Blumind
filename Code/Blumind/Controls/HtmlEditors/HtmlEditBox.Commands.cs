using System;
using System.Drawing;

namespace Blumind.Controls
{
    public partial class HtmlEditBox
    {
        #region Operations
        public bool ExecCommand(string command, object value)
        {
            return ExecCommand(command, false, value);
        }

        public bool ExecCommand(string command)
        {
            return ExecCommand(command, false, null);
        }

        public void Copy()
        {
            ExecCommand(HtmlCommandIdentifiers.Copy);
        }

        public void Cut()
        {
            ExecCommand(HtmlCommandIdentifiers.Cut);
        }

        public void Paste()
        {
            ExecCommand(HtmlCommandIdentifiers.Paste);
        }

        public void Delete()
        {
            ExecCommand(HtmlCommandIdentifiers.Delete);
        }

        public void Undo()
        {
            ExecCommand(HtmlCommandIdentifiers.Undo);
        }

        public void Redo()
        {
            ExecCommand(HtmlCommandIdentifiers.Redo);
        }

        public void SetBold()
        {
            ExecCommand(HtmlCommandIdentifiers.Bold);
        }

        public void SetItalic()
        {
            ExecCommand(HtmlCommandIdentifiers.Italic);
        }

        public void SetUnderline()
        {
            ExecCommand(HtmlCommandIdentifiers.Underline);
        }

        public void SetStrikeThrough()
        {
            ExecCommand(HtmlCommandIdentifiers.StrikeThrough);
        }

        public void InsertOrderedList()
        {
            ExecCommand(HtmlCommandIdentifiers.InsertOrderedList);
        }

        public void InsertUnOrderedList()
        {
            ExecCommand(HtmlCommandIdentifiers.InsertUnorderedList);
        }

        public void Outdent()
        {
            ExecCommand(HtmlCommandIdentifiers.Outdent);
        }

        public void Indent()
        {
            ExecCommand(HtmlCommandIdentifiers.Indent);
        }

        public void SetFont(Font font)
        {
            if (font == null)
                return;

            SetFontName(font.FontFamily.Name);

            float[] htmlSize = new float[] { 8, 10, 12, 14, 18, 24, 36 };
            int hs = htmlSize.Length - 1;
            for (int i = 0; i < htmlSize.Length; i++)
            {
                if (font.SizeInPoints <= htmlSize[i])
                {
                    hs = i;
                    break;
                }
            }
            SetFontSize((hs + 1).ToString());

            if ((font.Style & FontStyle.Bold) == FontStyle.Bold)
                SetBold();

            if ((font.Style & FontStyle.Italic) == FontStyle.Italic)
                SetItalic();

            if ((font.Style & FontStyle.Underline) == FontStyle.Underline)
                SetUnderline();

            if ((font.Style & FontStyle.Strikeout) == FontStyle.Strikeout)
                SetStrikeThrough();
        }

        public void SetFontName(string fontName)
        {
            ExecCommand(HtmlCommandIdentifiers.FontName, fontName);
        }

        public void SetFontSize(string fontSize)
        {
            ExecCommand(HtmlCommandIdentifiers.FontSize, fontSize);
        }

        public void IncreaseFontSize()
        {
            ExecCommand(HtmlCommandIdentifiers.IncreaseFontSize, "1");
        }

        public void DecreaseFontSize()
        {
            ExecCommand(HtmlCommandIdentifiers.DecreaseFontSize, "1");
        }

        public void SetForeColor(string color)
        {
            ExecCommand(HtmlCommandIdentifiers.ForeColor, color);
        }

        public void SetForeColor(Color color)
        {
            ExecCommand(HtmlCommandIdentifiers.ForeColor, color.ToWebColor());
        }

        public void SetBackColor(string color)
        {
            ExecCommand(HtmlCommandIdentifiers.BackColor, color);
        }

        public void SetBackColor(Color color)
        {
            ExecCommand(HtmlCommandIdentifiers.BackColor, color.ToWebColor());
        }

        public void AddHyperLink()
        {
            ExecCommand(HtmlCommandIdentifiers.CreateLink);
        }

        public void AddImage()
        {
            ExecCommand(HtmlCommandIdentifiers.InsertImage, true, null);
        }

        public void AlignmentLeft()
        {
            ExecCommand(HtmlCommandIdentifiers.JustifyLeft);
        }

        public void AlignmentCenter()
        {
            ExecCommand(HtmlCommandIdentifiers.JustifyCenter);
        }

        public void AlignmentRight()
        {
            ExecCommand(HtmlCommandIdentifiers.JustifyRight);
        }

        public void AlignmentJustify()
        {
            ExecCommand(HtmlCommandIdentifiers.JustifyFull);
        }
        #endregion
    }
}

