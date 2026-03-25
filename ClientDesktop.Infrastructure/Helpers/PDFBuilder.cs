using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;

namespace ClientDesktop.Infrastructure.Helpers
{
    /// <summary>
    /// Internal iText7 PDF builder helper.
    /// This class is ONLY used by PdfService — never referenced by ViewModels.
    /// All iText7 types stay inside this class and PdfService only.
    /// </summary>
    public class PDFBuilder
    {

        private readonly List<PdfComponent> _components = new();

        private static readonly HashSet<string> _skipColumns = new(StringComparer.OrdinalIgnoreCase)
        {
            "RowType", "IsHeader", "IsTotal", "Side", "SecurityName"
        };

        public Color HeaderBgColor { get; set; } = new DeviceRgb(41, 128, 185);
        public Color HeaderForeColor { get; set; } = ColorConstants.WHITE;
        public Color AltRowColor { get; set; } = new DeviceRgb(245, 248, 252);
        public Color SecurityHeaderBg { get; set; } = new DeviceRgb(239, 236, 200); // #EFECC8
        public Color TotalRowBg { get; set; } = new DeviceRgb(240, 240, 240);
        public Color GrandTotalRowBg { get; set; } = new DeviceRgb(220, 220, 220);

        /// <summary>Font size for normal data rows. Default: 9f (same as before).</summary>
        public float CellFontSize { get; set; } = 9f;

        /// <summary>Font size for header row. Default: 10f (same as before).</summary>
        public float HeaderFontSize { get; set; } = 10f;

        /// <summary>Padding for header cells. Default: 6f (same as before).</summary>
        public float HeaderPadding { get; set; } = 6f;

        /// <summary>Padding for data cells. Default: 5f (same as before).</summary>
        public float CellPadding { get; set; } = 5f;

        /// <summary>
        /// Show vertical (left/right) grid lines on all cells including headers.
        /// Default: false — existing callers see zero change.
        /// </summary>
        public bool ShowVerticalBorders { get; set; } = false;

        /// <summary>
        /// Optional per-column relative widths. Key = column name, Value = relative factor.
        /// Columns not listed get weight 1f. Default: empty = all columns equal (same as before).
        /// Example: new() { ["Sr"] = 0.4f, ["Time"] = 1.6f }
        /// </summary>
        public Dictionary<string, float> ColumnWidths { get; set; } = new();

        public event Action<string>? OnPdfSaved;
        public event Action<string>? OnError;

        public PDFBuilder AddTitle(string title, int fontSize = 18, bool centerAlign = true)
        {
            _components.Add(new PdfComponent
            {
                Type = ComponentType.Title,
                Text = title,
                FontSize = fontSize,
                CenterAlign = centerAlign
            });
            return this;
        }

        public PDFBuilder AddSubTitle(string subTitle, int fontSize = 13, bool centerAlign = false)
        {
            _components.Add(new PdfComponent
            {
                Type = ComponentType.SubTitle,
                Text = subTitle,
                FontSize = fontSize,
                CenterAlign = centerAlign
            });
            return this;
        }

        /// <summary>
        /// [FIX 1] columnAlignments accepts iText7 TextAlignment directly.
        /// PdfService has already converted PdfColumnAlignment → TextAlignment before calling this.
        /// </summary>
        public PDFBuilder AddGrid(
            DataTable dataTable,
            string? gridTitle = null,
            Dictionary<string, string>? footerData = null,
            Dictionary<string, TextAlignment>? columnAlignments = null)   // ← TextAlignment, NOT PdfColumnAlignment
        {
            _components.Add(new PdfComponent
            {
                Type = ComponentType.Grid,
                Text = gridTitle,
                DataTable = dataTable,
                FooterData = footerData,
                ColumnAlignments = columnAlignments
            });
            return this;
        }

        public PDFBuilder AddInfoSection(Dictionary<string, string> data, int columns = 2)
        {
            _components.Add(new PdfComponent
            {
                Type = ComponentType.InfoSection,
                FooterData = data,
                Columns = columns
            });
            return this;
        }

        public PDFBuilder AddSpacing(float spacing = 10)
        {
            _components.Add(new PdfComponent
            {
                Type = ComponentType.Spacing,
                Spacing = spacing
            });
            return this;
        }

