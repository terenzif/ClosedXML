using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using ClosedXML.Graphics;
using SixLabors.Fonts;
using SixLabors.Fonts.Unicode;

namespace ClosedXML.Graphic.SixLabors
{
    public class SixLaborsGraphicEngine : GraphicEngine
    {
        private const string EmbeddedFontName = "CarlitoBare";

        private readonly Lazy<IReadOnlyFontCollection> _fontCollection;
        private readonly string _fallbackFont;

        private readonly ConcurrentDictionary<MetricId, Font> _fonts = new ConcurrentDictionary<MetricId, Font>();
        private readonly Func<MetricId, Font> _loadFont;

        private readonly ConcurrentDictionary<MetricId, double> _maxDigitWidths = new ConcurrentDictionary<MetricId, double>();
        private readonly Func<MetricId, double> _calculateMaxDigitWidth;

        public static Lazy<SixLaborsGraphicEngine> Instance { get; } = new Lazy<SixLaborsGraphicEngine>(() => new SixLaborsGraphicEngine("Microsoft Sans Serif"));

        public SixLaborsGraphicEngine(string fallbackFont)
        {
            if (string.IsNullOrWhiteSpace(fallbackFont))
                throw new ArgumentException(nameof(fallbackFont));

            var fontCollection = new FontCollection();
            AddEmbeddedFont(fontCollection);

            _fontCollection = new Lazy<IReadOnlyFontCollection>(() => fontCollection.AddSystemFonts());
            _fallbackFont = fallbackFont;
            _loadFont = LoadFont;
            _calculateMaxDigitWidth = CalculateMaxDigitWidth;
        }

        private SixLaborsGraphicEngine(Stream fallbackFontStream, bool useSystemFonts, Stream[] fontStreams)
        {
            if (fallbackFontStream is null)
                throw new ArgumentNullException(nameof(fallbackFontStream));

            if (fontStreams is null)
                throw new ArgumentNullException(nameof(fontStreams));

            var fontCollection = new FontCollection();
            AddEmbeddedFont(fontCollection);
            var fallbackFamily = fontCollection.Add(fallbackFontStream);
            foreach (var fontStream in fontStreams)
                fontCollection.Add(fontStream);

            _fontCollection = useSystemFonts
                ? new Lazy<IReadOnlyFontCollection>(() => fontCollection.AddSystemFonts())
                : new Lazy<IReadOnlyFontCollection>(() => fontCollection);
            _fallbackFont = fallbackFamily.Name;
            _loadFont = LoadFont;
            _calculateMaxDigitWidth = CalculateMaxDigitWidth;
        }

        public static IXLGraphicEngine CreateOnlyWithFonts(Stream fallbackFontStream, params Stream[] fontStreams)
        {
            return new SixLaborsGraphicEngine(fallbackFontStream, false, fontStreams);
        }

        public static IXLGraphicEngine CreateWithFontsAndSystemFonts(Stream fallbackFontStream, params Stream[] fontStreams)
        {
            return new SixLaborsGraphicEngine(fallbackFontStream, true, fontStreams);
        }

        public override double GetDescent(IXLFontBase font, double dpiY)
        {
            var metrics = GetMetrics(font);
            return PointsToPixels(-metrics.VerticalMetrics.Descender * font.FontSize / metrics.UnitsPerEm, dpiY);
        }

        public override double GetMaxDigitWidth(IXLFontBase fontBase, double dpiX)
        {
            var metricId = new MetricId(fontBase);
            var maxDigitWidth = _maxDigitWidths.GetOrAdd(metricId, _calculateMaxDigitWidth);
            return PointsToPixels(maxDigitWidth * fontBase.FontSize, dpiX);
        }

        public override double GetTextHeight(IXLFontBase font, double dpiY)
        {
            var metrics = GetMetrics(font);
            return PointsToPixels((metrics.VerticalMetrics.Ascender - 2 * metrics.VerticalMetrics.Descender) * font.FontSize / metrics.UnitsPerEm, dpiY);
        }

        public override double GetTextWidth(string text, IXLFontBase fontBase, double dpiX)
        {
            var font = GetFont(fontBase);
            var dimensionsPx = TextMeasurer.MeasureAdvance(text, new TextOptions(font)
            {
                Dpi = 72, // Normalize DPI, so 1px is 1pt
                KerningMode = KerningMode.None
            });
            return PointsToPixels(dimensionsPx.Width / FontMetricSize * fontBase.FontSize, dpiX);
        }

