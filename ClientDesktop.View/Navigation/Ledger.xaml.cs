using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.ViewModel;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace ClientDesktop.View.Navigation
{
    /// <summary>
    /// Interaction logic for Ledger.xaml
    /// </summary>
    public partial class Ledger : UserControl
    {
        #region Variables

        public static decimal LblAmount = decimal.Zero;
        public static string LblAmountFormatted;
        Label lblNoData = new Label();

        // New: Private fields for DI
        private readonly LedgerViewModel _viewModel;
        private readonly SessionService _sessionService;

        #endregion Variables

        #region Constructor
        public Ledger()
        {
            InitializeComponent();
            DgvLedgerRecord.RowHeight = 25;
            Dtpstartdate.SelectedDate = DateTime.Today;
            Dtpenddate.SelectedDate = DateTime.Today;
            Btngo.IsEnabled = false;

            // FIX: Get ViewModel & SessionService from DI
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _sessionService = AppServiceLocator.GetService<SessionService>();
                _viewModel = AppServiceLocator.GetService<LedgerViewModel>();

                // Set DataContext once
                this.DataContext = _viewModel;
            }
        }

        #endregion Constructor

        #region Events
        private void TxtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(TxtPassword.Password))
            {
                Btngo.IsEnabled = true;
            }
            else
            {
                Btngo.IsEnabled = false;
            }
        }

        private async void Btngo_Click(object sender, RoutedEventArgs e)
        {
            Btngo.IsEnabled = false;

            // OLD: var vm = new LedgerViewModel();
            // NEW: Use injected _viewModel

            bool isValid = await _viewModel.VerifyPasswordAsync(TxtPassword.Password);
            if (isValid)
            {
                TxtNoData.Visibility = Visibility.Visible;
                Mainpanel.Visibility = Visibility.Collapsed;
                ChildPanel.Visibility = Visibility.Visible;
                Lblprintamount.Visibility = Visibility.Visible;
                Lblprintamount.Text = LblAmountFormatted?.ToString(); // Added null check
                DgvLedgerRecord.ColumnHeaderHeight = 35;
                DgvLedgerRecord.Columns.Clear();
                DgvLedgerRecord.Items.Clear();

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
                    Width = 200,
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

        private async void Btngetdata_Click(object sender, RoutedEventArgs e)
        {
            DgvLedgerRecord.Columns.Clear();
            DgvLedgerRecord.Items.Clear();

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
                Width = 200,
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

            // OLD: var viewmodels = new LedgerViewModel();
            // NEW: Use injected _viewModel

            if (Dtpstartdate.SelectedDate.HasValue && Dtpenddate.SelectedDate.HasValue)
            {
                // Logic preserved, using injected services
                var (success, error, ledgerData) = await _viewModel.LoadLedgerListAsync(
                    _sessionService.UserId,
                    Dtpstartdate.SelectedDate.Value.Date,
                    Dtpenddate.SelectedDate.Value.Date,
                    _sessionService.LicenseId
                );

                if (!success || ledgerData == null)
                {
                    TxtNoData.Visibility = Visibility.Visible;
                    return;
                }

                bool hasTransactions = ledgerData.Transactions != null && ledgerData.Transactions.Count > 0;

                // Add Opening Amount record
                if (hasTransactions)
                {
                    dynamic record = new ExpandoObject();
                    record.Sr = "";
                    record.Date = "Opening Amount";
                    record.Type = "";
                    record.Amount = ledgerData.OpeningAmount;
                    record.Remarks = "";

                    DgvLedgerRecord.Items.Add(record);
                }

                // Add transaction records
                int sr = 1;
                if (hasTransactions)
                {
                    foreach (var led in ledgerData.Transactions)
                    {
                        DateTime istTime = CommonHelper.ConvertUtcToIst(led.LedgerDate);
                        string displayTime = istTime.ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture);

                        dynamic record = new ExpandoObject();
                        record.Sr = sr.ToString();
                        record.Date = displayTime;
                        record.Type = led.TransactionType;
                        record.Amount = led.Amount;
                        record.Remarks = led.Remarks;
                        DgvLedgerRecord.Items.Add(record);
                        sr++;
                    }
                }
                // Add Closing Amount record
                if (hasTransactions)
                {
                    dynamic record = new ExpandoObject();
                    record.Sr = "";
                    record.Date = "Closing Amount";
                    record.Type = "";
                    record.Amount = ledgerData.ClosingAmount;
                    record.Remarks = "";
                    DgvLedgerRecord.Items.Add(record);
                }
                TxtNoData.Visibility = hasTransactions ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;

            // Using existing instance instead of creating new
            await _viewModel.LoadLedgerUserDetailAsync();

            if (_viewModel.LedgerUser != null)
            {
                LblAmount = _viewModel.LedgerUser.Amount;
                LblAmountFormatted = LblAmount.ToString("0.################");
            }
        }

        #endregion Events
    }
}
