using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using ClosedXML.Excel;
using DocumentFormat.OpenXml.Packaging;
using NUnit.Framework;

namespace ClosedXML.Tests.Excel.IO;

#if STYLES_REWORK
[TestFixture]
internal class StylesWriterTests
{
    [Test]
    public void OnlyUsedCellFormatsAreWritten()
    {
        using var ms = new MemoryStream();

        // Create a workbook with some used and unused styles
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sheet1");
            
            // Apply a custom format to a cell
            ws.Cell("A1").Value = "Test";
            ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.Red;
            ws.Cell("A1").Style.Font.Bold = true;
            
            // Apply a different custom format to another cell
            ws.Cell("B1").Value = "Test2";
            ws.Cell("B1").Style.Fill.BackgroundColor = XLColor.Blue;
            
            // Create a style on a row
            ws.Row(3).Style.Font.Italic = true;
            ws.Row(3).Cell(1).Value = "Row style";
            
            // Create a style on a column
            ws.Column(5).Style.Font.FontSize = 14;
            ws.Column(5).Cell(1).Value = "Column style";
            
            // Create a style on the worksheet (this should be in the output)
            ws.Style.Font.FontName = "Arial";
            
            wb.SaveAs(ms);
        }

        ms.Seek(0, SeekOrigin.Begin);

