using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Logger;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    /// <summary>
    /// ViewModel for managing the Disclaimer dialog state and actions.
    /// </summary>
    public class DisclaimerViewModel : ViewModelBase, ICloseable
    {
        public Action CloseAction { get; set; }

        // Use this property to track if the user acknowledged or just closed the dialog.
        public bool IsAcknowledged { get; private set; } = false;

        public ICommand CloseCommand { get; }
        public ICommand AcknowledgeCommand { get; }

        public DisclaimerViewModel()
        {
            try
            {
                CloseCommand = new RelayCommand(OnClose);
                AcknowledgeCommand = new RelayCommand(OnAcknowledge);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(DisclaimerViewModel), ex);
            }
        }

        private void OnClose(object parameter)
        {
            try
            {
                IsAcknowledged = false;
                CloseAction?.Invoke();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(OnClose), ex);
            }
        }

        private void OnAcknowledge(object parameter)
        {
            try
            {
                IsAcknowledged = true;
                CloseAction?.Invoke();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(OnAcknowledge), ex);
            }
        }
    }
}