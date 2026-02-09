namespace ClientDesktop.Core.Interfaces
{
    public interface ICloseable
    {
        Action? CloseAction { get; set; }
    }
}