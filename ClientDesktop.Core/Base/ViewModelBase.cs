using ClientDesktop.Core.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClientDesktop.Core.Base
{
    public class ViewModelBase : INotifyPropertyChanged, ICloseable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Action? CloseAction { get; set; }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
