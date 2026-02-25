using ClientDesktop.Core.Config;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.ViewModel;
using DocumentFormat.OpenXml.Drawing;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
    /// Interaction logic for InvoiceView.xaml
    /// </summary>
    public partial class InvoiceView : UserControl
    {
        private readonly InvoiceViewModel _viewModel;
        private readonly SessionService _sessionService;
        bool isDataLoaded = false;       
        DateTime today = DateTime.Today;
        DateTime thisWeekStart = new DateTime();
        DateTime thisWeekEnd = new DateTime();
        public static string fromdatefilter = string.Empty;
        public static string todatefilter = string.Empty;
        private PDFBuilder _pdfBuilder = new PDFBuilder();
        Invoicemodel invoice = new Invoicemodel();
        public InvoiceView()
        {
            InitializeComponent();
            Childpanel.Visibility = Visibility.Hidden;
            Btngo.IsEnabled = false;
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _sessionService = AppServiceLocator.GetService<SessionService>();
                _viewModel = AppServiceLocator.GetService<InvoiceViewModel>();

                // Set DataContext once
                this.DataContext = _viewModel;
            }
        }

        private async void Btngo_Click(object sender, RoutedEventArgs e)
        {
            Btngo.IsEnabled = false;
            if (Childpanel.Visibility == Visibility.Hidden)                
            {
                bool isValid = await _viewModel.VerifyPasswordAsync(TxtPassword.Password);
                if (isValid)
                {
                    Childpanel.Visibility = Visibility.Visible;
                    isDataLoaded = true;
                    if (Cmbselectweek.SelectedItem != null)
                    {
                        Gridpanel.Visibility = Visibility.Visible;
                        Btngetdata.IsEnabled = true;
                    }
                    else
                    {
                        Gridpanel.Visibility = Visibility.Hidden;
                        Btngetdata.IsEnabled = true;
                    }
                    if (Cmbselectweek.SelectedItem is ComboBoxItem item && item.Content.ToString() == "Select")
                    {
                        Btngetdata.IsEnabled = false;
                        Lblfrom.Visibility = Visibility.Hidden;
                        Lblfromdate.Content = CommonMessages.NoDataAvailable;
                        Lblfromdate.Visibility = Visibility.Visible;
                    }
                }
                else
                {
                    Btngo.IsEnabled = true;
                }
            }
        }

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

        private void Cmbselectweek_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Btngetdata == null) return;
            ComboBoxItem selectedItem = Cmbselectweek.SelectedItem as ComboBoxItem;
            if (selectedItem == null) return;
            string selection = selectedItem.Content.ToString();

            if (!isDataLoaded)
            {
                Lblfromdate.Content = "No Data Avaliable";
                Lblfromdate.FontWeight = FontWeights.Bold;
                Lblfrom.Visibility = Visibility.Collapsed;
                Lblfromdate.Visibility = Visibility.Visible;
            }
            else { Lblfromdate.Visibility = Visibility.Collapsed; }

            int diff = (7 + ((int)today.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
            switch (selection)
            {
                case "Current week":
                    Btngetdata.IsEnabled = true;
                    thisWeekStart = today.AddDays(-diff);
                    thisWeekEnd = thisWeekStart.AddDays(5);
                    break;
                case "Previous Week":
                    Btngetdata.IsEnabled = true;
                    thisWeekStart = today.AddDays(-1 * diff).AddDays(-7);
                    thisWeekEnd = thisWeekStart.AddDays(5);
                    break;
                case "Last Previous Week":
                    Btngetdata.IsEnabled = true;
                    thisWeekStart = today.AddDays(-diff).AddDays(-14);
                    thisWeekEnd = thisWeekStart.AddDays(5);
                    break;
                default:
                    Btngetdata.IsEnabled = false;
                    break;
            }
            Lblfrom.Content = $"From {thisWeekStart:dd-MM-yyyy} To {thisWeekEnd:dd-MM-yyyy}";
            fromdatefilter = thisWeekStart.ToString("yyyy-MM-dd");
            todatefilter = thisWeekEnd.ToString("yyyy-MM-dd");
        }

        private async void Btngetdata_Click(object sender, RoutedEventArgs e)
        {
            if (Cmbselectweek.SelectedIndex == 0)
            {
                Gridpanel.Visibility = Visibility.Collapsed;
                Btngetdata.IsEnabled = true;
                return;
            }

            GridpanelContent.Children.Clear();
            Gridpanel.Visibility = Visibility.Visible;
            Btngetdata.IsEnabled = false;

            Lblfrom.Content = "";
            Lblfrom.Visibility = Visibility.Visible;
            Lblfromdate.Visibility = Visibility.Collapsed;
            DateTime today = DateTime.Today;

            if (_viewModel == null) return;
            await _viewModel.LoadInvoiceDetailAsync(fromdatefilter, todatefilter);
            

            if (_viewModel.InvoiceDetails != null)
            {
                 List<string> Securities = _viewModel.InvoiceDetails
                .Select(s => s.SecurityName)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

                if (Securities == null || Securities.Count == 0)
                {
                    Lblfrom.Visibility = Visibility.Collapsed;
                    Lblfromdate.Visibility = Visibility.Visible;
                    Lblfromdate.Content = "No Data Available";
                    Btngetdata.IsEnabled = true;
                    return;
                }
                if (Cmbselectweek.Text == "Current week")
                {
                    int diff = (7 + ((int)today.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
                    thisWeekStart = today.AddDays(-diff);
                    thisWeekEnd = thisWeekStart.AddDays(5);
                    Lblfrom.Content = $"From {thisWeekStart:dd-MM-yyyy} To {thisWeekEnd:dd-MM-yyyy}";
                }
                else if (Cmbselectweek.Text == "Previous Week")
                {
                    int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                    thisWeekStart = today.AddDays(-1 * diff);
                    thisWeekStart = thisWeekStart.AddDays(-7);
                    thisWeekEnd = thisWeekStart.AddDays(5);
                    Lblfrom.Content = $"From {thisWeekStart:dd-MM-yyyy} To {thisWeekEnd:dd-MM-yyyy}";
                }
                else if (Cmbselectweek.Text == "Last Previous Week")
                {
                    int diff = (7 + ((int)today.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
                    thisWeekStart = today.AddDays(-diff);
                    thisWeekStart = thisWeekStart.AddDays(-14);
                    thisWeekEnd = thisWeekStart.AddDays(5);
                    Lblfrom.Content = $"From {thisWeekStart:dd-MM-yyyy} To {thisWeekEnd:dd-MM-yyyy}";
                }
                else
                {
                    Btngetdata.IsEnabled = false;
                    Lblfrom.Content = "";
                }

                var invoiceData = _viewModel.InvoiceDetails;

                if (invoiceData == null || invoiceData.Count == 0)
                {
                    Btngetdata.IsEnabled = true;
                    return;
                }
                DataTable securityTable = ToDataTable<Invoicemodel>(_viewModel.InvoiceDetails.ToList());

                #region SecurityGrid

                foreach (var security in Securities)
                {
                    TextBlock lbl = new TextBlock
                    {
                        Text = security,
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 15, 0, 5),
                        FontSize = 14
                    };
                    GridpanelContent.Children.Add(lbl);

                    DataGrid dg = CreateBaseGrid();
                    string[] RightSecurityCols = { "B Vol", "S Vol", "Rate", "Comm", "Net" };

                    foreach (var col in RightSecurityCols)
                    {
                        var column = dg.Columns
                               .FirstOrDefault(c => c.Header?.ToString() == col);

                        if (column is DataGridTextColumn textColumn)
                        {
                            Style cellStyle = new Style(typeof(TextBlock));
                            cellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                            cellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
                            textColumn.ElementStyle = cellStyle;

                            Style headerStyle = new Style(typeof(DataGridColumnHeader));
                            headerStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Right));
                            column.HeaderStyle = headerStyle;
                        }
                    }
                    var symbols = securityTable.AsEnumerable()
                                .Where(r => r.Field<string>("securityName") == security)
                                .Select(r => r.Field<string>("symbolName"))
                                .Where(s => !string.IsNullOrEmpty(s))
                                .Distinct()
                                .OrderBy(s => s)
                                .ToList();

                    DataTable dtSecurity = new DataTable();
                    dtSecurity.Columns.Add("Date", typeof(string));
                    dtSecurity.Columns.Add("Type", typeof(string));
                    dtSecurity.Columns.Add("BVol", typeof(string));
                    dtSecurity.Columns.Add("SVol", typeof(string));
                    dtSecurity.Columns.Add("Rate", typeof(string));
                    dtSecurity.Columns.Add("Comm", typeof(string));
                    dtSecurity.Columns.Add("Net", typeof(string));
                    dtSecurity.Columns.Add("IsHeader", typeof(bool));
                    dtSecurity.Columns.Add("IsTotal", typeof(bool));
                    dtSecurity.Columns.Add("RowColor", typeof(string));

                    foreach (var symbol in symbols)
                    {
                        dtSecurity.Rows.Add(symbol, "", "", "", "", "", "", true, false, "Header");

                        var symbolRows = securityTable.AsEnumerable()
                                    .Where(r => r.Field<string>("symbolName") == symbol &&
                                              r.Field<string>("securityName") == security)
                                    .OrderBy(r => r.Field<string>("symbolName"))
                                    .ThenBy(r => r.Field<DateTime>("dealCreatedOn"))
                                    .ToList();

                      
                        foreach (var row in symbolRows)
                        {
                            string side = row["Side"]?.ToString();
                            invoice.Volume = Convert.ToDouble(row["Volume"]);
                            string bVol = side == "Buy" ? invoice.Volume.ToString() : "-";
                            string sVol = side == "Sell" ? invoice.Volume.ToString() : "-";
                            invoice.Price = double.TryParse(row["price"]?.ToString(), out var dRate) ? dRate : 0.00;
                            invoice.UplineCommission = double.TryParse(row["uplineCommission"]?.ToString(), out var dComm) ? dComm : 0.00;
                            invoice.Pnl = double.TryParse(row["pnl"]?.ToString(), out var dNet) ? dNet : 0.00;
                            DateTime utcTime = DateTime.Parse(row["dealCreatedOn"]?.ToString());
                            DateTime istTime = CommonHelper.ConvertUtcToIst(utcTime);
                            invoice.DealCreatedOn = Convert.ToDateTime(istTime.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture));

                            invoice.Side = (row["OrderType"]?.ToString() == "Market" &&
                                            row["Reason"]?.ToString() == "RollOver" &&
                                            row["DealType"]?.ToString() == "IN") ? "CF" : side;

                            dtSecurity.Rows.Add(
                            invoice.DealCreatedOn,
                            invoice.Side,
                            bVol,
                            sVol,
                            CommonHelper.FormatAmount(invoice.Price),
                            CommonHelper.FormatAmount(invoice.UplineCommission),
                            CommonHelper.FormatAmount(invoice.Pnl),
                            false, false, "Normal");
                        }
                        double totalPnl = symbolRows.Sum(r => Convert.ToDouble(r["pnl"]));
                        double totalComm = symbolRows.Sum(r => Convert.ToDouble(r["uplineCommission"]));
                        dtSecurity.Rows.Add(
                            "", "", "", "", "",
                            CommonHelper.FormatAmount(totalComm),
                            CommonHelper.FormatAmount(totalPnl),
                            false, false, "Summary"
                        );
                        dtSecurity.Rows.Add(
                            "", "",
                            "", "",
                            "", "Total",
                            CommonHelper.FormatAmount(totalPnl + totalComm),
                            false, true, "Total"
                        );
                        ApplyGridStyle(dg, "default");

                        dg.ItemsSource = null;

                        var gridData = new List<SecurityRow>();

                        foreach (DataRow dr in dtSecurity.Rows)
                        {
                            bool isHeader = Convert.ToBoolean(dr["IsHeader"]);
                            bool isTotal = Convert.ToBoolean(dr["IsTotal"]);
                            string rowColor = dr["RowColor"].ToString();

                            dg.LoadingRow += (s, ess) =>
                            {
                                var row = ess.Row.Item as SecurityRow;

                                if (row != null && row.IsHeader)
                                {
                                    ess.Row.Foreground = Brushes.Black;
                                    var color = (Color)ColorConverter.ConvertFromString("#EFECC8");
                                    ess.Row.Background = new SolidColorBrush(color);
                                    ess.Row.FontWeight = FontWeights.Bold;
                                }
                                else if (row.IsTotal)
                                {
                                    ess.Row.FontWeight = FontWeights.Regular;
                                    ess.Row.Background = Brushes.Transparent;
                                }
                                else
                                {
                                    ess.Row.Background = ess.Row.GetIndex() % 2 == 0 ? Brushes.White : new SolidColorBrush(Color.FromRgb(240, 240, 240)); // Example: grayish alternating rows
                                    ess.Row.FontWeight = FontWeights.Normal;
                                }
                            };
                            gridData.Add(new SecurityRow
                            {
                                Date = dr["Date"]?.ToString(),
                                Type = dr["Type"]?.ToString(),
                                BVol = dr["BVol"]?.ToString(),
                                SVol = dr["SVol"]?.ToString(),
                                Rate = dr["Rate"]?.ToString(),
                                Comm = dr["Comm"]?.ToString(),
                                Net = dr["Net"]?.ToString(),
                                IsHeader = Convert.ToBoolean(dr["IsHeader"]),
                                IsTotal = Convert.ToBoolean(dr["IsTotal"])
                            });
                        }

                        dg.ItemsSource = gridData;
                    }
                    GridpanelContent.Children.Add(dg);
                }

                #endregion SecurityGrid

                #region SummaryGrid

                Securities = _viewModel.InvoiceDetails
                             .Select(security => security.SecurityName.ToString())
                             .Where(name => !string.IsNullOrEmpty(name))
                             .Distinct()
                             .ToList();

                var summaryData = _viewModel.InvoiceDetails;
                if (summaryData == null || summaryData.Count == 0)
                {
                    Btngetdata.IsEnabled = true;
                    return;
                }
                DataTable summaryTable = ToDataTable<Invoicemodel>(_viewModel.InvoiceDetails.ToList());

                var filteredRows = summaryTable.AsEnumerable()
                                    .Where(r =>
                                    (
                                        r["pnl"] != DBNull.Value &&
                                        decimal.TryParse(r["pnl"].ToString(), out decimal pnlValue) &&
                                        pnlValue != 0
                                    )
                                    ||
                                    (
                                        r.Field<string>("orderType") == "Market" &&
                                        (
                                            r.Field<string>("dealType") == "IN" ||
                                            r.Field<string>("dealType") == "OUT"
                                        ) &&
                                        r.Field<string>("side") == "Sell"
                                    )
                                    ||
                                    (
                                        r.Table.Columns.Contains("uplineCommission") &&
                                        r["uplineCommission"] != DBNull.Value &&
                                        Convert.ToDecimal(r["uplineCommission"]) != 0
                                    )
                                    ||
                                    (
                                        r.Field<string>("orderType") == "ClearBalance"
                                    )
                                    ).ToList();

                if (filteredRows.Any())
                {
                    TextBlock summaryTitle = new TextBlock
                    {
                        Text = "SUMMARY",
                        FontWeight = FontWeights.Bold,
                        FontSize = 15,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 10)
                    };

                    GridpanelContent.Children.Add(summaryTitle);

                    DataGrid dgSummary = new DataGrid
                    {
                        AutoGenerateColumns = false,
                        IsReadOnly = true,
                        CanUserAddRows = false,
                        HeadersVisibility = DataGridHeadersVisibility.Column,
                        HorizontalGridLinesBrush = Brushes.Gray,
                        VerticalGridLinesBrush = Brushes.Gray,
                        Margin = new Thickness(0, 0, 0, 20),
                        VerticalAlignment = VerticalAlignment.Top,
                        ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star),
                        EnableRowVirtualization = false,
                        CanUserSortColumns = false,
                        ColumnHeaderHeight = 30,
                        RowHeight = 25
                    };

                    dgSummary.Columns.Add(new DataGridTextColumn { Header = "Symbol", Binding = new Binding("Symbol") });
                    dgSummary.Columns.Add(new DataGridTextColumn { Header = "M2M", Binding = new Binding("M2M") });
                    dgSummary.Columns.Add(new DataGridTextColumn { Header = "Comm", Binding = new Binding("Comm") });
                    dgSummary.Columns.Add(new DataGridTextColumn { Header = "Total", Binding = new Binding("Total") });

                    string[] RightSummaryCols = { "M2M", "Comm", "Total" };

                    foreach (var col in RightSummaryCols)
                    {
                        var column = dgSummary.Columns
                               .FirstOrDefault(c => c.Header?.ToString() == col);

                        if (column is DataGridTextColumn textColumn)
                        {
                            Style cellStyle = new Style(typeof(TextBlock));
                            cellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                            cellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
                            textColumn.ElementStyle = cellStyle;

                            Style headerStyle = new Style(typeof(DataGridColumnHeader));
                            headerStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Right));
                            column.HeaderStyle = headerStyle;
                        }
                    }

                    DataTable dtSummary = new DataTable();
                    dtSummary.Columns.Add("Symbol", typeof(string));
                    dtSummary.Columns.Add("M2M", typeof(string));
                    dtSummary.Columns.Add("Comm", typeof(string));
                    dtSummary.Columns.Add("Total", typeof(string));
                    dtSummary.Columns.Add("RowType", typeof(string));
                    dtSummary.Columns.Add("SecurityName", typeof(string));

                    DataTable Summarydt = filteredRows.CopyToDataTable();

                    var securityGroups = Summarydt.AsEnumerable()
                        .GroupBy(r => r.Field<string>("securityName"))
                        .OrderBy(g => g.Key);

                    decimal grandM2M = 0;
                    decimal grandComm = 0;
                    decimal grandTotal = 0;

                    foreach (var secGroup in securityGroups)
                    {
                        string securityName = secGroup.Key;

                        dtSummary.Rows.Add(securityName, "", "", "", "SecurityHeader", securityName);

                        decimal securityM2M = 0;
                        decimal securityComm = 0;
                        decimal securityTotal = 0;

                        var symbolGroups = secGroup
                            .GroupBy(r => r.Field<string>("symbolName"))
                            .OrderBy(g => g.Key);

                        foreach (var symGroup in symbolGroups)
                        {
                            string symbol = symGroup.Key;

                            decimal sumM2M = symGroup.Where(r => r["pnl"] != DBNull.Value)
                                                     .Sum(r => Convert.ToDecimal(r["pnl"]));

                            decimal sumComm = symGroup.Sum(r => Convert.ToDecimal(r["uplineCommission"]));
                            decimal sumTotal = sumM2M + sumComm;

                            dtSummary.Rows.Add(symbol,
                                               CommonHelper.FormatAmount(sumM2M),
                                               CommonHelper.FormatAmount(sumComm),
                                               CommonHelper.FormatAmount(sumTotal),
                                               "SymbolData",
                                               securityName);

                            securityM2M += sumM2M;
                            securityComm += sumComm;
                            securityTotal += sumTotal;
                        }

                        dtSummary.Rows.Add("Total",
                                           securityM2M.ToString("0.00"),
                                           securityComm.ToString("0.00"),
                                           securityTotal.ToString("0.00"),
                                           "SecurityTotal",
                                           securityName);

                        grandM2M += securityM2M;
                        grandComm += securityComm;
                        grandTotal += securityTotal;
                    }

                    dtSummary.Rows.Add("Grand Total",
                                       CommonHelper.FormatAmount(grandM2M),
                                       CommonHelper.FormatAmount(grandComm),
                                       CommonHelper.FormatAmount(grandTotal),
                                       "GrandTotal",
                                       "");

                    ApplyGridStyle(dgSummary, "summary");

                    // ✅ Proper WPF Row Styling (Correct Way)
                    dgSummary.LoadingRow += (s, es) =>
                    {
                        var rowView = es.Row.Item as DataRowView;
                        if (rowView == null) return;

                        string rowType = rowView["RowType"]?.ToString();

                        if (rowType == "SecurityHeader")
                        {
                            var color = (Color)ColorConverter.ConvertFromString("#EFECC8");
                            es.Row.Background = new SolidColorBrush(color);
                            es.Row.FontWeight = FontWeights.Bold;
                        }
                        else if (rowType == "SecurityTotal")
                        {
                            es.Row.FontWeight = FontWeights.Bold;
                        }
                        else if (rowType == "GrandTotal")
                        {
                            es.Row.Background = Brushes.Gainsboro;
                            es.Row.FontWeight = FontWeights.Bold;
                        }
                        else
                        {
                            es.Row.Background = es.Row.GetIndex() % 2 == 0 ? Brushes.White : new SolidColorBrush(Color.FromRgb(240, 240, 240)); // Example: grayish alternating rows
                            es.Row.FontWeight = FontWeights.Normal;
                        }
                    };

                    dgSummary.ItemsSource = dtSummary.DefaultView;

                    GridpanelContent.Children.Add(dgSummary);
                }

                #endregion SummaryGrid

                #region CarryForwardGrid
                
                var carryData = _viewModel.InvoiceDetails;        
                if (carryData == null || carryData.Count == 0)
                {
                    Btngetdata.IsEnabled = true;
                    return;
                }

                DataTable carryTable = ToDataTable<Invoicemodel>(_viewModel.InvoiceDetails.ToList());

                var filteredRowscarry = carryTable.AsEnumerable()
                                   .Where(r =>
                                       r.Field<string>("orderType") == "Market" &&
                                       r.Field<string>("reason") == "RollOver" &&
                                       r.Field<string>("dealType") == "IN")
                                   .ToList();

                if (filteredRowscarry.Any())
                {
                    TextBlock carryTitle = new TextBlock
                    {
                        Text = "CARRY FORWARD",
                        FontWeight = FontWeights.Bold,
                        FontSize = 15,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Margin = new Thickness(0, 20, 0, 10)
                    };

                    GridpanelContent.Children.Add(carryTitle);

                    DataGrid CarryforwardDataGrid = new DataGrid
                    {
                        AutoGenerateColumns = false,
                        IsReadOnly = true,
                        HeadersVisibility = DataGridHeadersVisibility.Column,
                        HorizontalGridLinesBrush = Brushes.Gray,
                        VerticalGridLinesBrush = Brushes.Gray,
                        CanUserAddRows = false,
                        Margin = new Thickness(0, 0, 0, 20),
                        ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star),
                        CanUserSortColumns = false,
                        ColumnHeaderHeight = 30,
                        RowHeight = 25
                    };


                    CarryforwardDataGrid.Columns.Add(new DataGridTextColumn { Header = "Symbol", Binding = new Binding("Symbol") });
                    CarryforwardDataGrid.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new Binding("Type") });
                    CarryforwardDataGrid.Columns.Add(new DataGridTextColumn { Header = "Quantity", Binding = new Binding("Quantity") });
                    CarryforwardDataGrid.Columns.Add(new DataGridTextColumn { Header = "Net", Binding = new Binding("Net") });

                    string[] RightSummaryCols = { "Type", "Quantity", "Net" };

                    foreach (var col in RightSummaryCols)
                    {
                        var column = CarryforwardDataGrid.Columns
                               .FirstOrDefault(c => c.Header?.ToString() == col);

                        if (column is DataGridTextColumn textColumn)
                        {
                            Style cellStyle = new Style(typeof(TextBlock));
                            cellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                            cellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
                            textColumn.ElementStyle = cellStyle;

                            Style headerStyle = new Style(typeof(DataGridColumnHeader));
                            headerStyle.Setters.Add(new Setter(HorizontalContentAlignmentProperty, HorizontalAlignment.Right));
                            column.HeaderStyle = headerStyle;
                        }
                    }

                    DataTable dtCarry = new DataTable();
                    dtCarry.Columns.Add("Symbol", typeof(string));
                    dtCarry.Columns.Add("Type", typeof(string));
                    dtCarry.Columns.Add("Quantity", typeof(string));
                    dtCarry.Columns.Add("Net", typeof(string));
                    dtCarry.Columns.Add("RowType", typeof(string));
                    dtCarry.Columns.Add("Side", typeof(string));
                    dtCarry.Columns.Add("SecurityName", typeof(string));

                    DataTable carryforwardTable = filteredRowscarry.CopyToDataTable();

                    var groupedData = carryforwardTable.AsEnumerable()
                                        .Where(r => _viewModel.InvoiceDetails.Any(s => s.SecurityName == r.Field<string>("SecurityName")))
                                        .GroupBy(r => r.Field<string>("SecurityName"))
                                        .OrderBy(g => g.Key);
                    foreach (var group in groupedData)
                    {
                        string securityName = group.Key;

                        dtCarry.Rows.Add(securityName, "", "", "", "SecurityHeader", "", securityName);
                        foreach (var row in group)
                        {
                            invoice.SymbolName = row.Field<string>("symbolName");
                            invoice.Side = row.Field<string>("side");
                            invoice.Volume = Convert.ToDouble(row["volume"]);
                            invoice.Pnl = double.TryParse(row["pnl"]?.ToString(), out var dNet) ? dNet : 0.00;

                            dtCarry.Rows.Add(
                                invoice.SymbolName,
                                invoice.Side,
                                invoice.Volume,
                                CommonHelper.FormatAmount(invoice.Pnl),
                                "SymbolData",
                                invoice.Side,
                                securityName
                            );
                        }
                    }
                    CarryforwardDataGrid.ItemsSource = null;

                    CarryforwardDataGrid.LoadingRow += (s, ec) =>
                    {
                        DataRowView row = ec.Row.Item as DataRowView;
                        if (row == null) return;

                        string rowType = row["RowType"]?.ToString();
                        string side = row["Side"]?.ToString();

                        ec.Row.Background = Brushes.Transparent;
                        ec.Row.Foreground = Brushes.Black;
                        ec.Row.FontWeight = FontWeights.Normal;

                        if (rowType == "SecurityHeader")
                        {
                            ec.Row.FontWeight = FontWeights.Bold;
                            var color = (Color)ColorConverter.ConvertFromString("#EFECC8");
                            ec.Row.Background = new SolidColorBrush(color);
                            ec.Row.Padding = new Thickness(10, 4, 0, 4);
                        }
                        else if (rowType == "SymbolData")
                        {

                            Brush textBrush = (side?.Equals("Sell", StringComparison.OrdinalIgnoreCase) ?? false)
                                              ? Brushes.Red : Brushes.Blue;

                            Dispatcher.BeginInvoke(new Action(() =>
                            {
                                for (int i = 1; i < CarryforwardDataGrid.Columns.Count; i++)
                                {
                                    var cellContent = CarryforwardDataGrid.Columns[i].GetCellContent(ec.Row);
                                    if (cellContent is TextBlock tb)
                                    {
                                        tb.Foreground = textBrush;
                                    }
                                }
                            }), System.Windows.Threading.DispatcherPriority.Render);
                        }
                    };


                    CarryforwardDataGrid.ItemsSource = dtCarry.DefaultView;
                    GridpanelContent.Children.Add(CarryforwardDataGrid);
                }

                #endregion CarryForwardGrid

            }
        }   

        public DataTable ToDataTable<T>(List<T> items)
        {
            var dt = new DataTable(typeof(T).Name);
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
                dt.Columns.Add(prop.Name, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);

            foreach (var item in items)
            {
                var values = new object[props.Length];
                for (int i = 0; i < props.Length; i++)
                    values[i] = props[i].GetValue(item) ?? DBNull.Value;

                dt.Rows.Add(values);
            }

            return dt;
        }

        private DataGrid CreateBaseGrid()
        {
            DataGrid dg = new DataGrid
            {
                AutoGenerateColumns = false,
                IsReadOnly = true,
                CanUserAddRows = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                HorizontalGridLinesBrush = Brushes.Gray,
                VerticalGridLinesBrush = Brushes.Gray,
                Margin = new Thickness(0, 0, 0, 20),
                ColumnWidth = new DataGridLength(1, DataGridLengthUnitType.Star),
                CanUserSortColumns = false,
                ColumnHeaderHeight = 30,
                RowHeight = 25
            };

            dg.Columns.Add(new DataGridTextColumn { Header = "Date", Binding = new System.Windows.Data.Binding("Date") });
            dg.Columns.Add(new DataGridTextColumn { Header = "Type", Binding = new System.Windows.Data.Binding("Type") });
            dg.Columns.Add(new DataGridTextColumn { Header = "B Vol", Binding = new System.Windows.Data.Binding("BVol") });
            dg.Columns.Add(new DataGridTextColumn { Header = "S Vol", Binding = new System.Windows.Data.Binding("SVol") });
            dg.Columns.Add(new DataGridTextColumn { Header = "Rate", Binding = new System.Windows.Data.Binding("Rate") });
            dg.Columns.Add(new DataGridTextColumn { Header = "Comm", Binding = new System.Windows.Data.Binding("Comm") });
            dg.Columns.Add(new DataGridTextColumn { Header = "Net", Binding = new System.Windows.Data.Binding("Net") });

            return dg;
        }

        private void ApplyGridStyle(DataGrid dg, string styleType = "default")
        {
            try
            {
                // Common setup for all grids
                dg.IsReadOnly = true;
                dg.CanUserAddRows = false;
                dg.CanUserDeleteRows = false;
                dg.CanUserReorderColumns = false;
                dg.CanUserResizeColumns = true;
                dg.CanUserSortColumns = false;
                dg.HeadersVisibility = DataGridHeadersVisibility.Column;
                dg.GridLinesVisibility = DataGridGridLinesVisibility.All;
                dg.HorizontalGridLinesBrush = new SolidColorBrush(Colors.LightGray);
                dg.VerticalGridLinesBrush = new SolidColorBrush(Colors.LightGray);
                dg.SelectionMode = DataGridSelectionMode.Single;
                dg.SelectionUnit = DataGridSelectionUnit.FullRow;
                dg.BorderBrush = new SolidColorBrush(Colors.LightGray);
                dg.BorderThickness = new Thickness(1);
                dg.RowHeight = 25;
                dg.ColumnHeaderHeight = 35;
                dg.Margin = new Thickness(0, 0, 0, 20);
                dg.EnableRowVirtualization = false;
                dg.EnableColumnVirtualization = false;

                // Disable selection highlighting
                dg.SelectionChanged += (s, e) =>
                {
                    if (dg.SelectedItem != null)
                    {
                        dg.UnselectAll();
                    }
                };

                dg.LoadingRow += (sender, e) =>
                {
                    var row = e.Row;
                    var item = row.Item;
                    int rowIndex = row.GetIndex();

                    // Common row colors
                    var rowColors = new[]
                    {
                Brushes.White,
                new SolidColorBrush(Color.FromRgb(240, 240, 240)) // ThemeManager.GridRowBackColor
            };
                    var headerColor = new SolidColorBrush(Color.FromRgb(200, 200, 200)); // ThemeManager.InvoiceBackColor

                    bool isTotalRow = false;
                    bool isHeaderRow = false;
                    bool isGrandTotalRow = false;

                    DataRowView rowView = item as DataRowView;
                    if (rowView != null)
                    {
                        if (styleType == "default")
                        {
                            // Check for Total row
                            foreach (object cellValue in rowView.Row.ItemArray)
                            {
                                if (cellValue != null && cellValue.ToString().Trim().Equals("Total", StringComparison.OrdinalIgnoreCase))
                                {
                                    isTotalRow = true;
                                    break;
                                }
                            }

                            if (!isTotalRow)
                            {
                                // Check for Header row
                                string dateVal = rowView["Date"]?.ToString();
                                string typeVal = rowView["Type"]?.ToString();
                                string bVolVal = rowView["BVol"]?.ToString();
                                string sVolVal = rowView["SVol"]?.ToString();

                                isHeaderRow = string.IsNullOrWhiteSpace(typeVal) &&
                                             string.IsNullOrWhiteSpace(bVolVal) &&
                                             string.IsNullOrWhiteSpace(sVolVal) &&
                                             !string.IsNullOrWhiteSpace(dateVal);
                            }
                        }
                        else if (styleType == "summary")
                        {
                            string symbol = rowView["Symbol"]?.ToString()?.Trim().ToLower();
                            isTotalRow = symbol == "total";
                            isGrandTotalRow = symbol == "grand total";

                            if (!isTotalRow && !isGrandTotalRow)
                            {
                                string m2mVal = rowView["M2M"]?.ToString();
                                string commVal = rowView["Comm"]?.ToString();
                                string totalVal = rowView["Total"]?.ToString();

                                isHeaderRow = string.IsNullOrWhiteSpace(m2mVal) &&
                                             string.IsNullOrWhiteSpace(commVal) &&
                                             string.IsNullOrWhiteSpace(totalVal) &&
                                             !string.IsNullOrWhiteSpace(symbol);
                            }
                        }
                        else if (styleType == "carry")
                        {
                            string symbol = rowView["Symbol"]?.ToString()?.Trim().ToLower();
                            isTotalRow = symbol == "total";

                            if (!isTotalRow)
                            {
                                string type = rowView["Type"]?.ToString();
                                string quantity = rowView["Quantity"]?.ToString();
                                string netVal = rowView["Net"]?.ToString();

                                isHeaderRow = string.IsNullOrWhiteSpace(type) &&
                                             string.IsNullOrWhiteSpace(quantity) &&
                                             string.IsNullOrWhiteSpace(netVal) &&
                                             !string.IsNullOrWhiteSpace(symbol);
                            }
                        }
                    }
                    else
                    {
                        SecurityRow securityRow = item as SecurityRow;
                        if (securityRow != null)
                        {
                            isHeaderRow = securityRow.IsHeader;
                            isTotalRow = securityRow.IsTotal;
                        }
                    }

                    // Apply styles
                    if (isGrandTotalRow)
                    {
                        row.Background = new SolidColorBrush(Color.FromRgb(220, 220, 220)); // Gainsboro
                        row.FontWeight = FontWeights.Bold;
                        row.FontSize = 14;
                    }
                    else if (isTotalRow)
                    {
                        row.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)); // ThemeManager.GridRowBackColor
                        row.Foreground = Brushes.Black;
                        row.FontWeight = FontWeights.Bold;
                    }
                    else if (isHeaderRow)
                    {
                        var color = (Color)ColorConverter.ConvertFromString("#EFECC8");
                        row.Background = new SolidColorBrush(color);
                        row.Foreground = Brushes.Black;
                        row.FontWeight = FontWeights.Bold;
                        row.Padding = new Thickness(10, 4, 0, 4);
                    }
                    else
                    {
                        // Alternating row colors
                        row.Background = rowIndex % 2 == 0 ?
                            Brushes.White :
                            new SolidColorBrush(Color.FromRgb(240, 240, 240));
                        row.FontWeight = FontWeights.Normal;
                    }
                };

                // Cell formatting for colors based on values
                dg.LoadingRow += (sender, e) =>
                {
                    var row = e.Row;
                    var item = row.Item;

                    // Apply cell-specific formatting after row is loaded
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        for (int i = 0; i < dg.Columns.Count; i++)
                        {
                            var column = dg.Columns[i];
                            var cellContent = column.GetCellContent(row);

                            TextBlock textBlock = cellContent as TextBlock;
                            if (textBlock != null)
                            {
                                string columnName = column.Header?.ToString();
                                string cellText = textBlock.Text;

                                if (styleType == "default")
                                {
                                    switch (columnName)
                                    {
                                        case "B Vol":
                                            if (!string.IsNullOrEmpty(cellText) && cellText != "-")
                                                textBlock.Foreground = new SolidColorBrush(Colors.Blue);
                                            break;
                                        case "S Vol":
                                            if (!string.IsNullOrEmpty(cellText) && cellText != "-")
                                                textBlock.Foreground = new SolidColorBrush(Colors.Red);
                                            break;
                                        case "Comm":
                                        case "Net":
                                            decimal value;
                                            if (decimal.TryParse(cellText?.Replace(" ", ""), out value))
                                            {
                                                textBlock.Foreground = value < 0 ?
                                                    new SolidColorBrush(Colors.Red) :
                                                    new SolidColorBrush(Colors.Blue);
                                            }
                                            break;
                                        case "Type":
                                            if (cellText == "Buy")
                                                textBlock.Foreground = new SolidColorBrush(Colors.Blue);
                                            else if (cellText == "Sell")
                                                textBlock.Foreground = new SolidColorBrush(Colors.Red);
                                            else if (cellText == "CF")
                                                textBlock.Foreground = new SolidColorBrush(Colors.Green);
                                            break;
                                    }
                                }
                                else if (styleType == "summary")
                                {
                                    if (columnName == "M2M" || columnName == "Comm" || columnName == "Total")
                                    {
                                        decimal value;
                                        if (decimal.TryParse(cellText?.Replace(" ", ""), out value))
                                        {
                                            bool makeRed = value < 0 || (value == 0 && columnName == "Comm");
                                            textBlock.Foreground = makeRed ?
                                                new SolidColorBrush(Colors.Red) :
                                                new SolidColorBrush(Colors.Blue);
                                        }
                                    }

                                    // Check if this is a grand total row
                                    DataRowView rowView = item as DataRowView;
                                    if (rowView != null)
                                    {
                                        string symbol = rowView["Symbol"]?.ToString()?.Trim().ToLower();
                                        if (symbol == "grand total")
                                        {
                                            textBlock.FontSize = 14;
                                            textBlock.FontWeight = FontWeights.Bold;
                                        }
                                    }
                                }
                                else if (styleType == "carry")
                                {
                                    DataRowView rowView = item as DataRowView;
                                    if (rowView != null)
                                    {
                                        string side = rowView["Side"]?.ToString();

                                        if (columnName == "Type" && !string.IsNullOrEmpty(side))
                                        {
                                            textBlock.Foreground = side.Equals("Sell", StringComparison.OrdinalIgnoreCase) ?
                                                new SolidColorBrush(Colors.Red) :
                                                new SolidColorBrush(Colors.Blue);
                                        }
                                        else if (columnName == "Quantity" || columnName == "Net")
                                        {
                                            textBlock.Foreground = new SolidColorBrush(Colors.Blue);
                                        }
                                    }
                                }
                            }
                        }
                    }), System.Windows.Threading.DispatcherPriority.Render);
                };

                // Set column styles based on type
                string[] rightAlignedColumns;
                if (styleType == "default")
                {
                    rightAlignedColumns = new[] { "B Vol", "S Vol", "Rate", "Comm", "Net" };
                }
                else if (styleType == "summary")
                {
                    rightAlignedColumns = new[] { "M2M", "Comm", "Total" };
                }
                else if (styleType == "carry")
                {
                    rightAlignedColumns = new[] { "Type", "Quantity", "Net" };
                }
                else
                {
                    rightAlignedColumns = new string[0];
                }

                foreach (var column in dg.Columns)
                {
                    string header = column.Header?.ToString();

                    // Right align specific columns
                    if (Array.IndexOf(rightAlignedColumns, header) >= 0)
                    {
                        DataGridTextColumn textColumn = column as DataGridTextColumn;
                        if (textColumn != null)
                        {
                            // Right align cell content
                            Style cellStyle = new Style(typeof(TextBlock));
                            cellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Right));
                            cellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Right));
                            cellStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(5, 2, 10, 2)));
                            textColumn.ElementStyle = cellStyle;

                            // Right align header
                            Style headerStyle = new Style(typeof(DataGridColumnHeader));
                            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Right));
                            headerStyle.Setters.Add(new Setter(DataGridColumnHeader.PaddingProperty, new Thickness(0, 0, 10, 0)));
                            column.HeaderStyle = headerStyle;
                        }
                    }
                    else
                    {
                        // Left align for other columns
                        DataGridTextColumn textColumn = column as DataGridTextColumn;
                        if (textColumn != null)
                        {
                            Style cellStyle = new Style(typeof(TextBlock));
                            cellStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Left));
                            cellStyle.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left));
                            cellStyle.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(10, 2, 5, 2)));
                            textColumn.ElementStyle = cellStyle;
                        }
                    }

                    // Make columns not sortable
                    column.CanUserSort = false;
                }

                // Handle special column widths for different grid types
                if (styleType == "default")
                {
                    var dateColumn = dg.Columns.FirstOrDefault(c =>
                    {
                        string header = c.Header?.ToString();
                        return header == "Date";
                    });

                    if (dateColumn != null)
                    {
                        dateColumn.MinWidth = 180;
                        dateColumn.Width = new DataGridLength(180);
                    }
                }
                else if (styleType == "summary" || styleType == "carry")
                {
                    var symbolColumn = dg.Columns.FirstOrDefault(c =>
                    {
                        string header = c.Header?.ToString();
                        return header == "Symbol";
                    });

                    if (symbolColumn != null)
                    {
                        symbolColumn.MinWidth = 400;
                        symbolColumn.Width = new DataGridLength(400);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error in ApplyGridStyle: {ex.Message}");
            }
        }
    }
}
