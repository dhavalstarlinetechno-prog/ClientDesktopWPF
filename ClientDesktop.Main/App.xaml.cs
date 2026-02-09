using ClientDesktop.Core.Interfaces;
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
            services.AddSingleton<AuthService>();
            services.AddSingleton<ClientService>();

            // 2. Dialog Service (Interface mapping)
            services.AddSingleton<IDialogService, DialogService>();

            // 3. ViewModels
            services.AddTransient<LoginPageViewModel>();
            services.AddSingleton<MainWindowViewModel>();

            // 4. Views (Windows)
            services.AddSingleton<MainWindow>();
        }
    }
}