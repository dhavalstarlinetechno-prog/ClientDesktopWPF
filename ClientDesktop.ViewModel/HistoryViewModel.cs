using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Events;
using ClientDesktop.Core.Interfaces;
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
        private readonly IPdfService _pdfService;

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

        private ObservableCollection<HistoryModel> _historyItems = new ObservableCollection<HistoryModel>();
        public ObservableCollection<HistoryModel> HistoryItems { get => _historyItems; set => SetProperty(ref _historyItems, value); }

        private ObservableCollection<PositionHistoryModel> _positionHistoryItems = new ObservableCollection<PositionHistoryModel>();
        public ObservableCollection<PositionHistoryModel> PositionHistoryItems { get => _positionHistoryItems; set => SetProperty(ref _positionHistoryItems, value); }

        private ObservableCollection<string> _availableSymbols = new ObservableCollection<string> { "All" };
        public ObservableCollection<string> AvailableSymbols { get => _availableSymbols; set => SetProperty(ref _availableSymbols, value); }

        private ObservableCollection<string> _availableExecutions = new ObservableCollection<string> { "All" };
        public ObservableCollection<string> AvailableExecutions { get => _availableExecutions; set => SetProperty(ref _availableExecutions, value); }

        private ObservableCollection<string> _availableTypes = new ObservableCollection<string> { "All" };
        public ObservableCollection<string> AvailableTypes { get => _availableTypes; set => SetProperty(ref _availableTypes, value); }

        private ObservableCollection<string> _availableEntries = new ObservableCollection<string> { "All" };
        public ObservableCollection<string> AvailableEntries { get => _availableEntries; set => SetProperty(ref _availableEntries, value); }

        private ObservableCollection<string> _availablePosSymbols = new ObservableCollection<string> { "All" };
        public ObservableCollection<string> AvailablePosSymbols { get => _availablePosSymbols; set => SetProperty(ref _availablePosSymbols, value); }

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
                if (string.IsNullOrEmpty(value)) return;
                if (SetProperty(ref _selectedSymbol, value) && !_isUpdatingFilters) ApplyFilters();
            }
        }

        public string SelectedExecution
        {
            get => _selectedExecution;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                if (SetProperty(ref _selectedExecution, value) && !_isUpdatingFilters) ApplyFilters();
            }
        }

        public string SelectedType
        {
            get => _selectedType;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                if (SetProperty(ref _selectedType, value) && !_isUpdatingFilters) ApplyFilters();
            }
        }

        public string SelectedEntry
        {
            get => _selectedEntry;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                if (SetProperty(ref _selectedEntry, value) && !_isUpdatingFilters) ApplyFilters();
            }
        }

        public string SelectedPosSymbol
        {
            get => _selectedPosSymbol;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
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
                if (SetProperty(ref _selectedPeriod, value)) UpdateDatesBasedOnPeriod();
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
        public HistoryViewModel(SessionService sessionService, HistoryService historyService, IPdfService pdfService)
        {
            _sessionService = sessionService;
            _historyService = historyService;
            _pdfService = pdfService;

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

                var historyTask = _historyService.FetchHistoryFromApiAsync(fromDate, toDate);
                var positionTask = _historyService.FetchPositionHistoryFromApiAsync(fromDate, toDate);

                await Task.WhenAll(historyTask, positionTask);

                _allHistoryItems = historyTask.Result?.Data ?? new List<HistoryModel>();
                _allPositionItems = positionTask.Result?.Data ?? new List<PositionHistoryModel>();
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
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(CalculateUplineBalance), ex);
                return 0d;
            }
        }

        /// <summary>
        /// Sorts the deals history items based on a given property name and direction.
        /// </summary>
        public async Task SortDeals(string propertyName, ListSortDirection newDir)
        {
            try
            {
                var dataRows = HistoryItems.Where(x => x.RefId != "FOOTER").ToList();
                var footer = HistoryItems.FirstOrDefault(x => x.RefId == "FOOTER");

                var prop = typeof(HistoryModel).GetProperty(propertyName);
                if (prop == null) return;

                var sorted = await Task.Run(() =>
                     newDir == ListSortDirection.Ascending
                         ? dataRows.OrderBy(r => prop.GetValue(r)).ToList()
                         : dataRows.OrderByDescending(r => prop.GetValue(r)).ToList()
                 );

                await SafeUIInvokeAsync(() =>
                {
                    var newList = new ObservableCollection<HistoryModel>(sorted);
                    if (footer != null) newList.Add(footer);
                    HistoryItems = newList;
                });
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SortDeals), ex);
            }
        }

        /// <summary>
        /// Sorts the positions history items based on a given property name and direction.
        /// </summary>
        public async Task SortPositions(string propertyName, ListSortDirection newDir)
        {
            try
            {
                var dataRows = PositionHistoryItems.Where(x => x.RefId != "FOOTER").ToList();
                var footer = PositionHistoryItems.FirstOrDefault(x => x.RefId == "FOOTER");

                var prop = typeof(PositionHistoryModel).GetProperty(propertyName);
                if (prop == null) return;

                var sorted = await Task.Run(() =>
                    newDir == ListSortDirection.Ascending
                        ? dataRows.OrderBy(r => prop.GetValue(r)).ToList()
                        : dataRows.OrderByDescending(r => prop.GetValue(r)).ToList()
                );

                await SafeUIInvokeAsync(() =>
                {
                    var newList = new ObservableCollection<PositionHistoryModel>(sorted);
                    if (footer != null) newList.Add(footer);
                    PositionHistoryItems = newList;
                });
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SortPositions), ex);
            }
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
                    {
                        string cleanCredit = parts[1].Replace(" ", "").Replace("\u00A0", "").Replace(",", "").Trim();
                        decimal.TryParse(cleanCredit, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out credit);
                    }
                    if (parts.Length >= 3)
                    {
                        string cleanBalance = parts[2].Replace("INR", "").Replace(" ", "").Replace("\u00A0", "").Replace(",", "").Trim();
                        decimal.TryParse(cleanBalance, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out balance);
                    }
                }

                bool? dialogResult = false;
                string filePath = string.Empty;

                SafeUIInvokeSync(() =>
                {
                    SaveFileDialog saveDialog = new SaveFileDialog
                    {
                        Filter = "Excel Workbook|*.xlsx",
                        Title = "Export to Excel",
                        FileName = $"{gridType}_History_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    };
                    dialogResult = saveDialog.ShowDialog();
                    filePath = saveDialog.FileName;
                });

                if (dialogResult != true)
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
                        dateCell.Value = CommonHelper.ConvertUtcToIst(item.CreatedOn);
                        dateCell.Style.DateFormat.Format = @"dd\/MM\/yy HH:mm";

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
                    sheet.Cell(rowIndex, 2).Value = CommonHelper.FormatAmount(totalProfit);
                    sheet.Cell(rowIndex, 3).Value = "Credit:";
                    sheet.Cell(rowIndex, 4).Value = CommonHelper.FormatAmount(credit);
                    sheet.Cell(rowIndex, 5).Value = "Balance:";
                    sheet.Cell(rowIndex, 6).Value = CommonHelper.FormatAmount(balance) + " INR";
                    sheet.Cell(rowIndex, 11).Value = CommonHelper.FormatAmount(totalComm);
                    sheet.Cell(rowIndex, 12).Value = CommonHelper.FormatAmount(totalProfit);
                    sheet.Cell(rowIndex, 13).Value = "";

                    var footerRange = sheet.Range(rowIndex, 1, rowIndex, 13);
                    footerRange.Style.Font.Bold = true;
                    footerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    sheet.Columns().AdjustToContents();
                    workbook.SaveAs(filePath);
                }

                FileLogger.Log("Journal", $"{gridType} history exported to Excel successfully.");
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExportToExcel), ex);
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
                    {
                        string cleanCredit = parts[1].Replace(" ", "").Replace("\u00A0", "").Replace(",", "").Trim();
                        decimal.TryParse(cleanCredit, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out credit);
                    }
                    if (parts.Length >= 3)
                    {
                        string cleanBalance = parts[2].Replace("INR", "").Replace(" ", "").Replace("\u00A0", "").Replace(",", "").Trim();
                        decimal.TryParse(cleanBalance, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out balance);
                    }
                }

                bool? dialogResult = false;
                string filePath = string.Empty;

                SafeUIInvokeSync(() =>
                {
                    SaveFileDialog saveDialog = new SaveFileDialog
                    {
                        Filter = "Excel Workbook|*.xlsx",
                        Title = "Export to Excel",
                        FileName = $"Position_History_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                    };
                    dialogResult = saveDialog.ShowDialog();
                    filePath = saveDialog.FileName;
                });

                if (dialogResult != true)
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
                        updateCell.Value = CommonHelper.ConvertUtcToIst(item.UpdatedAt);
                        updateCell.Style.DateFormat.Format = @"dd\/MM\/yy HH:mm:ss";
                        var lastOutCell = sheet.Cell(rowIndex, 3);
                        if (item.LastOutAt.HasValue)
                        {
                            lastOutCell.Value = CommonHelper.ConvertUtcToIst(item.LastOutAt.Value);
                            lastOutCell.Style.DateFormat.Format = @"dd\/MM\/yy HH:mm:ss";
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
                    sheet.Cell(rowIndex, 2).Value = CommonHelper.FormatAmount(totalProfit);
                    sheet.Cell(rowIndex, 3).Value = "Credit:";
                    sheet.Cell(rowIndex, 4).Value = CommonHelper.FormatAmount(credit);
                    sheet.Cell(rowIndex, 5).Value = "Balance:";
                    sheet.Cell(rowIndex, 6).Value = CommonHelper.FormatAmount(balance) + " INR";
                    sheet.Cell(rowIndex, 9).Value = "";
                    sheet.Cell(rowIndex, 10).Value = CommonHelper.FormatAmount(totalProfit);
                    sheet.Cell(rowIndex, 11).Value = "";

                    var footerRange = sheet.Range(rowIndex, 1, rowIndex, 11);
                    footerRange.Style.Font.Bold = true;
                    footerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                    sheet.Columns().AdjustToContents();
                    workbook.SaveAs(filePath);
                }

                FileLogger.Log("Journal", "Position history exported to Excel successfully.");
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExportPositionToExcel), ex);
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
                    return;
                }

                var footerRow = data.FirstOrDefault(x => x.RefId == "FOOTER");
                decimal totalProfit = footerRow?.Pnl ?? 0;
                decimal totalComm = footerRow?.UplineCommission ?? 0;
                decimal credit = 0, balance = 0;

                if (footerRow?.Comment != null && footerRow.Comment.Contains("Credit:"))
                {
                    var parts = footerRow.Comment.Split(new[] { "Credit:", "Balance:" }, StringSplitOptions.None);

                    if (parts.Length >= 2)
                    {
                        string cleanCredit = parts[1].Replace(" ", "").Replace("\u00A0", "").Replace(",", "").Trim();
                        decimal.TryParse(cleanCredit, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out credit);
                    }
                    if (parts.Length >= 3)
                    {
                        string cleanBalance = parts[2].Replace("INR", "").Replace(" ", "").Replace("\u00A0", "").Replace(",", "").Trim();
                        decimal.TryParse(cleanBalance, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out balance);
                    }
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
                        CommonHelper.FormatAmount((double)item.Volume),
                        item.DisplayPrice,
                        CommonHelper.FormatAmount(item.UplineCommission),
                        CommonHelper.FormatAmount(item.Pnl),
                        item.Comment ?? ""
                    );
                }

                dt.Rows.Add(
                    "GrandTotal",
                    "",
                    "Profit:",
                    CommonHelper.FormatAmount(totalProfit),
                    "Credit:",
                    CommonHelper.FormatAmount(credit),
                    "Balance:",
                    CommonHelper.FormatAmount(balance) + " INR",
                    "",
                    "",
                    "",
                    CommonHelper.FormatAmount(totalComm),
                    CommonHelper.FormatAmount(totalProfit),
                    ""
                );

                var alignments = new Dictionary<string, EnumPdfColumnAlignment>
                {
                    { "Volume", EnumPdfColumnAlignment.Right },
                    { "Comm.",  EnumPdfColumnAlignment.Right },
                    { "Price",  EnumPdfColumnAlignment.Right },
                    { "Profit", EnumPdfColumnAlignment.Right }
                };

                _pdfService.CellFontSize = 7.5f;
                _pdfService.HeaderFontSize = 8f;
                _pdfService.HeaderPadding = 4f;
                _pdfService.CellPadding = 3f;
                _pdfService.ShowVerticalBorders = true;
                _pdfService.ColumnWidths = new Dictionary<string, float>
                {
                    { "Sr",   0.4f },
                    { "Time", 1.6f }
                };

                _pdfService
                    .Clear()
                    .AddSubTitle("History", fontSize: 16, centerAlign: false)
                    .AddSpacing(6)
                    .AddGrid(dt, null, null, alignments)
                    .BuildPDF($"{gridType}_History", landscape: true, autoFormat: true);

                FileLogger.Log("Journal", $"{gridType} history exported to PDF successfully.");
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExportToPdf), ex);
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
                    return;
                }

                var footerRow = data.FirstOrDefault(x => x.RefId == "FOOTER");
                decimal totalProfit = (decimal)(footerRow?.Pnl ?? 0);
                decimal credit = 0, balance = 0;

                if (footerRow?.Comment != null && footerRow.Comment.Contains("Credit:"))
                {
                    var parts = footerRow.Comment.Split(new[] { "Credit:", "Balance:" }, StringSplitOptions.None);

                    if (parts.Length >= 2)
                    {
                        string cleanCredit = parts[1].Replace(" ", "").Replace("\u00A0", "").Replace(",", "").Trim();
                        decimal.TryParse(cleanCredit, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out credit);
                    }
                    if (parts.Length >= 3)
                    {
                        string cleanBalance = parts[2].Replace("INR", "").Replace(" ", "").Replace("\u00A0", "").Replace(",", "").Trim();
                        decimal.TryParse(cleanBalance, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out balance);
                    }
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
                    string displayTime = CommonHelper.ConvertUtcToIst(item.UpdatedAt).ToString("dd/MM/yy HH:mm:ss");
                    string lastOutTime = item.LastOutAt.HasValue
                                        ? CommonHelper.ConvertUtcToIst(item.LastOutAt.Value).ToString("dd/MM/yy HH:mm:ss")
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
                        CommonHelper.FormatAmount(item.Pnl),
                        item.Comment ?? ""
                    );
                }

                dt.Rows.Add(
                    "GrandTotal",
                    "",
                    "Profit:",
                    CommonHelper.FormatAmount(totalProfit),
                    "Credit:",
                    CommonHelper.FormatAmount(credit),
                    "Balance:",
                    CommonHelper.FormatAmount(balance) + " INR",
                    "",
                    "",
                    CommonHelper.FormatAmount(totalProfit),
                    ""
                );

                var alignments = new Dictionary<string, EnumPdfColumnAlignment>
                {
                    { "Profit", EnumPdfColumnAlignment.Right }
                };

                _pdfService.CellFontSize = 7.5f;
                _pdfService.HeaderFontSize = 8f;
                _pdfService.HeaderPadding = 4f;
                _pdfService.CellPadding = 3f;
                _pdfService.ShowVerticalBorders = true;
                _pdfService.ColumnWidths = new Dictionary<string, float>
                {
                    { "Sr",            0.4f },
                    { "Time",          1.6f },
                    { "Last Out Time", 1.6f }
                };

                _pdfService
                    .Clear()
                    .AddSubTitle("Position", fontSize: 16, centerAlign: false)
                    .AddSpacing(6)
                    .AddGrid(dt, null, null, alignments)
                    .BuildPDF("Position_History", landscape: true, autoFormat: true);

                FileLogger.Log("Journal", "Position history exported to PDF successfully.");
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExportPositionToPdf), ex);
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
                try
                {
                    if (message.IsLoggedIn)
                    {
                        if (message.IsDifferentUser)
                        {
                            SafeUIInvoke(() =>
                            {
                                _allHistoryItems?.Clear();
                                _allPositionItems?.Clear();
                                HistoryItems?.Clear();
                                PositionHistoryItems?.Clear();

                                _isDealsRequested = false;
                                _isOrdersRequested = false;
                                _isPositionRequested = false;
                                HasData = false;

                                ClientCredit = 0;
                                ClientBalance = 0;
                            });
                        }

                        await LoadFullDataAsync();
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.ApplicationLog(nameof(RegisterMessenger), ex);
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

                FileLogger.Log("Network", $"{CurrentViewType} history request from {apiFromDate:dd/MM/yyyy} to {apiToDate:dd/MM/yyyy}");

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
                FileLogger.ApplicationLog(nameof(ExecuteRequestAsync), ex);
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
            try
            {
                if (parameter is string modeStr && Enum.TryParse(modeStr, out EnumHistoryType mode))
                {
                    CurrentViewType = mode;

                    _isUpdatingFilters = true;

                    SelectedSymbol = "All";
                    SelectedExecution = "All";
                    SelectedType = "All";
                    SelectedEntry = "All";
                    SelectedPosSymbol = "All";

                    _isUpdatingFilters = false;
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExecuteChangeView), ex);
            }
        }

        /// <summary>
        /// Updates the start and end dates according to the currently selected period.
        /// </summary>
        private void UpdateDatesBasedOnPeriod()
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(UpdateDatesBasedOnPeriod), ex);
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
                    SafeUIInvoke(() =>
                    {
                        try
                        {
                            if (!_isPositionRequested)
                            {
                                PositionHistoryItems = new ObservableCollection<PositionHistoryModel>();
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

                            PositionHistoryItems = new ObservableCollection<PositionHistoryModel>(finalData);
                            HasData = PositionHistoryItems.Any(x => x.RefId != "FOOTER");
                        }
                        finally
                        {
                            _isUpdatingFilters = false;
                        }
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
                else
                {
                    SafeUIInvoke(() =>
                    {
                        try
                        {
                            if (CurrentViewType == EnumHistoryType.Deals && !_isDealsRequested)
                            {
                                HistoryItems = new ObservableCollection<HistoryModel>();
                                HasData = false;
                                return;
                            }

                            if (CurrentViewType == EnumHistoryType.Orders && !_isOrdersRequested)
                            {
                                HistoryItems = new ObservableCollection<HistoryModel>();
                                HasData = false;
                                return;
                            }

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

                            HistoryItems = new ObservableCollection<HistoryModel>(finalData);
                            HasData = HistoryItems.Any(x => x.RefId != "FOOTER");
                        }
                        finally
                        {
                            _isUpdatingFilters = false;
                        }
                    }, System.Windows.Threading.DispatcherPriority.Background);
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ApplyFilters), ex);
                _isUpdatingFilters = false;
            }
        }

        /// <summary>
        /// Updates the available position dropdown options based on current data.
        /// </summary>
        private void UpdatePositionDropdowns(List<PositionHistoryModel> baseData)
        {
            try
            {
                var oldPos = SelectedPosSymbol;
                var newSymbols = new ObservableCollection<string> { "All" };
                foreach (var s in baseData.Where(x => !string.IsNullOrEmpty(x.SymbolName)).Select(x => x.SymbolName).Distinct().OrderBy(x => x)) newSymbols.Add(s);

                SafeUIInvoke(() =>
                {
                    AvailablePosSymbols = newSymbols;
                    _selectedPosSymbol = AvailablePosSymbols.Contains(oldPos) ? oldPos : "All";
                    OnPropertyChanged(nameof(SelectedPosSymbol));
                });
            }
            catch (Exception ex) { FileLogger.ApplicationLog(nameof(UpdatePositionDropdowns), ex); }
        }

        /// <summary>
        /// Updates the available history dropdown options based on current data.
        /// </summary>
        private void UpdateHistoryDropdowns(List<HistoryModel> baseData)
        {
            try
            {
                var oldSym = SelectedSymbol; var oldExec = SelectedExecution; var oldType = SelectedType; var oldEntry = SelectedEntry;

                var newSymbols = new ObservableCollection<string> { "All" };
                foreach (var s in baseData.Where(x => !string.IsNullOrEmpty(x.SymbolName)).Select(x => x.SymbolName).Distinct().OrderBy(x => x)) newSymbols.Add(s);

                var newExecs = new ObservableCollection<string> { "All" };
                foreach (var e in baseData.Where(x => !string.IsNullOrEmpty(x.OrderType)).Select(x => x.OrderType).Distinct().OrderBy(x => x)) newExecs.Add(e);

                var newTypes = new ObservableCollection<string> { "All" };
                foreach (var t in baseData.Where(x => !string.IsNullOrEmpty(x.Side)).Select(x => x.Side).Distinct().OrderBy(x => x)) newTypes.Add(t);

                var newEntries = new ObservableCollection<string> { "All" };
                foreach (var e in baseData.Where(x => !string.IsNullOrEmpty(x.DealType)).Select(x => x.DealType).Distinct().OrderBy(x => x)) newEntries.Add(e);

                SafeUIInvoke(() =>
                {
                    AvailableSymbols = newSymbols;
                    AvailableExecutions = newExecs;
                    AvailableTypes = newTypes;
                    AvailableEntries = newEntries;

                    _selectedSymbol = AvailableSymbols.Contains(oldSym) ? oldSym : "All";
                    _selectedExecution = AvailableExecutions.Contains(oldExec) ? oldExec : "All";
                    _selectedType = AvailableTypes.Contains(oldType) ? oldType : "All";
                    _selectedEntry = AvailableEntries.Contains(oldEntry) ? oldEntry : "All";

                    OnPropertyChanged(nameof(SelectedSymbol)); OnPropertyChanged(nameof(SelectedExecution));
                    OnPropertyChanged(nameof(SelectedType)); OnPropertyChanged(nameof(SelectedEntry));
                });
            }
            catch (Exception ex) { FileLogger.ApplicationLog(nameof(UpdateHistoryDropdowns), ex); }
        }

        /// <summary>
        /// Triggers the appropriate export process based on the current view type.
        /// </summary>
        private void ExportData(bool isExcel)
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExportData), ex);
            }
        }

        #endregion 
    }
}