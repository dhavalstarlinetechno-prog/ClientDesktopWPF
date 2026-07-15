using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.View.Chart;
using ClientDesktop.ViewModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
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
        private const string ChartsStateFileName = "charts_state.json";

        public static readonly RoutedCommand ToggleMarketWatchCommand = new RoutedCommand();
        public static readonly RoutedCommand ToggleNavigatorCommand = new RoutedCommand();
        public static readonly RoutedCommand ToggleToolboxCommand = new RoutedCommand();
        public static readonly RoutedCommand ToggleChartCommand = new RoutedCommand();

        private IApiService _apiService;
        private SessionService _sessionService;
        private LiveTickService _liveTickService;
        private readonly List<LayoutDocument> _hiddenChartDocuments = new List<LayoutDocument>();
        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the MainWindow class and sets up view model bindings.
        /// </summary>
        public MainWindow(MainWindowViewModel viewModel, IApiService apiService, SessionService sessionService, LiveTickService liveTickService)
        {
            try
            {
                InitializeComponent();

                this.DataContext = viewModel;
                _apiService = apiService;
                _sessionService = sessionService;
                _liveTickService = liveTickService;
                UpdateLoginState(false, null);

                viewModel.PropertyChanged += (s, e) =>
                {
                    try
                    {
                        if (e.PropertyName == nameof(MainWindowViewModel.IsLoggedIn) ||
                            e.PropertyName == nameof(MainWindowViewModel.UserId))
                        {
                            UpdateLoginState(viewModel.IsLoggedIn, viewModel.UserId);
                        }
                        else if (e.PropertyName == nameof(MainWindowViewModel.IsPasswordReadonly))
                        {
                            MenuNewOrder.Visibility = (!viewModel.IsPasswordReadonly
                                                       && viewModel.IsLoggedIn)
                                ? Visibility.Visible
                                : Visibility.Collapsed;
                        }
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog(nameof(MainWindow) + "_PropertyChanged", ex);
                    }
                };

                this.Loaded += MainWindow_Loaded;
                this.Closing += MainWindow_Closing;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(MainWindow), ex);
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Updates the visibility and styling of the user profile UI based on the login state.
        /// </summary>
        public void UpdateLoginState(bool isLoggedIn, string username)
        {
            try
            {
                if (isLoggedIn)
                {
                    TxtUserName.Text = username;
                    TxtUserName.Visibility = Visibility.Visible;
                    UserIconPath.Fill = new SolidColorBrush(Colors.Green);
                    MenuConnect.Visibility = Visibility.Collapsed;
                    MenuDisconnect.Visibility = Visibility.Visible;
                    MenuChangePassword.Visibility = Visibility.Visible;
                }
                else
                {
                    TxtUserName.Text = string.Empty;
                    TxtUserName.Visibility = Visibility.Collapsed;
                    UserIconPath.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#666666"));
                    MenuConnect.Visibility = Visibility.Visible;
                    MenuDisconnect.Visibility = Visibility.Collapsed;
                    MenuChangePassword.Visibility = Visibility.Collapsed;
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(UpdateLoginState), ex);
            }
        }

        public void OpenDynamicChartTab(int symbolId, string symbolName, string masterSymbolName, int symbolDigits)
        {
            try
            {
                var documentPane = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                if (documentPane == null) return;

                string contentIdStr = $"Chart_{symbolName.Trim().ToUpper()}";

                //var existingDoc = documentPane.Children.FirstOrDefault(d => d.ContentId == contentIdStr);
                //if (existingDoc != null)
                //{
                //    existingDoc.IsActive = true;
                //    return;
                //}

                // Create chart window safely
                var chartControl = new ChartTradeWindow(symbolId, symbolName, masterSymbolName, symbolDigits, _apiService, _sessionService, _liveTickService);

                var newDocument = new LayoutDocument
                {
                    Title = $"{symbolName} - Chart",
                    ContentId = contentIdStr,
                    Content = chartControl,
                    CanClose = true
                };

                newDocument.Closed += (s, e) =>
                {
                    chartControl.CloseAndCleanup();
                };

                documentPane.Children.Add(newDocument);
                newDocument.IsActive = true;
                _ = chartControl.NotifyTabActive(true);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(OpenDynamicChartTab), ex);
            }
        }

        #endregion

        #region Window Events

        /// <summary>
        /// Handles the window loaded event to deserialize and apply the saved AvalonDock layout.
        /// </summary>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                this.WindowState = WindowState.Maximized;
                Window_StateChanged(this, EventArgs.Empty);

                var serializer = new XmlLayoutSerializer(dockManager);

                serializer.LayoutSerializationCallback += (s, args) =>
                {
                    //args.Content = args.Content;
                    if (args.Model.ContentId != null && args.Model.ContentId.StartsWith("Chart_", StringComparison.OrdinalIgnoreCase))
                    {
                        args.Cancel = true;
                    }
                };

                if (File.Exists(LayoutFileName))
                {
                    try
                    {
                        serializer.Deserialize(LayoutFileName);
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog(nameof(MainWindow_Loaded) + "_Deserialize", ex);
                    }
                }
                if (File.Exists(ChartsStateFileName))
                {
                    try
                    {
                        string json = File.ReadAllText(ChartsStateFileName);
                        var savedCharts = Newtonsoft.Json.JsonConvert.DeserializeObject<System.Collections.Generic.List<SavedChartInfo>>(json);

                        if (savedCharts != null)
                        {
                            foreach (var chart in savedCharts)
                            {
                                OpenDynamicChartTab(chart.SymbolId, chart.SymbolName, chart.MasterSymbolName, chart.SymbolDigits);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        FileLogger.ApplicationLog(nameof(MainWindow_Loaded) + "_RestoreCharts", ex);
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(MainWindow_Loaded), ex);
            }
        }

        /// <summary>
        /// Handles the window closing event to serialize and save the current AvalonDock layout.
        /// </summary>
        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            try
            {
                var serializer = new XmlLayoutSerializer(dockManager);
                serializer.Serialize(LayoutFileName);

                if (dockManager.Layout != null)
                {
                    var documentPane = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
                    if (documentPane != null)
                    {
                        var chartsToSave = new System.Collections.Generic.List<SavedChartInfo>();

                        foreach (var doc in documentPane.Children)
                        {
                            if (doc.Content is ChartTradeWindow chartWindow)
                            {
                                var vm = chartWindow.DataContext as ClientDesktop.ViewModel.Chart.ChartTradeWindowViewModel;
                                chartsToSave.Add(new SavedChartInfo
                                {
                                    SymbolId = chartWindow.SymbolId,
                                    SymbolName = chartWindow.SymbolName,
                                    MasterSymbolName = vm?.MasterSymbolName ?? chartWindow.SymbolName,
                                    SymbolDigits = vm?.SymbolDigits ?? 2
                                });
                            }
                        }

                        string json = Newtonsoft.Json.JsonConvert.SerializeObject(chartsToSave, Newtonsoft.Json.Formatting.Indented);
                        File.WriteAllText(ChartsStateFileName, json);
                    }
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(MainWindow_Closing), ex);
            }
        }

        /// <summary>
        /// Adjusts the window margins and maximize/restore button icons based on the window state.
        /// </summary>
        private void Window_StateChanged(object sender, EventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(Window_StateChanged), ex);
            }
        }

        #endregion

        #region Command Executed Handlers

        /// <summary>
        /// Toggles the visibility of the Market Watch anchorable window.
        /// </summary>
        private void ToggleMarketWatch_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                ToggleAnchorable("MarketWatch");
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ToggleMarketWatch_Executed), ex);
            }
        }

        /// <summary>
        /// Toggles the visibility of the Navigator anchorable window.
        /// </summary>
        private void ToggleNavigator_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                ToggleAnchorable("Navigator");
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ToggleNavigator_Executed), ex);
            }
        }

        /// <summary>
        /// Toggles the visibility of the Toolbox anchorable window.
        /// </summary>
        private void ToggleToolbox_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                ToggleAnchorable("Toolbox");
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ToggleToolbox_Executed), ex);
            }
        }

        private void ToggleChart_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                ToggleChartDocument();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ToggleChart_Executed), ex);
            }
        }

        #endregion

        #region UI Event Handlers

        /// <summary>
        /// Minimizes the application window.
        /// </summary>
        private void Minimize_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.WindowState = WindowState.Minimized;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(Minimize_Click), ex);
            }
        }

        /// <summary>
        /// Maximizes or restores the application window.
        /// </summary>
        private void Maximize_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(Maximize_Click), ex);
            }
        }

        /// <summary>
        /// Closes the application window.
        /// </summary>
        private void Close_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                this.Close();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(Close_Click), ex);
            }
        }

        /// <summary>
        /// Initiates the login process by opening the login window.
        /// </summary>
        private void MenuConnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.ShowLoginWindow();
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(MenuConnect_Click), ex);
            }
        }

        /// <summary>
        /// Initiates the disconnect process to log out the current user.
        /// </summary>
        private void MenuDisconnect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is MainWindowViewModel vm)
                {
                    vm.DisconnectCommand.Execute(null);
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(MenuDisconnect_Click), ex);
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Toggles the visibility of a layout anchorable pane based on its content ID.
        /// </summary>
        private void ToggleAnchorable(string contentId)
        {
            try
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
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ToggleAnchorable), ex);
            }
        }

        /// <summary>
        /// Toggles the visibility of a chart layout 
        /// </summary>
        private void ToggleChartDocument()
        {
            if (dockManager.Layout == null) return;

            var documentPane = dockManager.Layout.Descendents().OfType<LayoutDocumentPane>().FirstOrDefault();
            if (documentPane == null) return;
           
            if (_hiddenChartDocuments.Count > 0)
            {
                foreach (var doc in _hiddenChartDocuments)
                {
                    documentPane.Children.Add(doc);
                }
                _hiddenChartDocuments.Last().IsActive = true;
                _hiddenChartDocuments.Clear();
                return;
            }
            
            var chartsToHide = documentPane.Children.OfType<LayoutDocument>()
                .Where(d => d.ContentId == "Chart" ||
                            (d.ContentId != null && d.ContentId.StartsWith("Chart_", StringComparison.OrdinalIgnoreCase)))
                .ToList();

            if (chartsToHide.Count == 0)
            {               
                var newChartDoc = new LayoutDocument
                {
                    Title = "Chart",
                    ContentId = "Chart",
                    Content = new Grid { Background = System.Windows.Media.Brushes.White },
                    CanClose = true
                };
                documentPane.Children.Add(newChartDoc);
                newChartDoc.IsActive = true;
                return;
            }

            foreach (var doc in chartsToHide)
            {
                _hiddenChartDocuments.Add(doc);
                documentPane.Children.Remove(doc);
            }
        }

        #endregion
    }
    public class SavedChartInfo
    {
        public int SymbolId { get; set; }
        public string SymbolName { get; set; }
        public string MasterSymbolName { get; set; }
        public int SymbolDigits { get; set; }
    }
}