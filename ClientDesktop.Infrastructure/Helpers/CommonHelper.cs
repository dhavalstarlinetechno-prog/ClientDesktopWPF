using ClientDesktop.Core.Config;
using ClosedXML.Excel;
using Microsoft.Win32;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Collections;

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

        #region ExportExcel

        public static void ExportToExcel(DataGrid dgv, string title, string fileName)
        {
            try
            {
                // 1. Path & Folder Setup
                string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                string folder = Path.Combine(userProfile, "Downloads");

                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string fullPath = Path.Combine(folder, fileName + ".xlsx");

                int counter = 1;
                while (File.Exists(fullPath))
                {
                    fullPath = Path.Combine(folder, $"{fileName} ({counter}).xlsx");
                    counter++;
                }

                // 2. WPF SaveFileDialog
                SaveFileDialog save = new SaveFileDialog();
                save.Filter = "Excel Workbook|*.xlsx";
                save.Title = "Save Excel File";
                save.InitialDirectory = folder;
                save.FileName = Path.GetFileName(fullPath);

                if (save.ShowDialog() != true)
                    return;

                using (var workbook = new XLWorkbook())
                {
                    var sheet = workbook.Worksheets.Add("Sheet1");
                    int rowIndex = 1;

                    // ---- TITLE ----
                    var titleCell = sheet.Cell(rowIndex, 1);
                    titleCell.Value = title;
                    var titleRange = sheet.Range(rowIndex, 1, rowIndex, dgv.Columns.Count);
                    titleRange.Merge().Style.Font.SetBold().Font.SetFontSize(14).Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

                    rowIndex += 2;

                    // ---- COLUMN HEADERS ----
                    for (int i = 0; i < dgv.Columns.Count; i++)
                    {
                        var header = sheet.Cell(rowIndex, i + 1);
                        header.Value = dgv.Columns[i].Header?.ToString() ?? $"Col {i + 1}";
                        header.Style.Font.Bold = true;
                        header.Style.Fill.BackgroundColor = XLColor.LightGray;
                        header.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    }

                    rowIndex++;

                    var items = dgv.ItemsSource as IEnumerable;
                    if (items == null)
                        return;


                    // ---- DATA ROWS ----
                    foreach (var item in dgv.Items)
                    {
                        if (item == System.Windows.Data.CollectionView.NewItemPlaceholder) continue;

                        for (int c = 0; c < dgv.Columns.Count; c++)
                        {
                            var excelCell = sheet.Cell(rowIndex, c + 1);
                            var column = dgv.Columns[c];

                            string value = "";

                            // ✅ CASE 1: ExpandoObject (Dynamic)
                            if (item is IDictionary<string, object> dict)
                            {
                                if (column is DataGridBoundColumn boundColumn && boundColumn.Binding is Binding binding)
                                {
                                    string key = binding.Path.Path;

                                    if (dict.ContainsKey(key) && dict[key] != null)
                                        value = dict[key].ToString();
                                }
                            }
                            // ✅ CASE 2: Normal Object (Existing logic)
                            else
                            {
                                if (column is DataGridBoundColumn boundColumn && boundColumn.Binding is Binding binding)
                                {
                                    var prop = item.GetType().GetProperty(binding.Path.Path);
                                    value = prop?.GetValue(item, null)?.ToString() ?? "";
                                }
                            }

                            excelCell.Value = value;

                            // Alignment
                            excelCell.Style.Alignment.Horizontal = GetColumnAlignment(column);
                        }

                        rowIndex++;
                    }

                    sheet.Columns().AdjustToContents();
                    workbook.SaveAs(save.FileName);
                }

                MessageBox.Show("Export Done!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch 
            {
                
            }
        }
    
        private static XLAlignmentHorizontalValues GetColumnAlignment(DataGridColumn column)
        {
            // Bhidu, yahan hum check kar rahe hain ki kya column TextColumn ya BoundColumn hai
            // Kyunki ElementStyle inhi ke paas hota hai
            Style elementStyle = null;

            if (column is DataGridBoundColumn boundColumn)
            {
                elementStyle = boundColumn.ElementStyle;
            }
            else if (column is DataGridComboBoxColumn comboColumn)
            {
                elementStyle = comboColumn.ElementStyle;
            }

            if (elementStyle != null)
            {
                foreach (Setter setter in elementStyle.Setters)
                {
                    if (setter.Property == FrameworkElement.HorizontalAlignmentProperty ||
                        setter.Property == Control.HorizontalContentAlignmentProperty)
                    {
                        if (setter.Value is HorizontalAlignment align)
                        {
                            return ConvertAlign(align);
                        }
                    }
                }
            }

            // Agar kuch nahi mila toh default Left alignment
            return XLAlignmentHorizontalValues.Left;
        }

        private static XLAlignmentHorizontalValues ConvertAlign(HorizontalAlignment align)
        {
            return align switch
            {
                HorizontalAlignment.Right => XLAlignmentHorizontalValues.Right,
                HorizontalAlignment.Center => XLAlignmentHorizontalValues.Center,
                _ => XLAlignmentHorizontalValues.Left,
            };
        }

        #endregion ExportExcel
    }
}
