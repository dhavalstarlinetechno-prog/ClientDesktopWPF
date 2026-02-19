using System.Globalization;

namespace ClientDesktop.Infrastructure.Helpers
{
    public static class CommonHelper
    {
        #region Events And Basic Helpers
        // Helper Functions 
        public static string ToReplaceUrl(this string str, string domain, string replaceWith = "api")
        {
            return string.IsNullOrEmpty(str) || string.IsNullOrEmpty(replaceWith) || domain == null
                ? str
                : str.Replace(replaceWith, replaceWith + "." + domain);
        }

        public static string ToWebSocketUrl(this string serverName, int port = 6011)
        {
            if (string.IsNullOrWhiteSpace(serverName))
                throw new ArgumentException("Server name cannot be empty", nameof(serverName));

            // Ensure serverName does not contain protocol
            serverName = serverName.Replace("http://", "")
                                   .Replace("https://", "")
                                   .TrimEnd('/');

            return $"wss://skt.{serverName}:{port}/socket.io/?EIO=4&transport=websocket";
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
            catch
            {
                return "127.0.0.1";
            }
        }
        #endregion

        #region AmountFormatter
        public static string FormatAmount(decimal amount)
        {
            return FormatAmountInternal(amount);
        }

        public static string FormatAmount(double amount)
        {
            return FormatAmountInternal((decimal)amount); // Convert double to decimal for precise formatting
        }

        /// <summary>
        /// Internal method to handle formatting
        /// </summary>
        private static string FormatAmountInternal(decimal amount)
        {
            NumberFormatInfo nfi = new NumberFormatInfo()
            {
                NumberGroupSeparator = " ",
                NumberDecimalDigits = 2,
                NumberDecimalSeparator = "."
            };

            // "#,0.00" pattern with comma replaced by space
            //return amount.ToString("#,0.00", nfi).Replace(",", " ");

            // Format with comma pattern but output uses space separator
            return amount.ToString("#,0.00", nfi);
        }
        #endregion AmountFormatterm

        #region GMT Time
        public static DateTime ConvertUtcToIst(DateTime utcTime)
        {
            DateTime utcDateTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

            DateTime istTime = TimeZoneInfo.ConvertTimeFromUtc(
                utcDateTime,
                TimeZoneInfo.FindSystemTimeZoneById("India Standard Time")
            );
            return istTime;
        }
        #endregion
    }
}
