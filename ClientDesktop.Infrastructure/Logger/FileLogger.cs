using ClientDesktop.Core.Config;

namespace ClientDesktop.Infrastructure.Logger
{
    public static class FileLogger
    {
        public static Action<string, string, string> OnLogReceived;

        private static readonly string LogDirectory = Path.Combine(AppConfig.AppDataPath, "Logs");

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
    }
}