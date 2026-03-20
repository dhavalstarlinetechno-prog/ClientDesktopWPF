using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Events;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Models;
using ClosedXML.Excel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Data;
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
        private readonly PDFBuilder _pdfBuilder = new PDFBuilder();
        public double ClientCrdeit { get; set; }
        public double ClientBalance { get; set; }
        public Action OnHistoryDataLoaded { get; set; }
        public HistoryViewModel(SessionService sessionService, HistoryService historyService)
        {
            _sessionService = sessionService;
            _historyService = historyService;

            RegisterMessenger();
        }

        /// <summary>
        /// Registers listeners for application-wide signals using the Event Aggregator.
        /// </summary>
        private void RegisterMessenger()
        {
            WeakReferenceMessenger.Default.Register<UserAuthEvent>(this, async (recipient, message) =>
            {
                if (message.IsLoggedIn)
                {
                    LoadDataAsync();
                    ClientCrdeit = _sessionService.ClientListData.FirstOrDefault()?.CreditAmount ?? 0;
                    ClientBalance = _sessionService.ClientListData.FirstOrDefault()?.Balance ?? 0;
                }
            });
        }

        public async Task LoadDataAsync()
        {
            if (IsLoading) return;

            IsLoading = true;

            try
            {
                var fromDate = _sessionService.LicenseId == "1" ? new DateTime(2025, 6, 1) : new DateTime(1970, 1, 1);
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

        #region Export Data to excel 
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


        #endregion Export Data to excel and PDF

        #region Export Data to PDF

        // Strips trailing zeros: 0.863660000 → 0.86366
        private static string FormatPrice(decimal value)
            => value.ToString("0.##########");

        public void ExportToPdf(List<HistoryModel> data, HistoryType gridType)
        {
            try
            {
                if (data == null || data.Count <= 1)
                {
                    MessageBox.Show("No data to export", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // ── Footer values ─────────────────────────────────────────────
                var footerRow = data.FirstOrDefault(x => x.RefId == "FOOTER");
                decimal totalProfit = footerRow?.Pnl ?? 0;
                decimal totalComm = footerRow?.UplineCommission ?? 0;
                decimal credit = 0, balance = 0;

                if (footerRow?.Comment != null && footerRow.Comment.Contains("Credit:"))
                {
                    var parts = footerRow.Comment.Split(new[] { "Credit:", "Balance:" }, StringSplitOptions.None);
                    if (parts.Length >= 2) decimal.TryParse(parts[1].Trim().Split(' ')[0].Replace(",", ""), out credit);
                    if (parts.Length >= 3) decimal.TryParse(parts[2].Replace("INR", "").Trim().Replace(",", ""), out balance);
                }

                // ── DataTable ─────────────────────────────────────────────────
                var dt = new DataTable();
                dt.Columns.Add("RowType", typeof(string));   // hidden by PDFBuilder._skipColumns
                dt.Columns.Add("Sr", typeof(string));
                dt.Columns.Add("Time", typeof(string));
                dt.Columns.Add("Deal Id", typeof(string));
                dt.Columns.Add("Position Id", typeof(string));
                dt.Columns.Add("Symbol", typeof(string));
                dt.Columns.Add("Execution", typeof(string));
                dt.Columns.Add("Type", typeof(string));
                dt.Columns.Add("Entry", typeof(string));
                dt.Columns.Add("Volume", typeof(string));
                dt.Columns.Add("Price", typeof(string));
                dt.Columns.Add("Comm.", typeof(string));
                dt.Columns.Add("Profit", typeof(string));
                dt.Columns.Add("Comment", typeof(string));

                int sr = 1;
                foreach (var item in data.Where(x => x.RefId != "FOOTER"))
                {
                    var ist = CommonHelper.ConvertUtcToIst(item.CreatedOn);
                    dt.Rows.Add(
                        "",
                        sr++.ToString(),
                        ist.ToString("dd/MM/yy HH:mm"),
                        item.RefId ?? "",
                        item.PositionId ?? "--",
                        item.SymbolName ?? "",
                        item.OrderType ?? "",
                        item.Side ?? "",
                        item.DealType ?? "",
                        item.Volume.ToString(),
                        FormatPrice(item.Price),
                        item.UplineCommission.ToString("N2"),
                        item.Pnl.ToString("N2"),
                        item.Comment ?? ""
                    );
                }

                // ── Footer row inside table (GrandTotal = bold + grey) ────────
                dt.Rows.Add(
                    "GrandTotal",
                    "Profit:", totalProfit.ToString("N2"),
                    "Credit:", credit.ToString("N2"),
                    "Balance:", balance.ToString("N3") + " INR",
                    "", "", "",
                    totalComm.ToString("N2"),
                    totalProfit.ToString("N2"),
                    ""
                );

                // ── PDFBuilder uses iText7 TextAlignment directly ─────────────
                var alignments = new Dictionary<string, iText.Layout.Properties.TextAlignment>
                {
                    { "Volume", iText.Layout.Properties.TextAlignment.RIGHT },
                    { "Comm.",  iText.Layout.Properties.TextAlignment.RIGHT },
                    { "Profit", iText.Layout.Properties.TextAlignment.RIGHT }
                };

                _pdfBuilder
                    .Clear()
                    .AddSubTitle("History", fontSize: 16, centerAlign: false)
                    .AddSpacing(6)
                    .AddGrid(dt, null, null, alignments)
                    ;

                // Compact settings for wide 13-column table
                _pdfBuilder.CellFontSize = 7.5f;
                _pdfBuilder.HeaderFontSize = 8f;
                _pdfBuilder.HeaderPadding = 4f;
                _pdfBuilder.CellPadding = 3f;
                _pdfBuilder.ShowVerticalBorders = true;
                _pdfBuilder.ColumnWidths = new Dictionary<string, float>
                {
                    { "Sr",   0.4f },   // narrow
                    { "Time", 1.6f }    // wider for date+time
                };

                _pdfBuilder.BuildPDF($"{gridType}_History", landscape: true, autoFormat: true);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExportToPdf), ex);
                MessageBox.Show($"PDF export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                // ── Footer values ─────────────────────────────────────────────
                var footerRow = data.FirstOrDefault(x => x.RefId == "FOOTER");
                decimal totalProfit = (decimal)(footerRow?.Pnl ?? 0);
                decimal credit = 0, balance = 0;

                if (footerRow?.Comment != null && footerRow.Comment.Contains("Credit:"))
                {
                    var parts = footerRow.Comment.Split(new[] { "Credit:", "Balance:" }, StringSplitOptions.None);
                    if (parts.Length >= 2) decimal.TryParse(parts[1].Trim().Split(' ')[0].Replace(",", ""), out credit);
                    if (parts.Length >= 3) decimal.TryParse(parts[2].Replace("INR", "").Trim().Replace(",", ""), out balance);
                }

                // ── DataTable ─────────────────────────────────────────────────
                var dt = new DataTable();
                dt.Columns.Add("RowType", typeof(string));
                dt.Columns.Add("Sr", typeof(string));
                dt.Columns.Add("Time", typeof(string));
                dt.Columns.Add("Last Out Time", typeof(string));
                dt.Columns.Add("Position Id", typeof(string));
                dt.Columns.Add("Type", typeof(string));
                dt.Columns.Add("Volume", typeof(string));
                dt.Columns.Add("Symbol", typeof(string));
                dt.Columns.Add("Price", typeof(string));
                dt.Columns.Add("Comm.", typeof(string));
                dt.Columns.Add("Profit", typeof(string));
                dt.Columns.Add("Comment", typeof(string));

                int sr = 1;
                foreach (var item in data.Where(x => x.RefId != "FOOTER"))
                {
                    string displayTime = CommonHelper.ConvertUtcToIst(item.UpdatedAt).ToString("dd/MM/yy HH:mm");
                    string lastOutTime = item.LastOutAt.HasValue
                                        ? CommonHelper.ConvertUtcToIst(item.LastOutAt.Value).ToString("dd/MM/yy HH:mm")
                                        : "--";
                    dt.Rows.Add(
                        "",
                        sr++.ToString(),
                        displayTime,
                        lastOutTime,
                        item.RefId ?? "",
                        item.Side ?? "",
                        item.VolumeDisplay.ToString(),
                        item.SymbolName ?? "",
                        FormatPrice((decimal)item.AveragePrice),
                        "",
                        item.Pnl.ToString("N2"),
                        item.Comment ?? ""
                    );
                }

                // ── Footer row inside table ───────────────────────────────────
                dt.Rows.Add(
                    "GrandTotal",
                    "Profit:", totalProfit.ToString("N2"),
                    "Credit:", credit.ToString("N2"),
                    "Balance:", balance.ToString("N3") + " INR",
                    "", "", "",
                    totalProfit.ToString("N2"),
                    ""
                );

                var alignments = new Dictionary<string, iText.Layout.Properties.TextAlignment>
                {
                    { "Profit", iText.Layout.Properties.TextAlignment.RIGHT }
                };

                _pdfBuilder
                    .Clear()
                    .AddSubTitle("Position", fontSize: 16, centerAlign: false)
                    .AddSpacing(6)
                    .AddGrid(dt, null, null, alignments)
                    ;

                // Compact settings for wide 11-column table
                _pdfBuilder.CellFontSize = 7.5f;
                _pdfBuilder.HeaderFontSize = 8f;
                _pdfBuilder.HeaderPadding = 4f;
                _pdfBuilder.CellPadding = 3f;
                _pdfBuilder.ShowVerticalBorders = true;
                _pdfBuilder.ColumnWidths = new Dictionary<string, float>
                {
                    { "Sr",            0.4f },
                    { "Time",          1.6f },
                    { "Last Out Time", 1.6f }
                };

                _pdfBuilder.BuildPDF("Position_History", landscape: true, autoFormat: true);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExportPositionToPdf), ex);
                MessageBox.Show($"PDF export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #endregion Export Data to PDF

    }
}