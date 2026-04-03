using ClientDesktop.Infrastructure.Logger;
using System;

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
            try
            {
                if (Current == null)
                {
                    FileLogger.ApplicationLog(nameof(GetService), "AppServiceLocator is not initialized. Call AppServiceLocator.Current = provider in App.xaml.cs");
                    return default;
                }

                return (T)Current.GetService(typeof(T));
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetService), ex);
                return default;
            }
        }
    }
}