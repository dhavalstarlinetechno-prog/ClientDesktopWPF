using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq; // Required for LINQ queries
using System.Windows;
using System.Windows.Data;
using System.Windows.Input; // Required for RoutedCommand
using System.Windows.Media;
using AvalonDock.Layout; // Required for LayoutAnchorable and Descendents
using AvalonDock.Layout.Serialization;

namespace WpfApp1
{
    public partial class MainWindow : Window
    {
        private const string LayoutFileName = "layout.xml";

        // Define Commands for Shortcuts
        public static RoutedCommand ToggleMarketWatchCommand = new RoutedCommand();
        public static RoutedCommand ToggleNavigatorCommand = new RoutedCommand();
        public static RoutedCommand ToggleToolboxCommand = new RoutedCommand();

        public MainWindow()
        {
            InitializeComponent();

            // LoadData(); // Commented as per empty UI requirement

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        // --- COMMAND HANDLERS ---
        // We use a helper method to find the pane dynamically because after 
        // loading a layout, the original x:Name references (like MarketWatchPane) 
        // might point to old objects that are no longer part of the UI.

        private void ToggleMarketWatch_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleAnchorable("MarketWatch");
        }

        private void ToggleNavigator_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleAnchorable("Navigator");
        }

        private void ToggleToolbox_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleAnchorable("Toolbox");
        }

        // --- HELPER FOR TOGGLING ---
        private void ToggleAnchorable(string contentId)
        {
            if (dockManager.Layout == null) return;

            // Find the anchorable by ContentId in the CURRENT layout tree
            var anchorable = dockManager.Layout.Descendents()
                .OfType<LayoutAnchorable>()
                .FirstOrDefault(a => a.ContentId == contentId);

            if (anchorable != null)
            {
                if (anchorable.IsVisible)
                    anchorable.Hide();
                else
                    anchorable.Show();
            }
        }

        // --- LAYOUT LOGIC ---
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var serializer = new XmlLayoutSerializer(dockManager);
            serializer.LayoutSerializationCallback += (s, args) =>
            {
                args.Content = args.Content;
            };

            if (File.Exists(LayoutFileName))
            {
                try
                {
                    serializer.Deserialize(LayoutFileName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to load layout: " + ex.Message);
                }
            }
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var serializer = new XmlLayoutSerializer(dockManager);
            try
            {
                serializer.Serialize(LayoutFileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to save layout: " + ex.Message);
            }
        }

        private void LoadData()
        {
            /* Data Loading Logic (Commented out) */
        }
    }

    // --- MODELS & CONVERTERS (Same as before) ---
    public class MarketData { public string Symbol { get; set; } public double Bid { get; set; } public double Ask { get; set; } public int ChangeDirection { get; set; } }
    public class TradeEntry { public string Ticket { get; set; } public string OpenTime { get; set; } public string Type { get; set; } public string Volume { get; set; } public string Symbol { get; set; } public string Price { get; set; } public string Profit { get; set; } }
    public class JournalEntry { public string Time { get; set; } public string Source { get; set; } public string Message { get; set; } }

    public class ProfitToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) { if (value is string s) return s.Contains("+"); return null; }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class PriceChangeColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) { if (value is int change) return change > 0 ? Brushes.Blue : Brushes.Red; return Brushes.Black; }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}