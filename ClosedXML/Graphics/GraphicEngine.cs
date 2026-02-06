using System;
using System.IO;
using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;

namespace ClosedXML.Graphics
{
    public abstract class GraphicEngine : IXLGraphicEngine
    {
        private readonly ImageInfoReader[] _imageReaders =
        {
            new PngInfoReader(),
            new JpegInfoReader(),
            new GifInfoReader(),
            new TiffInfoReader(),
            new BmpInfoReader(),
            new EmfInfoReader(),
            new WmfInfoReader(),
            new WebpInfoReader(),
            new PcxInfoReader() // Due to poor magic detection, keep last
        };

        protected const float FontMetricSize = 16f;

        public XLPictureInfo GetPictureInfo(Stream stream, XLPictureFormat expectedFormat)
        {
            foreach (var imageReader in _imageReaders)
            {
                if (imageReader.TryGetInfo(stream, out var dimensions))
                    return dimensions;
            }

            throw new ArgumentException("Unable to determine the format of the image.");
        }

        public abstract double GetTextHeight(IXLFontBase font, double dpiY);
        public abstract double GetTextWidth(string text, IXLFontBase font, double dpiX);
        public abstract double GetMaxDigitWidth(IXLFontBase font, double dpiX);
        public abstract double GetDescent(IXLFontBase font, double dpiY);
        public abstract GlyphBox GetGlyphBox(ReadOnlySpan<int> graphemeCluster, IXLFontBase font, Dpi dpi);

        protected static double PointsToPixels(double points, double dpi) => points / 72d * dpi;
    }
}
