using System;
using System.Linq;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;

namespace ClosedXML.Graphics
{
    public class ApproximateGraphicEngine : GraphicEngine
    {
        private static readonly FontMetricInfo CarlitoRegular = new FontMetricInfo
        {
            UnitsPerEm = 2048,
            Ascender = 1950,
            Descender = -550,
            LineGap = 0,
            MaxDigitWidth = 1038,
            CharWidths = new ushort[]
            {
                463, 667, 821, 1020, 1038, 1464, 1397, 452, 621, 621, 1020, 1020, 511, 627, 517, 791,
                1038, 1038, 1038, 1038, 1038, 1038, 1038, 1038, 1038, 1038, 548, 548, 1020, 1020, 1020, 949,
                1831, 1185, 1114, 1092, 1260, 1000, 941, 1292, 1276, 516, 653, 1064, 861, 1751, 1322, 1356,
                1058, 1378, 1112, 941, 998, 1314, 1162, 1822, 1063, 998, 959, 628, 791, 628, 1020, 1020,
                596, 981, 1076, 866, 1076, 1019, 625, 964, 1076, 470, 490, 931, 470, 1636, 1076, 1080,
                1076, 1076, 714, 801, 686, 1076, 925, 1464, 887, 927, 809, 644, 943, 644, 1020,
            }
        };

        private static readonly FontMetricInfo CarlitoBold = new FontMetricInfo
        {
            UnitsPerEm = 2048,
            Ascender = 1950,
            Descender = -550,
            LineGap = 0,
            MaxDigitWidth = 1038,
            CharWidths = new ushort[]
            {
                463, 667, 898, 1020, 1038, 1493, 1443, 478, 638, 638, 1020, 1020, 528, 627, 547, 880,
                1038, 1038, 1038, 1038, 1038, 1038, 1038, 1038, 1038, 1038, 565, 565, 1020, 1020, 1020, 949,
                1840, 1241, 1148, 1084, 1291, 999, 940, 1305, 1292, 546, 678, 1120, 866, 1790, 1349, 1385,
                1090, 1405, 1153, 968, 1014, 1337, 1211, 1856, 1128, 1064, 979, 665, 880, 665, 1020, 1020,
                615, 1011, 1099, 857, 1099, 1031, 648, 971, 1099, 503, 523, 983, 503, 1666, 1099, 1101,
                1099, 1099, 728, 817, 710, 1099, 969, 1526, 941, 970, 814, 704, 973, 704, 1020,
            }
        };

        private static readonly FontMetricInfo CarlitoItalic = new FontMetricInfo
        {
            UnitsPerEm = 2048,
            Ascender = 1950,
            Descender = -550,
            LineGap = 0,
            MaxDigitWidth = 1038,
            CharWidths = new ushort[]
            {
                463, 667, 821, 1020, 1038, 1464, 1397, 452, 621, 621, 1020, 1020, 511, 627, 517, 794,
                1038, 1038, 1038, 1038, 1038, 1038, 1038, 1038, 1038, 1038, 548, 548, 1020, 1020, 1020, 949,
                1831, 1185, 1114, 1070, 1260, 1000, 941, 1292, 1276, 516, 653, 1064, 861, 1751, 1320, 1340,
                1058, 1360, 1112, 926, 998, 1314, 1162, 1823, 1063, 998, 959, 628, 787, 628, 1020, 1020,
                596, 1053, 1053, 852, 1053, 978, 625, 1053, 1053, 470, 490, 931, 470, 1620, 1053, 1051,
                1053, 1053, 702, 797, 686, 1053, 913, 1464, 887, 916, 809, 644, 943, 644, 1020,
            }
        };

