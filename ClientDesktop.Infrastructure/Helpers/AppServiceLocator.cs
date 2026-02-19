using ClientDesktop.Infrastructure.Logger;

namespace ClientDesktop.Infrastructure.Helpers
{
    /// <summary>
    /// A simple Service Locator to access DI Container from projects 
    /// that cannot reference App.xaml.cs (like Views).
    /// </summary>
    public static class AppServiceLocator
    {
        public static IServiceProvider Current { get; set; }

        public static T GetService<T>()
        {
            if (Current == null)
                FileLogger.Log("Error", "AppServiceLocator is not initialized. Call AppServiceLocator.Current = provider in App.xaml.cs");

            return (T)Current.GetService(typeof(T));
        }
    }
}
