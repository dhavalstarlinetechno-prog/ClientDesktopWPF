using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using ClientDesktop.ViewModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ClientDesktop.Main
{
    /// <summary>
    /// Interaction logic for the main application window, managing layout, docking, and session UI state.
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Fields

        private const string LayoutFileName = "layout.xml";

        public static readonly RoutedCommand ToggleMarketWatchCommand = new RoutedCommand();
        public static readonly RoutedCommand ToggleNavigatorCommand = new RoutedCommand();
        public static readonly RoutedCommand ToggleToolboxCommand = new RoutedCommand();

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainWindow class and sets up view model bindings.
        /// </summary>
        public MainWindow(MainWindowViewModel viewModel)
        {
            InitializeComponent();
            this.DataContext = viewModel;

            UpdateLoginState(false, null);

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

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the visibility and styling of the user profile UI based on the login state.
        /// </summary>
        public void UpdateLoginState(bool isLoggedIn, string username)
        {
            if (isLoggedIn)
            {
                TxtUserName.Text = username;
                TxtUserName.Visibility = Visibility.Visible;
                UserIconPath.Fill = new SolidColorBrush(Colors.Green);
                MenuConnect.Visibility = Visibility.Collapsed;
                MenuDisconnect.Visibility = Visibility.Visible;
            }
            else
            {
                TxtUserName.Text = string.Empty;
                TxtUserName.Visibility = Visibility.Collapsed;
                UserIconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
                MenuConnect.Visibility = Visibility.Visible;
                MenuDisconnect.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region Window Events

        /// <summary>
        /// Handles the window loaded event to deserialize and apply the saved AvalonDock layout.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Maximized;
            Window_StateChanged(this, EventArgs.Empty);

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

        /// <summary>
        /// Handles the window closing event to serialize and save the current AvalonDock layout.
        /// </summary>
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

        /// <summary>
        /// Adjusts the window margins and maximize/restore button icons based on the window state.
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                RootGrid.Margin = new Thickness(8);
            }
            else
            {
                RootGrid.Margin = new Thickness(0);
            }
        }

        #endregion

        #region Command Executed Handlers

        /// <summary>
        /// Toggles the visibility of the Market Watch anchorable window.
        /// </summary>
        private void ToggleMarketWatch_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleAnchorable("MarketWatch");
        }

        /// <summary>
        /// Toggles the visibility of the Navigator anchorable window.
        /// </summary>
        private void ToggleNavigator_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleAnchorable("Navigator");
        }

        /// <summary>
        /// Toggles the visibility of the Toolbox anchorable window.
        /// </summary>
        private void ToggleToolbox_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ToggleAnchorable("Toolbox");
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// Minimizes the application window.
        /// </summary>
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        /// <summary>
        /// Maximizes or restores the application window.
        /// </summary>
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }

        /// <summary>
        /// Closes the application window.
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        /// <summary>
        /// Initiates the login process by opening the login window.
        /// </summary>
        private void MenuConnect_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.ShowLoginWindow();
            }
        }

        /// <summary>
        /// Initiates the disconnect process to log out the current user.
        /// </summary>
        private void MenuDisconnect_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainWindowViewModel vm)
            {
                vm.DisconnectCommand.Execute(null);
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Toggles the visibility of a layout anchorable pane based on its content ID.
        /// </summary>
        private void ToggleAnchorable(string contentId)
        {
            if (dockManager.Layout == null) return;

            var anchorable = dockManager.Layout.Descendents()
                .OfType<LayoutAnchorable>()
                .FirstOrDefault(a => a.ContentId == contentId);

            if (anchorable != null)
            {
                if (anchorable.IsVisible)
                {
                    anchorable.Hide();
                }
                else
                {
                    anchorable.Show();
                }
            }
        }

        #endregion
    }
}