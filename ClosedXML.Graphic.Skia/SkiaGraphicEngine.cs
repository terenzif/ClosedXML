using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using ClosedXML.Graphics;
using SkiaSharp;

namespace ClosedXML.Graphic.Skia
{
    public class SkiaGraphicEngine : GraphicEngine
    {
        private readonly SKTypeface _fallbackFont;
        private readonly ConcurrentDictionary<MetricId, SKTypeface> _fonts = new ConcurrentDictionary<MetricId, SKTypeface>();
        private readonly Func<MetricId, SKTypeface> _loadFont;

        public static Lazy<SkiaGraphicEngine> Instance { get; } = new Lazy<SkiaGraphicEngine>(() => new SkiaGraphicEngine());

        public SkiaGraphicEngine()
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourcePath = "ClosedXML.Graphic.Skia.Resources.Fonts.CarlitoBare-Regular.ttf";

            using var stream = assembly.GetManifestResourceStream(resourcePath);
            if (stream == null) throw new InvalidOperationException("Embedded font not found.");

            _fallbackFont = SKTypeface.FromStream(stream);
            _loadFont = LoadFont;
        }

        public override double GetTextHeight(IXLFontBase font, double dpiY)
        {
            var typeface = GetTypeface(font);
            using var skFont = new SKFont(typeface, FontMetricSize);
            SKFontMetrics metrics = skFont.Metrics;

            var heightPoints = (-metrics.Ascent + metrics.Descent) * font.FontSize / FontMetricSize;
            return PointsToPixels(heightPoints, dpiY);
        }

        public override double GetTextWidth(string text, IXLFontBase font, double dpiX)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var typeface = GetTypeface(font);
            using var paint = new SKPaint
            {
                Typeface = typeface,
                TextSize = FontMetricSize
            };

            var width = paint.MeasureText(text);
            return PointsToPixels(width * font.FontSize / FontMetricSize, dpiX);
        }

        public override double GetMaxDigitWidth(IXLFontBase font, double dpiX)
        {
            var typeface = GetTypeface(font);
            using var paint = new SKPaint
            {
                Typeface = typeface,
                TextSize = FontMetricSize
            };

            float maxWidth = 0;
            for (char c = '0'; c <= '9'; c++)
            {
                var width = paint.MeasureText(c.ToString());
                if (width > maxWidth) maxWidth = width;
            }

            return PointsToPixels(maxWidth * font.FontSize / FontMetricSize, dpiX);
        }

        public override double GetDescent(IXLFontBase font, double dpiY)
        {
             var typeface = GetTypeface(font);
            using var skFont = new SKFont(typeface, FontMetricSize);
            SKFontMetrics metrics = skFont.Metrics;

            return PointsToPixels(metrics.Descent * font.FontSize / FontMetricSize, dpiY);
        }

        public override GlyphBox GetGlyphBox(ReadOnlySpan<int> graphemeCluster, IXLFontBase font, Dpi dpi)
        {
            var typeface = GetTypeface(font);
            using var paint = new SKPaint
            {
                Typeface = typeface,
                TextSize = FontMetricSize
            };

            string text = string.Empty;
            foreach(var cp in graphemeCluster)
            {
                 text += char.ConvertFromUtf32(cp);
            }

            var width = paint.MeasureText(text);

            var emInPx = font.FontSize / 72d * dpi.X;
            var advancePx = PointsToPixels(width * font.FontSize / FontMetricSize, dpi.X);
            var descentPx = GetDescent(font, dpi.Y);

            return new GlyphBox(
                (float)Math.Round(advancePx, MidpointRounding.AwayFromZero),
                (float)Math.Round(emInPx, MidpointRounding.AwayFromZero),
                (float)Math.Round(descentPx, MidpointRounding.AwayFromZero));
        }

        private SKTypeface GetTypeface(IXLFontBase fontBase)
        {
            return _fonts.GetOrAdd(new MetricId(fontBase), _loadFont);
        }

        private SKTypeface LoadFont(MetricId metricId)
        {
             var assembly = Assembly.GetExecutingAssembly();
             string resourceName = $"ClosedXML.Graphic.Skia.Resources.Fonts.CarlitoBare-{metricId.Style}.ttf";
             switch(metricId.Style)
             {
                 case FontStyle.Regular: resourceName = "ClosedXML.Graphic.Skia.Resources.Fonts.CarlitoBare-Regular.ttf"; break;
                 case FontStyle.Bold: resourceName = "ClosedXML.Graphic.Skia.Resources.Fonts.CarlitoBare-Bold.ttf"; break;
                 case FontStyle.Italic: resourceName = "ClosedXML.Graphic.Skia.Resources.Fonts.CarlitoBare-Italic.ttf"; break;
                 case FontStyle.BoldItalic: resourceName = "ClosedXML.Graphic.Skia.Resources.Fonts.CarlitoBare-BoldItalic.ttf"; break;
             }

             using var stream = assembly.GetManifestResourceStream(resourceName);
             if (stream != null)
             {
                 return SKTypeface.FromStream(stream);
             }

             return _fallbackFont;
        }

        private readonly struct MetricId : IEquatable<MetricId>
        {
            private readonly FontStyle _style;
            public FontStyle Style => _style;

            public MetricId(IXLFontBase fontBase)
            {
                Name = fontBase.FontName;
                _style = GetFontStyle(fontBase);
            }

            public string Name { get; }

            public bool Equals(MetricId other) => Name == other.Name && _style == other._style;

            public override bool Equals(object obj) => obj is MetricId other && Equals(other);

            public override int GetHashCode() => (Name.GetHashCode() * 397) ^ (int)_style;

            private static FontStyle GetFontStyle(IXLFontBase fontBase)
            {
                return fontBase switch
                {
                    { Bold: true, Italic: true } => FontStyle.BoldItalic,
                    { Bold: true } => FontStyle.Bold,
                    { Italic: true } => FontStyle.Italic,
                    _ => FontStyle.Regular
                };
            }
        }

        private enum FontStyle
        {
            Regular,
            Bold,
            Italic,
            BoldItalic
        }
    }
}
