using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.ViewModel;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ClientDesktop.View.Navigation
{
    /// <summary>
    /// Interaction logic for Ledger.xaml
    /// </summary>
    /// 
    public partial class Ledger : UserControl
    {
        #region Variables

        public static decimal LblAmount = decimal.Zero;
        public static string LblAmountFormatted;
        Label lblNoData = new Label();

        #endregion Variables

        #region Constructor
        public Ledger()
        {
            InitializeComponent();
            DgvLedgerRecord.RowHeight = 25;
            Dtpstartdate.SelectedDate = DateTime.Today;
            Dtpenddate.SelectedDate = DateTime.Today;
            Btngo.IsEnabled = false;                    
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
            var vm = new LedgerViewModel();
            DataContext = vm;

            bool isValid = await vm.VerifyPasswordAsync(TxtPassword.Password);
            if (isValid)
            {
                TxtNoData.Visibility = Visibility.Visible;
                Mainpanel.Visibility = Visibility.Collapsed;
                ChildPanel.Visibility = Visibility.Visible;
                Lblprintamount.Visibility = Visibility.Visible;
                Lblprintamount.Text = LblAmountFormatted.ToString();
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
            var viewmodels = new LedgerViewModel();

            if (Dtpstartdate.SelectedDate.HasValue && Dtpenddate.SelectedDate.HasValue)
            {
                var (success, error, ledgerData) = await viewmodels.LoadLedgerListAsync(SessionManager.UserId, Dtpstartdate.SelectedDate.Value.Date, Dtpenddate.SelectedDate.Value.Date, SessionManager.LicenseId);
                
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
                        DateTime istTime = GMTTime.ConvertUtcToIst(led.LedgerDate);
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
                TxtNoData.Visibility = hasTransactions? Visibility.Collapsed: Visibility.Visible;
            }
        }

        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            var viewmodels = new LedgerViewModel();
            await viewmodels.LoadLedgerUserDetailAsync();
            if (viewmodels.LedgerUser != null)
            {
                LblAmount = viewmodels.LedgerUser.Amount;
                LblAmountFormatted = LblAmount.ToString("0.################");
            }
        }
        
        #endregion Events

        
    }
}
