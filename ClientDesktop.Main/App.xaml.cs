using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Main.Login; 
using ClientDesktop.ViewModel;  
using Microsoft.Extensions.DependencyInjection; 
using System.Windows;

namespace ClientDesktop.Main
{
    public partial class App : Application
    {
        public static IServiceProvider ServiceProvider { get; private set; }

        protected override void OnStartup(StartupEventArgs e)
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            ServiceProvider = services.BuildServiceProvider();
            AppServiceLocator.Current = ServiceProvider;

            var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
            mainWindow.Show();

            if (mainWindow.DataContext is MainWindowViewModel vm)
            {
                _ = vm.InitializeHomeAsync();
            }

            base.OnStartup(e);
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // 1. Core Services
            services.AddSingleton<SessionService>();
            services.AddSingleton<AuthService>();
            services.AddSingleton<ClientService>();
            services.AddSingleton<MarketWatchService>();
            services.AddSingleton<PositionService>();
            services.AddSingleton<HistoryService>();
            services.AddSingleton<BanScriptService>();
            services.AddSingleton<LedgerService>();
            services.AddSingleton<SymbolSpecificationService>();

            // 2. Api Service
            services.AddSingleton<IApiService, ApiService>();

            // 3. Dialog Service (Interface mapping)
            services.AddSingleton<IDialogService, DialogService>();

            // 4. ViewModels
            services.AddTransient<LoginPageViewModel>();
            services.AddSingleton<MainWindowViewModel>();
            services.AddSingleton<MarketWatchViewModel>();
            services.AddSingleton<HistoryViewModel>();
            services.AddSingleton<PositionViewModel>();
            services.AddTransient<BanScriptViewModel>();
            services.AddTransient<LedgerViewModel>();
            services.AddTransient<SymbolSpecificationViewModel>();           
            // 5. Views (Windows)
            services.AddSingleton<MainWindow>();
        }
    }
}