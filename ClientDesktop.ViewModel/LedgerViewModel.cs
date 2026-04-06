using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ClientDesktop.ViewModel
{
    public class LedgerViewModel : ViewModelBase, ICloseable
    {
        #region Fields

        private readonly SessionService _sessionService;
        private readonly LedgerService _ledgerService;
        private readonly IPdfService _pdfService;
        private readonly IExcelService _excelService;
        private readonly ISocketService _socketService;

        private LedgerUserDetail _ledgerUser;
        private bool _isBusy;
        private bool _isViewLocked;

        #endregion Fields

        #region Constructor      
        public LedgerViewModel(SessionService sessionService,LedgerService ledgerService,IPdfService pdfService,IExcelService excelService,ISocketService socketService)           
        {
            _sessionService = sessionService;
            _ledgerService = ledgerService;
            _pdfService = pdfService;
            _excelService = excelService;
            _socketService = socketService;

            GridRows = new ObservableCollection<LedgerRowModel>();

            _isViewLocked = MainWindowViewModel.isViewLocked;

            _socketService.OnViewLockChanged += OnViewLockChangedHandler;

            if (!_socketService.IsConnected)
                _socketService.Start();
        }

        #endregion Constructor

        #region Properties

        public ObservableCollection<LedgerRowModel> GridRows { get; }
        public LedgerUserDetail LedgerUser
        {
            get => _ledgerUser;
            set { _ledgerUser = value; OnPropertyChanged(); }
        }        
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public bool IsViewLocked
        {
            get => _isViewLocked;
            set
            {
                if (_isViewLocked == value) return;
                _isViewLocked = value;
                OnPropertyChanged();
            }
        }

        #endregion Properties

        #region WebSocket Handler  
        private void OnViewLockChangedHandler(bool isLocked)
        {
            Application.Current?.Dispatcher.Invoke(() =>
            {
                IsViewLocked = isLocked;
            });
        }

        public void Cleanup()
        {
            _socketService.OnViewLockChanged -= OnViewLockChangedHandler;
        }

        #endregion WebSocket Handler  

        #region Auth & User Detail

        public async Task<bool> VerifyPasswordAsync(string password)
        {
            try
            {
                if (!_sessionService.IsInternetAvailable)
                    return false;

                var result = await _ledgerService.VerifyUserPasswordAsync(
                    _sessionService.UserId, password, _sessionService.LicenseId);

                if (!result.Success)
                {
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(VerifyPasswordAsync), ex.Message);
                return false;
            }
            
        }

        public async Task LoadLedgerUserDetailAsync()
        {
            try
            {
                if (!_sessionService.IsInternetAvailable)
                    return;

                var result = await _ledgerService.GetLedgerUserDetail();
                if (result != null)
                    LedgerUser = result;
            }
            catch(Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadLedgerUserDetailAsync), ex.Message);
            }
        }

        #endregion Auth & User Detail

        #region Grid Population

        public async Task<bool> LoadAndPopulateGridAsync(DateTime fromDate,DateTime toDate)
        {
            if (!_sessionService.IsInternetAvailable)
                return false;

            IsBusy = true;
            GridRows.Clear();

            try
            {
                var (success, error, ledgerData) = await _ledgerService.GetLedgerListAsync(
                    _sessionService.UserId, fromDate, toDate, _sessionService.LicenseId);

                if (!success || ledgerData == null)
                {
                    MessageBox.Show(error ?? "Failed to load ledger.",
                        "Ledger Load Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                    return false;
                }

                bool hasTransactions =
                    ledgerData.Transactions != null && ledgerData.Transactions.Count > 0;

                if (!hasTransactions)
                    return false;  
                
                GridRows.Add(new LedgerRowModel
                {
                    Sr = "",
                    Date = "Opening Amount",
                    Type = "",
                    Amount = CommonHelper.FormatAmount(ledgerData.OpeningAmount),
                    Remarks = "",
                    IsSummaryRow = true
                });
               
                int sr = 1;
                foreach (var led in ledgerData.Transactions)
                {
                    DateTime istTime = CommonHelper.ConvertUtcToIst(led.LedgerDate);
                    string displayTime = istTime.ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture);

                    GridRows.Add(new LedgerRowModel
                    {
                        Sr = sr.ToString(),
                        Date = displayTime,
                        Type = led.TransactionType,
                        Amount = CommonHelper.FormatAmount(led.Amount),
                        Remarks = led.Remarks ?? ""
                    });
                    sr++;
                }
               
                GridRows.Add(new LedgerRowModel
                {
                    Sr = "",
                    Date = "Closing Amount",
                    Type = "",
                    Amount = CommonHelper.FormatAmount(ledgerData.ClosingAmount),
                    Remarks = "",
                    IsSummaryRow = true
                });

                return true;
            }
            catch(Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadAndPopulateGridAsync), ex.Message);
                return false;
            }
            finally
            {
                IsBusy = false;
            }
        }

        #endregion Grid Population

        #region PDF Export     
        public void ExportToPdf(DateTime fromDate, DateTime toDate)
        {          
            if (GridRows == null || GridRows.Count == 0)
            {
                FileLogger.ApplicationLog("Export", "No data available to export");
                return;
            }

            try
            {                
                DataTable dt = BuildPdfDataTable();
              
                var columnAlignments = new Dictionary<string, EnumPdfColumnAlignment>
                {
                    ["Amount"] = EnumPdfColumnAlignment.Right
                };
              
                string title =
                    $"Ledger History Report for User Id: {_sessionService.UserId} (Client)" +
                    $"   From: {fromDate:dd-MM-yyyy}   To: {toDate:dd-MM-yyyy}";
               
                _pdfService
                    .Clear()
                    .AddSubTitle(title, fontSize: 14, centerAlign: true)
                    .AddGrid(
                        dataTable: dt,
                        columnAlignments: columnAlignments)
                    .BuildPDF("Ledger_History", landscape: true, autoFormat: true);              
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExportToPdf), ex.Message);
            }
        }      
        private DataTable BuildPdfDataTable()
        {
            var dt = new DataTable("LedgerHistory");
            dt.Columns.Add("Sr", typeof(string));
            dt.Columns.Add("Date", typeof(string));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Amount", typeof(string));
            dt.Columns.Add("Remarks", typeof(string));

            foreach (var row in GridRows)
            {
                string srVal = row.Sr;
                string dateVal = row.Date;
                
                if (row.IsSummaryRow)
                {
                    if (dateVal.Contains("Opening Amount"))
                    {
                        srVal = "Opening Balance";
                        dateVal = string.Empty;
                    }
                    else if (dateVal.Contains("Closing Amount"))
                    {
                        srVal = "Closing Balance";
                        dateVal = string.Empty;
                    }
                }

                dt.Rows.Add(srVal, dateVal, row.Type, row.Amount, row.Remarks);
            }

            return dt;
        }

        #endregion PDF Export

        #region Excel Export

        public void ExportToExcel(DateTime fromDate, DateTime toDate)
        {
            if (GridRows == null || GridRows.Count == 0)
            {
                FileLogger.ApplicationLog("Export", "No data available to export");
                return;
            }

            try
            {              
                DataTable dt = BuildExportDataTable();
                
                string title =
                    $"Ledger History Report for User Id:{_sessionService.UserId}(Client)" +
                    $"  From: {fromDate:dd-MM-yyyy} To: {toDate:dd-MM-yyyy}";

                var columnAlignments = new Dictionary<string, EnumExcelColumnAlignment>
                {
                    ["Amount"] = EnumExcelColumnAlignment.Right
                };

                _excelService
                    .Clear()
                    .AddSheet(dt, title, sheetName: "Ledger", columnAlignments: columnAlignments)
                    .SaveExcel("LedgerHistory");
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExportToExcel), ex.Message);
            }
        }

        #endregion Excel Export

        #region Shared DataTable Builder       
        private DataTable BuildExportDataTable()
        {
            var dt = new DataTable("LedgerHistory");
            dt.Columns.Add("Sr", typeof(string));
            dt.Columns.Add("Date", typeof(string));
            dt.Columns.Add("Type", typeof(string));
            dt.Columns.Add("Amount", typeof(string));
            dt.Columns.Add("Remarks", typeof(string));

            foreach (var row in GridRows)
            {
                string srVal = row.Sr;
                string dateVal = row.Date;

                if (row.IsSummaryRow)
                {
                    if (dateVal.Contains("Opening Amount"))
                    {
                        dateVal = "Opening Balance";
                        srVal = string.Empty;
                    }
                    else if (dateVal.Contains("Closing Amount"))
                    {
                        dateVal = "Closing Balance";
                        srVal = string.Empty;
                    }
                }

                dt.Rows.Add(srVal, dateVal, row.Type, row.Amount, row.Remarks);
            }

            return dt;
        }

        #endregion Shared DataTable Builder

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion INotifyPropertyChanged
    }
}