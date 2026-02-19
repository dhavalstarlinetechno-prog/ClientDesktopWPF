using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Models;
using ClosedXML.Excel;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.draw;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;

namespace ClientDesktop.ViewModel
{

    public class HistoryViewModel
    {
        public ObservableCollection<HistoryModel> HistoryItems { get; }
    = new ObservableCollection<HistoryModel>();

        public ObservableCollection<PositionHistoryModel> PositionHistoryItems { get; }
            = new ObservableCollection<PositionHistoryModel>();

        public bool IsLoading { get; private set; }

        private readonly SessionService _sessionService;
        private readonly HistoryService _historyService;
        public Action OnHistoryDataLoaded { get; set; }
        public HistoryViewModel(SessionService sessionService, HistoryService historyService)
        {
            _sessionService = sessionService;
            _historyService = historyService;

            _sessionService.OnLoginSuccess += HandleLogin;
        }

        private void HandleLogin()
        {
            LoadDataAsync();
        }

        public async Task LoadDataAsync()
        {
            if (IsLoading) return;

            IsLoading = true;

            try
            {
                var fromDate = new DateTime(1970, 1, 1);
                var toDate = DateTime.Now;

                var historyTask = _historyService.FetchHistoryFromApiAsync(fromDate, toDate);
                var positionTask = _historyService.FetchPositionHistoryFromApiAsync(fromDate, toDate);

                var historyResult = await historyTask;
                var positionResult = await positionTask;

                UpdateCollection(HistoryItems, historyResult.Data);
                UpdateCollection(PositionHistoryItems, positionResult.Data);

                if (historyResult.IsFromCache)
                {
                    FileLogger.Log("System", "History loaded from local cache.");
                }

                if (!historyResult.IsSuccess)
                {
                    FileLogger.Log("System" , "History sync failed.");
                }

                OnHistoryDataLoaded?.Invoke();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog("LoadDataAsync", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void UpdateCollection<T>(
    ObservableCollection<T> collection,
    List<T> data)
        {
            collection.Clear();

            if (data == null) return;

            foreach (var item in data)
                collection.Add(item);
        }

        public List<HistoryModel> GetHistoryData() => _historyService.GetStoredHistory();

        public List<PositionHistoryModel> GetPositionHistoryData() => _historyService.GetStoredPositionHistory();

        #region Export Data to excel and PDF
        public void ExportToExcel(List<HistoryModel> data, HistoryType gridType)
        {
            try
            {
                if (data == null || data.Count <= 1)
                {
                    MessageBox.Show("No data to export", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var footerRow = data.FirstOrDefault(x => x.RefId == "FOOTER");
                decimal totalProfit = footerRow?.Pnl ?? 0;
                decimal totalComm = footerRow?.UplineCommission ?? 0;

                decimal credit = 0;
                decimal balance = 0;
                if (footerRow?.Comment != null && footerRow.Comment.Contains("Credit:"))
                {
                    var parts = footerRow.Comment.Split(new[] { "Credit:", "Balance:" }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                        decimal.TryParse(parts[1].Trim().Split(' ')[0].Replace(",", ""), out credit);
                    if (parts.Length >= 3)
                        decimal.TryParse(parts[2].Replace("INR", "").Trim().Replace(",", ""), out balance);
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook|*.xlsx",
                    Title = "Export to Excel",
                    FileName = $"{gridType}_History_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                using (var workbook = new XLWorkbook())
                {
                    var sheet = workbook.Worksheets.Add("History");
                    int rowIndex = 1;

                    var titleCell = sheet.Cell(rowIndex, 1);
                    titleCell.Value = $"{gridType} History";
                    var titleRange = sheet.Range(rowIndex, 1, rowIndex, 13);
                    titleRange.Merge();
                    titleRange.Style.Font.Bold = true;
                    titleRange.Style.Font.FontSize = 14;
                    titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rowIndex += 2;

                    string[] headers = { "Sr", "Time", "Deal Id", "Position Id", "Symbol", "Execution", "Type", "Entry", "Volume", "Price", "Comm.", "Profit", "Comment" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var header = sheet.Cell(rowIndex, i + 1);
                        header.Value = headers[i];
                        header.Style.Font.Bold = true;
                        header.Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                    rowIndex++;

                    int sr = 1;
                    foreach (var item in data.Where(x => x.RefId != "FOOTER"))
                    {
                        sheet.Cell(rowIndex, 1).Value = sr++;

                        // ✅ FIX: DateTime object with format
                        var dateCell = sheet.Cell(rowIndex, 2);
                        dateCell.Value = item.CreatedOn;
                        dateCell.Style.DateFormat.Format = "dd/MM/yy HH:mm";

                        sheet.Cell(rowIndex, 3).Value = item.RefId;
                        sheet.Cell(rowIndex, 4).Value = item.PositionId ?? "--";
                        sheet.Cell(rowIndex, 5).Value = item.SymbolName;
                        sheet.Cell(rowIndex, 6).Value = item.OrderType;
                        sheet.Cell(rowIndex, 7).Value = item.Side;
                        sheet.Cell(rowIndex, 8).Value = item.DealType;
                        sheet.Cell(rowIndex, 9).Value = item.Volume;
                        sheet.Cell(rowIndex, 10).Value = item.Price;
                        sheet.Cell(rowIndex, 11).Value = item.UplineCommission;
                        sheet.Cell(rowIndex, 12).Value = item.Pnl;
                        sheet.Cell(rowIndex, 13).Value = item.Comment ?? "";
                        rowIndex++;
                    }

                    sheet.Cell(rowIndex, 1).Value = "Profit:";
                    sheet.Cell(rowIndex, 2).Value = totalProfit.ToString("N2");
                    sheet.Cell(rowIndex, 3).Value = "Credit:";
                    sheet.Cell(rowIndex, 4).Value = credit.ToString("N2");
                    sheet.Cell(rowIndex, 5).Value = "Balance:";
                    sheet.Cell(rowIndex, 6).Value = balance.ToString("N3") + "INR";
                    sheet.Cell(rowIndex, 11).Value = totalComm;
                    sheet.Cell(rowIndex, 12).Value = totalProfit;
                    sheet.Cell(rowIndex, 13).Value = "";

                    var footerRange = sheet.Range(rowIndex, 1, rowIndex, 13);
                    footerRange.Style.Font.Bold = true;
                    footerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    sheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveDialog.FileName);
                }

                MessageBox.Show("Excel exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportPositionToExcel(List<PositionHistoryModel> data)
        {
            try
            {
                if (data == null || data.Count <= 1)
                {
                    MessageBox.Show("No data to export", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var footerRow = data.FirstOrDefault(x => x.RefId == "FOOTER");
                decimal totalProfit = (decimal)(footerRow?.Pnl ?? 0);

                decimal credit = 0;
                decimal balance = 0;
                if (footerRow?.Comment != null && footerRow.Comment.Contains("Credit:"))
                {
                    var parts = footerRow.Comment.Split(new[] { "Credit:", "Balance:" }, StringSplitOptions.None);
                    if (parts.Length >= 2)
                        decimal.TryParse(parts[1].Trim().Split(' ')[0].Replace(",", ""), out credit);
                    if (parts.Length >= 3)
                        decimal.TryParse(parts[2].Replace("INR", "").Trim().Replace(",", ""), out balance);
                }

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook|*.xlsx",
                    Title = "Export to Excel",
                    FileName = $"Position_History_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                using (var workbook = new XLWorkbook())
                {
                    var sheet = workbook.Worksheets.Add("Position");
                    int rowIndex = 1;

                    var titleCell = sheet.Cell(rowIndex, 1);
                    titleCell.Value = "Position History";
                    var titleRange = sheet.Range(rowIndex, 1, rowIndex, 11);
                    titleRange.Merge();
                    titleRange.Style.Font.Bold = true;
                    titleRange.Style.Font.FontSize = 14;
                    titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    rowIndex += 2;

                    string[] headers = { "Sr", "Time", "Last Out Time", "Position Id", "Type", "Volume", "Symbol", "Price", "Comm.", "Profit", "Comment" };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        var header = sheet.Cell(rowIndex, i + 1);
                        header.Value = headers[i];
                        header.Style.Font.Bold = true;
                        header.Style.Fill.BackgroundColor = XLColor.LightGray;
                    }
                    rowIndex++;

                    int sr = 1;
                    foreach (var item in data.Where(x => x.RefId != "FOOTER"))
                    {
                        sheet.Cell(rowIndex, 1).Value = sr++;

                        // ✅ FIX: UpdatedAt with DateTime format
                        var updateCell = sheet.Cell(rowIndex, 2);
                        updateCell.Value = item.UpdatedAt;
                        updateCell.Style.DateFormat.Format = "dd/MM/yy HH:mm";

                        // ✅ FIX: LastOutAt with DateTime format (handle null)
                        var lastOutCell = sheet.Cell(rowIndex, 3);
                        if (item.LastOutAt.HasValue)
                        {
                            lastOutCell.Value = item.LastOutAt.Value;
                            lastOutCell.Style.DateFormat.Format = "dd/MM/yy HH:mm";
                        }
                        else
                        {
                            lastOutCell.Value = "--";
                        }

                        sheet.Cell(rowIndex, 4).Value = item.RefId;
                        sheet.Cell(rowIndex, 5).Value = item.Side;
                        sheet.Cell(rowIndex, 6).Value = item.TotalVolume;
                        sheet.Cell(rowIndex, 7).Value = item.SymbolName;
                        sheet.Cell(rowIndex, 8).Value = item.AveragePrice;
                        sheet.Cell(rowIndex, 9).Value = "";
                        sheet.Cell(rowIndex, 10).Value = item.Pnl;
                        sheet.Cell(rowIndex, 11).Value = item.Comment ?? "";
                        rowIndex++;
                    }

                    sheet.Cell(rowIndex, 1).Value = "Profit:";
                    sheet.Cell(rowIndex, 2).Value = totalProfit.ToString("N2");
                    sheet.Cell(rowIndex, 3).Value = "Credit:";
                    sheet.Cell(rowIndex, 4).Value = credit.ToString("N2");
                    sheet.Cell(rowIndex, 5).Value = "Balance:";
                    sheet.Cell(rowIndex, 6).Value = balance.ToString("N3") + "INR";
                    sheet.Cell(rowIndex, 9).Value = "";
                    sheet.Cell(rowIndex, 10).Value = totalProfit;
                    sheet.Cell(rowIndex, 11).Value = "";

                    var footerRange = sheet.Range(rowIndex, 1, rowIndex, 11);
                    footerRange.Style.Font.Bold = true;
                    footerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    sheet.Columns().AdjustToContents();
                    workbook.SaveAs(saveDialog.FileName);
                }

                MessageBox.Show("Excel exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ExportToPdf(List<HistoryModel> data, HistoryType gridType)
        {
            try
            {
                if (data == null || data.Count <= 1)
                {
                    MessageBox.Show("No data to export", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var exportData = data.Where(x => x.RefId != "FOOTER").ToList();
                var footerRow = data.FirstOrDefault(x => x.RefId == "FOOTER");

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "PDF Files|*.pdf",
                    Title = "Export to PDF",
                    FileName = $"{gridType}_History_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                // ✅ Landscape for better table view
                Document document = new Document(PageSize.A4.Rotate(), 30, 30, 40, 40);
                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(saveDialog.FileName, FileMode.Create));
                document.Open();

                // ============ HEADER ============
                // Company/App Name
                Font companyFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, new BaseColor(0, 51, 102)); // Dark Blue
                //Paragraph company = new Paragraph("TRADING PLATFORM", companyFont)
                //{
                //    Alignment = Element.ALIGN_CENTER,
                //    SpacingAfter = 5
                //};
                //document.Add(company);

                // Report Title
                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLACK);
                Paragraph title = new Paragraph($"{gridType} History Report", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 3
                };
                document.Add(title);

                // Date Range / Generated Date
                Font dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.DARK_GRAY);
                Paragraph dateInfo = new Paragraph($"Generated on: {DateTime.Now:dd MMM yyyy HH:mm:ss}", dateFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(dateInfo);

                // Separator Line
                LineSeparator line = new LineSeparator(1f, 100f, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, -2);
                document.Add(new Chunk(line));
                document.Add(new Paragraph(" ") { SpacingAfter = 10 });

                // ============ DATA TABLE ============
                PdfPTable table = new PdfPTable(12)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 5,
                    SpacingAfter = 15
                };

                // Column widths (adjust based on content)
                float[] columnWidths = { 4f, 12f, 10f, 10f, 10f, 9f, 7f, 7f, 7f, 8f, 7f, 8f };
                table.SetWidths(columnWidths);

                // ✅ Table Header Row
                string[] headers = { "Sr.", "Time", "Deal ID", "Position ID", "Symbol", "Execution", "Type", "Entry", "Volume", "Price", "Comm.", "Profit" };
                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, BaseColor.WHITE);
                BaseColor headerBg = new BaseColor(41, 128, 185); // Blue background

                foreach (string header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, headerFont))
                    {
                        BackgroundColor = headerBg,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 6,
                        BorderWidth = 0.5f,
                        BorderColor = BaseColor.WHITE
                    };
                    table.AddCell(cell);
                }

                // ✅ Data Rows
                Font dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 7, BaseColor.BLACK);
                Font dataBoldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7, BaseColor.BLACK);
                BaseColor evenRowColor = new BaseColor(245, 245, 245); // Light gray
                BaseColor oddRowColor = BaseColor.WHITE;

                int srNo = 1;
                foreach (var item in exportData)
                {
                    BaseColor rowBg = (srNo % 2 == 0) ? evenRowColor : oddRowColor;

                    // Sr.
                    table.AddCell(CreateStyledCell(srNo.ToString(), dataFont, Element.ALIGN_CENTER, rowBg));

                    // Time
                    table.AddCell(CreateStyledCell(item.CreatedOn.ToString("dd/MM/yyyy HH:mm:ss"), dataFont, Element.ALIGN_LEFT, rowBg));

                    // Deal ID
                    table.AddCell(CreateStyledCell(item.RefId, dataFont, Element.ALIGN_LEFT, rowBg));

                    // Position ID
                    table.AddCell(CreateStyledCell(item.PositionId ?? "--", dataFont, Element.ALIGN_LEFT, rowBg));

                    // Symbol
                    table.AddCell(CreateStyledCell(item.SymbolName, dataBoldFont, Element.ALIGN_LEFT, rowBg));

                    // Execution
                    table.AddCell(CreateStyledCell(item.OrderType, dataFont, Element.ALIGN_CENTER, rowBg));

                    // Type (Buy/Sell with color)
                    BaseColor sideColor = item.Side.ToUpper() == "BUY" ? new BaseColor(34, 139, 34) : new BaseColor(220, 20, 60);
                    Font sideFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7, sideColor);
                    table.AddCell(CreateStyledCell(item.Side, sideFont, Element.ALIGN_CENTER, rowBg));

                    // Entry
                    table.AddCell(CreateStyledCell(item.DealType, dataFont, Element.ALIGN_CENTER, rowBg));

                    // Volume
                    table.AddCell(CreateStyledCell(item.Volume.ToString("N2"), dataFont, Element.ALIGN_RIGHT, rowBg));

                    // Price
                    table.AddCell(CreateStyledCell(item.Price.ToString("N2"), dataFont, Element.ALIGN_RIGHT, rowBg));

                    // Commission
                    table.AddCell(CreateStyledCell(item.UplineCommission.ToString("N2"), dataFont, Element.ALIGN_RIGHT, rowBg));

                    // Profit (with color)
                    BaseColor profitColor = item.Pnl >= 0 ? new BaseColor(34, 139, 34) : new BaseColor(220, 20, 60);
                    Font profitFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7, profitColor);
                    table.AddCell(CreateStyledCell(item.Pnl.ToString("N2"), profitFont, Element.ALIGN_RIGHT, rowBg));

                    srNo++;
                }

                document.Add(table);

                // ============ SUMMARY SECTION ============
                if (footerRow != null)
                {
                    decimal totalProfit = footerRow.Pnl;
                    decimal totalComm = footerRow.UplineCommission;

                    decimal credit = 0;
                    decimal balance = 0;

                    if (footerRow.Comment != null && footerRow.Comment.Contains("Credit:"))
                    {
                        var parts = footerRow.Comment.Split(new[] { "Credit:", "Balance:" }, StringSplitOptions.None);
                        if (parts.Length >= 2)
                            decimal.TryParse(parts[1].Trim().Split(' ')[0].Replace(",", ""), out credit);
                        if (parts.Length >= 3)
                            decimal.TryParse(parts[2].Replace("INR", "").Trim().Replace(",", ""), out balance);
                    }

                    // Summary Table
                    PdfPTable summaryTable = new PdfPTable(4)
                    {
                        WidthPercentage = 60,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        SpacingBefore = 10
                    };

                    float[] summaryWidths = { 1f, 1f, 1f, 1f };
                    summaryTable.SetWidths(summaryWidths);

                    Font summaryLabelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.BLACK);
                    Font summaryValueFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);

                    // Total Commission
                    summaryTable.AddCell(CreateSummaryCell("Total Commission:", summaryLabelFont, Element.ALIGN_RIGHT));
                    summaryTable.AddCell(CreateSummaryCell(totalComm.ToString("N2"), summaryValueFont, Element.ALIGN_RIGHT));

                    // Total Profit
                    BaseColor totalProfitColor = totalProfit >= 0 ? new BaseColor(34, 139, 34) : new BaseColor(220, 20, 60);
                    Font totalProfitFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, totalProfitColor);
                    summaryTable.AddCell(CreateSummaryCell("Total Profit:", summaryLabelFont, Element.ALIGN_RIGHT));
                    summaryTable.AddCell(CreateSummaryCell(totalProfit.ToString("N2"), totalProfitFont, Element.ALIGN_RIGHT));

                    // Credit
                    summaryTable.AddCell(CreateSummaryCell("Credit:", summaryLabelFont, Element.ALIGN_RIGHT));
                    summaryTable.AddCell(CreateSummaryCell(credit.ToString("N2"), summaryValueFont, Element.ALIGN_RIGHT));

                    // Balance
                    summaryTable.AddCell(CreateSummaryCell("Balance:", summaryLabelFont, Element.ALIGN_RIGHT));
                    summaryTable.AddCell(CreateSummaryCell(balance.ToString("N3") + " INR", summaryValueFont, Element.ALIGN_RIGHT));

                    document.Add(summaryTable);
                }

                // ============ FOOTER ============
                document.Add(new Paragraph(" ") { SpacingBefore = 20 });
                LineSeparator footerLine = new LineSeparator(0.5f, 100f, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, -2);
                document.Add(new Chunk(footerLine));

                Font footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 7, BaseColor.GRAY);
                Paragraph footer = new Paragraph($"Report generated | Page 1 of 1 | {DateTime.Now:dd MMM yyyy HH:mm:ss}", footerFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 5
                };
                document.Add(footer);

                document.Close();

                MessageBox.Show("PDF exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private PdfPCell CreateStyledCell(string text, Font font, int alignment, BaseColor bgColor)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = alignment,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 4,
                BackgroundColor = bgColor,
                BorderWidth = 0.3f,
                BorderColor = new BaseColor(200, 200, 200)
            };
            return cell;
        }

        private PdfPCell CreateSummaryCell(string text, Font font, int alignment)
        {
            PdfPCell cell = new PdfPCell(new Phrase(text, font))
            {
                HorizontalAlignment = alignment,
                VerticalAlignment = Element.ALIGN_MIDDLE,
                Padding = 5,
                Border = Rectangle.NO_BORDER
            };
            return cell;
        }

        public void ExportPositionToPdf(List<PositionHistoryModel> data)
        {
            try
            {
                if (data == null || data.Count <= 1)
                {
                    MessageBox.Show("No data to export", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var exportData = data.Where(x => x.RefId != "FOOTER").ToList();
                var footerRow = data.FirstOrDefault(x => x.RefId == "FOOTER");

                SaveFileDialog saveDialog = new SaveFileDialog
                {
                    Filter = "PDF Files|*.pdf",
                    Title = "Export to PDF",
                    FileName = $"Position_History_{DateTime.Now:yyyyMMdd_HHmmss}.pdf"
                };

                if (saveDialog.ShowDialog() != true)
                    return;

                Document document = new Document(PageSize.A4.Rotate(), 30, 30, 40, 40);
                PdfWriter writer = PdfWriter.GetInstance(document, new FileStream(saveDialog.FileName, FileMode.Create));
                document.Open();

                // Header
                Font companyFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 16, new BaseColor(0, 51, 102));
                Paragraph company = new Paragraph("TRADING PLATFORM", companyFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 5
                };
                document.Add(company);

                Font titleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLACK);
                Paragraph title = new Paragraph("Position History Report", titleFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 3
                };
                document.Add(title);

                Font dateFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.DARK_GRAY);
                Paragraph dateInfo = new Paragraph($"Generated on: {DateTime.Now:dd MMM yyyy HH:mm:ss}", dateFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingAfter = 20
                };
                document.Add(dateInfo);

                LineSeparator line = new LineSeparator(1f, 100f, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, -2);
                document.Add(new Chunk(line));
                document.Add(new Paragraph(" ") { SpacingAfter = 10 });

                // Table
                PdfPTable table = new PdfPTable(10)
                {
                    WidthPercentage = 100,
                    SpacingBefore = 5,
                    SpacingAfter = 15
                };

                float[] columnWidths = { 4f, 12f, 12f, 10f, 8f, 8f, 12f, 8f, 8f, 9f };
                table.SetWidths(columnWidths);

                // Headers
                string[] headers = { "Sr.", "Time", "Last Out Time", "Position ID", "Type", "Volume", "Symbol", "Price", "Comm.", "Profit" };
                Font headerFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 8, BaseColor.WHITE);
                BaseColor headerBg = new BaseColor(41, 128, 185);

                foreach (string header in headers)
                {
                    PdfPCell cell = new PdfPCell(new Phrase(header, headerFont))
                    {
                        BackgroundColor = headerBg,
                        HorizontalAlignment = Element.ALIGN_CENTER,
                        VerticalAlignment = Element.ALIGN_MIDDLE,
                        Padding = 6,
                        BorderWidth = 0.5f,
                        BorderColor = BaseColor.WHITE
                    };
                    table.AddCell(cell);
                }

                // Data
                Font dataFont = FontFactory.GetFont(FontFactory.HELVETICA, 7, BaseColor.BLACK);
                Font dataBoldFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7, BaseColor.BLACK);
                BaseColor evenRowColor = new BaseColor(245, 245, 245);
                BaseColor oddRowColor = BaseColor.WHITE;

                int srNo = 1;
                foreach (var item in exportData)
                {
                    BaseColor rowBg = (srNo % 2 == 0) ? evenRowColor : oddRowColor;

                    table.AddCell(CreateStyledCell(srNo.ToString(), dataFont, Element.ALIGN_CENTER, rowBg));
                    table.AddCell(CreateStyledCell(item.UpdatedAt.ToString("dd/MM/yyyy HH:mm:ss"), dataFont, Element.ALIGN_LEFT, rowBg));
                    table.AddCell(CreateStyledCell(item.LastOutAt?.ToString("dd/MM/yyyy HH:mm:ss") ?? "--", dataFont, Element.ALIGN_LEFT, rowBg));
                    table.AddCell(CreateStyledCell(item.RefId, dataFont, Element.ALIGN_LEFT, rowBg));

                    BaseColor sideColor = item.Side.ToUpper() == "BUY" ? new BaseColor(34, 139, 34) : new BaseColor(220, 20, 60);
                    Font sideFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7, sideColor);
                    table.AddCell(CreateStyledCell(item.Side, sideFont, Element.ALIGN_CENTER, rowBg));

                    table.AddCell(CreateStyledCell(item.TotalVolume.ToString("N2"), dataFont, Element.ALIGN_RIGHT, rowBg));
                    table.AddCell(CreateStyledCell(item.SymbolName, dataBoldFont, Element.ALIGN_LEFT, rowBg));
                    table.AddCell(CreateStyledCell(item.AveragePrice.ToString("N2"), dataFont, Element.ALIGN_RIGHT, rowBg));
                    table.AddCell(CreateStyledCell(item.CurrentPrice.ToString("N2"), dataFont, Element.ALIGN_RIGHT, rowBg));

                    BaseColor profitColor = item.Pnl >= 0 ? new BaseColor(34, 139, 34) : new BaseColor(220, 20, 60);
                    Font profitFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 7, profitColor);
                    table.AddCell(CreateStyledCell(item.Pnl.ToString("N2"), profitFont, Element.ALIGN_RIGHT, rowBg));

