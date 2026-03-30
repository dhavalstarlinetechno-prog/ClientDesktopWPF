using ClientDesktop.Core.Config;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.ViewModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Xml;

namespace ClientDesktop.View.Navigation
{
  
    public partial class Ledger : UserControl
    {
        #region Fields

        private readonly LedgerViewModel _viewModel;
        private readonly SessionService _sessionService;
        public static string LblAmountFormatted;    
        private DateTime _currentFromDate = DateTime.Today;
        private DateTime _currentToDate = DateTime.Today;

        #endregion

        #region Constructor

        public Ledger()
        {
            InitializeComponent();

            Dtpstartdate.SelectedDate = DateTime.Today;
            Dtpenddate.SelectedDate = DateTime.Today;
            Btngo.IsEnabled = false;

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _sessionService = AppServiceLocator.GetService<SessionService>();
                _viewModel = AppServiceLocator.GetService<LedgerViewModel>();
                
                DgvLedgerRecord.ItemsSource = _viewModel.GridRows;

                this.DataContext = _viewModel;

                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            this.Loaded += Ledger_Loaded;

            this.Unloaded += Ledger_Unloaded;


            //if (MainWindowViewModel.isViewLocked == true)
            //{
            //    Lbldetails.Text = CommonMessages.InvoiceLedgerWrongPassword;
            //    Lbldetails.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            //    Lbldetails.Foreground = System.Windows.Media.Brushes.Red;
            //    Lbldetails.Margin = new System.Windows.Thickness(0, 10, 250, 0);
            //    TxtPassword.Visibility = System.Windows.Visibility.Collapsed;
            //    Btngo.Visibility = System.Windows.Visibility.Collapsed;
            //}
        }

        #endregion

        #region Loaded / Unloaded

        private void Ledger_Loaded(object sender, RoutedEventArgs e)
        {
            // ✅ Apply initial lock state once View is fully rendered
            ApplyViewLockUI(_viewModel?.IsViewLocked ?? MainWindowViewModel.isViewLocked);
        }

