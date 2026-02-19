using System.Collections.Generic;
using ClosedXML.Excel;
using ClosedXML.Excel.CalcEngine;
using NUnit.Framework;

namespace ClosedXML.Tests.Excel.CalcEngine
{
    [TestFixture]
    internal class FormulaDependenciesTests
    {
        [Test]
        public void RenameSheet_updates_matching_areas()
        {
            var dependencies = new FormulaDependencies();
            var area1 = new XLBookArea("Sheet1", new XLSheetRange(1, 1, 2, 2)); // A1:B2
            var area2 = new XLBookArea("Sheet2", new XLSheetRange(3, 3, 4, 4)); // C3:D4
            dependencies.AddAreas(new List<XLBookArea> { area1, area2 });

            dependencies.RenameSheet("Sheet1", "RenamedSheet");

            var expectedArea1 = new XLBookArea("RenamedSheet", new XLSheetRange(1, 1, 2, 2));
            CollectionAssert.AreEquivalent(new[] { expectedArea1, area2 }, dependencies.Areas);
        }

        [Test]
        public void RenameSheet_to_same_name_doesnt_change_anything()
        {
            var dependencies = new FormulaDependencies();
            var area1 = new XLBookArea("Sheet1", new XLSheetRange(1, 1, 2, 2));
            var name1 = new XLName("Sheet1", "Name1");
            dependencies.AddAreas(new List<XLBookArea> { area1 });
            dependencies.AddName(name1);

            dependencies.RenameSheet("Sheet1", "Sheet1");

            CollectionAssert.AreEquivalent(new[] { area1 }, dependencies.Areas);
            CollectionAssert.AreEquivalent(new[] { name1 }, dependencies.Names);
        }

        [Test]
        public void RenameSheet_updates_multiple_dependencies_on_same_sheet()
        {
            var dependencies = new FormulaDependencies();
            var area1 = new XLBookArea("Sheet1", new XLSheetRange(1, 1, 2, 2));
            var area2 = new XLBookArea("Sheet1", new XLSheetRange(3, 3, 4, 4));
            var name1 = new XLName("Sheet1", "Name1");
            var name2 = new XLName("Sheet1", "Name2");
            dependencies.AddAreas(new List<XLBookArea> { area1, area2 });
            dependencies.AddName(name1);
            dependencies.AddName(name2);

            dependencies.RenameSheet("Sheet1", "RenamedSheet");

            var expectedArea1 = new XLBookArea("RenamedSheet", new XLSheetRange(1, 1, 2, 2));
            var expectedArea2 = new XLBookArea("RenamedSheet", new XLSheetRange(3, 3, 4, 4));
            var expectedName1 = new XLName("RenamedSheet", "Name1");
            var expectedName2 = new XLName("RenamedSheet", "Name2");

            CollectionAssert.AreEquivalent(new[] { expectedArea1, expectedArea2 }, dependencies.Areas);
            CollectionAssert.AreEquivalent(new[] { expectedName1, expectedName2 }, dependencies.Names);
        }

        [Test]
        public void RenameSheet_is_case_insensitive_for_areas()
        {
            var dependencies = new FormulaDependencies();
            var area1 = new XLBookArea("Sheet1", new XLSheetRange(1, 1, 2, 2));
            dependencies.AddAreas(new List<XLBookArea> { area1 });

            dependencies.RenameSheet("SHEET1", "RenamedSheet");

            var expectedArea1 = new XLBookArea("RenamedSheet", new XLSheetRange(1, 1, 2, 2));
            CollectionAssert.AreEquivalent(new[] { expectedArea1 }, dependencies.Areas);
        }

        [Test]
        public void RenameSheet_updates_matching_names()
        {
            var dependencies = new FormulaDependencies();
            var name1 = new XLName("Sheet1", "Name1");
            var name2 = new XLName("Sheet2", "Name2");
            var workbookName = new XLName("WorkbookName");
            dependencies.AddName(name1);
            dependencies.AddName(name2);
            dependencies.AddName(workbookName);

            dependencies.RenameSheet("Sheet1", "RenamedSheet");

            var expectedName1 = new XLName("RenamedSheet", "Name1");
            CollectionAssert.AreEquivalent(new[] { expectedName1, name2, workbookName }, dependencies.Names);
        }

        [Test]
        public void RenameSheet_is_case_insensitive_for_names()
        {
            var dependencies = new FormulaDependencies();
            var name1 = new XLName("Sheet1", "Name1");
            dependencies.AddName(name1);

            dependencies.RenameSheet("SHEET1", "RenamedSheet");

            var expectedName1 = new XLName("RenamedSheet", "Name1");
            CollectionAssert.AreEquivalent(new[] { expectedName1 }, dependencies.Names);
        }

        [Test]
        public void RenameSheet_handles_no_dependencies()
        {
            var dependencies = new FormulaDependencies();
            Assert.DoesNotThrow(() => dependencies.RenameSheet("Sheet1", "RenamedSheet"));
            CollectionAssert.IsEmpty(dependencies.Areas);
            CollectionAssert.IsEmpty(dependencies.Names);
        }

        [Test]
        public void RenameSheet_handles_no_matching_dependencies()
        {
            var dependencies = new FormulaDependencies();
            var area1 = new XLBookArea("OtherSheet", new XLSheetRange(1, 1, 2, 2));
            var name1 = new XLName("OtherSheet", "Name1");
            dependencies.AddAreas(new List<XLBookArea> { area1 });
            dependencies.AddName(name1);

            dependencies.RenameSheet("Sheet1", "RenamedSheet");

            CollectionAssert.AreEquivalent(new[] { area1 }, dependencies.Areas);
            CollectionAssert.AreEquivalent(new[] { name1 }, dependencies.Names);
        }
    }
}
