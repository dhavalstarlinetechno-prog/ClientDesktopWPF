using ClientDesktop.Core.Config;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.ViewModel;
using DocumentFormat.OpenXml.Drawing;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ClientDesktop.View.Navigation
{

    public class SecurityGridItem
    {
        public string SecurityName { get; set; }
        public List<SecurityRow> SecurityData { get; set; }
    }

    public class SecurityRow
    {
        public string Date { get; set; }
        public string Type { get; set; }
        public string BVol { get; set; }
        public string SVol { get; set; }
        public string Rate { get; set; }
        public string Comm { get; set; }
        public string Net { get; set; }
        public bool IsHeader { get; set; }
        public bool IsTotal { get; set; }
    }

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
                this.DataContext = _viewModel;
                _viewModel.PropertyChanged += ViewModel_PropertyChanged;
            }

            ApplyViewLockUI(_viewModel?.IsViewLocked ?? MainWindowViewModel.isViewLocked);

            this.Unloaded += InvoiceView_Unloaded;


            //if (MainWindowViewModel.isViewLocked == true)
            //{
            //    Lbltext.Text = CommonMessages.InvoiceLedgerWrongPassword;
            //    Lbltext.FontFamily = new System.Windows.Media.FontFamily("Segoe UI");
            //    Lbltext.Foreground = System.Windows.Media.Brushes.Red;
            //    Lbltext.Margin = new System.Windows.Thickness(0, 10, 250, 0);
            //    TxtPassword.Visibility = System.Windows.Visibility.Collapsed;
            //    Btngo.Visibility = System.Windows.Visibility.Collapsed;
            //}
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(InvoiceViewModel.IsViewLocked))
            {
                ApplyViewLockUI(_viewModel.IsViewLocked);
            }
        }

        private void ApplyViewLockUI(bool isLocked)
        {
            if (isLocked)
            {
                Lbltext.Text = CommonMessages.InvoiceLedgerWrongPassword;
                Lbltext.FontFamily = new System.Windows.Media.FontFamily("Microsoft Sans Serif");
                Lbltext.Foreground = System.Windows.Media.Brushes.Red;
                Lbltext.Margin = new System.Windows.Thickness(0, 10, 250, 0);
                TxtPassword.Visibility = System.Windows.Visibility.Collapsed;
                Btngo.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                // ✅ Restore normal UI when unlocked via socket
                Lbltext.Text = "This report represents sample invoice format. It contains sample data only for education purpose. Invoice can be displayed in below structure";
                Lbltext.FontFamily = new System.Windows.Media.FontFamily("Microsoft Sans Serif");
                Lbltext.Foreground = System.Windows.Media.Brushes.Black;
                Lbltext.Margin = new System.Windows.Thickness(40,30,60,20);
                TxtPassword.Visibility = System.Windows.Visibility.Visible;
                Btngo.Visibility = System.Windows.Visibility.Visible;
                Btngo.IsEnabled = !string.IsNullOrEmpty(TxtPassword.Password);
            }
        }

        private void InvoiceView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                _viewModel.Cleanup();
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

                    if (Cmbselectweek.SelectedItem is ComboBoxItem item &&
                        item.Content.ToString() == "Select")
                    {
                        Btngetdata.IsEnabled = false;
                        Lblfrom.Visibility = Visibility.Hidden;
                        Lblfromdate.Content = CommonMessages.NoDataAvailable;
                        Lblfromdate.FontSize = 20;
                        Lblfromdate.Foreground = Brushes.Gray;
                        Lblfromdate.FontWeight = FontWeights.Regular;
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
            Btngo.IsEnabled = !string.IsNullOrEmpty(TxtPassword.Password);
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
            else
            {
                Lblfromdate.Visibility = Visibility.Collapsed;
            }

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

            // Clear previous bindings
            SecurityGridsList.ItemsSource = null;
            SummaryDataGrid.ItemsSource = null;
            SummaryPanel.Visibility = Visibility.Collapsed;
            CarryForwardDataGrid.ItemsSource = null;
            CarryForwardPanel.Visibility = Visibility.Collapsed;

            Gridpanel.Visibility = Visibility.Visible;
            Btngetdata.IsEnabled = false;
            Lblfrom.Content = "";
            Lblfrom.Visibility = Visibility.Visible;
            Lblfromdate.Visibility = Visibility.Collapsed;

            if (_viewModel == null) return;

            await _viewModel.LoadInvoiceDetailAsync(fromdatefilter, todatefilter);

            if (_viewModel.InvoiceDetails == null || _viewModel.InvoiceDetails.Count == 0)
            {
                ShowNoData();
                return;
            }
           
            List<string> Securities = _viewModel.InvoiceDetails
                .Select(s => s.SecurityName)
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .OrderBy(x => x)
                .ToList();

            if (Securities.Count == 0)
            {
                ShowNoData();
                return;
            }
            PdfExportBtn.Visibility = Visibility.Visible;

            // Update date label
            UpdateDateLabel();

            DataTable securityTable = ToDataTable(_viewModel.InvoiceDetails.ToList());          
            var securityPdfTables = new List<(string SecurityName, DataTable Table)>();
         
            var securityGridItemsList = new List<SecurityGridItem>();

            foreach (var security in Securities)
            {
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
                
                DataTable pdfSecurity = new DataTable();
                pdfSecurity.Columns.Add("Date", typeof(string));
                pdfSecurity.Columns.Add("Type", typeof(string));
                pdfSecurity.Columns.Add("BVol", typeof(string));
                pdfSecurity.Columns.Add("SVol", typeof(string));
                pdfSecurity.Columns.Add("Rate", typeof(string));
                pdfSecurity.Columns.Add("Comm", typeof(string));
                pdfSecurity.Columns.Add("Net", typeof(string));
                pdfSecurity.Columns.Add("RowType", typeof(string)); 

                foreach (var symbol in symbols)
                {
                    // Header row
                    dtSecurity.Rows.Add(symbol, "", "", "", "", "", "", true, false);
                    pdfSecurity.Rows.Add(symbol, "", "", "", "", "", "", "SecurityHeader");

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
                        invoice.DealCreatedOn = Convert.ToDateTime(
                            istTime.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture));

                        invoice.Side = (row["OrderType"]?.ToString() == "Market" &&
                                        row["Reason"]?.ToString() == "RollOver" &&
                                        row["DealType"]?.ToString() == "IN") ? "CF" : side;

                        string fRate = CommonHelper.FormatAmount(invoice.Price);
                        string fComm = CommonHelper.FormatAmount(invoice.UplineCommission);
                        string fNet = CommonHelper.FormatAmount(invoice.Pnl);

                        // UI row
                        dtSecurity.Rows.Add(invoice.DealCreatedOn, invoice.Side,
                                            bVol, sVol, fRate, fComm, fNet,
                                            false, false);
                        // PDF row
                        pdfSecurity.Rows.Add(invoice.DealCreatedOn, invoice.Side,
                                             bVol, sVol, fRate, fComm, fNet,
                                             "Data");
                    }

                    double totalPnl = symbolRows.Sum(r => Convert.ToDouble(r["pnl"]));
                    double totalComm = symbolRows.Sum(r => Convert.ToDouble(r["uplineCommission"]));

                    string fTotalComm = CommonHelper.FormatAmount(totalComm);
                    string fTotalPnl = CommonHelper.FormatAmount(totalPnl);
                    string fGrandNet = CommonHelper.FormatAmount(totalPnl + totalComm);
                  
                    dtSecurity.Rows.Add("", "", "", "", "", fTotalComm, fTotalPnl, false, false);
                    pdfSecurity.Rows.Add("", "", "", "", "", fTotalComm, fTotalPnl, "SubTotal");

                    // Total row
                    dtSecurity.Rows.Add("", "", "", "", "", "Total", fGrandNet, false, true);
                    pdfSecurity.Rows.Add("", "", "", "", "", "Total", fGrandNet, "Total");
                }
               
                var gridData = new List<SecurityRow>();
                foreach (DataRow dr in dtSecurity.Rows)
                {
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

                securityGridItemsList.Add(new SecurityGridItem
                {
                    SecurityName = security,
                    SecurityData = gridData
                });
                
                securityPdfTables.Add((security, pdfSecurity));
            }

            SecurityGridsList.ItemsSource = securityGridItemsList;
         
            DataTable summaryPdfTable = null;

            DataTable summaryTable = ToDataTable(_viewModel.InvoiceDetails.ToList());

            var filteredRows = summaryTable.AsEnumerable()
                .Where(r =>
                    (r["pnl"] != DBNull.Value &&
                     decimal.TryParse(r["pnl"].ToString(), out decimal pnlValue) &&
                     pnlValue != 0)
                    ||
                    (r.Field<string>("orderType") == "Market" &&
                     (r.Field<string>("dealType") == "IN" || r.Field<string>("dealType") == "OUT") &&
                     r.Field<string>("side") == "Sell")
                    ||
                    (r.Table.Columns.Contains("uplineCommission") &&
                     r["uplineCommission"] != DBNull.Value &&
                     Convert.ToDecimal(r["uplineCommission"]) != 0)
                    ||
                    r.Field<string>("orderType") == "ClearBalance"
                ).ToList();

            if (filteredRows.Any())
            {
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

                decimal grandM2M = 0, grandComm = 0, grandTotal = 0;

                foreach (var secGroup in securityGroups)
                {
                    string securityName = secGroup.Key;
                    dtSummary.Rows.Add(securityName, "", "", "", "SecurityHeader", securityName);

                    decimal secM2M = 0, secComm = 0, secTotal = 0;

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

                        secM2M += sumM2M;
                        secComm += sumComm;
                        secTotal += sumTotal;
                    }

                    dtSummary.Rows.Add("Total",
                                       secM2M.ToString("0.00"),
                                       secComm.ToString("0.00"),
                                       secTotal.ToString("0.00"),
                                       "SecurityTotal",
                                       securityName);

                    grandM2M += secM2M;
                    grandComm += secComm;
                    grandTotal += secTotal;
                }

                dtSummary.Rows.Add("Grand Total",
                                   CommonHelper.FormatAmount(grandM2M),
                                   CommonHelper.FormatAmount(grandComm),
                                   CommonHelper.FormatAmount(grandTotal),
                                   "GrandTotal",
                                   "");

                
                SummaryDataGrid.ItemsSource = dtSummary.DefaultView;
                SummaryPanel.Visibility = Visibility.Visible;               
                summaryPdfTable = BuildSummaryPdfTable(dtSummary);
            }          
            DataTable carryPdfTable = null;

            DataTable carryTable = ToDataTable(_viewModel.InvoiceDetails.ToList());

            var filteredRowsCarry = carryTable.AsEnumerable()
                .Where(r =>
                    r.Field<string>("orderType") == "Market" &&
                    r.Field<string>("reason") == "RollOver" &&
                    r.Field<string>("dealType") == "IN")
                .ToList();

            if (filteredRowsCarry.Any())
            {
                DataTable dtCarry = new DataTable();
                dtCarry.Columns.Add("Symbol", typeof(string));
                dtCarry.Columns.Add("Type", typeof(string));
                dtCarry.Columns.Add("Quantity", typeof(string));
                dtCarry.Columns.Add("Net", typeof(string));
                dtCarry.Columns.Add("RowType", typeof(string));
                dtCarry.Columns.Add("Side", typeof(string));
                dtCarry.Columns.Add("SecurityName", typeof(string));

                DataTable carryforwardTable = filteredRowsCarry.CopyToDataTable();

                var groupedData = carryforwardTable.AsEnumerable()
                    .Where(r => _viewModel.InvoiceDetails
                                           .Any(s => s.SecurityName == r.Field<string>("SecurityName")))
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

                CarryForwardDataGrid.ItemsSource = dtCarry.DefaultView;
                CarryForwardPanel.Visibility = Visibility.Visible;
              
                carryPdfTable = BuildCarryPdfTable(dtCarry);
            }
            else
            {
                CarryForwardPanel.Visibility = Visibility.Visible;
            }

            // ──────────────────────────────────────────────────────────────────
            // Hand off all PDF data to ViewModel  ← KEY CALL
            // ──────────────────────────────────────────────────────────────────
            string pdfTitle = $"{_sessionService.UserId} - {_sessionService.Username}";
            string pdfSubTitle = Lblfrom.Content?.ToString() ?? string.Empty;

            _viewModel.PreparePdfData(
                pdfTitle,
                pdfSubTitle,
                securityPdfTables,
                summaryPdfTable,
                carryPdfTable);

            Btngetdata.IsEnabled = true;
        }       
          
        private DataTable BuildSummaryPdfTable(DataTable source)
        {
            DataTable pdf = new DataTable();
            pdf.Columns.Add("Symbol", typeof(string));
            pdf.Columns.Add("M2M", typeof(string));
            pdf.Columns.Add("Comm", typeof(string));
            pdf.Columns.Add("Total", typeof(string));
            pdf.Columns.Add("RowType", typeof(string));

            foreach (DataRow dr in source.Rows)
            {
                pdf.Rows.Add(
                    dr["Symbol"]?.ToString(),
                    dr["M2M"]?.ToString(),
                    dr["Comm"]?.ToString(),
                    dr["Total"]?.ToString(),
                    dr["RowType"]?.ToString());
            }

            return pdf;
        }
    
        private DataTable BuildCarryPdfTable(DataTable source)
        {
            DataTable pdf = new DataTable();
            pdf.Columns.Add("Symbol", typeof(string));
            pdf.Columns.Add("Type", typeof(string));
            pdf.Columns.Add("Quantity", typeof(string));
            pdf.Columns.Add("Net", typeof(string));
            pdf.Columns.Add("RowType", typeof(string));

            foreach (DataRow dr in source.Rows)
            {
                pdf.Rows.Add(
                    dr["Symbol"]?.ToString(),
                    dr["Type"]?.ToString(),
                    dr["Quantity"]?.ToString(),
                    dr["Net"]?.ToString(),
                    dr["RowType"]?.ToString());
            }

            return pdf;
        }

        private void ShowNoData()
        {
            Lblfrom.Visibility = Visibility.Collapsed;
            Lblfromdate.Visibility = Visibility.Visible;
            Lblfromdate.Content = CommonMessages.NoDataAvailable;
            Lblfromdate.FontSize = 20;
            Lblfromdate.Foreground = Brushes.Gray;
            Lblfromdate.FontWeight = FontWeights.Regular;
            isDataLoaded = false;
            Btngetdata.IsEnabled = true;
        }

        private void UpdateDateLabel()
        {
            DateTime localToday = DateTime.Today;
            if (Cmbselectweek.Text == "Current week")
            {
                int diff = (7 + ((int)localToday.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
                thisWeekStart = localToday.AddDays(-diff);
                thisWeekEnd = thisWeekStart.AddDays(5);
            }
            else if (Cmbselectweek.Text == "Previous Week")
            {
                int diff = (7 + (localToday.DayOfWeek - DayOfWeek.Monday)) % 7;
                thisWeekStart = localToday.AddDays(-1 * diff).AddDays(-7);
                thisWeekEnd = thisWeekStart.AddDays(5);
            }
            else if (Cmbselectweek.Text == "Last Previous Week")
            {
                int diff = (7 + ((int)localToday.DayOfWeek - (int)DayOfWeek.Monday)) % 7;
                thisWeekStart = localToday.AddDays(-diff).AddDays(-14);
                thisWeekEnd = thisWeekStart.AddDays(5);
            }

            Lblfrom.Content = $"From {thisWeekStart:dd-MM-yyyy} To {thisWeekEnd:dd-MM-yyyy}";
        }

        /// <summary>Converts a List&lt;T&gt; to DataTable using reflection.</summary>
        public DataTable ToDataTable<T>(List<T> items)
        {
            var dt = new DataTable(typeof(T).Name);
            var props = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (var prop in props)
                dt.Columns.Add(prop.Name,
                    Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);

            foreach (var item in items)
            {
                var values = new object[props.Length];
                for (int i = 0; i < props.Length; i++)
                    values[i] = props[i].GetValue(item) ?? DBNull.Value;
                dt.Rows.Add(values);
            }

            return dt;
        }

        private void SecurityDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var row = e.Row;
            var item = row.Item as SecurityRow;
            if (item == null) return;

            int rowIndex = row.GetIndex();

            if (item.IsHeader)
            {
                row.Foreground = Brushes.Black;
                row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFECC8"));
                row.FontWeight = FontWeights.Bold;
                row.Padding = new Thickness(10, 4, 0, 4);
            }
            else if (item.IsTotal)
            {
                row.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                row.Foreground = Brushes.Black;
                row.FontWeight = FontWeights.Regular;
            }
            else
            {
                row.Background = rowIndex % 2 == 0
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(240, 240, 240));
                row.FontWeight = FontWeights.Normal;
            }

            DataGrid dg = sender as DataGrid;
            dg.Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = 0; i < dg.Columns.Count; i++)
                {
                    var column = dg.Columns[i];
                    var cellContent = column.GetCellContent(row);
                    if (cellContent is System.Windows.Controls.TextBlock tb)
                    {
                        string colName = column.Header?.ToString();
                        string cellText = tb.Text;

                        switch (colName)
                        {
                            case "B Vol":
                                if (!string.IsNullOrEmpty(cellText) && cellText != "-")
                                    tb.Foreground = Brushes.Blue;
                                break;
                            case "S Vol":
                                if (!string.IsNullOrEmpty(cellText) && cellText != "-")
                                    tb.Foreground = Brushes.Red;
                                break;
                            case "Comm":
                            case "Net":
                                if (decimal.TryParse(cellText?.Replace(" ", ""), out decimal val))
                                    tb.Foreground = val < 0 ? Brushes.Red : Brushes.Blue;
                                break;
                            case "Type":
                                if (cellText == "Buy") tb.Foreground = Brushes.Blue;
                                else if (cellText == "Sell") tb.Foreground = Brushes.Red;
                                else if (cellText == "CF") tb.Foreground = Brushes.Green;
                                break;
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void SummaryDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var row = e.Row;
            var rowView = row.Item as DataRowView;
            if (rowView == null) return;

            int rowIndex = row.GetIndex();
            string rowType = rowView["RowType"]?.ToString();
            string symbol = rowView["Symbol"]?.ToString()?.Trim().ToLower();

            if (rowType == "SecurityHeader")
            {
                row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFECC8"));
                row.Foreground = Brushes.Black;
                row.FontWeight = FontWeights.Bold;
                row.Padding = new Thickness(10, 4, 0, 4);
            }
            else if (rowType == "SecurityTotal" || symbol == "total")
            {
                row.Background = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                row.Foreground = Brushes.Black;
                row.FontWeight = FontWeights.Bold;
            }
            else if (rowType == "GrandTotal" || symbol == "grand total")
            {
                row.Background = new SolidColorBrush(Color.FromRgb(220, 220, 220));
                row.Foreground = Brushes.Black;
                row.FontWeight = FontWeights.Bold;
                row.FontSize = 14;
            }
            else
            {
                row.Background = rowIndex % 2 == 0
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(240, 240, 240));
                row.FontWeight = FontWeights.Normal;
            }

            DataGrid dg = sender as DataGrid;
            dg.Dispatcher.BeginInvoke(new Action(() =>
            {
                for (int i = 0; i < dg.Columns.Count; i++)
                {
                    var column = dg.Columns[i];
                    var cellContent = column.GetCellContent(row);
                    if (cellContent is System.Windows.Controls.TextBlock tb)
                    {
                        string colName = column.Header?.ToString();
                        string cellText = tb.Text;

                        if (colName == "M2M" || colName == "Comm" || colName == "Total")
                        {
                            if (decimal.TryParse(cellText?.Replace(" ", ""), out decimal val))
                            {
                                bool makeRed = val < 0 || (val == 0 && colName == "Comm");
                                tb.Foreground = makeRed ? Brushes.Red : Brushes.Blue;
                            }
                        }

                        if (symbol == "total" || symbol == "grand total")
                        {
                            tb.FontWeight = FontWeights.Bold;
                            tb.HorizontalAlignment = HorizontalAlignment.Right;
                            if (symbol == "grand total") tb.FontSize = 14;
                        }
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void CarryForwardDataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            var row = e.Row;
            var rowView = row.Item as DataRowView;
            if (rowView == null) return;

            int rowIndex = row.GetIndex();
            string rowType = rowView["RowType"]?.ToString();
            string side = rowView["Side"]?.ToString();

            if (rowType == "SecurityHeader")
            {
                row.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFECC8"));
                row.Foreground = Brushes.Black;
                row.FontWeight = FontWeights.Bold;
                row.Padding = new Thickness(10, 4, 0, 4);
            }
            else
            {
                row.Background = rowIndex % 2 == 0
                    ? Brushes.White
                    : new SolidColorBrush(Color.FromRgb(240, 240, 240));
                row.Foreground = Brushes.Black;
                row.FontWeight = FontWeights.Normal;
            }

            DataGrid dg = sender as DataGrid;
            dg.Dispatcher.BeginInvoke(new Action(() =>
            {
                Brush textBrush = (side?.Equals("Sell", StringComparison.OrdinalIgnoreCase) ?? false)
                    ? Brushes.Red : Brushes.Blue;

                for (int i = 0; i < dg.Columns.Count; i++)
                {
                    var column = dg.Columns[i];
                    var cellContent = column.GetCellContent(row);
                    if (cellContent is System.Windows.Controls.TextBlock tb)
                    {
                        string colName = column.Header?.ToString();
                        if (colName == "Type" || colName == "Quantity" || colName == "Net")
                            tb.Foreground = textBrush;
                    }
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is DataGrid dg && dg.SelectedItem != null)
                dg.UnselectAll();
        }

        private void PdfExportBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _viewModel?.BuildInvoicePdf();
                FileLogger.Log("Export", "PDF Generate Successfully.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PDF export failed: {ex.Message}",
                                "Error",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }
    }
}