        private void Ledger_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _viewModel.Cleanup();   // unsubscribe socket event — no memory leak
            }
        }

        #endregion

        #region WebSocket — Real-time View Lock

        // ✅ Called whenever CLIENT_UPDATE fires isViewLocked via WebSocket
        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(LedgerViewModel.IsViewLocked))
            {
                ApplyViewLockUI(_viewModel.IsViewLocked);
            }
        }

        private void ApplyViewLockUI(bool isLocked)
        {
            if (isLocked)
            {
                Lbldetails.Text = CommonMessages.InvoiceLedgerWrongPassword;
                Lbldetails.FontFamily = new System.Windows.Media.FontFamily("Microsoft Sans Serif");
                Lbldetails.Foreground = System.Windows.Media.Brushes.Red;
                Lbldetails.Margin = new System.Windows.Thickness(0, 10, 250, 0);
                TxtPassword.Visibility = System.Windows.Visibility.Collapsed;
                Btngo.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                // ✅ Restore normal UI when unlocked via socket
                Lbldetails.Text = "This report represents sample ledger format. It contains sample data only for education purpose. Ledger can be displayed in below structure.";
                Lbldetails.FontFamily = new System.Windows.Media.FontFamily("Microsoft Sans Serif");
                Lbldetails.Foreground = System.Windows.Media.Brushes.Black;
                Lbldetails.Margin = new System.Windows.Thickness(0);
                TxtPassword.Visibility = System.Windows.Visibility.Visible;
                Btngo.Visibility = System.Windows.Visibility.Visible;
                Btngo.IsEnabled = !string.IsNullOrEmpty(TxtPassword.Password);
            }
        }

        #endregion


        #region Loaded(existing)

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_sessionService.IsLoggedIn || !_sessionService.IsInternetAvailable)
            {
                Window.GetWindow(this)?.Close();
                return;
            }

            if (_viewModel == null) return;

            await _viewModel.LoadLedgerUserDetailAsync();

            if (_viewModel.LedgerUser != null)
            {
                decimal amount = _viewModel.LedgerUser.Amount;
                Lblprintamount.Text = amount.ToString("0.################");
            }
        }

        #endregion

        #region Password & Auth

        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            Btngo.IsEnabled = !string.IsNullOrEmpty(TxtPassword.Password);
        }

        private async void Btngo_Click(object sender, RoutedEventArgs e)
        {
            Btngo.IsEnabled = false;

            bool isValid = await _viewModel.VerifyPasswordAsync(TxtPassword.Password);

            if (isValid)
            {
                TxtNoData.Visibility = Visibility.Visible;
                Mainpanel.Visibility = Visibility.Collapsed;
                ChildPanel.Visibility = Visibility.Visible;
                Lblprintamount.Visibility = Visibility.Visible;
                Lblprintamount.Text = LblAmountFormatted?.ToString(); // Added null check
                DgvLedgerRecord.ColumnHeaderHeight = 35;
                _viewModel.GridRows.Clear();

                // Add Columns
                DgvLedgerRecord.Columns.Add(new DataGridTextColumn
                {
                    Header = "Sr",
                    Width = 40,
                    Binding = new Binding("Sr")
                });

                DgvLedgerRecord.Columns.Add(new DataGridTextColumn
                {
                    Header = "Date",
                    Width = 240,
                    Binding = new Binding("Date")
                });

                DgvLedgerRecord.Columns.Add(new DataGridTextColumn
                {
                    Header = "Type",
                    Width = 232,
                    Binding = new Binding("Type")
                });

                DgvLedgerRecord.Columns.Add(new DataGridTextColumn
                {
                    Header = "Amount",
                    Width = 240,
                    Binding = new Binding("Amount"),
                    ElementStyle = new Style(typeof(TextBlock))
                    {
                        Setters =
                        {
                            new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right)
                        }
                    }
                });

                DgvLedgerRecord.Columns.Add(new DataGridTextColumn
                {
                    Header = "Remarks",
                    Width = 240,
                    Binding = new Binding("Remarks")
                });
            }
            else
            {
                Btngo.IsEnabled = true;
            }
        }

        #endregion

        #region Get Data

        private async void Btngetdata_Click(object sender, RoutedEventArgs e)
        {
            DgvLedgerRecord.Columns.Clear();
            //DgvLedgerRecord.Items.Clear();

            _viewModel.GridRows.Clear();

            // Add Columns (Ideally this duplication should be refactored, but keeping logic as requested)
            DgvLedgerRecord.Columns.Add(new DataGridTextColumn
            {
                Header = "Sr",
                Width = 40,
                Binding = new Binding("Sr")
            });

            DgvLedgerRecord.Columns.Add(new DataGridTextColumn
            {
                Header = "Date",
                Width = 240,
                Binding = new Binding("Date")
            });

            DgvLedgerRecord.Columns.Add(new DataGridTextColumn
            {
                Header = "Type",
                Width = 215,
                Binding = new Binding("Type")
            });

            DgvLedgerRecord.Columns.Add(new DataGridTextColumn
            {
                Header = "Amount",
                Width = 240,
                Binding = new Binding("Amount"),
                ElementStyle = new Style(typeof(TextBlock))
                {
                    Setters =
                    {
                        new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right)
                    }
                }
            });

            DgvLedgerRecord.Columns.Add(new DataGridTextColumn
            {
                Header = "Remarks",
                Width = 240,
                Binding = new Binding("Remarks")
            });

            if (!Dtpstartdate.SelectedDate.HasValue || !Dtpenddate.SelectedDate.HasValue)
                return;

            // Cache for PdfExport_Click
            _currentFromDate = Dtpstartdate.SelectedDate.Value.Date;
            _currentToDate = Dtpenddate.SelectedDate.Value.Date;

            // ViewModel fetches data and fills GridRows (DataGrid auto-updates via binding)
            bool hasData = await _viewModel.LoadAndPopulateGridAsync(
                _currentFromDate,
                _currentToDate);

            // Show / hide icons and "No Data" label
            TxtNoData.Visibility = hasData ? Visibility.Collapsed : Visibility.Visible;
            PdfExportBtn.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
            ExcelExportBtn.Visibility = hasData ? Visibility.Visible : Visibility.Collapsed;
        }

        #endregion

        #region Export — PDF

        /// <summary>
        /// PDF export: fully delegated to ViewModel.
        /// Code-behind only passes the date range (UI values not known to VM).
        /// </summary>
        private void PdfExportBtn_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportToPdf(_currentFromDate, _currentToDate);
            FileLogger.Log("Export", "PDF Generate Successfully.");
        }

        #endregion

        #region Export — Excel
        private void ExcelExportBtn_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ExportToExcel(_currentFromDate, _currentToDate);
            FileLogger.Log("Export", "Excel Generate Successfully.");
        }

        #endregion

    }
}