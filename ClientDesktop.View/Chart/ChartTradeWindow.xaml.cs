using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.ViewModel.Chart;
using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ClientDesktop.View.Chart
{
    public partial class ChartTradeWindow : UserControl
    {
        public int SymbolId { get; private set; }
        public string SymbolName { get; private set; }

        private ChartTradeWindowViewModel _viewModel;
        private bool _isWebViewInitialized = false;

        public ChartTradeWindow(int symbolId, string symbolName, string masterSymbolName, int symbolDigits, IApiService apiService, SessionService sessionService, LiveTickService liveTickService)
        {
            InitializeComponent();
            SymbolId = symbolId;
            SymbolName = symbolName;
            var chartService = new ChartService(apiService, sessionService);
            _viewModel = new ChartTradeWindowViewModel(symbolId, symbolName, masterSymbolName, symbolDigits, chartService, liveTickService);
            DataContext = _viewModel;
        }
        public async Task NotifyTabActive(bool isActive)
        {
            await _viewModel.SetTabActive(isActive);
        }
        public async Task FlushOnRestore()
        {
            await _viewModel.FlushOnAppRestore();
        }
        private async void ChartTradeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isWebViewInitialized) return;
            try
            {
                UpdateLoadingText("Initializing Chart Environment...");

                var env = await CoreWebView2Environment.CreateAsync(null, null, null);
                await ChartWebView.EnsureCoreWebView2Async(env);

                string localFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Charts", "WebAssets");
                if (!Directory.Exists(localFolder))
                {
                    UpdateLoadingText($"Error: WebAssets folder not found at:\n{localFolder}");
                    return;
                }

                ChartWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                    ChartTradeWindowViewModel.VirtualHostName, localFolder, CoreWebView2HostResourceAccessKind.Allow);

                ChartWebView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                _viewModel.RegisterScriptExecutor(async (script) =>
                {
                    await Dispatcher.InvokeAsync(async () =>
                    {
                        if (ChartWebView?.CoreWebView2 != null && _isWebViewInitialized)
                        {
                            try
                            {
                                await ChartWebView.ExecuteScriptAsync(script);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[WEBVIEW2] Script Error: {ex.Message}");
                            }
                        }
                    });
                });

                _isWebViewInitialized = true;
                UpdateLoadingText("Loading Chart Components...");

                string querySymbol = Uri.EscapeDataString(_viewModel.SymbolName);
                string queryMaster = Uri.EscapeDataString(_viewModel.MasterSymbolName);
                int queryDigits = _viewModel.SymbolDigits;

                string dynamicUrl = $"https://{ChartTradeWindowViewModel.VirtualHostName}/index.html" +
                                   $"?symbol={querySymbol}" +
                                   $"&masterSymbol={queryMaster}" +
                                   $"&digits={queryDigits}";

                ChartWebView.CoreWebView2.Navigate(dynamicUrl);

                await Task.Delay(1500);
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                UpdateLoadingText($"Initialization failed: {ex.Message}");
            }
        }
        private async void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                string jsonMessage = e.TryGetWebMessageAsString();
                if (string.IsNullOrEmpty(jsonMessage)) return;

                if (jsonMessage.Contains("HISTORY_REQ"))
                {
                    var result = await _viewModel.HandleHistoryRequestAsync(jsonMessage);
                    KeepHistoryDispatched(result.ReqId, result.JsonData);
                }

                else if (jsonMessage.Contains("LAYOUT_SAVE"))
                {
                    try
                    {
                        string folderPath = Path.Combine(Directory.GetParent(AppConfig.ChartLayoutPath).FullName, "ChartLayouts");
                        if (!Directory.Exists(folderPath))
                        {
                            Directory.CreateDirectory(folderPath);
                        }

                        var incomingObj = Newtonsoft.Json.JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(jsonMessage);

                        string customLayoutName = incomingObj["layoutName"]?.ToString();

                        if (string.IsNullOrEmpty(customLayoutName))
                        {
                            Console.WriteLine("[CHART] Layout name is empty. Skipping save.");
                            return;
                        }

                        string filePath = Path.Combine(folderPath, $"{customLayoutName}_layout.json");
                        await File.WriteAllTextAsync(filePath, jsonMessage);
                        Console.WriteLine($"[CHART] Layout successfully saved to file: {filePath}");

                        string pointerPath = Path.Combine(folderPath, $"{_viewModel.MasterSymbolName}_pointer.txt");
                        await File.WriteAllTextAsync(pointerPath, customLayoutName);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CHART] Error in C# LAYOUT_SAVE: {ex.Message}");
                    }
                }

                else if (jsonMessage.Contains("LAYOUT_REQ"))
                {
                    try
                    {
                        string folderPath = Path.Combine(Directory.GetParent(AppConfig.ChartLayoutPath).FullName, "ChartLayouts");
                        string pointerPath = Path.Combine(folderPath, $"{_viewModel.MasterSymbolName}_pointer.txt");

                        string targetLayoutName = null;

                        if (File.Exists(pointerPath))
                        {
                            targetLayoutName = await File.ReadAllTextAsync(pointerPath);
                        }

                        if (!string.IsNullOrEmpty(targetLayoutName))
                        {
                            string filePath = Path.Combine(folderPath, $"{targetLayoutName}_layout.json");

                            if (File.Exists(filePath))
                            {
                                string savedLayout = await File.ReadAllTextAsync(filePath);
                                string safeJson = savedLayout.Replace("\\", "\\\\").Replace("'", "\\'");

                                await ChartWebView.ExecuteScriptAsync($"window.applyLayout('{safeJson}')");
                                Console.WriteLine($"[CHART] Custom Layout [{targetLayoutName}] loaded for {SymbolName}");
                                return;
                            }
                        }

                        await ChartWebView.ExecuteScriptAsync("window.applyLayout('')");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CHART] Error loading layout file: {ex.Message}");
                        await ChartWebView.ExecuteScriptAsync("window.applyLayout('')");
                    }
                }

                else if (jsonMessage.Contains("LAYOUT_DELETE"))
                {
                    try
                    {
                        string folderPath = Path.Combine(Directory.GetParent(AppConfig.ChartLayoutPath).FullName, "ChartLayouts");
                        string pointerPath = Path.Combine(folderPath, $"{_viewModel.MasterSymbolName}_pointer.txt");

                        if (File.Exists(pointerPath))
                        {
                            string existingLayoutName = await File.ReadAllTextAsync(pointerPath);
                            string filePath = Path.Combine(folderPath, $"{existingLayoutName}_layout.json");

                            if (File.Exists(filePath))
                                File.Delete(filePath);

                            File.Delete(pointerPath);
                            Console.WriteLine($"[CHART] Layout permanently deleted for {_viewModel.MasterSymbolName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[CHART] Error deleting layout file: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CHART] WebMessage Error: {ex.Message}");
            }
        }
        private async void KeepHistoryDispatched(string reqId, string jsonData)
        {
            try
            {
                if (ChartWebView?.CoreWebView2 == null) return;

                string safeReqId = reqId.Replace("'", "\\'");
                string safeJson = jsonData.Replace("\\", "\\\\").Replace("'", "\\'");

                await ChartWebView.ExecuteScriptAsync($"window.loadHistory('{safeReqId}', '{safeJson}')");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CHART] SendHistory Error: {ex.Message}");
            }
        }
        private void UpdateLoadingText(string message)
        {
            Dispatcher.Invoke(() =>
            {               
                if (LoadingText != null)
                    LoadingText.Text = message;
            });
            Console.WriteLine($"[CHART] {message}");
        }
        private void ChartTradeWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine($"[CHART] Unloaded: {SymbolName}");
        }
        public void CloseAndCleanup()
        {
            try
            {
                if (ChartWebView?.CoreWebView2 != null)
                    ChartWebView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
                _viewModel.Cleanup();
                _isWebViewInitialized = false;
            }
            catch { }
        }
    }
}
