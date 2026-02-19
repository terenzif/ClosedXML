using ClosedXML.Excel;
using NUnit.Framework;
using System.Linq;

namespace ClosedXML.Tests.Excel.PivotTables
{
    [TestFixture]
    public class XLPivotTableCopyTests
    {
        [Test]
        public void CopyTo_should_copy_StyleFormats()
        {
            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Data");
            ws.Cell("A1").Value = "Category";
            ws.Cell("A2").Value = "A";
            ws.Cell("A3").Value = "B";
            ws.Cell("B1").Value = "Value";
            ws.Cell("B2").Value = 10;
            ws.Cell("B3").Value = 20;

            var ptSheet = wb.AddWorksheet("Pivot");
            var pt = ptSheet.PivotTables.Add("PivotTable", ptSheet.Cell("A1"), ws.RangeUsed());
            pt.RowLabels.Add("Category");
            pt.Values.Add("Value");

            // Set some style formats
            pt.StyleFormats.RowGrandTotalFormats.ForElement(XLPivotStyleFormatElement.All).Style.Font.SetBold();
            pt.StyleFormats.ColumnGrandTotalFormats.ForElement(XLPivotStyleFormatElement.All).Style.Font.SetItalic();

            var ptCopy = (XLPivotTable)pt.CopyTo(ptSheet.Cell("E1"));

            Assert.AreEqual(pt.StyleFormats.RowGrandTotalFormats.Count(), ptCopy.StyleFormats.RowGrandTotalFormats.Count());
            Assert.AreEqual(pt.StyleFormats.ColumnGrandTotalFormats.Count(), ptCopy.StyleFormats.ColumnGrandTotalFormats.Count());

            Assert.IsTrue(ptCopy.StyleFormats.RowGrandTotalFormats.ForElement(XLPivotStyleFormatElement.All).Style.Font.Bold);
            Assert.IsTrue(ptCopy.StyleFormats.ColumnGrandTotalFormats.ForElement(XLPivotStyleFormatElement.All).Style.Font.Italic);
        }
    }
}