        private static readonly FontMetricInfo CarlitoBoldItalic = new FontMetricInfo
        {
            UnitsPerEm = 2048,
            Ascender = 1950,
            Descender = -550,
            LineGap = 0,
            MaxDigitWidth = 1038,
            CharWidths = new ushort[]
            {
                463, 667, 898, 1020, 1038, 1493, 1443, 478, 638, 638, 1020, 1020, 528, 627, 547, 889,
                1038, 1038, 1038, 1038, 1038, 1038, 1038, 1038, 1038, 1038, 565, 565, 1020, 1020, 1020, 949,
                1840, 1241, 1148, 1062, 1291, 999, 940, 1305, 1292, 546, 678, 1120, 866, 1790, 1344, 1369,
                1090, 1387, 1153, 953, 1014, 1337, 1211, 1857, 1128, 1064, 979, 665, 870, 665, 1020, 1020,
                615, 1081, 1081, 843, 1081, 1006, 648, 1081, 1080, 503, 523, 983, 503, 1646, 1080, 1080,
                1081, 1081, 721, 807, 710, 1080, 961, 1526, 941, 963, 814, 704, 973, 704, 1020,
            }
        };

        public static readonly ApproximateGraphicEngine Instance = new ApproximateGraphicEngine();

        private FontMetricInfo GetMetric(IXLFontBase font)
        {
            if (font.Bold && font.Italic) return CarlitoBoldItalic;
            if (font.Bold) return CarlitoBold;
            if (font.Italic) return CarlitoItalic;
            return CarlitoRegular;
        }

        public override double GetTextHeight(IXLFontBase font, double dpiY)
        {
            var metrics = GetMetric(font);
            return PointsToPixels((metrics.Ascender - 2 * metrics.Descender) * font.FontSize / metrics.UnitsPerEm, dpiY);
        }

        public override double GetTextWidth(string text, IXLFontBase font, double dpiX)
        {
            if (string.IsNullOrEmpty(text)) return 0;

            var metrics = GetMetric(font);
            double totalWidth = 0;
            foreach (var c in text)
            {
                if (c >= 32 && c <= 126)
                {
                    totalWidth += metrics.CharWidths[c - 32];
                }
                else
                {
                    // Fallback for non-ASCII characters: use average width of '0' or similar
                    totalWidth += metrics.MaxDigitWidth;
                }
            }
            return PointsToPixels(totalWidth * font.FontSize / metrics.UnitsPerEm, dpiX);
        }

        public override double GetMaxDigitWidth(IXLFontBase font, double dpiX)
        {
            var metrics = GetMetric(font);
            return PointsToPixels(metrics.MaxDigitWidth * font.FontSize / metrics.UnitsPerEm, dpiX);
        }

        public override double GetDescent(IXLFontBase font, double dpiY)
        {
            var metrics = GetMetric(font);
            return PointsToPixels(-metrics.Descender * font.FontSize / metrics.UnitsPerEm, dpiY);
        }

        public override GlyphBox GetGlyphBox(ReadOnlySpan<int> graphemeCluster, IXLFontBase font, Dpi dpi)
        {
            var metrics = GetMetric(font);
            double advanceWidth = 0;
            foreach (var c in graphemeCluster)
            {
                 if (c >= 32 && c <= 126)
                {
                    advanceWidth += metrics.CharWidths[c - 32];
                }
                else
                {
                    advanceWidth += metrics.MaxDigitWidth;
                }
            }

            var emInPx = font.FontSize / 72d * dpi.X;
            var advancePx = PointsToPixels(advanceWidth * font.FontSize / metrics.UnitsPerEm, dpi.X);
            var descentPx = GetDescent(font, dpi.Y);

             return new GlyphBox(
                (float)Math.Round(advancePx, MidpointRounding.AwayFromZero),
                (float)Math.Round(emInPx, MidpointRounding.AwayFromZero),
                (float)Math.Round(descentPx, MidpointRounding.AwayFromZero));
        }

        private struct FontMetricInfo
        {
            public int UnitsPerEm;
            public int Ascender;
            public int Descender;
            public int LineGap;
            public float MaxDigitWidth;
            public ushort[] CharWidths;
        }
    }
}
