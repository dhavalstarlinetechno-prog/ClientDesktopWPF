using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Services;
using ClientDesktop.ViewModel;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

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

            // Api Service, Tradeservice , Socketservice
            services.AddSingleton<IApiService, ApiService>();
            services.AddSingleton<ISocketService, SocketService>();
            services.AddSingleton<ITradeService, TradeService>();

            // SignalR Service
            services.AddSingleton<LiveTickService>();

            // Dialog Service (Interface mapping)
            services.AddSingleton<IDialogService, DialogService>();

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

            // Views (Windows)
            services.AddSingleton<MainWindow>();
        }

        #endregion
    }
}