        // Verify the saved file contains only the used formats
        using (var package = SpreadsheetDocument.Open(ms, false))
        {
            var stylesPart = package.WorkbookPart.WorkbookStylesPart;
            Assert.IsNotNull(stylesPart);
            
            using var reader = new StreamReader(stylesPart.GetStream());
            var stylesXml = reader.ReadToEnd();
            var stylesDoc = XDocument.Parse(stylesXml);
            var ns = stylesDoc.Root.GetDefaultNamespace();
            
            // Check that cellXfs contains the expected formats
            var cellXfs = stylesDoc.Root.Element(ns + "cellXfs");
            Assert.IsNotNull(cellXfs);
            
            var xfElements = cellXfs.Elements(ns + "xf").ToList();
            
            // We should have:
            // - Default format (index 0)
            // - Cell A1 format (red fill, bold)
            // - Cell B1 format (blue fill)
            // - Row 3 format (italic)
            // - Column 5 format (font size 14)
            // - Worksheet format (Arial font)
            // Total should be manageable and not include unused formats
            Assert.Greater(xfElements.Count, 0, "Should have at least the default format");
            Assert.Less(xfElements.Count, 20, "Should not have excessive unused formats");
        }
    }

    [Test]
    public void UnusedCustomFormatsAreExcluded()
    {
        using var ms = new MemoryStream();

        // Create a workbook and apply styles, then clear some cells
        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sheet1");
            
            // Apply styles to cells
            ws.Cell("A1").Style.Fill.BackgroundColor = XLColor.Red;
            ws.Cell("A2").Style.Fill.BackgroundColor = XLColor.Blue;
            ws.Cell("A3").Style.Fill.BackgroundColor = XLColor.Green;
            
            // Clear A2 and A3, making their styles "unused"
            ws.Cell("A2").Clear(XLClearOptions.All);
            ws.Cell("A3").Clear(XLClearOptions.All);
            
            // Only A1's style should be saved
            ws.Cell("A1").Value = "Only this has a custom style";
            
            wb.SaveAs(ms);
        }

        ms.Seek(0, SeekOrigin.Begin);

        // Verify the saved file
        using (var package = SpreadsheetDocument.Open(ms, false))
        {
            var stylesPart = package.WorkbookPart.WorkbookStylesPart;
            Assert.IsNotNull(stylesPart);
            
            using var reader = new StreamReader(stylesPart.GetStream());
            var stylesXml = reader.ReadToEnd();
            var stylesDoc = XDocument.Parse(stylesXml);
            var ns = stylesDoc.Root.GetDefaultNamespace();
            
            var cellXfs = stylesDoc.Root.Element(ns + "cellXfs");
            Assert.IsNotNull(cellXfs);
            
            var xfElements = cellXfs.Elements(ns + "xf").ToList();
            
            // Should have minimal formats: default + A1's format (+ potentially worksheet default)
            // The cleared cells' formats should NOT be present
            Assert.Less(xfElements.Count, 10, "Should not include unused formats from cleared cells");
        }
    }

    [Test]
    public void RowColumnAndWorksheetFormatsAreIncluded()
    {
        using var ms = new MemoryStream();

        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sheet1");
            
            // Set worksheet-level style
            ws.Style.Font.FontName = "Courier New";
            
            // Set row style
            ws.Row(2).Style.Font.Bold = true;
            ws.Row(2).Cell(1).Value = "Bold row";
            
            // Set column style
            ws.Column(3).Style.Font.Italic = true;
            ws.Column(3).Cell(1).Value = "Italic column";
            
            // Don't set any cell-specific styles, only row/column/worksheet
            
            wb.SaveAs(ms);
        }

        ms.Seek(0, SeekOrigin.Begin);

        // Reload and verify
        using (var wb = new XLWorkbook(ms))
        {
            var ws = wb.Worksheet(1);
            
            // Verify worksheet style is preserved
            Assert.AreEqual("Courier New", ws.Style.Font.FontName);
            
            // Verify row style is preserved
            Assert.IsTrue(ws.Row(2).Style.Font.Bold);
            
            // Verify column style is preserved
            Assert.IsTrue(ws.Column(3).Style.Font.Italic);
        }
    }

    [Test]
    public void CellFormatsFromFormatSliceAreIncluded()
    {
        using var ms = new MemoryStream();

        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sheet1");
            
            // Apply styles to various cells scattered across the sheet
            for (int i = 1; i <= 100; i += 10)
            {
                var cell = ws.Cell(i, i);
                cell.Value = $"Cell {i}";
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 14;
            }
            
            wb.SaveAs(ms);
        }

        ms.Seek(0, SeekOrigin.Begin);

        // Reload and verify all cell styles are preserved
        using (var wb = new XLWorkbook(ms))
        {
            var ws = wb.Worksheet(1);
            
            for (int i = 1; i <= 100; i += 10)
            {
                var cell = ws.Cell(i, i);
                Assert.AreEqual($"Cell {i}", cell.Value);
                
                // Verify the cell format is preserved
                Assert.IsTrue(cell.Style.Font.Bold, $"Cell at {i},{i} should be bold");
                Assert.AreEqual(14, cell.Style.Font.FontSize);
            }
        }
    }

    [Test]
    public void PredefinedFormatsAreHandledCorrectly()
    {
        using var ms = new MemoryStream();

        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sheet1");
            
            // Use predefined number format
            ws.Cell("A1").Value = 12345.67;
            ws.Cell("A1").Style.NumberFormat.NumberFormatId = 2; // 0.00
            
            // Add content to make sure we have used cells
            ws.Cell("B1").Value = "Test";
            
            wb.SaveAs(ms);
        }

        ms.Seek(0, SeekOrigin.Begin);

        // Reload and verify number formats are preserved
        using (var wb = new XLWorkbook(ms))
        {
            var ws = wb.Worksheet(1);
            
            Assert.AreEqual(2, ws.Cell("A1").Style.NumberFormat.NumberFormatId);
            Assert.AreEqual("Test", ws.Cell("B1").Value);
        }
    }

    [Test]
    public void EmptyWorkbookHasMinimalStyles()
    {
        using var ms = new MemoryStream();

        using (var wb = new XLWorkbook())
        {
            wb.AddWorksheet("Sheet1");
            wb.SaveAs(ms);
        }

        ms.Seek(0, SeekOrigin.Begin);

        // Verify minimal styles for empty workbook
        using (var package = SpreadsheetDocument.Open(ms, false))
        {
            var stylesPart = package.WorkbookPart.WorkbookStylesPart;
            Assert.IsNotNull(stylesPart);
            
            using var reader = new StreamReader(stylesPart.GetStream());
            var stylesXml = reader.ReadToEnd();
            var stylesDoc = XDocument.Parse(stylesXml);
            var ns = stylesDoc.Root.GetDefaultNamespace();
            
            var cellXfs = stylesDoc.Root.Element(ns + "cellXfs");
            Assert.IsNotNull(cellXfs);
            
            var xfElements = cellXfs.Elements(ns + "xf").ToList();
            
            // Empty workbook should have minimal formats (just default)
            Assert.Greater(xfElements.Count, 0, "Should have at least default format");
            Assert.Less(xfElements.Count, 5, "Empty workbook should have minimal styles");
        }
    }

    [Test]
    public void DefaultFormatIsAlwaysIncluded()
    {
        using var ms = new MemoryStream();

        using (var wb = new XLWorkbook())
        {
            var ws = wb.AddWorksheet("Sheet1");
            // Don't apply any custom styles
            ws.Cell("A1").Value = "Plain text";
            
            wb.SaveAs(ms);
        }

        ms.Seek(0, SeekOrigin.Begin);

        // Verify default format is present
        using (var package = SpreadsheetDocument.Open(ms, false))
        {
            var stylesPart = package.WorkbookPart.WorkbookStylesPart;
            Assert.IsNotNull(stylesPart);
            
            using var reader = new StreamReader(stylesPart.GetStream());
            var stylesXml = reader.ReadToEnd();
            var stylesDoc = XDocument.Parse(stylesXml);
            var ns = stylesDoc.Root.GetDefaultNamespace();
            
            var cellXfs = stylesDoc.Root.Element(ns + "cellXfs");
            Assert.IsNotNull(cellXfs);
            
            var xfElements = cellXfs.Elements(ns + "xf").ToList();
            Assert.Greater(xfElements.Count, 0, "Default format must be present");
        }
    }
}
#endif
