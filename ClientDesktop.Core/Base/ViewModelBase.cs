using ClientDesktop.Core.Interfaces;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Threading;

namespace ClientDesktop.Core.Base
{
    public class ViewModelBase : INotifyPropertyChanged, ICloseable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public Action? CloseAction { get; set; }

        /// <summary>
        /// Raises the PropertyChanged event to notify listeners that a property value has changed.
        /// </summary>
        /// <remarks>Call this method in a property's setter to notify subscribers that the property's
        /// value has changed. This is commonly used to support data binding in applications that implement the
        /// INotifyPropertyChanged interface.</remarks>
        /// <param name="propertyName">The name of the property that changed. This value is optional and is automatically provided when called from
        /// a property setter.</param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

       /// <summary>
       /// Sets the specified field to the given value and raises a property changed notification if the value has
       /// changed.
       /// </summary>
       /// <remarks>This method is typically used in property setters to implement the
       /// INotifyPropertyChanged pattern. It prevents unnecessary property change notifications when the value has not
       /// changed.</remarks>
       /// <typeparam name="T">The type of the property being set.</typeparam>
       /// <param name="storage">A reference to the field that stores the property's current value. This value will be updated if it differs
       /// from <paramref name="value"/>.</param>
       /// <param name="value">The new value to assign to the property.</param>
       /// <param name="propertyName">The name of the property that changed. This parameter is optional and is automatically provided by the
       /// compiler when called from a property setter.</param>
       /// <returns><see langword="true"/> if the value was changed and the notification was raised; otherwise, <see
       /// langword="false"/>.</returns>
        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(storage, value)) return false;
            storage = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Method 1: Fire & Forget (No Await)
        /// Use for High-Frequency updates like Live Ticks or Timers. Doesn't block the background thread.
        /// </summary>
        protected void SafeUIInvoke(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            var app = Application.Current;

            if (app == null || app.Dispatcher == null || app.Dispatcher.HasShutdownStarted)
                return;

            app.Dispatcher.InvokeAsync(action, priority);
        }

        /// <summary>
        /// Method 2: Synchronous Wait (Block)
        /// Use for Dialogs, MessageBox, where the code MUST wait for user input before proceeding.
        /// </summary>
        protected void SafeUIInvokeSync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            var app = Application.Current;

            if (app == null || app.Dispatcher == null || app.Dispatcher.HasShutdownStarted)
                return;

            app.Dispatcher.Invoke(action, priority);
        }

        /// <summary>
        /// Method 3: Asynchronous Wait (Task)
        /// Use for Heavy Data Loading where you want to await the UI update completion without blocking the thread.
        /// </summary>
        protected Task SafeUIInvokeAsync(Action action, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            var app = Application.Current;

            if (app == null || app.Dispatcher == null || app.Dispatcher.HasShutdownStarted)
                return Task.CompletedTask;

            return app.Dispatcher.InvokeAsync(action, priority).Task;
        }

        /// <summary>
        /// BONUS Method 4: Get Data from UI Thread
        /// Use when you need to read a property from a UI element or perform a calculation on the UI thread and get the result back.
        /// </summary>
        protected Task<T?> SafeUIInvokeAsync<T>(Func<T> func, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            var app = Application.Current;

            if (app == null || app.Dispatcher == null || app.Dispatcher.HasShutdownStarted)
                return Task.FromResult(default(T));

            return app.Dispatcher.InvokeAsync(func, priority).Task;
        }
    }
}
