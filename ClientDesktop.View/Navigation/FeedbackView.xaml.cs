using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Services;
using ClientDesktop.ViewModel;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using RtfPipe;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ClientDesktop.View.Navigation
{
    public partial class FeedbackView : UserControl
    {
        #region Variable

        private readonly FeedbackViewModel _viewModel;
        private readonly SessionService _sessionService;
        Label lblNoData = new Label();
        private string lastSavedRtf = "";
        private Stack<string> undoStack = new Stack<string>();
        private Stack<string> redoStack = new Stack<string>();
        private string ImagePath = string.Empty;
        private string ReplayImagePath = string.Empty;
        private readonly IDialogService _dialogService;
        private int _currentFeedbackId = 0;
        private DispatcherTimer _scrollTimer;
        private double _scrollTarget;
        private bool _isManualScroll = false;
        private const double ScrollEasingFactor = 0.12;
        private Microsoft.Web.WebView2.Wpf.WebView2 _chatWebView;
        private bool _chatWebViewReady = false;
        private List<ChatList> _pendingChatData = null;

        #endregion Variable

        #region Constructor
        public FeedbackView()
        {
            InitializeComponent();
            DgvFeedbackRecord.RowHeight = 28;
            DgvFeedbackRecord.ColumnHeaderHeight = 30;
            ReplyPanel.Visibility = Visibility.Collapsed;

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                _sessionService = AppServiceLocator.GetService<SessionService>();
                _viewModel = AppServiceLocator.GetService<FeedbackViewModel>();
                _dialogService = AppServiceLocator.GetService<IDialogService>();
                _viewModel.OnNewReplyReceived += OnNewReplyReceivedFromViewModel;

                FeedbackViewModel.OnRecordDeletedExternally = (id) =>
                {
                    this.Dispatcher.Invoke(() => {
                        RefreshGridAfterDelete(id);
                    });
                };
            }
            InitSmoothScroll();
            this.Loaded += FeedbackView_FirstLoaded;
        }
        private void FeedbackView_FirstLoaded(object sender, RoutedEventArgs e)
        {
            this.Loaded -= FeedbackView_FirstLoaded;
            InitChatWebView();
        }

        #endregion Constructor

        #region SmoothScroll
        private void InitSmoothScroll()
        {
            _scrollTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };

            _scrollTimer.Tick += (s, e) =>
            {
                if (_isManualScroll) return;

                double current = ReplyPanel.VerticalOffset;
                double diff = _scrollTarget - current;

                if (Math.Abs(diff) < 0.8)
                {
                    ReplyPanel.ScrollToVerticalOffset(_scrollTarget);
                    _scrollTimer.Stop();
                }
                else
                {
                    ReplyPanel.ScrollToVerticalOffset(current + diff * ScrollEasingFactor);
                }
            };
            ReplyPanel.PreviewMouseWheel += ReplyPanel_PreviewMouseWheel;
            ReplyPanel.ScrollChanged += ReplyPanel_ScrollChanged;

            ReplyPanel.PreviewMouseDown += (s, e) =>
            {
                _isManualScroll = true;
                _scrollTimer.Stop();
            };
            ReplyPanel.PreviewMouseUp += (s, e) =>
            {
                _isManualScroll = false;
            };
        }
        private void ReplyPanel_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (_isManualScroll)
            {
                _scrollTarget = ReplyPanel.VerticalOffset;
            }
        }
        private void ReplyPanel_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            _isManualScroll = false;

            SmoothScrollBy(e.Delta < 0 ? 150 : -150);
            e.Handled = true;
        }
        private void SmoothScrollBy(double pixelDelta)
        {
            double startFrom = _scrollTimer.IsEnabled ? _scrollTarget : ReplyPanel.VerticalOffset;

            _scrollTarget = Math.Max(0,
                            Math.Min(ReplyPanel.ScrollableHeight, startFrom + pixelDelta));

            if (!_scrollTimer.IsEnabled)
                _scrollTimer.Start();
        }

        #endregion SmoothScroll

        #region PanelNavigation
        private void ShowDataGridPanel()
        {
            DataGridPanel.Visibility = Visibility.Visible;
            DgvFeedbackRecord.Visibility = Visibility.Visible;
            BtnAddFeedback.Visibility = Visibility.Visible;
            FeedbackFormPanel.Visibility = Visibility.Collapsed;
            ReplyPanel.Visibility = Visibility.Collapsed;
            _viewModel.UnsubscribeFromFeedbackSocket();
        }
        private void ShowFeedbackFormPanel()
        {
            DataGridPanel.Visibility = Visibility.Collapsed;
            FeedbackFormPanel.Visibility = Visibility.Visible;
            ReplyPanel.Visibility = Visibility.Collapsed;
            _viewModel.UnsubscribeFromFeedbackSocket();
        }
        private void ShowReplyPanel()
        {
            DataGridPanel.Visibility = Visibility.Collapsed;
            FeedbackFormPanel.Visibility = Visibility.Collapsed;
            ReplyPanel.Visibility = Visibility.Visible;
            _viewModel.SubscribeToFeedbackSocket();
        }

        #endregion PanelNavigation

        #region Single-WebView2 Chat
        private void InitChatWebView()
        {
            try
            {
                ChatPanel.Visibility = Visibility.Collapsed;

                _chatWebView = new Microsoft.Web.WebView2.Wpf.WebView2
                {
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    Height = 40,
                    MinHeight = 40,
                    Margin = new Thickness(0, 0, 0, 0),
                    Visibility = Visibility.Hidden
                };

                _chatWebView.PreviewMouseWheel += (s, e) =>
                {
                    SmoothScrollBy(e.Delta < 0 ? 150 : -150);
                    e.Handled = true;
                };

                FeedbackChatPanel.Children.Insert(0, _chatWebView);

                _ = InitChatWebViewCoreAsync();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(InitChatWebView), $"InitChatWebView error: {ex.Message}");
                ChatPanel.Visibility = Visibility.Visible;
            }
        }
        private async Task InitChatWebViewCoreAsync()
        {
            try
            {
                await _chatWebView.EnsureCoreWebView2Async();

                _chatWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _chatWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _chatWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                _chatWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

                _chatWebView.CoreWebView2.WebMessageReceived += (wvSender, wvArgs) =>
                {
                    try
                    {
                        string msg = wvArgs.TryGetWebMessageAsString();

                        if (msg != null && msg.StartsWith("scroll_wheel:"))
                        {
                            string deltaStr = msg.Substring("scroll_wheel:".Length);
                            if (double.TryParse(deltaStr,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out double delta))
                            {
                                Application.Current.Dispatcher.InvokeAsync(() =>
                                    SmoothScrollBy(delta * 0.5));
                            }
                        }
                        else if (msg != null && msg.StartsWith("content_height:"))
                        {
                            string hStr = msg.Substring("content_height:".Length);
                            if (double.TryParse(hStr,
                                System.Globalization.NumberStyles.Float,
                                System.Globalization.CultureInfo.InvariantCulture,
                                out double h) && h > 10)
                            {
                                Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    _chatWebView.Height = h + 10;

                                    if (_chatWebView.Visibility != Visibility.Visible)
                                        _chatWebView.Visibility = Visibility.Visible;

                                    ReplyPanel.ScrollToEnd();
                                });
                            }
                        }
                    }
                    catch { }
                };

                _chatWebViewReady = true;

                if (_pendingChatData != null)
                {
                    var data = _pendingChatData;
                    _pendingChatData = null;
                    await RenderAllMessagesAsync(data);
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(InitChatWebViewCoreAsync), $"WebView2 core init error: {ex.Message}");
                _ = Application.Current.Dispatcher.InvokeAsync(() =>
                    ChatPanel.Visibility = Visibility.Visible);
            }
        }
        private async Task RenderAllMessagesAsync(List<ChatList> chatItems)
        {
            if (!_chatWebViewReady) return;

            if (chatItems == null || chatItems.Count == 0)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    _chatWebView.Height = 40;
                    _chatWebView.Visibility = Visibility.Hidden; 
                    _chatWebView.NavigateToString(
                        "<html><body style='margin:0;background:#f5f5f5'></body></html>");
                });
                return;
            }

            var imageMap = new Dictionary<int, string>();

            var imageTasks = chatItems
                .Select((c, i) => new { c, i })
                .Where(x => x.c.filePath != null
                         && x.c.filePath.Count > 0
                         && !string.IsNullOrEmpty(x.c.filePath[0]))
                .Select(async x =>
                {
                    try
                    {
                        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                        var bytes = await http.GetByteArrayAsync(x.c.filePath[0]);
                        string ext = System.IO.Path.GetExtension(
                            new Uri(x.c.filePath[0]).LocalPath).ToLowerInvariant();
                        string mime = ext == ".png" ? "image/png"
                                    : ext == ".gif" ? "image/gif"
                                    : "image/jpeg";
                        return (x.i, uri: $"data:{mime};base64,{Convert.ToBase64String(bytes)}");
                    }
                    catch
                    {
                        return (x.i, uri: string.Empty);
                    }
                });

            var results = await Task.WhenAll(imageTasks);
            foreach (var r in results)
                if (!string.IsNullOrEmpty(r.uri))
                    imageMap[r.i] = r.uri;

            var sb = new StringBuilder();
            sb.Append(@"<!DOCTYPE html>
                            <html>
                            <head>
                            <meta charset='utf-8'>
                            <style>
                            *, *::before, *::after { margin: 0; padding: 0; box-sizing: border-box; }
                            html, body {
                                overflow: hidden;
                                width: 100%;
                                font-family: 'Segoe UI', Arial, sans-serif;
                                font-size: 13px;
                            }
                            #chat {
                                padding-bottom: 0px;
                            }
                            .bubble {
                                background: #fff;
                                border: 1px solid #ccc;
                                border-radius: 6px;
                                padding: 8px 8px 6px 8px;
                                margin: 0 0 8px 0;
                                width: 100%;
                            }
                            .body {
                                word-break: break-word;
                                overflow-wrap: break-word;
                                white-space: pre-wrap;
                            }
                            .body p { margin: 0; padding: 0; }
                            .img {
                                width: 80px;
                                height: 80px;
                                object-fit: contain;
                                margin-top: 6px;
                                cursor: pointer;
                                display: block;
                            }
                            .footer {
                                display: flex;
                                align-items: center;
                                margin-top: 8px;
                            }
                            .time { font-size: 12px; color: #888; }
                            .tick { width: 13px; height: 13px; margin-left: 4px; }
                            #overlay {
                                display: none;
                                position: fixed;
                                top: 0; left: 0;
                                width: 100%; height: 100%;
                                background: rgba(0,0,0,0.78);
                                z-index: 9999;
                                align-items: center;
                                justify-content: center;
                                cursor: zoom-out;
                            }
                            #overlay.on { display: flex; }
                            #overlay img { max-width: 92%; max-height: 92%; object-fit: contain; }
                            </style>
                            </head>
                            <body>
                            <div id='chat'>
                            ");

            for (int i = 0; i < chatItems.Count; i++)
            {
                var c = chatItems[i];
                bool hasMessage = !string.IsNullOrWhiteSpace(c.feedbackMessage);
                bool hasImage = imageMap.ContainsKey(i);
                if (!hasMessage && !hasImage) continue;

                DateTime msgTime = CommonHelper.ConvertUtcToIst(c.createdOn);
                string timeStr = msgTime.ToString("dd/MM/yy HH:mm");
                string body = hasMessage ? c.feedbackMessage : "";
               
                bool isLast = (i == chatItems.Count - 1);
                string bubbleMargin = isLast ? "margin: 0 0 0 0" : "margin: 0 0 8px 0";

                sb.Append($"<div class='bubble' style='{bubbleMargin}'>");

                if (hasMessage)
                    sb.Append($"<div class='body'>{body}</div>");

                if (hasImage)
                    sb.Append($"<img class='img' src='{imageMap[i]}' onclick='showImg(this.src)'/>");

                sb.Append($@"<div class='footer'>
    <span class='time'>{timeStr}</span>
    <svg class='tick' viewBox='0 0 24 24' fill='none' stroke='#888'
         stroke-width='2.2' stroke-linecap='round' stroke-linejoin='round'>
      <polyline points='2,13 7,18 18,7'/>
      <polyline points='7,18 12,23 23,12'/>
    </svg>
</div>");
                sb.Append("</div>");
            }

            sb.Append(@"
</div>
 
<div id='overlay' onclick='hideImg()'><img id='oImg' src=''/></div>
 
<script>
// Forward scroll to WPF
window.addEventListener('wheel', function(e) {
    window.chrome.webview.postMessage('scroll_wheel:' + e.deltaY);
    e.preventDefault();
}, { passive: false });
 
function showImg(src) {
    document.getElementById('oImg').src = src;
    document.getElementById('overlay').classList.add('on');
}
function hideImg() {
    document.getElementById('overlay').classList.remove('on');
}
 
// ✅ FIX: ResizeObserver — Collapsed hoy to JS run nahi thatu
//    Hidden use karyo etle ResizeObserver properly fire thase
//    Debounce 60ms — rapid updates spam nahi kare WPF ne
var _timer = null;
var _lastH  = 0;
var chatEl  = document.getElementById('chat');
 
function reportHeight() {
    var h = chatEl.scrollHeight;
    if (h === _lastH) return;
    _lastH = h;
    window.chrome.webview.postMessage('content_height:' + h);
}
 
var ro = new ResizeObserver(function() {
    clearTimeout(_timer);
    _timer = setTimeout(reportHeight, 60);
});
ro.observe(chatEl);
 
// Immediate first report
reportHeight();
</script>
</body>
</html>");

            string finalHtml = sb.ToString();
            var tcs = new TaskCompletionSource<bool>();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                _chatWebView.Visibility = Visibility.Hidden;

                EventHandler<CoreWebView2NavigationCompletedEventArgs> handler = null;
                handler = (s, navArgs) =>
                {
                    _chatWebView.CoreWebView2.NavigationCompleted -= handler;
                 
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay(3000);
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            if (_chatWebView.Visibility != Visibility.Visible)
                                _chatWebView.Visibility = Visibility.Visible;
                        });
                        tcs.TrySetResult(true);
                    });
                };

                _chatWebView.CoreWebView2.NavigationCompleted += handler;
                _chatWebView.NavigateToString(finalHtml);
            });

            await Task.WhenAny(tcs.Task, Task.Delay(5000));          
        }

        #endregion Single-WebView2 Chat

        #region Methods
        private void ScrollChatToBottom()
        {
            ReplyPanel.Dispatcher.BeginInvoke(new Action(() =>
            {
                ReplyPanel.ScrollToEnd();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
        private async void OnNewReplyReceivedFromViewModel(ChatList chatItem)
        {
            try
            {
                var newListItem = await CreateChatListBoxItemAsync(chatItem);
                if (ChatPanel.ItemsSource != null)
                {
                    var existing = ChatPanel.Items.Cast<object>().ToList();
                    ChatPanel.ItemsSource = null;
                    ChatPanel.Items.Clear();
                    foreach (var item in existing)
                        ChatPanel.Items.Add(item);
                }

                ChatPanel.Items.Add(newListItem);

                await Task.Delay(150);
                ScrollChatToBottom();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Socket reply render error: {ex.Message}");
            }
        }
        private async Task<ListBoxItem> CreateChatListBoxItemAsync(ChatList chat)
        {
            var outerBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 0, 8, 6),
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Width = double.NaN
            };

            var messageLayout = new StackPanel { Orientation = Orientation.Vertical };

            var webView = new Microsoft.Web.WebView2.Wpf.WebView2
            {
                Height = 1,
                MinHeight = 20,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0)
            };

            var webViewWrapper = new Border
            {
                Opacity = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch
            };
            webViewWrapper.Child = webView;

            string wrappedHtml = $@"<!DOCTYPE html>
<html>
<head>
    <style>
        *, *::before, *::after {{
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }}
        html {{
            overflow: hidden;
            width: 100%;
        }}
        body {{
            margin: 0 !important;
            padding: 0 !important;
            overflow: hidden;
            background: white;
            width: 100%;                      
            display: block;
        }}
        p {{
            margin: 0;
            padding: 0;
            word-break: break-word;
            overflow-wrap: break-word;
            white-space: pre-wrap;
        }}
    </style>
    <script>      
       window.addEventListener('wheel', function(e) {{
             if (Math.abs(e.deltaY) > 0) {{
            window.chrome.webview.postMessage('scroll_wheel:' + e.deltaY);
            }}
        }}, {{ passive: true }});
    </script>
</head>
<body><p>{chat.feedbackMessage}</p></body>
</html>";

            messageLayout.Children.Add(webViewWrapper);

            if (chat.filePath != null && chat.filePath.Count > 0)
            {
                try
                {
                    using (var httpClient = new HttpClient())
                    {
                        var imageBytes = await httpClient.GetByteArrayAsync(chat.filePath[0]);
                        var bitmap = new BitmapImage();

                        using (var ms = new MemoryStream(imageBytes))
                        {
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.EndInit();
                            bitmap.Freeze();
                        }

                        var chatImage = new System.Windows.Controls.Image
                        {
                            Source = bitmap,
                            Width = 80,
                            Height = 80,
                            Stretch = Stretch.Uniform,
                            Margin = new Thickness(0, 5, 0, 0),
                            Cursor = Cursors.Hand,
                            HorizontalAlignment = HorizontalAlignment.Left
                        };

                        chatImage.MouseLeftButtonUp += (s, ep) =>
                        {
                            new Window
                            {
                                Title = "Image Preview",
                                Width = 600,
                                Height = 600,
                                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                                Content = new System.Windows.Controls.Image
                                {
                                    Source = bitmap,
                                    Stretch = Stretch.Uniform
                                }
                            }.ShowDialog();
                        };

                        messageLayout.Children.Add(chatImage);
                    }
                }
                catch { }
            }

            DateTime msgTime = CommonHelper.ConvertUtcToIst(chat.createdOn);
            StackPanel timeContainer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left
            };

            timeContainer.Children.Add(new TextBlock
            {
                Text = msgTime.ToString("dd/MM/yy HH:mm "),
                FontSize = 13,
                Foreground = Brushes.Gray,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            });

            System.Windows.Shapes.Path doubleCheckIcon = new System.Windows.Shapes.Path
            {
                Data = Geometry.Parse("M2,13L7,18L18,7M7,18L12,23L23,12"),
                Stroke = Brushes.Gray,
                StrokeThickness = 2,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                StrokeLineJoin = PenLineJoin.Round,
                Width = 14,
                Height = 14,
                Stretch = Stretch.Uniform,
                VerticalAlignment = System.Windows.VerticalAlignment.Center,
                Margin = new Thickness(2, 0, 0, 0)
            };

            timeContainer.Children.Add(doubleCheckIcon);
            messageLayout.Children.Add(timeContainer);
            outerBorder.Child = messageLayout;

            var listItem = new ListBoxItem
            {
                Content = outerBorder,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(0),
                BorderThickness = new Thickness(0),
                Background = Brushes.Transparent
            };


            var capturedWebView = webView;
            var capturedWrapper = webViewWrapper;
            var capturedHtml = wrappedHtml;

            _ = Application.Current.Dispatcher.InvokeAsync(async () =>
            {
                try
                {
                    await capturedWebView.EnsureCoreWebView2Async();

                    capturedWebView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                    capturedWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                    capturedWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
                    capturedWebView.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;

                    capturedWebView.CoreWebView2.WebMessageReceived += (wvSender, wvArgs) =>
                    {
                        try
                        {
                            string msg = wvArgs.TryGetWebMessageAsString();
                            if (msg != null && msg.StartsWith("scroll_wheel:"))
                            {
                                string deltaStr = msg.Substring("scroll_wheel:".Length);
                                if (double.TryParse(deltaStr, System.Globalization.NumberStyles.Float,
                                    System.Globalization.CultureInfo.InvariantCulture, out double delta))
                                {
                                    Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        SmoothScrollBy(delta * 0.5);
                                    });
                                }
                            }
                        }
                        catch { }
                    };

                    bool heightSet = false;

                    capturedWebView.NavigationCompleted += async (s, navArgs) =>
                    {

                        if (heightSet) return;
                        heightSet = true;

                        try
                        {

                            await Task.Delay(200);


                            string heightStr = await capturedWebView.CoreWebView2.ExecuteScriptAsync(@"
                                (function() {
                                    document.body.style.overflow = 'hidden';
                                    var h = Math.max(
                                        document.body.scrollHeight,
                                        document.body.offsetHeight,
                                        document.documentElement.scrollHeight,
                                        document.documentElement.offsetHeight
                                    );
                                    return h.toString();
                                })()
                            ");

                            if (double.TryParse(heightStr.Trim('"'), out double htmlHeight) && htmlHeight > 0)
                            {
                                await Application.Current.Dispatcher.InvokeAsync(() =>
                                {
                                    capturedWebView.Height = htmlHeight;
                                });

                                await Task.Delay(500);

                                string heightStr2 = await capturedWebView.CoreWebView2.ExecuteScriptAsync(@"                                                   
                                                    (function() {
                                                        document.body.style.overflow = 'hidden';
                                                        var h = Math.max(
                                                        document.body.scrollHeight,
                                                        document.body.offsetHeight,
                                                        document.documentElement.scrollHeight,
                                                        document.documentElement.offsetHeight
                                    );
                                    return h.toString();
                                })()    
                                ");

                                if (double.TryParse(heightStr2.Trim('"'), out double htmlHeight2) && htmlHeight2 > htmlHeight)
                                {
                                    await Application.Current.Dispatcher.InvokeAsync(() =>
                                    {
                                        capturedWebView.Height = htmlHeight2;
                                        capturedWrapper.Opacity = 1;
                                    });
                                }
                            }
                        }
                        catch
                        {
                            await Application.Current.Dispatcher.InvokeAsync(() =>
                            {
                                capturedWebView.Height = 40;
                                capturedWrapper.Opacity = 1;
                            });
                        }
                    };

                    capturedWebView.NavigateToString(capturedHtml);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"WebView2 error: {ex.Message}");
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        capturedWebView.Height = 40;
                        capturedWrapper.Opacity = 1;
                    });
                }
            });


            return listItem;
        }
        private async Task LoadChatPanel(FeedbackData feedback)
        {
            try
            {
                ChatPanel.ItemsSource = null;
                ChatPanel.Items.Clear();

                if (feedback?.ChatList == null || feedback.ChatList.Count == 0)
                {
                    if (_chatWebViewReady)
                    {
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            _chatWebView.Height = 40;
                            _chatWebView.Visibility = Visibility.Hidden;
                            _chatWebView.NavigateToString(
                                "<html><body style='margin:0;background:#f5f5f5'></body></html>");
                        });
                    }
                    return;
                }

                if (_chatWebViewReady)
                {
                    await RenderAllMessagesAsync(feedback.ChatList);
                }
                else
                {
                    _pendingChatData = feedback.ChatList;
                }
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadChatPanel), $"error: {ex.Message}");
            }
        }
        private async Task RefreshFeedbackGrid()
        {
            DgvFeedbackRecord.Items.Clear();
            int sr = 1;

            if (_viewModel.FeedbackList != null && _viewModel.FeedbackList.Count > 0)
            {
                lblNoData.Visibility = Visibility.Collapsed;

                foreach (var items in _viewModel.FeedbackList)
                {
                    DateTime istTime = CommonHelper.ConvertUtcToIst(items.FeedbackDate);
                    string feedbackDateTime = istTime.ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture);
                    string status = items.IsClosed ? "Closed" : "Open";

                    var row = new
                    {
                        SrNo = sr,
                        FeedbackId = items.FeedbackId,
                        Subject = items.FeedbackSubject,
                        Date = feedbackDateTime,
                        Status = status
                    };

                    DgvFeedbackRecord.Items.Add(row);
                    sr++;
                }

                DgvFeedbackRecord.SelectedIndex = 0;
            }
        }
        private void RefreshGridAfterDelete(int feedbackId)
        {
            var updatedItems = DgvFeedbackRecord.Items
                .Cast<dynamic>()
                .Where(x => x.FeedbackId != feedbackId)
                .Select((item, index) => new
                {
                    SrNo = index + 1,
                    FeedbackId = (int)item.FeedbackId,
                    Subject = (string)item.Subject,
                    Date = (string)item.Date,
                    Status = (string)item.Status
                })
                .ToList();

            DgvFeedbackRecord.ItemsSource = null;
            DgvFeedbackRecord.Items.Clear();
            DgvFeedbackRecord.ItemsSource = updatedItems;
        }

        #endregion Methods

        #region FormattingHelpers
        private void ToggleFormatting(DependencyProperty property, object value, object normalValue)
        {
            if (TxtMessage.Selection.IsEmpty)
                return;

            TextSelection selection = TxtMessage.Selection;
            var currentValue = selection.GetPropertyValue(property);

            if (currentValue != DependencyProperty.UnsetValue && currentValue.Equals(value))
            {
                selection.ApplyPropertyValue(property, normalValue);
            }
            else
            {
                selection.ApplyPropertyValue(property, value);
            }
        }
        private void LoadMyEmojis()
        {
            if (EmojiWrapPanel.Children.Count > 0) return;

            string[] emojis =
            {
                "😀","😃","😄","😁","😆","😅","😂","🤣",
                "😊","😇","🙂","🙃","😉","😌","😍","🥰",
                "😘","😗","😙","😚","😋","😛","😝","😜",
                "🤪","🤨","🧐","🤓","😎","🤩","🥳","😏",
                "😒","😞","😔","😟","😕","🙁","☹️","😣",
                "😖","😫","😩","🥺","😢","😭","😤","😠",
                "😡","🤬","🤯","😳","🥵","🥶","😱","😨",
                "😰","😥","😓","🤗","🤔","🤭","🤫","🤥",
                "😶","😐","😑","😬","🙄","😯","😦","😧",
                "😮","😲","🥱","😴","🤤","😪","😵","🤐",
                "🥴","🤢","🤮","🤧","😷","🤒","🤕","🤑",
                "🤠","😈","👿","👹","👺","🤡","💩","👻",
                "💀","☠️","👽","👾","🤖","🎃","😺","😸",
                "😹","😻","😼","😽","🙀","😿","😾","👍",
                "👎","👌","🤏","✌️","🤞","🤟","🤘","🤙",
                "👈","👉","👆","👇","☝️","✋","🤚","🖐️",
                "🖖","👋","🤙","💪","🦾","🦵","🦿","🦶"
            };

            foreach (var emoji in emojis)
            {
                Button btn = new Button
                {
                    Content = emoji,
                    FontSize = 20,
                    Width = 30,
                    Height = 30,
                    Background = System.Windows.Media.Brushes.Yellow,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(2)
                };

                btn.Click += EmojiItem_Click;
                EmojiWrapPanel.Children.Add(btn);
            }
        }
        private void LoadMyReplayEmojis()
        {
            if (ReplayeEmojiWrapPanel.Children.Count > 0) return;

            string[] emojis =
            {
                "😀","😃","😄","😁","😆","😅","😂","🤣",
                "😊","😇","🙂","🙃","😉","😌","😍","🥰",
                "😘","😗","😙","😚","😋","😛","😝","😜",
                "🤪","🤨","🧐","🤓","😎","🤩","🥳","😏",
                "😒","😞","😔","😟","😕","🙁","☹️","😣",
                "😖","😫","😩","🥺","😢","😭","😤","😠",
                "😡","🤬","🤯","😳","🥵","🥶","😱","😨",
                "😰","😥","😓","🤗","🤔","🤭","🤫","🤥",
                "😶","😐","😑","😬","🙄","😯","😦","😧",
                "😮","😲","🥱","😴","🤤","😪","😵","🤐",
                "🥴","🤢","🤮","🤧","😷","🤒","🤕","🤑",
                "🤠","😈","👿","👹","👺","🤡","💩","👻",
                "💀","☠️","👽","👾","🤖","🎃","😺","😸",
                "😹","😻","😼","😽","🙀","😿","😾","👍",
                "👎","👌","🤏","✌️","🤞","🤟","🤘","🤙",
                "👈","👉","👆","👇","☝️","✋","🤚","🖐️",
                "🖖","👋","🤙","💪","🦾","🦵","🦿","🦶"
            };

            foreach (var emoji in emojis)
            {
                Button btn = new Button
                {
                    Content = emoji,
                    FontSize = 20,
                    Width = 30,
                    Height = 30,
                    Background = System.Windows.Media.Brushes.Yellow,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    Margin = new Thickness(2)
                };

                btn.Click += EmojiReplayItem_Click;
                ReplayeEmojiWrapPanel.Children.Add(btn);
            }
        }
        private void ToggleFormattingForReplay(DependencyProperty property, object value, object normalValue)
        {
            if (TxtReply.Selection.IsEmpty)
                return;

            TextSelection selection = TxtReply.Selection;
            var currentValue = selection.GetPropertyValue(property);

            if (currentValue != DependencyProperty.UnsetValue && currentValue.Equals(value))
            {
                selection.ApplyPropertyValue(property, normalValue);
            }
            else
            {
                selection.ApplyPropertyValue(property, value);
            }
        }

        #endregion FormattingHelpers

        #region Events — UserControl / Grid
        private async void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_sessionService.IsLoggedIn || !_sessionService.IsInternetAvailable)
            {
                Window.GetWindow(this)?.Close();
                return;
            }

            _ = InitialLoadAsync();
        }
        private async Task InitialLoadAsync()
        {
            await _viewModel.LoadFeedbackAsync();
            await RefreshFeedbackGrid();
        }
        private async void DgvFeedbackRecord_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var grid = sender as DataGrid;

            if (grid == null || grid.CurrentItem == null) return;

            dynamic selectedRow = grid.CurrentItem;
            var currentColumn = grid.CurrentColumn;

            if (currentColumn == null) return;

            string header = currentColumn.Header?.ToString();

            if (header == "Subject")
            {
                int feedbackId = selectedRow.FeedbackId;
                _currentFeedbackId = feedbackId;

                await _viewModel.GetFeedbackDetailsAsync(feedbackId);

                if (_viewModel.SelectedFeedbackDetails != null)
                {
                    var feedback = _viewModel.SelectedFeedbackDetails;
                    if (feedback != null)
                    {
                        DateTime istTime = CommonHelper.ConvertUtcToIst(feedback.CreatedOn);
                        TxtDateValue.Text = istTime.ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture);
                        TxtSubjectValue.Text = feedback.FeedbackSubject ?? string.Empty;
                        TxtMessageValue.Text = Regex.Replace(feedback.FeedbackMessage ?? string.Empty, "<.*?>", string.Empty);
                        TxtFeedbackId.Text = feedback.FeedbackId.ToString();

                        if (feedback.FilePath != null && feedback.FilePath.Count > 0)
                        {
                            try
                            {
                                BitmapImage bitmap = new BitmapImage();
                                bitmap.BeginInit();
                                bitmap.UriSource = new Uri(feedback.FilePath[0], UriKind.Absolute);
                                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                bitmap.EndInit();

                                ImgAttachment.Source = bitmap;
                                ImgAttachment.Stretch = Stretch.Uniform;
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Failed to load image: " + ex.Message);
                            }
                        }
                        else
                        {
                            ImgAttachment.Source = null;
                        }
                    }

                    ShowReplyPanel();
                    GroupBoxPanel.Visibility = Visibility.Collapsed;
                    await LoadChatPanel(feedback);
                }
            }
        }

        #endregion Events — UserControl / Grid

        #region Events — Feedback Form
        private void BtnAddFeedback_Click(object sender, RoutedEventArgs e)
        {
            BtnColorPicker.Background = Brushes.Transparent;
            BtnColorPicker.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"));
            BtnColorPicker.Foreground = Brushes.Black;

            if (ImgPreview.Source != null)
            {
                ImgPreview.Source = null;
                LblChosenFile.Content = string.Empty;
                LblChosenFile.Content = "No file chosen";
            }

            CmbFontSize.Text = "12";
            lblNoData.Content = string.Empty;
            lblNoData.Visibility = Visibility.Collapsed;

            ShowFeedbackFormPanel();
            DgvFeedbackRecord.Visibility = Visibility.Collapsed;
            BtnAddFeedback.Visibility = Visibility.Collapsed;

            string lastSavedRtf = string.Empty;
            var range = new TextRange(TxtMessage.Document.ContentStart, TxtMessage.Document.ContentEnd);
            using (var memoryStream = new MemoryStream())
            {
                range.Save(memoryStream, DataFormats.Rtf);
                memoryStream.Position = 0;
                using (var reader = new StreamReader(memoryStream))
                {
                    lastSavedRtf = reader.ReadToEnd();
                }
            }
            undoStack.Push(lastSavedRtf);
            TxtSubject.Background = new SolidColorBrush(Colors.White);
            TxtMessage.Background = new SolidColorBrush(Colors.White);
        }
        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            string subject = TxtSubject.Text?.Trim();
            string rtfContent = "";
            TextRange range = new TextRange(TxtMessage.Document.ContentStart, TxtMessage.Document.ContentEnd);

            using (MemoryStream ms = new MemoryStream())
            {
                range.Save(ms, DataFormats.Rtf);
                ms.Seek(0, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(ms))
                {
                    rtfContent = sr.ReadToEnd();
                }
            }

            string html = Rtf.ToHtml(rtfContent);
            string plainText = range.Text.Trim();
            string file = this.ImagePath;
            bool isValid = true;

            bool isMatchFound = _viewModel.FeedbackList != null &&
                _viewModel.FeedbackList.Any(f => string.Equals(f.FeedbackSubject, subject, StringComparison.OrdinalIgnoreCase));

            if (isMatchFound)
            {
                FileLogger.Log("Feedback", "Feedback Already Exists!!");
            }
            else
            {
                var errorBg = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#fce4e4"));
                var errorBorder = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#c03"));

                if (string.IsNullOrEmpty(subject))
                {
                    TxtSubject.Background = errorBg;
                    TxtSubject.BorderBrush = errorBorder;
                    TxtSubject.BorderThickness = new Thickness(1);
                    isValid = false;
                }
                if (string.IsNullOrEmpty(plainText))
                {
                    TxtMessage.Background = errorBg;
                    TxtMessage.BorderBrush = errorBorder;
                    TxtMessage.BorderThickness = new Thickness(1);
                    isValid = false;
                }

                if (!isValid)
                {
                    return;
                }
                else
                {
                    TxtErrorMessage.Text = string.Empty;
                    await _viewModel.SubmitFeedbackAsync(subject, html, file);

                    ShowDataGridPanel();
                    lblNoData.Visibility = Visibility.Collapsed;
                    TxtSubject.Text = "";
                    TxtMessage.Document.Blocks.Clear();
                    this.ImagePath = "";

                    await _viewModel.LoadFeedbackAsync();
                    await RefreshFeedbackGrid();
                }
            }
        }

        #endregion Events — Feedback Form

        #region Events — Reply Panel
        private void BtnReply_Click(object sender, RoutedEventArgs e)
        {
            GroupBoxPanel.Visibility = Visibility.Visible;
            GrpBoxReply.Visibility = Visibility.Visible;
            BtnReplySubmit.IsEnabled = false;
            BtnReplySubmit.Foreground = new SolidColorBrush(Colors.Black);
            ScrollChatToBottom();

            if (ReplayPicturebox.Source != null)
            {
                ReplayPicturebox.Source = null;
                LableReplayFileName.Content = string.Empty;
                LableReplayFileName.Content = "No file choosen";
            }

            CmbReplyFontSize.Text = "12";
            ButtonReplayFillColor.Background = Brushes.Transparent;
            ButtonReplayFillColor.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#CCCCCC"));
            ButtonReplayFillColor.Foreground = Brushes.Black;
        }
        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ResetCurrentFeedback();
            ShowDataGridPanel();
        }
        private async void BtnReplySubmit_Click(object sender, RoutedEventArgs e)
        {
            BtnReplySubmit.IsEnabled = false;

            int feedbackid = Convert.ToInt32(TxtFeedbackId.Text);
            string rtfContent = "";
            TextRange range = new TextRange(TxtReply.Document.ContentStart, TxtReply.Document.ContentEnd);

            using (MemoryStream ms = new MemoryStream())
            {
                range.Save(ms, DataFormats.Rtf);
                ms.Seek(0, SeekOrigin.Begin);
                using (StreamReader sr = new StreamReader(ms))
                {
                    rtfContent = sr.ReadToEnd();
                }
            }

            string html = Rtf.ToHtml(rtfContent);
            string file = this.ReplayImagePath;
            var result = await _viewModel.SubmitFeedbackReplyAsync(feedbackid, html, file);

            GroupBoxPanel.Visibility = Visibility.Collapsed;
            ChatPanel.Visibility = Visibility.Visible;
            TxtReply.Document.Blocks.Clear();
            this.ReplayImagePath = string.Empty;

            if (result != null && result.IsSuccess)
            {
                if (!_viewModel.IsSocketConnected)
                {
                    await _viewModel.GetFeedbackDetailsAsync(feedbackid);
                    if (_viewModel.SelectedFeedbackDetails != null)
                        await LoadChatPanel(_viewModel.SelectedFeedbackDetails);
                }
            }
            else if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
            {
                MessageBox.Show(_viewModel.ErrorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                BtnReplySubmit.IsEnabled = true;
            }
        }
        private void TxtReply_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (TxtReply == null) return;

            TextRange range = new TextRange(TxtReply.Document.ContentStart, TxtReply.Document.ContentEnd);
            string plainText = range.Text.Trim();

            if (string.IsNullOrWhiteSpace(plainText))
            {
                BtnReplySubmit.IsEnabled = false;
                BtnReplySubmit.Foreground = new SolidColorBrush(Colors.Black);
            }
            else
            {
                BtnReplySubmit.IsEnabled = true;
                BtnReplySubmit.Foreground = new SolidColorBrush(Colors.White);
                TxtReply.Background = new SolidColorBrush(Colors.White);
            }
        }

        #endregion Events — Reply Panel

        #region Events — New Feedback Formatting
        private void BtnBold_Click(object sender, RoutedEventArgs e)
        {
            ToggleFormatting(TextElement.FontWeightProperty, FontWeights.Bold, FontWeights.Normal);
        }
        private void BtnItalic_Click(object sender, RoutedEventArgs e)
        {
            ToggleFormatting(TextElement.FontStyleProperty, FontStyles.Italic, FontStyles.Normal);
        }
        private void BtnUnderline_Click(object sender, RoutedEventArgs e)
        {
            ToggleFormatting(Inline.TextDecorationsProperty, TextDecorations.Underline, null);
        }
        private void BtnStrikeout_Click(object sender, RoutedEventArgs e)
        {
            ToggleFormatting(Inline.TextDecorationsProperty, TextDecorations.Strikethrough, null);
        }
        private void BtnMonospace_Click(object sender, RoutedEventArgs e)
        {
            TextRange range = new TextRange(TxtMessage.Document.ContentStart, TxtMessage.Document.ContentEnd);
            if (!string.IsNullOrWhiteSpace(range.Text))
            {
                TxtMessage.AppendText(" ");
                TxtMessage.Focus();
                TxtMessage.CaretPosition = TxtMessage.Document.ContentEnd;
            }
        }
        private void BtnSuperscript_Click(object sender, RoutedEventArgs e)
        {
            if (TxtMessage.Selection.IsEmpty) return;

            TextSelection selection = TxtMessage.Selection;
            var currentAlignment = selection.GetPropertyValue(Inline.BaselineAlignmentProperty);

            if (currentAlignment == DependencyProperty.UnsetValue ||
                (BaselineAlignment)currentAlignment != BaselineAlignment.Superscript)
            {
                selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Superscript);
                selection.ApplyPropertyValue(TextElement.FontSizeProperty, 8.0);
            }
            else
            {
                selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Baseline);
                selection.ApplyPropertyValue(TextElement.FontSizeProperty, 12.0);
            }
        }
        private void BtnSubscript_Click(object sender, RoutedEventArgs e)
        {
            if (TxtMessage.Selection.IsEmpty) return;

            TextSelection selection = TxtMessage.Selection;
            var currentAlignment = selection.GetPropertyValue(Inline.BaselineAlignmentProperty);

            if (currentAlignment == DependencyProperty.UnsetValue ||
                (BaselineAlignment)currentAlignment != BaselineAlignment.Subscript)
            {
                selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Subscript);
                selection.ApplyPropertyValue(TextElement.FontSizeProperty, 8.0);
            }
            else
            {
                selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Baseline);
                selection.ApplyPropertyValue(TextElement.FontSizeProperty, 12.0);
            }
        }
        private void CmbFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TxtMessage == null) return;

            if (CmbFontSize.SelectedItem is ComboBoxItem item)
            {
                string content = item.Content.ToString();
                if (double.TryParse(content, out double newSize))
                    TxtMessage.FontSize = newSize;
            }
        }
        private void BtnColorPicker_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow picker = new ColorPickerWindow();

            Window hostingWindow = new Window
            {
                Title = "Select Color",
                Content = picker,
                Height = 300,
                Width = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            if (hostingWindow.ShowDialog() == true)
            {
                var brush = picker.SelectedBrush;
                BtnColorPicker.Background = brush;

                if (TxtMessage.Selection != null)
                    TxtMessage.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            }
        }
        private void BtnEmoji_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (EmojiPopup.PlacementTarget == null)
                    EmojiPopup.PlacementTarget = BtnEmoji;

                if (EmojiPopup.IsOpen)
                    EmojiPopup.IsOpen = false;
                else
                {
                    LoadMyEmojis();
                    EmojiPopup.IsOpen = true;
                }
            }
            catch { }
        }
        private void EmojiItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedBtn && clickedBtn.Content != null)
            {
                string selectedEmoji = clickedBtn.Content.ToString();

                if (!TxtMessage.Selection.IsEmpty)
                    TxtMessage.Selection.Text = string.Empty;

                TextPointer caretPos = TxtMessage.CaretPosition;
                Run emojiRun = new Run(selectedEmoji, caretPos);
                emojiRun.ClearValue(TextElement.ForegroundProperty);

                TxtMessage.CaretPosition = emojiRun.ElementEnd;
                TxtMessage.Focus();
                EmojiPopup.IsOpen = false;
            }
        }
        private void BtnCloseEmoji_Click(object sender, RoutedEventArgs e)
        {
            EmojiPopup.IsOpen = false;
        }
        private void BtnUndo_Click(object sender, RoutedEventArgs e)
        {
            while (TxtMessage.CanUndo)
            {
                TxtMessage.Undo();
            }
        }
        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            while (TxtMessage.CanRedo)
            {
                TxtMessage.Redo();
            }
        }
        private void BtnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Select Image File";
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedImagePath = openFileDialog.FileName;

                    LblChosenFile.Content = System.IO.Path.GetFileName(selectedImagePath);

                    BitmapImage bitmapImage = new BitmapImage(new Uri(selectedImagePath));
                    ImgPreview.Source = bitmapImage;

                    this.ImagePath = selectedImagePath;
                }
            }
            catch { }
        }

        #endregion  Events — New Feedback Formatting

        #region Events — Reply Formatting
        private void ButtonReplayBold_Click(object sender, RoutedEventArgs e)
        {
            ToggleFormattingForReplay(TextElement.FontWeightProperty, FontWeights.Bold, FontWeights.Normal);
        }
        private void ButtonReplayItalic_Click(object sender, RoutedEventArgs e)
        {
            ToggleFormattingForReplay(TextElement.FontStyleProperty, FontStyles.Italic, FontStyles.Normal);
        }
        private void ButtonReplayUnderLine_Click(object sender, RoutedEventArgs e)
        {
            ToggleFormattingForReplay(Inline.TextDecorationsProperty, TextDecorations.Underline, null);
        }
        private void ButtonReplayStrick_Click(object sender, RoutedEventArgs e)
        {
            ToggleFormattingForReplay(Inline.TextDecorationsProperty, TextDecorations.Strikethrough, null);
        }
        private void ButtonReplayMonospace_Click(object sender, RoutedEventArgs e)
        {
            TextRange range = new TextRange(TxtReply.Document.ContentStart, TxtReply.Document.ContentEnd);
            if (!string.IsNullOrWhiteSpace(range.Text))
            {
                TxtReply.AppendText(" ");
                TxtReply.Focus();
                TxtReply.CaretPosition = TxtReply.Document.ContentEnd;
            }
        }
        private void ButtonReplaysuperscrip_Click(object sender, RoutedEventArgs e)
        {
            if (TxtReply.Selection.IsEmpty) return;

            TextSelection selection = TxtReply.Selection;
            var currentAlignment = selection.GetPropertyValue(Inline.BaselineAlignmentProperty);

            if (currentAlignment == DependencyProperty.UnsetValue ||
                (BaselineAlignment)currentAlignment != BaselineAlignment.Superscript)
            {
                selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Superscript);
                selection.ApplyPropertyValue(TextElement.FontSizeProperty, 8.0);
            }
            else
            {
                selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Baseline);
                selection.ApplyPropertyValue(TextElement.FontSizeProperty, 12.0);
            }
        }
        private void ButtonReplaysubscript_Click(object sender, RoutedEventArgs e)
        {
            if (TxtReply.Selection.IsEmpty) return;

            TextSelection selection = TxtReply.Selection;
            var currentAlignment = selection.GetPropertyValue(Inline.BaselineAlignmentProperty);

            if (currentAlignment == DependencyProperty.UnsetValue ||
                (BaselineAlignment)currentAlignment != BaselineAlignment.Subscript)
            {
                selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Subscript);
                selection.ApplyPropertyValue(TextElement.FontSizeProperty, 8.0);
            }
            else
            {
                selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Baseline);
                selection.ApplyPropertyValue(TextElement.FontSizeProperty, 12.0);
            }
        }
        private void CmbReplyFontSize_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TxtReply == null) return;

            if (CmbReplyFontSize.SelectedItem is ComboBoxItem item)
            {
                string content = item.Content.ToString();
                if (double.TryParse(content, out double newSize))
                    TxtReply.FontSize = newSize;
            }
        }
        private void ButtonReplayFillColor_Click(object sender, RoutedEventArgs e)
        {
            ColorPickerWindow picker = new ColorPickerWindow();

            Window hostingWindow = new Window
            {
                Title = "Select Color",
                Content = picker,
                Height = 300,
                Width = 300,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            if (hostingWindow.ShowDialog() == true)
            {
                var brush = picker.SelectedBrush;
                ButtonReplayFillColor.Background = brush;

                if (TxtReply.Selection != null)
                    TxtReply.Selection.ApplyPropertyValue(TextElement.ForegroundProperty, brush);
            }
        }
        private void ButtonReplayemoji_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ReplayeEmojiPopup.PlacementTarget == null)
                    ReplayeEmojiPopup.PlacementTarget = BtnEmoji;

                if (ReplayeEmojiPopup.IsOpen)
                    ReplayeEmojiPopup.IsOpen = false;
                else
                {
                    LoadMyReplayEmojis();
                    ReplayeEmojiPopup.IsOpen = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Emoji Panel error: " + ex.Message);
            }
        }
        private void BtnReplayCloseEmoji_Click(object sender, RoutedEventArgs e)
        {
            ReplayeEmojiPopup.IsOpen = false;
        }
        private void ButtonReplayUndo_Click(object sender, RoutedEventArgs e)
        {
            while (TxtReply.CanUndo)
            {
                TxtReply.Undo();
            }
        }
        private void ButtonReplayRedo_Click(object sender, RoutedEventArgs e)
        {
            while (TxtReply.CanRedo)
            {
                TxtReply.Redo();
            }
        }
        private void EmojiReplayItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedBtn && clickedBtn.Content != null)
            {
                string selectedEmoji = clickedBtn.Content.ToString();

                if (!TxtReply.Selection.IsEmpty)
                    TxtReply.Selection.Text = string.Empty;

                TextPointer caretPos = TxtReply.CaretPosition;
                Run emojiRun = new Run(selectedEmoji, caretPos);
                emojiRun.ClearValue(TextElement.ForegroundProperty);

                TxtReply.CaretPosition = emojiRun.ElementEnd;
                TxtReply.Focus();
                ReplayeEmojiPopup.IsOpen = false;
            }
        }
        private void BtnFileUpload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Title = "Select Image File";
                openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";

                if (openFileDialog.ShowDialog() == true)
                {
                    string selectedImagePath = openFileDialog.FileName;

                    LableReplayFileName.Content = System.IO.Path.GetFileName(selectedImagePath);

                    BitmapImage bitmapImage = new BitmapImage(new Uri(selectedImagePath));
                    ReplayPicturebox.Source = bitmapImage;

                    this.ReplayImagePath = selectedImagePath;
                }
            }
            catch { }
        }

        #endregion Events — Reply Formatting

        #region Events — Delete
        private void BtnDeleteFeedback_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;

            if (button?.CommandParameter is int feedbackId && feedbackId > 0)
            {
                _dialogService?.ShowDialog<DeleteFeedbackViewModel>(
                    "Delete Feedback",
                    configureViewModel: vm =>
                    {
                        vm.FeedbackId = feedbackId;
                    });
            }
        }

        #endregion Events — Delete

    }
}