        public override GlyphBox GetGlyphBox(ReadOnlySpan<int> graphemeCluster, IXLFontBase font, Dpi dpi)
        {
            var metric = GetMetrics(font);
            var advanceFu = 0;
            for (var i = 0; i < graphemeCluster.Length; ++i)
            {
                var containsMetrics = metric.TryGetGlyphMetrics(
                    new CodePoint(graphemeCluster[i]),
                    TextAttributes.None,
                    TextDecorations.None,
                    LayoutMode.HorizontalTopBottom,
                    ColorFontSupport.None,
                    out var glyphs);

                if (!containsMetrics)
                    continue;

                foreach (var glyph in glyphs)
                    advanceFu += glyph.AdvanceWidth;
            }

            var emInPx = font.FontSize / 72d * dpi.X;
            var advancePx = PointsToPixels(advanceFu * font.FontSize / metric.UnitsPerEm, dpi.X);
            var descentPx = GetDescent(font, dpi.Y); // Use GetDescent method to account for dpi
            return new GlyphBox(
                (float)Math.Round(advancePx, MidpointRounding.AwayFromZero),
                (float)Math.Round(emInPx, MidpointRounding.AwayFromZero),
                (float)Math.Round(descentPx, MidpointRounding.AwayFromZero));
        }

        private FontMetrics GetMetrics(IXLFontBase fontBase)
        {
            var font = GetFont(fontBase);
            return font.FontMetrics;
        }

        private Font GetFont(IXLFontBase fontBase)
        {
            return GetFont(new MetricId(fontBase));
        }

        private Font GetFont(MetricId metricId)
        {
            return _fonts.GetOrAdd(metricId, _loadFont);
        }

        private Font LoadFont(MetricId metricId)
        {
            if (!_fontCollection.Value.TryGet(metricId.Name, out var fontFamily) &&
                !_fontCollection.Value.TryGet(_fallbackFont, out fontFamily))
            {
                fontFamily = _fontCollection.Value.Get(EmbeddedFontName);
            }

            return fontFamily.CreateFont(FontMetricSize);
        }

        private void AddEmbeddedFont(FontCollection fontCollection)
        {
            var assembly = Assembly.GetExecutingAssembly();
            const string resourcePath = "ClosedXML.Graphic.SixLabors.Resources.Fonts.CarlitoBare-{0}.ttf";

            using (var regular = assembly.GetManifestResourceStream(string.Format(resourcePath, "Regular")))
            {
                if(regular != null) fontCollection.Add(regular);
            }

            using (var bold = assembly.GetManifestResourceStream(string.Format(resourcePath, "Bold")))
            {
                if(bold != null) fontCollection.Add(bold);
            }

            using (var italic = assembly.GetManifestResourceStream(string.Format(resourcePath, "Italic")))
            {
                if(italic != null) fontCollection.Add(italic);
            }

            using (var boldItalic = assembly.GetManifestResourceStream(string.Format(resourcePath, "BoldItalic")))
            {
                if(boldItalic != null) fontCollection.Add(boldItalic);
            }
        }

        private double CalculateMaxDigitWidth(MetricId metricId)
        {
            var font = GetFont(metricId);
            var metrics = font.FontMetrics;
            var maxWidth = int.MinValue;
            for (var c = '0'; c <= '9'; ++c)
            {
                var containsMetrics = metrics.TryGetGlyphMetrics(
                    new CodePoint(c),
                    TextAttributes.None,
                    TextDecorations.None,
                    LayoutMode.HorizontalTopBottom,
                    ColorFontSupport.None,
                    out var glyphMetrics);
                if (!containsMetrics)
                    continue;

                var glyphAdvance = 0;
                foreach (var glyphMetric in glyphMetrics)
                    glyphAdvance += glyphMetric.AdvanceWidth;

                maxWidth = Math.Max(maxWidth, glyphAdvance);
            }
            return maxWidth / (double)metrics.UnitsPerEm;
        }

        private readonly struct MetricId : IEquatable<MetricId>
        {
            private readonly FontStyle _style;

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
    }
}
