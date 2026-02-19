using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace ClientDesktop.Infrastructure.Services
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
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

            var window = new Window
            {
                Title = title,
                Content = viewModel,
                SizeToContent = SizeToContent.WidthAndHeight,
                ResizeMode = ResizeMode.NoResize,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ShowInTaskbar = false,
                Owner = Application.Current.MainWindow
            };

            if (viewModel is ICloseable closeableVm)
            {
                closeableVm.CloseAction = () => window.Close();
            }

            window.ShowDialog();

            onDialogClose?.Invoke(viewModel);
        }
    }
}