        public PDFBuilder AddFooterNote(string note)
        {
            _components.Add(new PdfComponent
            {
                Type = ComponentType.FooterNote,
                Text = note
            });
            return this;
        }

        public PDFBuilder Clear()
        {
            _components.Clear();
            return this;
        }

        /// <summary>
        /// Opens WPF SaveFileDialog and writes PDF to disk.
        /// Fires OnPdfSaved or OnError — no MessageBox inside.
        /// </summary>
        public void BuildPDF(string baseFileName, bool landscape = true, bool autoFormat = true)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "PDF Files (*.pdf)|*.pdf",
                FileName = $"{baseFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    byte[] bytes = GeneratePdfBytes(landscape, autoFormat);
                    File.WriteAllBytes(dialog.FileName, bytes);
                    OnPdfSaved?.Invoke(dialog.FileName);
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex.Message);
                }
            }
        }

        /// <summary>
        /// Returns raw PDF bytes.
        ///
        /// [FIX 2] Do NOT use 'using' on PdfWriter / PdfDocument / Document.
        ///   iText7's doc.Close() already cascades Close() to pdfDoc → writer.
        ///   Adding 'using' causes a SECOND Dispose() → Unknown PdfException.
        ///
        ///   writer.SetCloseStream(false) prevents iText7 from closing MemoryStream,
        ///   so ms.ToArray() is safe after doc.Close().
        /// </summary>
        public byte[] GeneratePdfBytes(bool landscape = true, bool autoFormat = true)
        {
            var ms = new MemoryStream();

            // [FIX 2a] SetCloseStream(false) — iText7 will NOT close 'ms'
            var writer = new PdfWriter(ms);
            writer.SetCloseStream(false);

            // [FIX 2b] No 'using' — doc.Close() handles full cascade
            var pdfDoc = new PdfDocument(writer);
            PageSize size = landscape ? PageSize.A4.Rotate() : PageSize.A4;
            var doc = new Document(pdfDoc, size);
            doc.SetMargins(30, 20, 30, 20); // top, right, bottom, left

            foreach (var component in _components)
                ProcessComponent(doc, component, autoFormat);

            // doc.Close() → pdfDoc.Close() → writer.Close() (ms stays open)
            doc.Close();

            byte[] result = ms.ToArray();
            ms.Dispose();
            return result;
        }


        private void ProcessComponent(Document doc, PdfComponent comp, bool autoFormat)
        {
            switch (comp.Type)
            {
                case ComponentType.Title: RenderTitle(doc, comp); break;
                case ComponentType.SubTitle: RenderSubTitle(doc, comp); break;
                case ComponentType.Grid: RenderGrid(doc, comp, autoFormat); break;
                case ComponentType.InfoSection: RenderInfoSection(doc, comp); break;
                case ComponentType.FooterNote: RenderFooterNote(doc, comp); break;
                case ComponentType.Spacing:
                    doc.Add(new Paragraph(" ").SetMarginTop(comp.Spacing));
                    break;
            }
        }

        private static void RenderTitle(Document doc, PdfComponent comp)
        {
            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            TextAlignment align = comp.CenterAlign ? TextAlignment.CENTER : TextAlignment.LEFT;

            doc.Add(new Paragraph(comp.Text!.ToUpper())
                .SetFont(font)
                .SetFontSize(comp.FontSize)
                .SetFontColor(ColorConstants.BLACK)
                .SetTextAlignment(align)
                .SetPaddingTop(8)
                .SetPaddingBottom(8)
                .SetMarginBottom(4));
        }

        private static void RenderSubTitle(Document doc, PdfComponent comp)
        {
            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            TextAlignment align = comp.CenterAlign ? TextAlignment.CENTER : TextAlignment.LEFT;

            doc.Add(new Paragraph(comp.Text!)
                .SetFont(font)
                .SetFontSize(comp.FontSize)
                .SetFontColor(ColorConstants.DARK_GRAY)
                .SetTextAlignment(align)
                .SetMarginBottom(6));
        }

        private void RenderGrid(Document doc, PdfComponent comp, bool autoFormat)
        {
            if (!string.IsNullOrWhiteSpace(comp.Text))
            {
                PdfFont titleFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                doc.Add(new Paragraph(comp.Text)
                    .SetFont(titleFont)
                    .SetFontSize(13)
                    .SetMarginTop(6)
                    .SetMarginBottom(8));
            }

            if (comp.DataTable is { Columns.Count: > 0 })
            {
                Table table = BuildPdfTable(comp.DataTable, autoFormat, comp.ColumnAlignments);
                doc.Add(table);
            }

            if (comp.FooterData?.Count > 0)
                RenderKeyValuePairs(doc, comp.FooterData, 2, marginTop: 4);
        }

        private static void RenderInfoSection(Document doc, PdfComponent comp)
        {
            if (comp.FooterData?.Count > 0)
                RenderKeyValuePairs(doc, comp.FooterData, comp.Columns, marginTop: 0);
        }

        private static void RenderFooterNote(Document doc, PdfComponent comp)
        {
            PdfFont font = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_OBLIQUE);
            doc.Add(new Paragraph(comp.Text!)
                .SetFont(font)
                .SetFontSize(9)
                .SetFontColor(ColorConstants.GRAY)
                .SetTextAlignment(TextAlignment.CENTER)
                .SetMarginTop(20));
        }


        private Table BuildPdfTable(
            DataTable dt,
            bool autoFormat,
            Dictionary<string, TextAlignment>? columnAlignments)
        {
            // [FIX 3] Filter metadata columns — only display columns go to PDF
            var displayColumns = dt.Columns
                .Cast<DataColumn>()
                .Where(c => !_skipColumns.Contains(c.ColumnName))
                .ToList();

            if (displayColumns.Count == 0) return new Table(1);

            int colCount = displayColumns.Count;

            // Use ColumnWidths override if provided, else 1f each (default — same behavior as before)
            float[] widthArray = displayColumns
                .Select(c => ColumnWidths.TryGetValue(c.ColumnName, out float w) ? w : 1f)
                .ToArray();

            var table = new Table(UnitValue.CreatePercentArray(widthArray))
                        .UseAllAvailableWidth()
                        .SetMarginBottom(10);

            PdfFont headerFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont cellFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
            PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // ── Header row ──────────────────────────────────────────────────
            foreach (var col in displayColumns)
            {
                TextAlignment align = ResolveAlignment(col, columnAlignments, autoFormat);

                // PREVIOUS CODE:
                // table.AddHeaderCell( 
                // EXPLANATION: 'AddHeaderCell' was used here because it marks the cell as a table header in iText7. 
                // This built-in feature forces the library to automatically repeat this entire row at the top of every new page.

                // NEW CODE:
                table.AddCell( // Using 'AddCell' treats this as a normal first row, so it only prints once at the very beginning and doesn't repeat on new pages.
                    new Cell()
                        .Add(new Paragraph(col.ColumnName)
                            .SetFont(headerFont)
                            .SetFontSize(HeaderFontSize))
                        .SetBackgroundColor(HeaderBgColor)
                        .SetFontColor(HeaderForeColor)
                        .SetPadding(HeaderPadding)
                        .SetTextAlignment(align)
                        .SetBorder(Border.NO_BORDER)
                        .SetBorderBottom(ShowVerticalBorders ? new SolidBorder(ColorConstants.BLACK, 0.5f) : Border.NO_BORDER)
                        .SetBorderRight(ShowVerticalBorders ? new SolidBorder(ColorConstants.BLACK, 0.5f) : Border.NO_BORDER)
                        .SetBorderLeft(ShowVerticalBorders ? new SolidBorder(ColorConstants.BLACK, 0.5f) : Border.NO_BORDER));
            }

            // ── Data rows with RowType-based styling ────────────────────────
            bool hasRowType = dt.Columns.Contains("RowType");
            bool isAlternateRow = false;

            foreach (DataRow row in dt.Rows)
            {
                // [FIX 4] Determine row style
                string rowType = hasRowType ? row["RowType"]?.ToString() ?? "" : "";
                Color rowBg;
                bool isBold = false;
                float fontSize = CellFontSize;   // property — default 9f (same as before)

                switch (rowType)
                {
                    case "SecurityHeader":
                        rowBg = SecurityHeaderBg;
                        isBold = true;
                        break;

                    case "Total":
                    case "SecurityTotal":
                        rowBg = TotalRowBg;
                        isBold = true;
                        break;

                    case "GrandTotal":
                        rowBg = GrandTotalRowBg;
                        isBold = true;
                        fontSize = CellFontSize + 1f;
                        break;

                    case "SubTotal":
                        rowBg = TotalRowBg;
                        isBold = false;
                        break;

                    default:
                        rowBg = isAlternateRow ? AltRowColor : ColorConstants.WHITE;
                        isAlternateRow = !isAlternateRow;
                        break;
                }

                PdfFont rowFont = isBold ? boldFont : cellFont;

                foreach (var col in displayColumns)
                {
                    string cellValue = row[col.ColumnName]?.ToString() ?? string.Empty;
                    TextAlignment align = ResolveAlignment(col, columnAlignments, autoFormat);

                    table.AddCell(
                        new Cell()
                            .Add(new Paragraph(cellValue)
                                .SetFont(rowFont)
                                .SetFontSize(fontSize))
                            .SetBackgroundColor(rowBg)
                            .SetPadding(CellPadding)
                            .SetTextAlignment(align)
                            .SetBorder(Border.NO_BORDER)
                            .SetBorderBottom(new SolidBorder(ColorConstants.BLACK, 0.5f))
                            .SetBorderRight(ShowVerticalBorders ? new SolidBorder(ColorConstants.BLACK, 0.5f) : Border.NO_BORDER)
                            .SetBorderLeft(ShowVerticalBorders ? new SolidBorder(ColorConstants.BLACK, 0.5f) : Border.NO_BORDER));
                }
            }

            return table;
        }

        private static void RenderKeyValuePairs(
            Document doc,
            Dictionary<string, string> data,
            int pairColumns,
            float marginTop)
        {
            int totalCols = pairColumns * 2;
            float[] widths = Enumerable.Range(0, totalCols)
                                       .Select(i => i % 2 == 0 ? 1f : 2f)
                                       .ToArray();

            var table = new Table(UnitValue.CreatePercentArray(widths))
                        .UseAllAvailableWidth()
                        .SetMarginTop(marginTop)
                        .SetMarginBottom(8);

            PdfFont labelFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            PdfFont valueFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            int pairIndex = 0;
            foreach (var kvp in data)
            {
                table.AddCell(
                    new Cell()
                        .Add(new Paragraph(kvp.Key)
                            .SetFont(labelFont)
                            .SetFontSize(10))
                        .SetBorder(Border.NO_BORDER)
                        .SetPaddingRight(4));

                table.AddCell(
                    new Cell()
                        .Add(new Paragraph(kvp.Value ?? "")
                            .SetFont(valueFont)
                            .SetFontSize(10))
                        .SetBorder(Border.NO_BORDER)
                        .SetPaddingRight(16));

                pairIndex++;

                if (pairIndex == data.Count)
                {
                    int filled = (pairIndex % pairColumns) * 2;
                    int empties = totalCols - filled;
                    if (filled > 0 && empties > 0)
                        for (int i = 0; i < empties; i++)
                            table.AddCell(new Cell().SetBorder(Border.NO_BORDER));
                }
            }

            doc.Add(table);
        }

        private static TextAlignment ResolveAlignment(
            DataColumn col,
            Dictionary<string, TextAlignment>? overrides,
            bool autoFormat)
        {
            if (overrides != null && overrides.TryGetValue(col.ColumnName, out var forced))
                return forced;

            if (!autoFormat) return TextAlignment.LEFT;

            Type t = col.DataType;
            if (t == typeof(decimal) || t == typeof(double) ||
                t == typeof(float) || t == typeof(int) ||
                t == typeof(long) || t == typeof(short))
                return TextAlignment.RIGHT;

            if (t == typeof(DateTime))
                return TextAlignment.CENTER;

            return TextAlignment.LEFT;
        }
    }

    public enum ComponentType
    {
        Title,
        SubTitle,
        Grid,
        InfoSection,
        Spacing,
        FooterNote
    }

    internal class PdfComponent
    {
        public ComponentType Type { get; set; }
        public string? Text { get; set; }
        public int FontSize { get; set; } = 12;
        public float Spacing { get; set; } = 10;
        public bool CenterAlign { get; set; } = true;
        public DataTable? DataTable { get; set; }
        public Dictionary<string, string>? FooterData { get; set; }
        public Dictionary<string, TextAlignment>? ColumnAlignments { get; set; }
        public int Columns { get; set; } = 2;
    }
}