namespace ClientDesktop.Core.Interfaces
{
    public interface IRepository<T>
    {
        void Save(string filename, T data, string key = null);

        T Load(string filename, string key = null);
    }
}