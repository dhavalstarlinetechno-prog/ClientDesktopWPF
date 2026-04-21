namespace ClientDesktop.Core.Interfaces
{
    /// <summary>
    /// Implement this interface on any ViewModel to control
    /// the global ESC-to-close behavior for its dialog window.
    /// By default (without this interface), all dialog windows close on ESC.
    /// </summary>
    public interface IEscCloseable
    {
        /// <summary>
        /// Gets a value indicating whether the ESC key should close the dialog.
        /// Return false to prevent the dialog from closing on ESC.
        /// </summary>
        bool AllowEscClose { get; }
    }
}