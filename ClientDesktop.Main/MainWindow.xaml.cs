using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.View.Disclaimer;
using ClientDesktop.View.TradeOrder;
using ClientDesktop.ViewModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ClientDesktop.Main
{
    public partial class MainWindow : Window
    {
        private const string LayoutFileName = "layout.xml";

        public static RoutedCommand ToggleMarketWatchCommand = new RoutedCommand();
        public static RoutedCommand ToggleNavigatorCommand = new RoutedCommand();
        public static RoutedCommand ToggleToolboxCommand = new RoutedCommand();

        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            // --- CONNECT DISCLAIMER LOGIC ---
            viewModel.OpenDisclaimerAction = () =>
            {
                var disclaimer = new DisclaimerView();
                // Return true only if User clicked Acknowledge
                return disclaimer.ShowDialog() == true;
            };

            UpdateLoginState(false, null);

            // Listen for login changes
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.IsLoggedIn) ||
                    e.PropertyName == nameof(MainWindowViewModel.UserId))
                {
                    UpdateLoginState(viewModel.IsLoggedIn, viewModel.UserId);
                }
            };

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

            // Trigger Startup Logic
            if (DataContext is MainWindowViewModel vm)
            {
                _ = vm.InitializeHomeAsync();
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

        private void MenuConnect_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm) vm.ShowLoginWindow();
        }

        private void MenuDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                if (MessageBox.Show("Are you sure you want to disconnect?", "Disconnect", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    vm.DisconnectCommand.Execute(null);
                }
            }
        }

        public void UpdateLoginState(bool isLoggedIn, string username)
        {
            if (isLoggedIn)
            {
                TxtUserName.Text = username;
                TxtUserName.Visibility = Visibility.Visible;
                UserIconPath.Fill = new SolidColorBrush(Colors.Green);
                MenuConnect.Visibility = Visibility.Collapsed;
                MenuDisconnect.Visibility = Visibility.Visible;

                SessionManager.TriggerLogin();
            }
            else
            {
                TxtUserName.Text = "";
                TxtUserName.Visibility = Visibility.Collapsed;
                UserIconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
                MenuConnect.Visibility = Visibility.Visible;
                MenuDisconnect.Visibility = Visibility.Collapsed;

                SessionManager.TriggerLogout();
            }
        }

        private void NewOrder_Click(object sender, RoutedEventArgs e)
        {
            var orderWin = new OrderWindow();
            orderWin.Owner = this;
            orderWin.Show();
        }
    }
}