namespace ClientDesktop.Core.Enums
{
    /// <summary>
    /// PDF column text alignment.
    /// Core layer ka apna enum — iText7 ka koi reference nahi.
    /// IPdfService aur ViewModels yahi use karenge.
    /// </summary>
    public enum PdfColumnAlignment
    {
        Left,
        Center,
        Right
    }
}