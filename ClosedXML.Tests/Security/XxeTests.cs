using System.IO;
using System.Text;
using System.Xml;
using ClosedXML.Excel;
using NUnit.Framework;

namespace ClosedXML.Tests.Security
{
    [TestFixture]
    public class XxeTests
    {
        [Test]
        public void Load_ShouldProhibitDtd()
        {
            // This XML contains a DTD.
            // If DtdProcessing is Prohibit, it should throw an XmlException.
            // If DtdProcessing is Parse, it might try to resolve the entity if XmlResolver is set,
            // or just allow it if not.
            // By default, XDocument.Load(XmlReader) where XmlReader is created with default settings
            // might allow DTD depending on the framework.

            string xmlWithDtd = @"<?xml version=""1.0"" encoding=""UTF-8""?>
<!DOCTYPE foo [
  <!ELEMENT foo ANY >
  <!ENTITY xxe SYSTEM ""file:///etc/passwd"" >]>
<foo>&xxe;</foo>";

            byte[] byteArray = Encoding.UTF8.GetBytes(xmlWithDtd);
            using (MemoryStream stream = new MemoryStream(byteArray))
            {
                // In the current implementation, this might return an XDocument (vulnerable)
                // or null (if it caught XmlException).
                // If it caught XmlException, it's already "safe" in a way that it doesn't crash,
                // but we want to ENSURE it's prohibited.

                var xdoc = XDocumentExtensions.Load(stream);

                // If the fix is applied, XDocumentExtensions.Load should return null
                // because it catches the XmlException thrown by XmlReader when it encounters a DTD.
                Assert.That(xdoc, Is.Null, "DtdProcessing should be set to Prohibit, which should result in an XmlException and the method returning null.");
            }
        }
    }
}
