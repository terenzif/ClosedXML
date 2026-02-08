// Keep this file CodeMaid organised and cleaned
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace ClosedXML.Excel
{
    internal static class XDocumentExtensions
    {
        private static readonly XmlReaderSettings SafeSettings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null
        };

        public static XDocument? Load(Stream stream)
        {
            using (XmlReader reader = XmlReader.Create(stream, SafeSettings))
            {
                try
                {
                    return XDocument.Load(reader);
                }
                catch (XmlException)
                {
                    return null;
                }
            }
        }
    }
}
