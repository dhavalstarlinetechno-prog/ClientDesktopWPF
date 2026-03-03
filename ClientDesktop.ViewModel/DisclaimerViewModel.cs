using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
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
            CloseCommand = new RelayCommand(OnClose);
            AcknowledgeCommand = new RelayCommand(OnAcknowledge);
        }

        private void OnClose(object parameter)
        {
            IsAcknowledged = false;
            CloseAction?.Invoke();
        }

        private void OnAcknowledge(object parameter)
        {
            IsAcknowledged = true;
            CloseAction?.Invoke();
        }
    }
}
