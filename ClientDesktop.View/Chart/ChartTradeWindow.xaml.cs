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
    /// <summary>
    /// Interaction logic for ChartTradeWindow.xaml
    /// </summary>
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
                LoadingOverlay.Visibility = Visibility.Visible;
                if (LoadingText != null)
                    LoadingText.Text = message;
            });
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
