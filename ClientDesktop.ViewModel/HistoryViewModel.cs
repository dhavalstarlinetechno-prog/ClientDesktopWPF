using ClientDesktop.Core.Base;
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
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    /// <summary>
    /// ViewModel for managing the History view, including deals, orders, and position histories.
    /// </summary>
    public class HistoryViewModel : ViewModelBase
    {
        #region Fields

        private readonly SessionService _sessionService;
        private readonly HistoryService _historyService;
        private readonly PDFBuilder _pdfBuilder = new PDFBuilder();

        private bool _isLoading;
        private double _clientCredit;
        private double _clientBalance;
        private bool _hasData;
        private bool _isDealsRequested;
        private bool _isOrdersRequested;
        private bool _isPositionRequested;
        private bool _isUpdatingFilters;
        private EnumHistoryType _currentViewType = EnumHistoryType.Deals;

        private List<HistoryModel> _allHistoryItems = new List<HistoryModel>();
        private List<PositionHistoryModel> _allPositionItems = new List<PositionHistoryModel>();

        private string _selectedSymbol = "All";
        private string _selectedExecution = "All";
        private string _selectedType = "All";
        private string _selectedEntry = "All";
        private string _selectedPosSymbol = "All";

        private EnumPeriod _selectedPeriod = EnumPeriod.Today;
        private DateTime _startDate = DateTime.Now.Date;
        private DateTime _endDate = DateTime.Now.AddDays(1);

        #endregion

        #region Properties

        public ObservableCollection<HistoryModel> HistoryItems { get; } = new ObservableCollection<HistoryModel>();

        public ObservableCollection<PositionHistoryModel> PositionHistoryItems { get; } = new ObservableCollection<PositionHistoryModel>();

        public ObservableCollection<string> AvailableSymbols { get; } = new ObservableCollection<string> { "All" };

        public ObservableCollection<string> AvailableExecutions { get; } = new ObservableCollection<string> { "All" };

        public ObservableCollection<string> AvailableTypes { get; } = new ObservableCollection<string> { "All" };

        public ObservableCollection<string> AvailableEntries { get; } = new ObservableCollection<string> { "All" };

        public ObservableCollection<string> AvailablePosSymbols { get; } = new ObservableCollection<string> { "All" };

        public bool IsLoading
        {
            get => _isLoading;
            private set => SetProperty(ref _isLoading, value);
        }

        public double ClientCredit
        {
            get => _clientCredit;
            set => SetProperty(ref _clientCredit, value);
        }

        public double ClientBalance
        {
            get => _clientBalance;
            set => SetProperty(ref _clientBalance, value);
        }

        public bool HasData
        {
            get => _hasData;
            set
            {
                if (SetProperty(ref _hasData, value))
                    OnPropertyChanged(nameof(HasNoData));
            }
        }

        public bool HasNoData => !HasData;

        public bool IsDealsChecked => CurrentViewType == EnumHistoryType.Deals;

        public bool IsOrdersChecked => CurrentViewType == EnumHistoryType.Orders;

        public bool IsPositionChecked => CurrentViewType == EnumHistoryType.Position;

        public EnumHistoryType CurrentViewType
        {
            get => _currentViewType;
            set
            {
                if (SetProperty(ref _currentViewType, value))
                {
                    OnPropertyChanged(nameof(DealsGridVisibility));
                    OnPropertyChanged(nameof(PositionGridVisibility));
                    OnPropertyChanged(nameof(CurrentViewTitle));
                    OnPropertyChanged(nameof(IsDealsChecked));
                    OnPropertyChanged(nameof(IsOrdersChecked));
                    OnPropertyChanged(nameof(IsPositionChecked));
                }
            }
        }

        public Visibility DealsGridVisibility => (CurrentViewType == EnumHistoryType.Deals || CurrentViewType == EnumHistoryType.Orders) ? Visibility.Visible : Visibility.Collapsed;

        public Visibility PositionGridVisibility => CurrentViewType == EnumHistoryType.Position ? Visibility.Visible : Visibility.Collapsed;

        public string CurrentViewTitle => CurrentViewType == EnumHistoryType.Deals ? "Deals" : "Orders";

        public string SelectedSymbol
        {
            get => _selectedSymbol;
            set
            {
                if (SetProperty(ref _selectedSymbol, value) && !_isUpdatingFilters) ApplyFilters();
            }
        }

        public string SelectedExecution
        {
            get => _selectedExecution;
            set
            {
                if (SetProperty(ref _selectedExecution, value) && !_isUpdatingFilters) ApplyFilters();
            }
        }

        public string SelectedType
        {
            get => _selectedType;
            set
            {
                if (SetProperty(ref _selectedType, value) && !_isUpdatingFilters) ApplyFilters();
            }
        }

        public string SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (SetProperty(ref _selectedEntry, value) && !_isUpdatingFilters) ApplyFilters();
            }
        }

        public string SelectedPosSymbol
        {
            get => _selectedPosSymbol;
            set
            {
                if (SetProperty(ref _selectedPosSymbol, value) && !_isUpdatingFilters) ApplyFilters();
            }
        }

        public IEnumerable<EnumPeriod> EnumPeriods
        {
            get { return Enum.GetValues(typeof(EnumPeriod)).Cast<EnumPeriod>(); }
        }

        public EnumPeriod SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                if (SetProperty(ref _selectedPeriod, value))
                {
                    UpdateDatesBasedOnPeriod();
                }
            }
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        #endregion

        #region Commands

        public ICommand RequestCommand { get; }
        public ICommand ChangeViewCommand { get; }
        public ICommand ExportExcelCommand { get; }
        public ICommand ExportPdfCommand { get; }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the HistoryViewModel class.
        /// </summary>
        public HistoryViewModel(SessionService sessionService, HistoryService historyService)
        {
            _sessionService = sessionService;
            _historyService = historyService;

            RequestCommand = new AsyncRelayCommand(_ => ExecuteRequestAsync());
            ChangeViewCommand = new RelayCommand(ExecuteChangeView);
            ExportExcelCommand = new RelayCommand(_ => ExportData(isExcel: true));
            ExportPdfCommand = new RelayCommand(_ => ExportData(isExcel: false));

            RegisterMessenger();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Loads full historical data from the API asynchronously.
        /// </summary>
        public async Task LoadFullDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;
            try
            {
                var fromDate = _sessionService.LicenseId == "1" ? new DateTime(2025, 6, 1) : new DateTime(1970, 1, 1);
                var toDate = DateTime.Now;

                var historyResult = await Task.Run(async () => await _historyService.FetchHistoryFromApiAsync(fromDate, toDate));
                var positionResult = await Task.Run(async () => await _historyService.FetchPositionHistoryFromApiAsync(fromDate, toDate));

                _allHistoryItems = historyResult.Data ?? new List<HistoryModel>();
                _allPositionItems = positionResult.Data ?? new List<PositionHistoryModel>();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadFullDataAsync), ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Calculates the upline balance.
        /// </summary>
        public static double CalculateUplineBalance(double? uplineAmount, double? uplineCommission, bool realtimeCommission)
        {
            double value = 0d;

            if (uplineAmount != null && uplineCommission != null)
                value = uplineAmount.Value + (realtimeCommission ? uplineCommission.Value : 0d);
            else if (uplineAmount != null)
                value = uplineAmount.Value;
            else if (uplineCommission != null && realtimeCommission)
                value = uplineCommission.Value;

            return value;
        }

        /// <summary>
        /// Sorts the deals history items based on a given property name and direction.
        /// </summary>
        public void SortDeals(string propertyName, ListSortDirection newDir)
        {
            var dataRows = HistoryItems.Where(x => x.RefId != "FOOTER").ToList();
            var footer = HistoryItems.FirstOrDefault(x => x.RefId == "FOOTER");

            var prop = typeof(HistoryModel).GetProperty(propertyName);
            if (prop == null) return;

            var sorted = newDir == ListSortDirection.Ascending
                ? dataRows.OrderBy(r => prop.GetValue(r)).ToList()
                : dataRows.OrderByDescending(r => prop.GetValue(r)).ToList();

            HistoryItems.Clear();
            foreach (var item in sorted) HistoryItems.Add(item);
            if (footer != null) HistoryItems.Add(footer);
        }

        /// <summary>
        /// Sorts the positions history items based on a given property name and direction.
        /// </summary>
        public void SortPositions(string propertyName, ListSortDirection newDir)
        {
            var dataRows = PositionHistoryItems.Where(x => x.RefId != "FOOTER").ToList();
            var footer = PositionHistoryItems.FirstOrDefault(x => x.RefId == "FOOTER");

            var prop = typeof(PositionHistoryModel).GetProperty(propertyName);
            if (prop == null) return;

            var sorted = newDir == ListSortDirection.Ascending
                ? dataRows.OrderBy(r => prop.GetValue(r)).ToList()
                : dataRows.OrderByDescending(r => prop.GetValue(r)).ToList();

            PositionHistoryItems.Clear();
            foreach (var item in sorted) PositionHistoryItems.Add(item);
            if (footer != null) PositionHistoryItems.Add(footer);
        }

        /// <summary>
        /// Exports history data to an Excel file.
        /// </summary>
        public void ExportToExcel(List<HistoryModel> data, EnumHistoryType gridType)
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
                        sheet.Cell(rowIndex, 10).Value = item.DisplayPrice;
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

        /// <summary>
        /// Exports position data to an Excel file.
        /// </summary>
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

                        var updateCell = sheet.Cell(rowIndex, 2);
                        updateCell.Value = item.UpdatedAt;
                        updateCell.Style.DateFormat.Format = "dd/MM/yy HH:mm";

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

        /// <summary>
        /// Exports history data to a PDF file.
        /// </summary>
        public void ExportToPdf(List<HistoryModel> data, EnumHistoryType gridType)
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
                decimal credit = 0, balance = 0;

                if (footerRow?.Comment != null && footerRow.Comment.Contains("Credit:"))
                {
                    var parts = footerRow.Comment.Split(new[] { "Credit:", "Balance:" }, StringSplitOptions.None);
                    if (parts.Length >= 2) decimal.TryParse(parts[1].Trim().Split(' ')[0].Replace(",", ""), out credit);
                    if (parts.Length >= 3) decimal.TryParse(parts[2].Replace("INR", "").Trim().Replace(",", ""), out balance);
                }

                var dt = new DataTable();
                dt.Columns.Add("RowType", typeof(string));
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
                        ist.ToString("dd'/'MM'/'yy HH:mm", CultureInfo.InvariantCulture),
                        item.RefId ?? "",
                        item.PositionId ?? "--",
                        item.SymbolName ?? "",
                        item.OrderType ?? "",
                        item.Side ?? "",
                        item.DealType ?? "",
                        item.Volume.ToString("N2"),
                        item.DisplayPrice,
                        item.UplineCommission.ToString("N2"),
                        item.Pnl.ToString("N2"),
                        item.Comment ?? ""
                    );
                }

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

                var alignments = new Dictionary<string, iText.Layout.Properties.TextAlignment>
                {
                    { "Volume", iText.Layout.Properties.TextAlignment.RIGHT },
                    { "Comm.",  iText.Layout.Properties.TextAlignment.RIGHT },
                    { "Price", iText.Layout.Properties.TextAlignment.RIGHT },
                    { "Profit", iText.Layout.Properties.TextAlignment.RIGHT }
                };

                _pdfBuilder
                    .Clear()
                    .AddSubTitle("History", fontSize: 16, centerAlign: false)
                    .AddSpacing(6)
                    .AddGrid(dt, null, null, alignments);

                _pdfBuilder.CellFontSize = 7.5f;
                _pdfBuilder.HeaderFontSize = 8f;
                _pdfBuilder.HeaderPadding = 4f;
                _pdfBuilder.CellPadding = 3f;
                _pdfBuilder.ShowVerticalBorders = true;
                _pdfBuilder.ColumnWidths = new Dictionary<string, float>
                {
                    { "Sr",   0.4f },
                    { "Time", 1.6f }
                };

                _pdfBuilder.BuildPDF($"{gridType}_History", landscape: true, autoFormat: true);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExportToPdf), ex);
                MessageBox.Show($"PDF export error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Exports position data to a PDF file.
        /// </summary>
        public void ExportPositionToPdf(List<PositionHistoryModel> data)
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
                decimal credit = 0, balance = 0;

                if (footerRow?.Comment != null && footerRow.Comment.Contains("Credit:"))
                {
                    var parts = footerRow.Comment.Split(new[] { "Credit:", "Balance:" }, StringSplitOptions.None);
                    if (parts.Length >= 2) decimal.TryParse(parts[1].Trim().Split(' ')[0].Replace(",", ""), out credit);
                    if (parts.Length >= 3) decimal.TryParse(parts[2].Replace("INR", "").Trim().Replace(",", ""), out balance);
                }

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
                        item.DisplayAveragePrice,
                        "",
                        item.Pnl.ToString("N2"),
                        item.Comment ?? ""
                    );
                }

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
                    .AddGrid(dt, null, null, alignments);

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

        #endregion

        #region Private Methods

        /// <summary>
        /// Registers messenger events for user authentication updates.
        /// </summary>
        private void RegisterMessenger()
        {
            WeakReferenceMessenger.Default.Register<UserAuthEvent>(this, async (recipient, message) =>
            {
                if (message.IsLoggedIn)
                {
                    await LoadFullDataAsync();
                }
            });
        }

        /// <summary>
        /// Executes the data request based on the selected view type.
        /// </summary>
        private async Task ExecuteRequestAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                var apiToDate = DateTime.Now;
                var apiFromDate = apiToDate.AddDays(-7);

                if (CurrentViewType == EnumHistoryType.Position)
                {
                    var positionResult = await Task.Run(async () => await _historyService.FetchPositionHistoryFromApiAsync(apiFromDate, apiToDate));
                    _allPositionItems = positionResult.Data ?? new List<PositionHistoryModel>();
                    _isPositionRequested = true;
                }
                else
                {
                    var historyResult = await Task.Run(async () => await _historyService.FetchHistoryFromApiAsync(apiFromDate, apiToDate));
                    _allHistoryItems = historyResult.Data ?? new List<HistoryModel>();

                    if (CurrentViewType == EnumHistoryType.Deals)
                        _isDealsRequested = true;
                    else if (CurrentViewType == EnumHistoryType.Orders)
                        _isOrdersRequested = true;
                }

                ApplyFilters();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog("ExecuteRequestAsync", ex);
            }
            finally
            {
                IsLoading = false;
            }
        }

        /// <summary>
        /// Changes the current view type and resets applicable filters.
        /// </summary>
        private void ExecuteChangeView(object? parameter)
        {
            if (parameter is string modeStr && Enum.TryParse(modeStr, out EnumHistoryType mode))
            {
                CurrentViewType = mode;

                SelectedSymbol = "All";
                SelectedExecution = "All";
                SelectedType = "All";
                SelectedEntry = "All";
                SelectedPosSymbol = "All";

                ApplyFilters();
            }
        }

        /// <summary>
        /// Updates the start and end dates according to the currently selected period.
        /// </summary>
        private void UpdateDatesBasedOnPeriod()
        {
            DateTime toDate = DateTime.Today;
            EndDate = toDate.AddDays(1);

            switch (SelectedPeriod)
            {
                case EnumPeriod.Today:
                    StartDate = toDate;
                    break;
                case EnumPeriod.Last3Days:
                    StartDate = toDate.AddDays(-2);
                    break;
                case EnumPeriod.LastWeek:
                    int diff = (7 + (toDate.DayOfWeek - DayOfWeek.Sunday)) % 7;
                    StartDate = toDate.AddDays(-diff);
                    break;
                case EnumPeriod.LastMonth:
                    StartDate = new DateTime(toDate.Year, toDate.Month, 1);
                    break;
                case EnumPeriod.Last3Months:
                    var m3 = toDate.AddMonths(-2);
                    StartDate = new DateTime(m3.Year, m3.Month, 1);
                    break;
                case EnumPeriod.Last6Months:
                    var m6 = toDate.AddMonths(-5);
                    StartDate = new DateTime(m6.Year, m6.Month, 1);
                    break;
                case EnumPeriod.AllHistory:
                    StartDate = new DateTime(1970, 1, 1);
                    break;
            }
        }

        /// <summary>
        /// Applies the selected filters to the history or position datasets.
        /// </summary>
        private void ApplyFilters()
        {
            if (_allHistoryItems == null || _allPositionItems == null) return;
            if (_isUpdatingFilters) return;

            try
            {
                _isUpdatingFilters = true;
                var start = StartDate;
                var end = EndDate.Date.AddDays(1).AddTicks(-1);

                ClientCredit = _sessionService.CurrentClient?.CreditAmount ?? 0;
                ClientBalance = CalculateUplineBalance(_sessionService.CurrentClient?.UplineAmount, _sessionService.CurrentClient?.UplineCommission, _sessionService.CurrentClient?.RealtimeCommission == true);

                if (CurrentViewType == EnumHistoryType.Position)
                {
                    PositionHistoryItems.Clear();

                    if (!_isPositionRequested)
                    {
                        HasData = false;
                        return;
                    }

                    var basePos = _allPositionItems.Where(h =>
                        h.LastOutAt == null ||
                        (CommonHelper.ConvertUtcToIst(h.UpdatedAt) >= start.Date &&
                         CommonHelper.ConvertUtcToIst(h.UpdatedAt) <= end))
                        .ToList();

                    UpdatePositionDropdowns(basePos);

                    var filteredPos = basePos;
                    if (SelectedPosSymbol != "All")
                        filteredPos = filteredPos.Where(x => x.SymbolName == SelectedPosSymbol).ToList();

                    var finalData = filteredPos.OrderBy(s => s.UpdatedAt).ToList();

                    if (finalData.Any())
                    {
                        double totalProfit = finalData.Sum(x => x.Pnl);

                        finalData.Add(new PositionHistoryModel
                        {
                            RefId = "FOOTER",
                            UpdatedAt = DateTime.MaxValue,
                            SymbolName = "",
                            Side = "",
                            Comment = $"Profit: {CommonHelper.FormatAmount((decimal)totalProfit):N2}  Credit: {CommonHelper.FormatAmount((decimal)ClientCredit):N2}  Balance: {CommonHelper.FormatAmount((decimal)ClientBalance):N2} INR",
                            Pnl = totalProfit
                        });
                    }

                    foreach (var item in finalData) PositionHistoryItems.Add(item);
                    HasData = PositionHistoryItems.Any(x => x.RefId != "FOOTER");
                }
                else
                {
                    HistoryItems.Clear();

                    if (CurrentViewType == EnumHistoryType.Deals && !_isDealsRequested) { HasData = false; return; }
                    if (CurrentViewType == EnumHistoryType.Orders && !_isOrdersRequested) { HasData = false; return; }

                    HashSet<string> orderTypeFilter = CurrentViewType == EnumHistoryType.Deals ? new HashSet<string> { "BuyLimit", "SellLimit", "BuyStop", "SellStop" } : new HashSet<string>();
                    HashSet<string> symbolNameFilter = CurrentViewType == EnumHistoryType.Deals ? new HashSet<string> { "Credit", "Balance" } : new HashSet<string>();

                    var baseHist = _allHistoryItems.Where(h =>
                    {
                        if (orderTypeFilter.Contains(h.OrderType) || symbolNameFilter.Contains(h.SymbolName)) return false;
                        var istTime = CommonHelper.ConvertUtcToIst(h.CreatedOn);
                        return istTime >= start && istTime <= end;
                    }).ToList();

                    UpdateHistoryDropdowns(baseHist);

                    var filteredHistory = baseHist.AsEnumerable();
                    if (SelectedSymbol != "All") filteredHistory = filteredHistory.Where(x => x.SymbolName == SelectedSymbol);
                    if (SelectedExecution != "All") filteredHistory = filteredHistory.Where(x => x.OrderType == SelectedExecution);
                    if (SelectedType != "All") filteredHistory = filteredHistory.Where(x => x.Side == SelectedType);
                    if (SelectedEntry != "All") filteredHistory = filteredHistory.Where(x => x.DealType == SelectedEntry);

                    var finalData = filteredHistory.OrderBy(s => s.CreatedOn).ToList();

                    if (finalData.Any())
                    {
                        var validDataForSum = finalData.Where(x => x.OrderType != "Bill" && x.SymbolName != "Credit").ToList();

                        decimal totalProfit = validDataForSum.Sum(x => x.Pnl);
                        decimal totalComm = validDataForSum.Sum(x => x.UplineCommission);

                        decimal displayProfit = (totalProfit + totalComm) > 0 ? (totalProfit + totalComm) : 0.00m;

                        finalData.Add(new HistoryModel
                        {
                            RefId = "FOOTER",
                            CreatedOn = DateTime.MaxValue,
                            SymbolName = "",
                            OrderType = "",
                            Side = "",
                            Comment = $"Profit: {CommonHelper.FormatAmount(displayProfit):N2}  Credit: {CommonHelper.FormatAmount((decimal)ClientCredit):N2}  Balance: {CommonHelper.FormatAmount((decimal)ClientBalance):N2} INR",
                            UplineCommission = totalComm,
                            Pnl = totalProfit
                        });
                    }

                    foreach (var item in finalData) HistoryItems.Add(item);
                    HasData = HistoryItems.Any(x => x.RefId != "FOOTER");
                }
            }
            finally
            {
                _isUpdatingFilters = false;
            }
        }

        /// <summary>
        /// Updates the available position dropdown options based on current data.
        /// </summary>
        private void UpdatePositionDropdowns(List<PositionHistoryModel> baseData)
        {
            var oldPos = SelectedPosSymbol;
            AvailablePosSymbols.Clear();
            AvailablePosSymbols.Add("All");

            var posSymbols = baseData.Where(x => !string.IsNullOrEmpty(x.SymbolName)).Select(x => x.SymbolName).Distinct().OrderBy(x => x);
            foreach (var s in posSymbols) AvailablePosSymbols.Add(s);

            _selectedPosSymbol = AvailablePosSymbols.Contains(oldPos) ? oldPos : "All";
            OnPropertyChanged(nameof(SelectedPosSymbol));
        }

        /// <summary>
        /// Updates the available history dropdown options based on current data.
        /// </summary>
        private void UpdateHistoryDropdowns(List<HistoryModel> baseData)
        {
            var oldSym = SelectedSymbol;
            var oldExec = SelectedExecution;
            var oldType = SelectedType;
            var oldEntry = SelectedEntry;

            AvailableSymbols.Clear(); AvailableSymbols.Add("All");
            AvailableExecutions.Clear(); AvailableExecutions.Add("All");
            AvailableTypes.Clear(); AvailableTypes.Add("All");
            AvailableEntries.Clear(); AvailableEntries.Add("All");

            var symbols = baseData.Where(x => !string.IsNullOrEmpty(x.SymbolName)).Select(x => x.SymbolName).Distinct().OrderBy(x => x);
            foreach (var s in symbols) AvailableSymbols.Add(s);

            var executions = baseData.Where(x => !string.IsNullOrEmpty(x.OrderType)).Select(x => x.OrderType).Distinct().OrderBy(x => x);
            foreach (var e in executions) AvailableExecutions.Add(e);

            var types = baseData.Where(x => !string.IsNullOrEmpty(x.Side)).Select(x => x.Side).Distinct().OrderBy(x => x);
            foreach (var t in types) AvailableTypes.Add(t);

            var entries = baseData.Where(x => !string.IsNullOrEmpty(x.DealType)).Select(x => x.DealType).Distinct().OrderBy(x => x);
            foreach (var e in entries) AvailableEntries.Add(e);

            _selectedSymbol = AvailableSymbols.Contains(oldSym) ? oldSym : "All";
            _selectedExecution = AvailableExecutions.Contains(oldExec) ? oldExec : "All";
            _selectedType = AvailableTypes.Contains(oldType) ? oldType : "All";
            _selectedEntry = AvailableEntries.Contains(oldEntry) ? oldEntry : "All";

            OnPropertyChanged(nameof(SelectedSymbol));
            OnPropertyChanged(nameof(SelectedExecution));
            OnPropertyChanged(nameof(SelectedType));
            OnPropertyChanged(nameof(SelectedEntry));
        }

        /// <summary>
        /// Triggers the appropriate export process based on the current view type.
        /// </summary>
        private void ExportData(bool isExcel)
        {
            if (CurrentViewType == EnumHistoryType.Position)
            {
                var data = PositionHistoryItems.ToList();
                if (isExcel) ExportPositionToExcel(data);
                else ExportPositionToPdf(data);
            }
            else
            {
                var data = HistoryItems.ToList();
                if (isExcel) ExportToExcel(data, CurrentViewType);
                else ExportToPdf(data, CurrentViewType);
            }
        }

        #endregion 
    }
}