                    srNo++;
                }

                document.Add(table);

                // Summary
                if (footerRow != null)
                {
                    decimal totalProfit = (decimal)footerRow.Pnl;
                    decimal totalFee = (decimal)footerRow.Pnl; 

                    decimal credit = 0;
                    decimal balance = 0;

                    if (footerRow.Comment != null && footerRow.Comment.Contains("Credit:"))
                    {
                        var parts = footerRow.Comment.Split(new[] { "Credit:", "Balance:" }, StringSplitOptions.None);
                        if (parts.Length >= 2)
                            decimal.TryParse(parts[1].Trim().Split(' ')[0].Replace(",", ""), out credit);
                        if (parts.Length >= 3)
                            decimal.TryParse(parts[2].Replace("INR", "").Trim().Replace(",", ""), out balance);
                    }

                    PdfPTable summaryTable = new PdfPTable(4)
                    {
                        WidthPercentage = 60,
                        HorizontalAlignment = Element.ALIGN_RIGHT,
                        SpacingBefore = 10
                    };

                    float[] summaryWidths = { 1f, 1f, 1f, 1f };
                    summaryTable.SetWidths(summaryWidths);

                    Font summaryLabelFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, BaseColor.BLACK);
                    Font summaryValueFont = FontFactory.GetFont(FontFactory.HELVETICA, 9, BaseColor.BLACK);

