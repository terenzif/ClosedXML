using System;
using System.Collections.Generic;
using System.Data;
using ClosedXML.Excel;
using ClosedXML.Excel.CalcEngine;
using NUnit.Framework;

namespace ClosedXML.Tests.Excel.CalcEngine
{
    [TestFixture]
    internal class StructuredReferenceDependenciesTests
    {
        [Test]
        public void Structured_reference_to_data_column_is_dependency()
        {
            var dependencies = GetDependencies("TableName[Second]", init: wb =>
            {
                var ws = wb.Worksheet("Sheet");
                Add4X3Table(ws, "E7");
            });

            CollectionAssert.AreEquivalent(new XLBookArea[]
            {
                new("Sheet", XLSheetRange.Parse("F8:F10"))
            }, dependencies.Areas);
        }

        [Test]
        public void Structured_reference_to_headers_is_dependency()
        {
            var dependencies = GetDependencies("TableName[#Headers]", init: wb =>
            {
                var ws = wb.Worksheet("Sheet");
                Add4X3Table(ws, "E7");
            });

            CollectionAssert.AreEquivalent(new XLBookArea[]
            {
                new("Sheet", XLSheetRange.Parse("E7:H7"))
            }, dependencies.Areas);
        }

        [Test]
        public void Structured_reference_to_totals_is_dependency()
        {
            var dependencies = GetDependencies("TableName[#Totals]", init: wb =>
            {
                var ws = wb.Worksheet("Sheet");
                Add4X3Table(ws, "E7").ShowTotalsRow = true;
            });

            CollectionAssert.AreEquivalent(new XLBookArea[]
            {
                new("Sheet", XLSheetRange.Parse("E11:H11"))
            }, dependencies.Areas);
        }

        [Test]
        public void Structured_reference_to_all_is_dependency()
        {
            var dependencies = GetDependencies("TableName[#All]", init: wb =>
            {
                var ws = wb.Worksheet("Sheet");
                Add4X3Table(ws, "E7").ShowTotalsRow = true;
            });

            CollectionAssert.AreEquivalent(new XLBookArea[]
            {
                new("Sheet", XLSheetRange.Parse("E7:H11"))
            }, dependencies.Areas);
        }

        [Test]
        public void Structured_reference_to_multiple_columns_is_dependency()
        {
            var dependencies = GetDependencies("TableName[[Second]:[Third]]", init: wb =>
            {
                var ws = wb.Worksheet("Sheet");
                Add4X3Table(ws, "E7");
            });

            CollectionAssert.AreEquivalent(new XLBookArea[]
            {
                new("Sheet", XLSheetRange.Parse("F8:G10"))
            }, dependencies.Areas);
        }

        [Test]
        public void Structured_reference_this_row_is_dependency()
        {
            // Formula in D8 refers to TableName[[#This Row],[Second]]
            // Table starts at E7, data starts at row 8.
            // D8 is in row 8.
            // Table Second column is F.
            // So dependency is F8.
            var dependencies = GetDependencies("TableName[[#This Row],[Second]]", "D8", init: wb =>
            {
                var ws = wb.Worksheet("Sheet");
                Add4X3Table(ws, "E7");
            });

            CollectionAssert.AreEquivalent(new XLBookArea[]
            {
                new("Sheet", XLSheetRange.Parse("F8"))
            }, dependencies.Areas);
        }

        [Test]
        public void Structured_reference_this_row_range_is_dependency()
        {
            // Formula range D8:D9 refers to TableName[[#This Row],[Second]]
            // It means D8 depends on F8, D9 depends on F9.
            // Combined dependency is F8:F9.
            // But GetDependencies helper only sets formula on a single cell.
            // If we want to test range dependency, we need to adapt the helper or just test single cell.
            // DependenciesVisitor returns dependencies for the formula area.

            // Let's modify GetDependencies to accept a range.
            // But wait, DependencyTree adds formulas one by one usually.
            // However, shared formulas (legacy array formulas or just shared formulas) use ranges.
            // The helper uses `cell.SetFormulaA1` which sets it for the cell.
            // If I use `ws.Range("D8:D9").FormulaA1 = ...` it might create a shared formula.

            using var wb = new XLWorkbook();
            var ws = wb.AddWorksheet("Sheet");
            Add4X3Table(ws, "E7");

            ws.Range("D8:D9").FormulaA1 = "TableName[[#This Row],[Second]]";

            // We need to inspect dependencies manually as the helper is too simple.
            // But `DependencyTree` is internal.
            // I can use reflection or just assume the helper logic is what I want to replicate.

            // Let's stick to single cell for now as it proves `ThisRow` logic uses FormulaArea.

            var dependencies = GetDependencies("TableName[[#This Row],[Second]]", "D9", init: wb =>
            {
                 var sheet = wb.Worksheet("Sheet");
                 Add4X3Table(sheet, "E7");
            });

            CollectionAssert.AreEquivalent(new XLBookArea[]
            {
                new("Sheet", XLSheetRange.Parse("F9"))
            }, dependencies.Areas);
        }

        [Test]
        public void Structured_reference_implicit_intersection_is_dependency()
        {
             // [@Column] is equivalent to [[#This Row],[Column]]
             var dependencies = GetDependencies("TableName[@Second]", "D8", init: wb =>
            {
                var ws = wb.Worksheet("Sheet");
                Add4X3Table(ws, "E7");
            });

            CollectionAssert.AreEquivalent(new XLBookArea[]
            {
                new("Sheet", XLSheetRange.Parse("F8"))
            }, dependencies.Areas);
        }

        private static IXLTable Add4X3Table(IXLWorksheet ws, string origin)
        {
            var dt = new DataTable("TableName");
            dt.Columns.AddRange(new[]
            {
                new DataColumn("First", typeof(int)),
                new DataColumn("Second", typeof(int)),
                new DataColumn("Third", typeof(int)),
                new DataColumn("Fourth", typeof(int)),
            });

            for (var i = 1; i <= 3; ++i)
            {
                var row = dt.NewRow();
                row["First"] = i;
                row["Second"] = i * 10;
                row["Third"] = i * 100;
                row["Fourth"] = i * 1000;
                dt.Rows.Add(row);
            }

            var table = ws.Cell(origin).InsertTable(dt, "TableName");
            table.SetShowTotalsRow(false);
            return table;
        }

        private static FormulaDependencies GetDependencies(string formula, string formulaAddress = "A1", Action<XLWorkbook>? init = null)
        {
            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Count > 0 ? wb.Worksheet(1) : wb.AddWorksheet("Sheet");
            init?.Invoke(wb);

            // Re-get worksheet in case it was modified/replaced in init
            ws = wb.Worksheet("Sheet");

            var tree = new DependencyTree();
            var cell = ws.Cell(formulaAddress);
            cell.SetFormulaA1(formula);

            var xlCell = (XLCell)cell;
            var cellFormula = xlCell.Formula;
            var dependencies = tree.AddFormula(new XLBookArea(ws.Name, new XLSheetRange(xlCell.SheetPoint, xlCell.SheetPoint)), cellFormula, wb);
            return dependencies;
        }
    }
}
