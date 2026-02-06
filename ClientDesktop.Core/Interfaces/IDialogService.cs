using ClientDesktop.Core.Base;

namespace ClientDesktop.Core.Interfaces
{
    public interface IDialogService
    {
        void ShowDialog<TViewModel>(string title, Action<TViewModel> onDialogClose = null)
            where TViewModel : ViewModelBase;
    }
}
