using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace ClientDesktop.Main
{
    public partial class MainWindow : Window
    {
        private const string LayoutFileName = "layout.xml";

        public static RoutedCommand ToggleMarketWatchCommand = new RoutedCommand();
        public static RoutedCommand ToggleNavigatorCommand = new RoutedCommand();
        public static RoutedCommand ToggleToolboxCommand = new RoutedCommand();

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

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
    }
}