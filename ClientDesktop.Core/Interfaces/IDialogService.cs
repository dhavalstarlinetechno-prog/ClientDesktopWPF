using ClientDesktop.Core.Base;

namespace ClientDesktop.Core.Interfaces
{
    public interface IDialogService
    {
        void ShowDialog<TViewModel>(string title, Action<TViewModel> onDialogClose = null)
            where TViewModel : ViewModelBase;

        void ShowDialog<TViewModel>(string title, Action<TViewModel> configureViewModel, Action<TViewModel> onDialogClose) 
            where TViewModel : ViewModelBase;
    }
}
