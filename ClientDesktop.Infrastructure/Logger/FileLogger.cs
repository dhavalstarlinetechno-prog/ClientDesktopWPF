using ClientDesktop.Core.Config;

namespace ClientDesktop.Infrastructure.Logger
{
    public static class FileLogger
    { 
        public static Action<string, string, string> OnLogReceived;

        private static readonly string LogDirectory = Path.Combine(Directory.GetParent(AppConfig.AppDataPath).FullName, "Logs");
        private static readonly string ApplicationLogDirectory = Path.Combine(Directory.GetParent(AppConfig.AppDataPath).FullName, "Application Logger");

        public static void Log(string source, string message)
        {
            try
            {
                DateTime now = DateTime.Now;
                string timeForGrid = now.ToString("yyyy.MM.dd HH:mm:ss.fff");
                string dateForFile = now.ToString("yyyyMMdd");

                if (!Directory.Exists(LogDirectory)) Directory.CreateDirectory(LogDirectory);

                string filePath = Path.Combine(LogDirectory, $"{dateForFile}.log");

                string logLine = $"{timeForGrid}\t{source}\t{message}{Environment.NewLine}";
                File.AppendAllText(filePath, logLine);

                OnLogReceived?.Invoke(timeForGrid, source, message);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Logging Failed: " + ex.Message);
            }
        }

        public static void ApplicationLog(string methodName, string message)
        {
            try
            {
                DateTime now = DateTime.Now;
                string formateTime = now.ToString("yyyy.MM.dd HH:mm:ss.fff");

                if (!Directory.Exists(ApplicationLogDirectory)) Directory.CreateDirectory(ApplicationLogDirectory);

                string filePath = Path.Combine(ApplicationLogDirectory, "ApplicationLogs.log");

                string logLine = $"{formateTime}\t{methodName}\t{message}{Environment.NewLine}";
                File.AppendAllText(filePath, logLine);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Logging Failed: " + ex.Message);
            }
        }

        public static void ApplicationLog(string methodName = "", Exception ex = null)
        {
            try
            {
                DateTime now = DateTime.Now;
                string formattedTime = now.ToString("yyyy.MM.dd HH:mm:ss.fff");

                if (!Directory.Exists(ApplicationLogDirectory))
                    Directory.CreateDirectory(ApplicationLogDirectory);

                string filePath = Path.Combine(ApplicationLogDirectory, "ApplicationLogs.log");

                string exceptionDetails = ex == null
                    ? "No Exception Data"
                    : $"Message: {ex.Message} | StackTrace: {ex.StackTrace} | InnerException: {ex.InnerException?.Message}";

                string logLine = $"{formattedTime}\t{methodName}\t{exceptionDetails}{Environment.NewLine}";
                File.AppendAllText(filePath, logLine);
            }
            catch (Exception logEx)
            {
                System.Diagnostics.Debug.WriteLine("Logging Failed: " + logEx.Message);
            }
        }

    }
}