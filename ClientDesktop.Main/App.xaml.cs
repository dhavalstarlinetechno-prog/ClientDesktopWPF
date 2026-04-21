using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Services;
using ClientDesktop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Input;

namespace ClientDesktop.Main
{
    /// <summary>
    /// Interaction logic for App.xaml. Acts as the entry point and configures dependency injection.
    /// </summary>
    public partial class App : Application
    {
        #region Properties

        /// <summary>
        /// Gets the global service provider for dependency injection.
        /// </summary>
        public static IServiceProvider ServiceProvider { get; private set; }

        #endregion

        #region Protected Methods

        /// <summary>
        /// Triggers upon application startup to initialize services and launch the main window.
        /// </summary>
        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            ServiceProvider = services.BuildServiceProvider();
            AppServiceLocator.Current = ServiceProvider;

            _ = ServiceProvider.GetRequiredService<SystemMonitorService>();

            // ═══ Global ESC-to-Close: Register class-level handler for all Windows ═══
            EventManager.RegisterClassHandler(
                typeof(Window),
                Keyboard.KeyDownEvent,
                new KeyEventHandler(OnGlobalKeyDown));

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            if (mainWindow.DataContext is MainWindowViewModel vm)
            {
                _ = vm.InitializeHomeAsync();
            }

            base.OnStartup(e);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Global ESC key handler. Closes the active dialog window
        /// unless the ViewModel opts out via IEscCloseable.AllowEscClose = false.
        /// MainWindow is always protected from ESC close.
        /// </summary>
        private void OnGlobalKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;

            try
            {
                var window = sender as Window;
                if (window == null)
                    return;

                // Never close the MainWindow on ESC
                if (window is MainWindow)
                    return;

                // Check if the ViewModel opts out of ESC close
                if (window.Content is IEscCloseable escCloseable
                    && !escCloseable.AllowEscClose)
                    return;

                window.Close();
                e.Handled = true;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(OnGlobalKeyDown), ex);
            }
        }

        /// <summary>
        /// Registers all required core services, view models, and views into the dependency injection container.
        /// </summary>
        private void ConfigureServices(IServiceCollection services)
        {
            // Core Services
            services.AddSingleton<SessionService>();
            services.AddSingleton<SystemMonitorService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<ClientService>();
            services.AddSingleton<ChangePasswordService>();
            services.AddSingleton<MarketWatchService>();
            services.AddSingleton<PositionService>();
            services.AddSingleton<HistoryService>();
            services.AddSingleton<BanScriptService>();
            services.AddSingleton<LedgerService>();
            services.AddSingleton<SymbolSpecificationService>();
            services.AddSingleton<SymbolService>();
            services.AddSingleton<InvoiceService>();
            services.AddSingleton<FeedbackService>();

            // Api Service, Tradeservice , Socketservice
            services.AddSingleton<IApiService, ApiService>();

            // PDF Service
            services.AddTransient<IPdfService, PdfService>();

            // Excel Service — Transient: same reason as PDF
            services.AddTransient<IExcelService, ExcelService>();

            // Dialog Service (Interface mapping)
            services.AddSingleton<IDialogService, DialogService>();

            // Socket Service
            services.AddSingleton<ISocketService, SocketService>();
            services.AddSingleton<ITradeService, TradeService>();

            // SignalR Service
            services.AddSingleton<LiveTickService>();

            // ViewModels
            services.AddSingleton<MainWindowViewModel>();
            services.AddTransient<LoginPageViewModel>();
            services.AddTransient<ChangePasswordViewModel>();
            services.AddTransient<DisclaimerViewModel>();
            services.AddSingleton<MarketWatchViewModel>();
            services.AddSingleton<HistoryViewModel>();
            services.AddSingleton<PositionViewModel>();
            services.AddTransient<BanScriptViewModel>();
            services.AddTransient<LedgerViewModel>();
            services.AddTransient<SymbolSpecificationViewModel>();
            services.AddSingleton<SymbolViewModel>();
            services.AddTransient<InvoiceViewModel>();
            services.AddTransient<TradeViewModel>();
            services.AddSingleton<NavigationViewModel>();
            services.AddTransient<DeleteTradeViewModel>();
            services.AddTransient<FeedbackViewModel>();
            services.AddTransient<DeleteFeedbackViewModel>();
            // Views (Windows)
            services.AddSingleton<MainWindow>();
        }

        #endregion
    }
}