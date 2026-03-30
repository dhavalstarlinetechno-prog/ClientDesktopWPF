using ClosedXML.Excel;
using ClientDesktop.Core.Enums;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace ClientDesktop.Infrastructure.Helpers
{
    public class ExcelBuilder
    {      
        private readonly List<SheetData> _sheets = new();
        
        public event Action<string>? OnExcelSaved;
        public event Action<string>? OnError;

        public ExcelBuilder AddSheet(
            DataTable dataTable,
            string title,
            string sheetName = "Sheet1",
            Dictionary<string, EnumExcelColumnAlignment>? columnAlignments = null)
        {
            _sheets.Add(new SheetData
            {
                Table = dataTable,
                Title = title,
                SheetName = sheetName,
                ColumnAlignments = columnAlignments
            });
            return this;
        }
       
        public ExcelBuilder Clear()
        {
            _sheets.Clear();
            return this;
        }
       
        public void SaveExcel(string baseFileName)
        {          
            string downloadsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");

            if (!Directory.Exists(downloadsFolder))
                Directory.CreateDirectory(downloadsFolder);

            string defaultName = GetUniqueFileName(downloadsFolder, baseFileName);

            var dialog = new SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                Title = "Save Excel File",
                InitialDirectory = downloadsFolder,
                FileName = defaultName
            };

            if (dialog.ShowDialog() != true) return;

            try
            {
                byte[] bytes = GenerateExcelBytes();
                File.WriteAllBytes(dialog.FileName, bytes);
                OnExcelSaved?.Invoke(dialog.FileName);
            }
            catch (Exception ex)
            {
                OnError?.Invoke(ex.Message);
            }
        }

        public byte[] GenerateExcelBytes()
        {
            using var workbook = new XLWorkbook();

            // Track used sheet names to avoid duplicates
            var usedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var sheetData in _sheets)
            {
                string safeName = GetUniqueSheetName(sheetData.SheetName, usedNames);
                usedNames.Add(safeName);

                var sheet = workbook.Worksheets.Add(safeName);
                WriteSheet(sheet, sheetData);
            }

            using var ms = new MemoryStream();
            workbook.SaveAs(ms);
            return ms.ToArray();
        }


        private static void WriteSheet(IXLWorksheet sheet, SheetData data)
        {
            DataTable dt = data.Table;
            int colCount = dt.Columns.Count;
            int rowIndex = 1;

            // ── Title row ───────────────────────────────────────────────────
            if (!string.IsNullOrWhiteSpace(data.Title))
            {
                var titleRange = sheet.Range(rowIndex, 1, rowIndex, colCount);
                titleRange.Merge();
                titleRange.FirstCell().Value = data.Title;
                titleRange.Style.Font.Bold = true;
                titleRange.Style.Font.FontSize = 14;
                titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                sheet.Row(rowIndex).Height = 20;
                rowIndex += 2;   // blank row after title
            }

            // ── Column headers ──────────────────────────────────────────────
            for (int c = 0; c < colCount; c++)
            {
                string colName = dt.Columns[c].ColumnName;
                var cell = sheet.Cell(rowIndex, c + 1);

                cell.Value = colName;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = ResolveAlignment(colName, dt.Columns[c].DataType,
                                                                   data.ColumnAlignments, autoDetect: true);
            }
            rowIndex++;

            // ── Data rows ───────────────────────────────────────────────────
            for (int r = 0; r < dt.Rows.Count; r++)
            {
                DataRow dr = dt.Rows[r];
                bool isAlt = r % 2 != 0;   // zebra striping

                for (int c = 0; c < colCount; c++)
                {
                    string colName = dt.Columns[c].ColumnName;
                    var cell = sheet.Cell(rowIndex + r, c + 1);
                    object cellValue = dr[c];

                    // Set value with proper type (numbers stay numeric in Excel)
                    SetCellValue(cell, cellValue, dt.Columns[c].DataType);

                    cell.Style.Alignment.Horizontal = ResolveAlignment(colName, dt.Columns[c].DataType,
                                                                        data.ColumnAlignments, autoDetect: true);

                    if (isAlt)
                        cell.Style.Fill.BackgroundColor = XLColor.FromArgb(245, 248, 252);
                }
            }

            // ── Auto-fit columns ────────────────────────────────────────────
            sheet.Columns().AdjustToContents();
        }


     
        private static void SetCellValue(IXLCell cell, object value, Type colType)
        {
            if (value == null || value == DBNull.Value)
            {
                cell.Value = string.Empty;
                return;
            }

            // Store numeric types as numbers so Excel can SUM them
            if (colType == typeof(decimal) || colType == typeof(double) ||
                colType == typeof(float) || colType == typeof(int) ||
                colType == typeof(long) || colType == typeof(short))
            {
                if (double.TryParse(value.ToString(), out double d))
                    cell.Value = d;
                else
                    cell.Value = value.ToString();
                return;
            }

            if (colType == typeof(DateTime) && value is DateTime dt)
            {
                cell.Value = dt;
                cell.Style.DateFormat.Format = "dd/MM/yyyy HH:mm:ss";
                return;
            }

            cell.Value = value.ToString();
        }

      
        private static XLAlignmentHorizontalValues ResolveAlignment(
            string colName,
            Type colType,
            Dictionary<string, EnumExcelColumnAlignment>? overrides,
            bool autoDetect)
        {
            // Explicit override from caller
            if (overrides != null && overrides.TryGetValue(colName, out var forced))
                return ToXlAlign(forced);

            if (!autoDetect) return XLAlignmentHorizontalValues.Left;

            // Auto-detect by DataType
            if (colType == typeof(decimal) || colType == typeof(double) ||
                colType == typeof(float) || colType == typeof(int) ||
                colType == typeof(long) || colType == typeof(short))
                return XLAlignmentHorizontalValues.Right;

            if (colType == typeof(DateTime))
                return XLAlignmentHorizontalValues.Center;

            return XLAlignmentHorizontalValues.Left;
        }

        private static XLAlignmentHorizontalValues ToXlAlign(EnumExcelColumnAlignment align) =>
            align switch
            {
                EnumExcelColumnAlignment.Right => XLAlignmentHorizontalValues.Right,
                EnumExcelColumnAlignment.Center => XLAlignmentHorizontalValues.Center,
                _ => XLAlignmentHorizontalValues.Left
            };

        /// <summary>Returns filename with auto-increment if already exists.</summary>
        private static string GetUniqueFileName(string folder, string baseName)
        {
            string path = Path.Combine(folder, baseName + ".xlsx");
            int counter = 1;
            while (File.Exists(path))
            {
                path = Path.Combine(folder, $"{baseName} ({counter++}).xlsx");
            }
            return Path.GetFileName(path);
        }

        /// <summary>Excel sheet names must be unique and max 31 chars.</summary>
        private static string GetUniqueSheetName(string name, HashSet<string> used)
        {
            // Excel max sheet name = 31 chars
            string safe = name.Length > 31 ? name[..31] : name;
            if (!used.Contains(safe)) return safe;

            int i = 1;
            string candidate;
            do { candidate = $"{safe[..Math.Min(safe.Length, 28)]}_{i++}"; }
            while (used.Contains(candidate));
            return candidate;
        }
    }


    internal class SheetData
    {
        public DataTable Table { get; set; }
        public string Title { get; set; }
        public string SheetName { get; set; } = "Sheet1";
        public Dictionary<string, EnumExcelColumnAlignment>? ColumnAlignments { get; set; }
    }
}