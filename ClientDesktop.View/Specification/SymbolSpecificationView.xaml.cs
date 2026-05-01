using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.ViewModel;
using DocumentFormat.OpenXml.Drawing.Charts;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
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

namespace ClientDesktop.View.Specification
{   
    public partial class SymbolSpecificationView : UserControl
    {
        #region Variable
        private string  symbolExpiryclose = string.Empty;
        private string symbolExpiry = string.Empty;
        #endregion Variable

        #region Constructor
        public SymbolSpecificationView()
        {
            InitializeComponent();
            AddGridBorders();
            AddGridBordersSecondGrid();
           
        }
        
        #endregion Constructor

        #region Methods
        private void AddGridBorders()
        {
            int rowCount = Tablespecification.RowDefinitions.Count;
            int colCount = Tablespecification.ColumnDefinitions.Count;

            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {

                    double left = 0.5;
                    double top = 0.5;
                    double right = 0.5;
                    double bottom = 0.5;

                    if (i == 0)
                    {
                        left = 0;
                        right = 0;
                    }
                    else
                    {
                        if (j == 0) right = 0;
                        if (j == 1) left = 0;
                    }
                    Border b = new Border
                    {
                        BorderBrush = System.Windows.Media.Brushes.Gray,
                        BorderThickness = new System.Windows.Thickness(left, top, right, bottom),
                        SnapsToDevicePixels = true
                    };

                    Grid.SetRow(b, i);
                    Grid.SetColumn(b, j);
                    Tablespecification.Children.Add(b);
                }
            }
         }        
        private void AddGridBordersSecondGrid()
        {
            int rowCount = SecondGrid.RowDefinitions.Count;
            int colCount = SecondGrid.ColumnDefinitions.Count;

            for (int i = 0; i < rowCount; i++)
            {
                for (int j = 0; j < colCount; j++)
                {

                    double left = 0.5;
                    double top = 0.5;
                    double right = 0.5;
                    double bottom = 0.5;

                    if (j == 0)
                    {
                        right = 0;
                    }
                    if (j == 1)
                    {
                        left = 0;
                    }
                    Border b = new Border
                    {
                        BorderBrush = System.Windows.Media.Brushes.Gray,
                        BorderThickness = new System.Windows.Thickness(left, top, right, bottom),
                        SnapsToDevicePixels = true
                    };

                    Grid.SetRow(b, i);
                    Grid.SetColumn(b, j);
                    SecondGrid.Children.Add(b);
                }
            }
        }
        private string FormatValue(string input)
        {
            return Regex.Replace(input, "([a-z])([A-Z])", "$1 $2");
        }
        private List<string> SetDateAndTime(string TimeStr)
        {
            string[] ranges = TimeStr.Split(',');
            List<string> formatted = new List<string>();

            foreach (var range in ranges)
            {
                string[] parts = range.Split('~');
                if (parts.Length == 2)
                {
                    if (DateTime.TryParseExact(parts[0], "HH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime startTime) &&
                        DateTime.TryParseExact(parts[1], "HH:mm:ss",
                        CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime endTime))
                    {
                        DateTime istStart = CommonHelper.ConvertUtcToIst(startTime);
                        DateTime istEnd = CommonHelper.ConvertUtcToIst(endTime);
                        formatted.Add($"{istStart:HH:mm} - {istEnd:HH:mm}");
                    }
                }
            }

            return formatted;
        }

        #endregion

        #region Events
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            
            if (DataContext is SymbolSpecificationViewModel vm)
            {
                if (!vm._sessionService.IsLoggedIn || !vm._sessionService.IsInternetAvailable)
                {
                    Window.GetWindow(this)?.Close();
                    return;
                }

                await vm.LoadSymbolData();

                if (vm.SymbolData != null)
                {
                    LableSymbol.Content = vm.SymbolData.SymbolName + " , " + (vm.SymbolData.MasterSymbolName ?? string.Empty);
                    Lbldigitvalue.Content = vm.SymbolData.SymbolDigits.ToString();
                    Lblcontractsizevalue.Content = vm.SymbolData.SymbolContractsize.ToString();
                    Lblstopsizevalue.Content = vm.SymbolData.SymbolLimitstoplevel.ToString();
                    Lblticksizevalue.Content = vm.SymbolData.SymbolTicksize.ToString();
                    LblTradeValue.Content = FormatValue(vm.SymbolData.SymbolTrade ?? string.Empty);
                    bool advanceLimit = vm.SymbolData.SymbolAdvancelimit.ToString() != null &&
                                        (bool)vm.SymbolData.SymbolAdvancelimit;
                    Lbladvancevalue.Content = advanceLimit ? "Yes" : "No";
                    Lblgtcvalue.Content = FormatValue(vm.SymbolData.SecurityGtc ?? string.Empty);
                    Lblordervalue.Content = vm.SymbolData.SymbolOrder?.Replace(",", ", ");

                    var expiryCloseToken = vm.SymbolData.SymbolExpiryclose?.ToString();
                    if (!string.IsNullOrEmpty(expiryCloseToken))
                    {
                        if (DateTime.TryParse(expiryCloseToken, out DateTime utcTime))
                        {
                            DateTime istTime = CommonHelper.ConvertUtcToIst(utcTime);
                            symbolExpiryclose = istTime.ToString("dd/MM/yy HH:mm", CultureInfo.InvariantCulture);
                        }
                    }

                    var expiryToken = vm.SymbolData.SymbolExpiry;
                    if (expiryToken != null && !string.IsNullOrEmpty(expiryToken.ToString()))
                    {
                        if (DateTime.TryParse(expiryToken.ToString(), out DateTime utcTime))
                        {
                            DateTime istTime = CommonHelper.ConvertUtcToIst(utcTime);
                            symbolExpiry = istTime.ToString("dd/MM/yy HH:mm", CultureInfo.InvariantCulture);
                        }
                    }

                    Lblclosevalue.Content = symbolExpiryclose;
                    Lblpositionvalue.Content = symbolExpiry;
                    Lblminimumvalue.Content = vm.SymbolData.SymbolMinimumvalue.ToString();
                    Lblstepvalue.Content = vm.SymbolData.SymbolStepvalue.ToString();
                    Lbloneclickvalue.Content = vm.SymbolData.SymbolOneclickvalue.ToString();
                    Lbltotalvalue.Content = vm.SymbolData.SymbolTotalvalue.ToString();

                    var sessions = vm.SymbolData.Sessions;
                    if (sessions != null)
                    {
                        foreach (var session in sessions)
                        {
                            string day = session.SessionDay ?? string.Empty;
                            string quoteTimeStr = session.Quotetime ?? string.Empty;

                            if (!string.IsNullOrWhiteSpace(quoteTimeStr))
                            {
                                var formatted = SetDateAndTime(quoteTimeStr);
                                symbolExpiryclose = string.Join(",", formatted);
                            }

                            string tradeTimeStr = session.Tradetime ?? string.Empty;
                            if (!string.IsNullOrWhiteSpace(tradeTimeStr))
                            {
                                var formattedRanges = SetDateAndTime(tradeTimeStr);
                                symbolExpiry = string.Join(",", formattedRanges);
                            }
                            switch (day)
                            {
                                case "Monday":
                                    Lblmondayquotedate.Content = symbolExpiryclose.ToString().Replace(",", ", "); ;
                                    Lblmondaytradedate.Content = symbolExpiry.ToString().Replace(",", ", "); ;
                                    break;
                                case "Tuesday":
                                    Lbltuesdayquotedate.Content = symbolExpiryclose.ToString().Replace(",", ", "); ;
                                    Lbltuesdaytradedate.Content = symbolExpiry.ToString().Replace(",", ", "); ;
                                    break;
                                case "Wednesday":
                                    Lblwednesdayquotedate.Content = symbolExpiryclose.ToString().Replace(",", ", "); ;
                                    Lblwednesdaytradedate.Content = symbolExpiry.ToString().Replace(",", ", "); ;
                                    break;
                                case "Thursday":
                                    Lblthursdayquotedate.Content = symbolExpiryclose.ToString().Replace(",", ", "); ;
                                    Lblthursdaytradedate.Content = symbolExpiry.ToString().Replace(",", ", "); ;
                                    break;
                                case "Friday":
                                    Lblfridayquotedate.Content = symbolExpiryclose.ToString().Replace(",", ", "); ;
                                    Lblfridaytradedate.Content = symbolExpiry.ToString().Replace(",", ", "); ;
                                    break;
                                case "Saturday":
                                    Lblsaturdayquotedate.Content = symbolExpiryclose.ToString().Replace(",", ", "); ;
                                    Lblsaturdaytradedate.Content = symbolExpiry.ToString().Replace(",", ", "); ;
                                    break;
                                case "Sunday":
                                    Lblsundayquotedate.Content = symbolExpiryclose.ToString().Replace(",", ", "); ;
                                    Lblsundaytradedate.Content = symbolExpiry.ToString().Replace(",", ", "); ;
                                    break;
                            }
                        }
                    }
                }
            }
        }
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Window parentWindow = Window.GetWindow(this);
            if (parentWindow != null)
            {
                parentWindow.Close();
            }
        }

        #endregion Events
    }
}
