using ClientDesktop.Infrastructure.Logger;
using System.Globalization;

namespace ClientDesktop.Infrastructure.Helpers
{
    public static class CommonHelper
    {
        #region Events And Basic Helpers
        // Helper Functions 
        public static string ToReplaceUrl(this string str, string domain, string replaceWith = "api")
        {
            try
            {
                return string.IsNullOrEmpty(str) || string.IsNullOrEmpty(replaceWith) || domain == null
                    ? str
                    : str.Replace(replaceWith, replaceWith + "." + domain);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ToReplaceUrl), ex);
                return str;
            }
        }

        public static string ToWebSocketUrl(this string serverName, int port = 6011)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(serverName))
                    throw new ArgumentException("Server name cannot be empty", nameof(serverName));

                // Ensure serverName does not contain protocol
                serverName = serverName.Replace("http://", "")
                                       .Replace("https://", "")
                                       .TrimEnd('/');

                return $"wss://skt.{serverName}:{port}/socket.io/?EIO=4&transport=websocket";
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ToWebSocketUrl), ex);
                return string.Empty;
            }
        }

        #endregion

        #region Utility Methods
        public static string GetLocalIPAddress()
        {
            try
            {
                string hostName = System.Net.Dns.GetHostName();
                var ip = System.Net.Dns.GetHostEntry(hostName)
                    .AddressList.FirstOrDefault(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);
                return ip?.ToString() ?? "127.0.0.1";
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetLocalIPAddress), ex);
                return "127.0.0.1";
            }
        }
        #endregion

        #region AmountFormatter
        public static string FormatAmount(decimal amount)
        {
            try
            {
                return FormatAmountInternal(amount);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(FormatAmount), ex);
                return amount.ToString();
            }
        }

        public static string FormatAmount(double amount)
        {
            try
            {
                return FormatAmountInternal((decimal)amount); // Convert double to decimal for precise formatting
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(FormatAmount), ex);
                return amount.ToString();
            }
        }

        /// <summary>
        /// Internal method to handle formatting
        /// </summary>
        private static string FormatAmountInternal(decimal amount)
        {
            try
            {
                NumberFormatInfo nfi = new NumberFormatInfo()
                {
                    NumberGroupSeparator = " ",
                    NumberDecimalDigits = 2,
                    NumberDecimalSeparator = "."
                };

                // Format with comma pattern but output uses space separator
                return amount.ToString("#,0.00", nfi);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(FormatAmountInternal), ex);
                return amount.ToString();
            }
        }
        #endregion AmountFormatter

        #region GMT Time
        public static DateTime ConvertUtcToIst(DateTime utcTime)
        {
            try
            {
                DateTime utcDateTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

                DateTime istTime = TimeZoneInfo.ConvertTimeFromUtc(
                    utcDateTime,
                    TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
                );
                return istTime;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ConvertUtcToIst), ex);
                // Fallback: Manually add 5 hours 30 mins if TimeZone is not found (often happens on non-Windows systems)
                return utcTime.AddHours(5).AddMinutes(30);
            }
        }
        #endregion
    }
}