using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace ClientDesktop.Infrastructure.Services
{
    /// <summary>
    /// Service responsible for creating and displaying dialog windows mapped to view models.
    /// </summary>
    public class DialogService : IDialogService
    {
        #region Fields

        private readonly IServiceProvider _serviceProvider;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the DialogService class.
        /// </summary>
        public DialogService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Displays a modal dialog window for the specified ViewModel type.
        /// </summary>
        public void ShowDialog<TViewModel>(string title, Action<TViewModel> onDialogClose = null, Action<TViewModel> configureViewModel = null)
            where TViewModel : ViewModelBase
        {
            var viewModel = _serviceProvider.GetRequiredService<TViewModel>();

            configureViewModel?.Invoke(viewModel);

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

        #endregion
    }
}