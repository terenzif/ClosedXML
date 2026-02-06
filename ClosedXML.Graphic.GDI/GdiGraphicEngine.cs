using System;
using System.Drawing;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using ClosedXML.Graphics;
using FontStyle = System.Drawing.FontStyle;

namespace ClosedXML.Graphic.GDI
{
    public class GdiGraphicEngine : GraphicEngine, IDisposable
    {
        private readonly Bitmap _bitmap;
        private readonly System.Drawing.Graphics _graphics;
        private bool _disposed;

        public static Lazy<GdiGraphicEngine> Instance { get; } = new Lazy<GdiGraphicEngine>(() => new GdiGraphicEngine());

        public GdiGraphicEngine()
        {
            // Create a dummy bitmap/graphics context for measurement
            _bitmap = new Bitmap(1, 1);
            _graphics = System.Drawing.Graphics.FromImage(_bitmap);
            _graphics.PageUnit = GraphicsUnit.Pixel;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _graphics?.Dispose();
                _bitmap?.Dispose();
            }

            _disposed = true;
        }

        public override double GetTextHeight(IXLFontBase font, double dpiY)
        {
             using var gdiFont = GetGdiFont(font);
             // Height in pixels
             var height = gdiFont.GetHeight(96); // GetHeight(dpi)
             // We need it for specific DPI?
             return gdiFont.GetHeight((float)dpiY);
        }

        public override double GetTextWidth(string text, IXLFontBase font, double dpiX)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            using var gdiFont = GetGdiFont(font);

            // MeasureString returns size including some padding, using MeasureCharacterRanges or TextRenderer might be more accurate
            // but for GDI+ standard approximation MeasureString is often used.
            // Using StringFormat.GenericTypographic to reduce padding
            var size = _graphics.MeasureString(text, gdiFont, new PointF(0, 0), StringFormat.GenericTypographic);

            // MeasureString result is in pixels (PageUnit is Pixel).
            // We need to adjust for DPI if _graphics resolution differs from requested dpiX?
            // _graphics.DpiX usually matches system or 96.

            return size.Width * (dpiX / _graphics.DpiX);
        }

        public override double GetMaxDigitWidth(IXLFontBase font, double dpiX)
        {
            using var gdiFont = GetGdiFont(font);
            float maxWidth = 0;
            for (char c = '0'; c <= '9'; c++)
            {
                 var size = _graphics.MeasureString(c.ToString(), gdiFont, new PointF(0, 0), StringFormat.GenericTypographic);
                 if (size.Width > maxWidth) maxWidth = size.Width;
            }
            return maxWidth * (dpiX / _graphics.DpiX);
        }

        public override double GetDescent(IXLFontBase font, double dpiY)
        {
            using var gdiFont = GetGdiFont(font);
            var family = gdiFont.FontFamily;
            var style = gdiFont.Style;
            var descent = family.GetCellDescent(style);
            var emHeight = family.GetEmHeight(style);

            // Convert design units to pixels
            var descentPixels = (descent / (float)emHeight) * gdiFont.Size; // Size is in points? No, Size is in Unit (Pixel if not specified?)

            // Font.Size property returns the em-size of the font, in the units specified by the Unit property.
            // We created font using pixel size (derived from point size)

            // But we can be more precise:
            // pixel_size = point_size * dpi / 72
            var pixelSize = (float)(font.FontSize * dpiY / 72.0);

            return (descent / (float)emHeight) * pixelSize;
        }

        public override GlyphBox GetGlyphBox(ReadOnlySpan<int> graphemeCluster, IXLFontBase font, Dpi dpi)
        {
             // GDI+ doesn't easily support Glyph metrics directly from codepoints/spans efficiently without unsafe code or complex interop.
             // We will approximate using MeasureString on the cluster converted to string.

            string text = string.Empty;
            foreach(var cp in graphemeCluster)
            {
                 text += char.ConvertFromUtf32(cp);
            }

            using var gdiFont = GetGdiFont(font);
            var size = _graphics.MeasureString(text, gdiFont, new PointF(0, 0), StringFormat.GenericTypographic);

            var width = size.Width * (dpi.X / _graphics.DpiX);

            var emInPx = font.FontSize / 72d * dpi.X;
            var descentPx = GetDescent(font, dpi.Y);

            return new GlyphBox(
                (float)Math.Round(width, MidpointRounding.AwayFromZero),
                (float)Math.Round(emInPx, MidpointRounding.AwayFromZero),
                (float)Math.Round(descentPx, MidpointRounding.AwayFromZero));
        }

        private Font GetGdiFont(IXLFontBase fontBase)
        {
            var style = FontStyle.Regular;
            if (fontBase.Bold) style |= FontStyle.Bold;
            if (fontBase.Italic) style |= FontStyle.Italic;
            if (fontBase.Strikethrough) style |= FontStyle.Strikeout;
            if (fontBase.Underline != XLFontUnderlineValues.None) style |= FontStyle.Underline;

            // fontBase.FontSize is in Points.
            // System.Drawing.Font constructor with size takes size in "em-size, in units specified by unit parameter".
            // Default unit is Point.
            try
            {
                return new Font(fontBase.FontName, (float)fontBase.FontSize, style, GraphicsUnit.Point);
            }
            catch
            {
                // Fallback
                return new Font(FontFamily.GenericSansSerif, (float)fontBase.FontSize, style, GraphicsUnit.Point);
            }
        }
    }
}
