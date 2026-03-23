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
            }            
        }

        #endregion

        #region Loaded

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
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