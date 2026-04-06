using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace ClientDesktop.ViewModel
{
    public class InvoiceViewModel : ViewModelBase, ICloseable
    {
        #region Variables/Properties

        private readonly SessionService _sessionService;
        private readonly InvoiceService _invoiceService;
        private readonly IPdfService _pdfService;
        private readonly ISocketService _socketService;        
        private ObservableCollection<Invoicemodel> _invoiceData;

        public ObservableCollection<Invoicemodel> InvoiceDetails
        {
            get => _invoiceData;
            set { _invoiceData = value; OnPropertyChanged(); }
        }
        
        private bool _isViewLocked;
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

        
        
        private List<(string SecurityName, DataTable Table)> _securityPdfTables
            = new List<(string, DataTable)>();

        private DataTable _summaryPdfTable;
        private DataTable _carryPdfTable;
        private string _pdfTitle;
        private string _pdfSubTitle;

        #endregion Variables/Properties

        #region Constructor
        public InvoiceViewModel(SessionService sessionService,InvoiceService invoiceService,IPdfService pdfService,ISocketService socketService)          
        {
            _sessionService = sessionService;
            _invoiceService = invoiceService;
            _pdfService = pdfService;
            _socketService = socketService;

            _isViewLocked = MainWindowViewModel.isViewLocked;

            _socketService.OnViewLockChanged += OnViewLockChangedHandler;

            if (!_socketService.IsConnected)
            {
                _socketService.Start();
            }
        }

        #endregion Constructor

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

        #region Methods      
        public async Task<bool> VerifyPasswordAsync(string password)
        {

            try
            {
                if (!_sessionService.IsInternetAvailable)
                    return false;

                string clientId = _sessionService.UserId;
                string licenseId = _sessionService.LicenseId;

                var result = await _invoiceService
                    .VerifyUserPasswordAsync(clientId, password, licenseId);

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
        public async Task LoadInvoiceDetailAsync(string fromdate, string todate)
        {
            try
            {
                if (!_sessionService.IsInternetAvailable)
                    return;

                var result = await _invoiceService.InvoiceLoadData(fromdate, todate);

                if (result == null) return;

                InvoiceDetails = new ObservableCollection<Invoicemodel>(result);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadInvoiceDetailAsync), ex.Message);
            }
            
        }     
        public void PreparePdfData(string title,string subTitle,List<(string SecurityName, DataTable Table)> securityTables, DataTable summaryTable,DataTable carryTable)
        {
            _pdfTitle = title;
            _pdfSubTitle = subTitle;
            _securityPdfTables = securityTables ?? new List<(string, DataTable)>();
            _summaryPdfTable = summaryTable;
            _carryPdfTable = carryTable;
        }     
        public void BuildInvoicePdf()
        {
            try
            {
                if (_securityPdfTables == null || _securityPdfTables.Count == 0)
                {
                    FileLogger.ApplicationLog("Export", "Please load invoice data before exporting.");
                    return;
                }

                var securityAlignments = new Dictionary<string, EnumPdfColumnAlignment>
                {
                    ["BVol"] = EnumPdfColumnAlignment.Right,
                    ["SVol"] = EnumPdfColumnAlignment.Right,
                    ["Rate"] = EnumPdfColumnAlignment.Right,
                    ["Comm"] = EnumPdfColumnAlignment.Right,
                    ["Net"] = EnumPdfColumnAlignment.Right
                };

                _pdfService.Clear();

                _pdfService
                    .AddTitle(_pdfTitle, fontSize: 18, centerAlign: true)
                    .AddSubTitle(_pdfSubTitle, fontSize: 13, centerAlign: true);

                foreach (var (securityName, table) in _securityPdfTables)
                {
                    _pdfService
                        .AddGrid(
                            dataTable: table,
                            gridTitle: securityName,
                            footerData: null,
                            columnAlignments: securityAlignments, false)
                        .AddSpacing();
                }

                if (_summaryPdfTable != null && _summaryPdfTable.Rows.Count > 0)
                {
                    var summaryAlignments = new Dictionary<string, EnumPdfColumnAlignment>
                    {
                        ["M2M"] = EnumPdfColumnAlignment.Right,
                        ["Comm"] = EnumPdfColumnAlignment.Right,
                        ["Total"] = EnumPdfColumnAlignment.Right
                    };

                    _pdfService
                        .AddTitle("SUMMARY", fontSize: 14, centerAlign: true)
                        .AddGrid(
                            dataTable: _summaryPdfTable,
                            gridTitle: null,
                            footerData: null,
                            columnAlignments: summaryAlignments, false)
                        .AddSpacing();
                }

                if (_carryPdfTable != null && _carryPdfTable.Rows.Count > 0)
                {
                    var carryAlignments = new Dictionary<string, EnumPdfColumnAlignment>
                    {
                        ["Type"] = EnumPdfColumnAlignment.Right,
                        ["Quantity"] = EnumPdfColumnAlignment.Right,
                        ["Net"] = EnumPdfColumnAlignment.Right
                    };

                    _pdfService
                        .AddTitle("CARRY FORWARD", fontSize: 14, centerAlign: true)
                        .AddGrid(
                            dataTable: _carryPdfTable,
                            gridTitle: null,
                            footerData: null,
                            columnAlignments: carryAlignments, false)
                        .AddSpacing();
                }
                string baseFileName = _sessionService.UserId + "_Invoice";
                _pdfService.BuildPDF(baseFileName, landscape: false, autoFormat: true);
            }
            catch(Exception ex)
            {
                FileLogger.ApplicationLog(nameof(BuildInvoicePdf),ex.Message);
            }
        }

        #endregion Methods

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #endregion INotifyPropertyChanged

    }
}