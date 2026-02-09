using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace ClientDesktop.Main.Login
{
    public class DialogService : IDialogService
    {
        private readonly IServiceProvider _serviceProvider;

        public DialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public void ShowDialog<TViewModel>(string title, Action<TViewModel> onDialogClose = null)
            where TViewModel : ViewModelBase
        {
            // 1. ViewModel create karo DI container se
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

            // 2. Generic Window create karo (ek hi window sabke liye!)
            var window = new Window
            {
                Title = title,
                Content = viewModel, // WPF DataTemplate se View dhund lega
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            // 3. Logic handle karo
            if (viewModel is ICloseable closeableVm)
            {
                closeableVm.CloseAction = () => window.Close();
            }

            window.ShowDialog();

            // 4. Result wapas bhejo
            onDialogClose?.Invoke(viewModel);
        }
    }
}