                    summaryTable.AddCell(CreateSummaryCell("Total Fee:", summaryLabelFont, Element.ALIGN_RIGHT));
                    summaryTable.AddCell(CreateSummaryCell(totalFee.ToString("N2"), summaryValueFont, Element.ALIGN_RIGHT));

                    BaseColor totalProfitColor = totalProfit >= 0 ? new BaseColor(34, 139, 34) : new BaseColor(220, 20, 60);
                    Font totalProfitFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 9, totalProfitColor);
                    summaryTable.AddCell(CreateSummaryCell("Total Profit:", summaryLabelFont, Element.ALIGN_RIGHT));
                    summaryTable.AddCell(CreateSummaryCell(totalProfit.ToString("N2"), totalProfitFont, Element.ALIGN_RIGHT));

                    summaryTable.AddCell(CreateSummaryCell("Credit:", summaryLabelFont, Element.ALIGN_RIGHT));
                    summaryTable.AddCell(CreateSummaryCell(credit.ToString("N2"), summaryValueFont, Element.ALIGN_RIGHT));

                    summaryTable.AddCell(CreateSummaryCell("Balance:", summaryLabelFont, Element.ALIGN_RIGHT));
                    summaryTable.AddCell(CreateSummaryCell(balance.ToString("N3") + " INR", summaryValueFont, Element.ALIGN_RIGHT));

                    document.Add(summaryTable);
                }

                // Footer
                document.Add(new Paragraph(" ") { SpacingBefore = 20 });
                LineSeparator footerLine = new LineSeparator(0.5f, 100f, BaseColor.LIGHT_GRAY, Element.ALIGN_CENTER, -2);
                document.Add(new Chunk(footerLine));

                Font footerFont = FontFactory.GetFont(FontFactory.HELVETICA, 7, BaseColor.GRAY);
                Paragraph footer = new Paragraph($"Report generated| Page 1 of 1 | {DateTime.Now:dd MMM yyyy HH:mm:ss}", footerFont)
                {
                    Alignment = Element.ALIGN_CENTER,
                    SpacingBefore = 5
                };
                document.Add(footer);

                document.Close();

                MessageBox.Show("PDF exported successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion Export Data to excel and PDF

    }
}
