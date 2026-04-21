using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Services;
using ClientDesktop.ViewModel;
using Microsoft.Win32;
using RtfPipe;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
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
                    this.Dispatcher.Invoke(() =>
                    {
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
            //_isManualScroll = false;
            //SmoothScrollBy(e.Delta < 0 ? 150 : -150);
            //e.Handled = true;
          
            if (TxtReply != null && TxtReply.IsMouseOver && GroupBoxPanel.Visibility == Visibility.Visible)
            {
                var rtbScrollViewer = FindVisualChild<ScrollViewer>(TxtReply);
                if (rtbScrollViewer != null)
                {
                    bool canScrollDown = e.Delta < 0 && rtbScrollViewer.VerticalOffset < rtbScrollViewer.ScrollableHeight;
                    bool canScrollUp = e.Delta > 0 && rtbScrollViewer.VerticalOffset > 0;

                    if (canScrollDown || canScrollUp)
                    {                       
                        double delta = e.Delta < 0 ? 50 : -50;
                        rtbScrollViewer.ScrollToVerticalOffset(rtbScrollViewer.VerticalOffset + delta);
                        e.Handled = true;
                        return;
                    }
                }
            }
            
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

        #region Single Chat
        private void InitChatWebView()
        {
            ChatPanel.Visibility = Visibility.Visible;
        }
        private async Task InitChatWebViewCoreAsync()
        {
            await Task.CompletedTask;
        }
        private async Task RenderAllMessagesAsync(List<ChatList> chatItems)
        {
            ChatPanel.ItemsSource = null;
            ChatPanel.Items.Clear();
            ChatPanel.Visibility = Visibility.Visible;

            if (chatItems == null || chatItems.Count == 0)
                return;

            var imageMap = new Dictionary<int, BitmapImage>();

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
                        byte[] bytes = await http.GetByteArrayAsync(x.c.filePath[0]);

                        var bmp = new BitmapImage();
                        using (var ms = new MemoryStream(bytes))
                        {
                            bmp.BeginInit();
                            bmp.CacheOption = BitmapCacheOption.OnLoad;
                            bmp.StreamSource = ms;
                            bmp.EndInit();
                        }
                        bmp.Freeze();
                        return (x.i, bmp);
                    }
                    catch
                    {
                        return (x.i, (BitmapImage)null);
                    }
                });

            var results = await Task.WhenAll(imageTasks);
            foreach (var r in results)
                if (r.Item2 != null)
                    imageMap[r.i] = r.Item2;

            for (int i = 0; i < chatItems.Count; i++)
            {
                var c = chatItems[i];
                bool hasMessage = !string.IsNullOrWhiteSpace(c.feedbackMessage);
                bool hasImage = imageMap.ContainsKey(i);
                if (!hasMessage && !hasImage) continue;

                BitmapImage bitmap = imageMap.TryGetValue(i, out var bm) ? bm : null;
                var listItem = BuildChatListBoxItem(c, bitmap);
                ChatPanel.Items.Add(listItem);
            }

            await Task.Delay(100);
            ReplyPanel.ScrollToEnd();
        }

        #endregion Single Chat

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

            BitmapImage bitmap = null;
            if (chat.filePath != null && chat.filePath.Count > 0
                && !string.IsNullOrEmpty(chat.filePath[0]))
            {
                try
                {
                    using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
                    byte[] bytes = await http.GetByteArrayAsync(chat.filePath[0]);

                    bitmap = new BitmapImage();
                    using (var ms = new MemoryStream(bytes))
                    {
                        bitmap.BeginInit();
                        bitmap.CacheOption = BitmapCacheOption.OnLoad;
                        bitmap.StreamSource = ms;
                        bitmap.EndInit();
                    }
                    bitmap.Freeze();
                }
                catch { }
            }

            return BuildChatListBoxItem(chat, bitmap);
        }
        private ListBoxItem BuildChatListBoxItem(ChatList chat, BitmapImage bitmap)
        {
            var outerBorder = new Border
            {
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(204, 204, 204)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(8, 8, 8, 6),
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Width = double.NaN
            };

            var messageLayout = new StackPanel { Orientation = Orientation.Vertical };

            if (!string.IsNullOrWhiteSpace(chat.feedbackMessage))
            {
                var rtb = new RichTextBox
                {
                    IsReadOnly = true,
                    BorderThickness = new Thickness(0),
                    Background = Brushes.White,
                    Padding = new Thickness(0),
                    Margin = new Thickness(0),

                    Width = double.NaN,
                    Height = double.NaN,
                    MinHeight = 0,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    VerticalAlignment = System.Windows.VerticalAlignment.Top,

                    VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    IsDocumentEnabled = false
                };
                rtb.Document = HtmlToFlowDocument(chat.feedbackMessage);

                rtb.Document.TextAlignment = System.Windows.TextAlignment.Justify;
                rtb.Document.PagePadding = new Thickness(0);
                rtb.HorizontalContentAlignment = HorizontalAlignment.Stretch;

                messageLayout.Children.Add(rtb);
            }

            if (bitmap != null)
            {
                var capturedBitmap = bitmap;

                var chatImage = new System.Windows.Controls.Image
                {
                    Source = capturedBitmap,
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
                            Source = capturedBitmap,
                            Stretch = Stretch.Uniform
                        }
                    }.ShowDialog();
                };

                messageLayout.Children.Add(chatImage);
            }

            DateTime msgTime = CommonHelper.ConvertUtcToIst(chat.createdOn);

            var timeContainer = new StackPanel
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

            return listItem;
        }
        private async Task LoadChatPanel(FeedbackData feedback)
        {
            try
            {
                ChatPanel.ItemsSource = null;
                ChatPanel.Items.Clear();

                if (feedback?.ChatList == null || feedback.ChatList.Count == 0)
                    return;

                await RenderAllMessagesAsync(feedback.ChatList);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadChatPanel), $"error: {ex.Message}");
            }
        }
        private async Task RefreshFeedbackGrid()
        {
            int sr = 1;

            if (_viewModel.FeedbackList != null && _viewModel.FeedbackList.Count > 0)
            {
                LblNoData.Visibility = Visibility.Collapsed;

                var gridData = _viewModel.FeedbackList.Select(items =>
                {
                    DateTime istTime = CommonHelper.ConvertUtcToIst(items.FeedbackDate);
                    string feedbackDateTime = istTime.ToString("dd/MM/yy HH:mm:ss", CultureInfo.InvariantCulture);
                    string status = items.IsClosed ? "Closed" : "Open";

                    return new
                    {
                        SrNo = sr++,
                        FeedbackId = items.FeedbackId,
                        Subject = items.FeedbackSubject,
                        Date = feedbackDateTime,
                        Status = status
                    };
                }).ToList();
              
                DgvFeedbackRecord.ItemsSource = gridData;

                DgvFeedbackRecord.SelectedIndex = 0;
            }
            else
            {
                LblNoData.Visibility = Visibility.Visible;
                DgvFeedbackRecord.ItemsSource = null;
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

            if (DgvFeedbackRecord.Items.Count == 0)
            {
                LblNoData.Visibility = Visibility.Visible;
            }
            else
            {
                LblNoData.Visibility = Visibility.Collapsed;
            }
        }
        private static T FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                var descendant = FindVisualChild<T>(child);
                if (descendant != null) return descendant;
            }
            return null;
        }

        #endregion Methods

        #region HtmlToFlowDocument Helper
        private static FlowDocument HtmlToFlowDocument(string html)
        {
            var doc = new FlowDocument
            {
                PagePadding = new Thickness(0),
                TextAlignment = System.Windows.TextAlignment.Left
            };

            if (string.IsNullOrWhiteSpace(html))
            {
                doc.Blocks.Add(new Paragraph { Margin = new Thickness(0) });
                return doc;
            }

            var bodyMatch = Regex.Match(html,
                @"<body[^>]*>(.*?)</body>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
            string content = bodyMatch.Success ? bodyMatch.Groups[1].Value : html;

            string divStyle = "";
            var divMatch = Regex.Match(content,
                @"<div([^>]*)>", RegexOptions.IgnoreCase);
            if (divMatch.Success)
            {
                var divStyleMatch = Regex.Match(divMatch.Groups[1].Value,
                    @"style\s*=\s*[""']([^""']*)[""']", RegexOptions.IgnoreCase);
                if (divStyleMatch.Success)
                    divStyle = divStyleMatch.Groups[1].Value;
            }

            content = Regex.Replace(content, @"<br\s*/?>", "\n", RegexOptions.IgnoreCase);

            List<(string pStyle, string pContent)> paragraphs = ExtractParagraphs(content);

            bool anyAdded = false;
            foreach (var (pStyle, pContent) in paragraphs)
            {
                string stripped = Regex.Replace(pContent, "<[^>]+>", "");
                if (string.IsNullOrWhiteSpace(stripped) && !pContent.Contains("\n"))
                    continue;

                var para = new Paragraph { Margin = new Thickness(0) };
                
                ApplyParagraphStyle(para, pStyle, divStyle);

                string[] lines = pContent.Split('\n');
                bool firstLine = true;
                foreach (string line in lines)
                {
                    if (!firstLine) para.Inlines.Add(new LineBreak());
                    ParseInlines(line, para.Inlines);
                    firstLine = false;
                }

                doc.Blocks.Add(para);
                anyAdded = true;
            }

            if (!anyAdded)
                doc.Blocks.Add(new Paragraph { Margin = new Thickness(0) });

            return doc;
        }      
        private static void ApplyParagraphStyle(Paragraph para, string styleAttr, string parentStyle = "")
        {
            
            var ffMatch = Regex.Match(styleAttr, @"font-family\s*:\s*([^;]+)", RegexOptions.IgnoreCase);
            if (!ffMatch.Success && !string.IsNullOrEmpty(parentStyle))
                ffMatch = Regex.Match(parentStyle, @"font-family\s*:\s*([^;]+)", RegexOptions.IgnoreCase);

            if (ffMatch.Success)
            {
               
                string family = WebUtility.HtmlDecode(ffMatch.Groups[1].Value)
                                    .Trim().Trim('"', '\'');
                try { para.FontFamily = new FontFamily(family); } catch { }
            }

            
            bool fontSizeApplied = false;
            var fsMatch = Regex.Match(styleAttr,
                @"font-size\s*:\s*([\d\.]+)\s*(pt|px|%|(?=;|$))",
                RegexOptions.IgnoreCase);

            if (fsMatch.Success)
            {
                fontSizeApplied = ApplyFontSize(para, fsMatch);
            }

           
            if (!fontSizeApplied && !string.IsNullOrEmpty(parentStyle))
            {
                var parentFsMatch = Regex.Match(parentStyle,
                    @"font-size\s*:\s*([\d\.]+)\s*(pt|px|%|(?=;|$))",
                    RegexOptions.IgnoreCase);
                if (parentFsMatch.Success)
                    ApplyFontSize(para, parentFsMatch);
            }
        }
        private static bool ApplyFontSize(Paragraph para, Match fsMatch)
        {
            string sizeValue = fsMatch.Groups[1].Value.Trim();
            if (string.IsNullOrEmpty(sizeValue)) return false;

            if (!double.TryParse(sizeValue, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out double size))
                return false;

            string unit = fsMatch.Groups[2].Value.ToLower().Trim();
            para.FontSize = unit == "pt" ? size * (96.0 / 72.0) : size;
            return true;
        }
        private static List<(string pStyle, string pContent)> ExtractParagraphs(string content)
        {
            var result = new List<(string, string)>();
            int pos = 0;

            while (pos < content.Length)
            {
                
                var pOpen = Regex.Match(
                    content.Substring(pos), @"<p([^>]*)>", RegexOptions.IgnoreCase);

                if (!pOpen.Success)
                {
                    string remaining = content.Substring(pos).Trim();
                    if (!string.IsNullOrEmpty(remaining))
                        result.Add(("", remaining));
                    break;
                }

               
                if (pOpen.Index > 0)
                {
                    string before = content.Substring(pos, pOpen.Index).Trim();
                    if (!string.IsNullOrEmpty(before))
                        result.Add(("", before));
                }

               
                string pAttribs = pOpen.Groups[1].Value;
                var styleInP = Regex.Match(pAttribs,
                    @"style\s*=\s*[""']([^""']*)[""']", RegexOptions.IgnoreCase);
                string pStyle = styleInP.Success ? styleInP.Groups[1].Value : "";

                int pContentStart = pos + pOpen.Index + pOpen.Length;
                int pEnd = content.IndexOf("</p>", pContentStart,
                    StringComparison.OrdinalIgnoreCase);

                if (pEnd < 0)
                {
                    result.Add((pStyle, content.Substring(pContentStart)));
                    break;
                }

                result.Add((pStyle,
                    content.Substring(pContentStart, pEnd - pContentStart)));
                pos = pEnd + 4; 
            }

            return result.Count > 0
                ? result
                : new List<(string, string)> { ("", content) };
        }   
        private static void ParseInlines(string html, InlineCollection inlines)
        {
            if (string.IsNullOrEmpty(html)) return;

            int pos = 0;
            while (pos < html.Length)
            {
                int tagStart = html.IndexOf('<', pos);

                if (tagStart < 0)
                {
                    string text = WebUtility.HtmlDecode(html.Substring(pos));
                    if (!string.IsNullOrEmpty(text)) inlines.Add(new Run(text));
                    break;
                }

                if (tagStart > pos)
                {
                    string text = WebUtility.HtmlDecode(html.Substring(pos, tagStart - pos));
                    if (!string.IsNullOrEmpty(text)) inlines.Add(new Run(text));
                }

                int tagEnd = html.IndexOf('>', tagStart);
                if (tagEnd < 0) { pos = html.Length; break; }

                string fullTag = html.Substring(tagStart, tagEnd - tagStart + 1);

              
                if (Regex.IsMatch(fullTag, @"^<br[\s/]*>$", RegexOptions.IgnoreCase))
                {
                    inlines.Add(new LineBreak());
                    pos = tagEnd + 1;
                    continue;
                }

               
                if (fullTag.StartsWith("</"))
                {
                    pos = tagEnd + 1;
                    continue;
                }

                var nameMatch = Regex.Match(fullTag, @"^<(\w+)");
                if (!nameMatch.Success) { pos = tagEnd + 1; continue; }

                string tagName = nameMatch.Groups[1].Value.ToLowerInvariant();
                string closeTag = $"</{tagName}>";

                int innerStart = tagEnd + 1;
                int innerEnd = FindClosingTagPos(html, innerStart, tagName);

                string innerHtml;
                int afterClose;

                if (innerEnd >= 0)
                {
                    innerHtml = html.Substring(innerStart, innerEnd - innerStart);
                    afterClose = innerEnd + closeTag.Length;
                }
                else
                {
                    innerHtml = html.Substring(innerStart);
                    afterClose = html.Length;
                }

                var span = new Span();
                ParseInlines(innerHtml, span.Inlines);
                ApplyFormatting(span, tagName, fullTag);

                if (span.Inlines.Count > 0)
                    inlines.Add(span);

                pos = afterClose;
            }
        }    
        private static int FindClosingTagPos(string html, int startPos, string tagName)
        {
            string openTag = $"<{tagName}";
            string closeTag = $"</{tagName}>";
            int depth = 1;
            int pos = startPos;

            while (pos < html.Length && depth > 0)
            {
                int nextOpen = html.IndexOf(openTag, pos, StringComparison.OrdinalIgnoreCase);
                int nextClose = html.IndexOf(closeTag, pos, StringComparison.OrdinalIgnoreCase);

                if (nextClose < 0) return -1;

                if (nextOpen >= 0 && nextOpen < nextClose)
                {
                    depth++;
                    pos = nextOpen + openTag.Length;
                }
                else
                {
                    depth--;
                    if (depth == 0) return nextClose;
                    pos = nextClose + closeTag.Length;
                }
            }
            return -1;
        }    
        private static void ApplyFormatting(Span span, string tagName, string fullTag)
        {
            switch (tagName)
            {
                case "b":
                case "strong":
                    span.FontWeight = FontWeights.Bold;                    
                    ApplyStyleAttribute(span, fullTag);
                    break;

                case "i":
                case "em":
                    span.FontStyle = FontStyles.Italic;
                    ApplyStyleAttribute(span, fullTag);
                    break;

                case "u":
                    span.TextDecorations = TextDecorations.Underline;
                    ApplyStyleAttribute(span, fullTag);
                    break;

                case "s":
                case "del":
                case "strike":
                    span.TextDecorations = TextDecorations.Strikethrough;
                    ApplyStyleAttribute(span, fullTag);
                    break;

                case "sup":
                    span.BaselineAlignment = BaselineAlignment.Superscript;
                    ApplyStyleAttribute(span, fullTag);
                    break;

                case "sub":
                    span.BaselineAlignment = BaselineAlignment.Subscript;
                    ApplyStyleAttribute(span, fullTag);
                    break;

                case "tt":
                case "code":
                    span.FontFamily = new FontFamily("Courier New");
                    break;

                case "span":
                case "font":
                    ApplyStyleAttribute(span, fullTag);
                    break;
            }
        }      
        private static void ApplyStyleAttribute(Span span, string tag)
        {
            var styleMatch = Regex.Match(tag,
                @"style\s*=\s*[""']([^""']*)[""']", RegexOptions.IgnoreCase);

            if (styleMatch.Success)
            {
                string style = styleMatch.Groups[1].Value;

                
                if (Regex.IsMatch(style, @"font-weight\s*:\s*bold", RegexOptions.IgnoreCase))
                    span.FontWeight = FontWeights.Bold;

                
                if (Regex.IsMatch(style, @"font-style\s*:\s*italic", RegexOptions.IgnoreCase))
                    span.FontStyle = FontStyles.Italic;

                
                var decoMatch = Regex.Match(style,
                    @"text-decoration\s*:\s*([^;]+)", RegexOptions.IgnoreCase);
                if (decoMatch.Success)
                {
                    string decoVal = decoMatch.Groups[1].Value.ToLowerInvariant();
                    bool hasUnderline = decoVal.Contains("underline");
                    bool hasStrike = decoVal.Contains("line-through");

                    if (hasUnderline && hasStrike)
                    {
                        var td = new TextDecorationCollection();
                        td.Add(TextDecorations.Underline[0]);
                        td.Add(TextDecorations.Strikethrough[0]);
                        span.TextDecorations = td;
                    }
                    else if (hasUnderline)
                        span.TextDecorations = TextDecorations.Underline;
                    else if (hasStrike)
                        span.TextDecorations = TextDecorations.Strikethrough;
                }

               
                var fsPt = Regex.Match(style,
                    @"font-size\s*:\s*([\d.]+)\s*pt", RegexOptions.IgnoreCase);
                var fsPx = Regex.Match(style,
                    @"font-size\s*:\s*([\d.]+)\s*px", RegexOptions.IgnoreCase);

                if (fsPt.Success &&
                    double.TryParse(fsPt.Groups[1].Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double pts))
                    span.FontSize = pts * (96.0 / 72.0);          // pt → WPF device-independent px
                else if (fsPx.Success &&
                    double.TryParse(fsPx.Groups[1].Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out double pxs))
                    span.FontSize = pxs;

               
                var ffMatch = Regex.Match(style,
                    @"font-family\s*:\s*([^;]+)", RegexOptions.IgnoreCase);
                if (ffMatch.Success)
                {
                    string family = ffMatch.Groups[1].Value.Trim().Trim('"', '\'');
                    try { span.FontFamily = new FontFamily(family); } catch { }
                }
               
                var colorMatch = Regex.Match(style,
                    @"\bcolor\s*:\s*(#[0-9a-fA-F]{3,8}|rgb\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*\)|[a-zA-Z]+)",
                    RegexOptions.IgnoreCase);
                if (colorMatch.Success)
                {
                    var brush = ParseColorToBrush(colorMatch.Groups[1].Value.Trim());
                    if (brush != null) span.Foreground = brush;
                }
               
                if (Regex.IsMatch(style, @"vertical-align\s*:\s*super", RegexOptions.IgnoreCase))
                {
                    span.BaselineAlignment = BaselineAlignment.Superscript;
                    //span.FontSize = 8;
                }
                else if (Regex.IsMatch(style, @"vertical-align\s*:\s*sub", RegexOptions.IgnoreCase))
                {
                    span.BaselineAlignment = BaselineAlignment.Subscript;
                    //span.FontSize = 8;
                }
            }
          
            var colorAttr = Regex.Match(tag,
                @"\bcolor\s*=\s*[""']([^""']*)[""']", RegexOptions.IgnoreCase);
            if (colorAttr.Success)
            {
                var brush = ParseColorToBrush(colorAttr.Groups[1].Value.Trim());
                if (brush != null) span.Foreground = brush;
            }    
            var faceAttr = Regex.Match(tag,
                @"\bface\s*=\s*[""']([^""']*)[""']", RegexOptions.IgnoreCase);
            if (faceAttr.Success)
            {
                try { span.FontFamily = new FontFamily(faceAttr.Groups[1].Value); } catch { }
            }
        }
        private static SolidColorBrush ParseColorToBrush(string colorValue)
        {
            if (string.IsNullOrWhiteSpace(colorValue)) return null;

            colorValue = colorValue.Trim();

           
            var rgbMatch = Regex.Match(colorValue,
                @"^rgb\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)\s*\)$",
                RegexOptions.IgnoreCase);

            if (rgbMatch.Success)
            {
                if (byte.TryParse(rgbMatch.Groups[1].Value, out byte r) &&
                    byte.TryParse(rgbMatch.Groups[2].Value, out byte g) &&
                    byte.TryParse(rgbMatch.Groups[3].Value, out byte b))
                    return new SolidColorBrush(Color.FromRgb(r, g, b));
            }            
            try
            {
                return (SolidColorBrush)new BrushConverter().ConvertFromString(colorValue);
            }
            catch { return null; }
        }

        #endregion HtmlToFlowDocument Helper

        #region FlowDocumentToHtml
        private static string FlowDocumentToHtml(FlowDocument doc)
        {
            var sb = new StringBuilder();
            double docSize = double.IsNaN(doc.FontSize) ? 12.0 : doc.FontSize;
            string docFamily = doc.FontFamily?.Source ?? "Segoe UI";

            double docSizePt = docSize * (72.0 / 96.0);
            sb.Append($"<div style=\"font-size:{docSizePt.ToString("0.##", CultureInfo.InvariantCulture)}pt;" +
                      $"font-family:&quot;{docFamily}&quot;;\">");

            foreach (var block in doc.Blocks)
            {
                if (block is Paragraph para)
                    sb.Append(ParagraphToHtml(para, docSize));
            }

            sb.Append("</div>");
            return sb.ToString();
        }
        private static string ParagraphToHtml(Paragraph para, double parentSize)
        {
            var sb = new StringBuilder();
            double paraSize = double.IsNaN(para.FontSize) ? parentSize : para.FontSize;
            double paraSizePt = paraSize * (72.0 / 96.0);

            sb.Append($"<p style=\"font-size:{paraSizePt.ToString("0.##", CultureInfo.InvariantCulture)}pt;" +
                      $"text-indent:0;text-align:left;margin:0;\">");

            foreach (var inline in para.Inlines)
                sb.Append(InlineToHtml(inline, paraSize));

            sb.Append("</p>");
            return sb.ToString();
        }
        private static string InlineToHtml(Inline inline, double parentSize)
        {
            if (inline is LineBreak)
                return "<br/>";

            string innerText = "";
            double currentSize = parentSize;

            if (inline is Run run)
            {
                innerText = WebUtility.HtmlEncode(run.Text);
                currentSize = double.IsNaN(run.FontSize) ? parentSize : run.FontSize;
            }
            else if (inline is Span span)
            {
                currentSize = double.IsNaN(span.FontSize) ? parentSize : span.FontSize;
                innerText = string.Concat(span.Inlines.Select(i => InlineToHtml(i, currentSize)));
            }

            if (string.IsNullOrEmpty(innerText)) return "";

            string style = BuildInlineStyle(inline, parentSize);

            return string.IsNullOrEmpty(style)
                ? innerText
                : $"<span style=\"{style}\">{innerText}</span>";
        }
        private static string BuildInlineStyle(Inline inline, double parentSize)
        {
            var styles = new List<string>();

            // Font Size
            double fontSize = inline.FontSize;
            if (!double.IsNaN(fontSize) && Math.Abs(fontSize - parentSize) > 0.1)
            {
                double pt = fontSize * (72.0 / 96.0);
                styles.Add($"font-size:{pt.ToString("0.##", CultureInfo.InvariantCulture)}pt");
            }

            // Bold
            if (inline.FontWeight == FontWeights.Bold)
                styles.Add("font-weight:bold");

            // Italic
            if (inline.FontStyle == FontStyles.Italic)
                styles.Add("font-style:italic");

            // Font Family
            string family = inline.FontFamily?.Source;
            if (!string.IsNullOrEmpty(family))
                styles.Add($"font-family:{family}");

            // Color
            if (inline.Foreground is SolidColorBrush brush)
            {
                Color c = brush.Color;
                if (c != Colors.Black)
                    styles.Add($"color:#{c.R:X2}{c.G:X2}{c.B:X2}");
            }

            // ✅ Superscript / Subscript — MAIN FIX
            if (inline.BaselineAlignment == BaselineAlignment.Superscript)
                styles.Add("vertical-align:super");
            else if (inline.BaselineAlignment == BaselineAlignment.Subscript)
                styles.Add("vertical-align:sub");

            // TextDecorations
            if (inline.TextDecorations != null && inline.TextDecorations.Count > 0)
            {
                bool hasUnder = inline.TextDecorations.Any(d => d.Location == TextDecorationLocation.Underline);
                bool hasStrike = inline.TextDecorations.Any(d => d.Location == TextDecorationLocation.Strikethrough);

                if (hasUnder && hasStrike) styles.Add("text-decoration:underline line-through");
                else if (hasUnder) styles.Add("text-decoration:underline");
                else if (hasStrike) styles.Add("text-decoration:line-through");
            }

            return string.Join(";", styles);
        }

        #endregion FlowDocumentToHtml

        #region FormattingHelpers
        private void ToggleFormatting(DependencyProperty property, object value, object normalValue)
        {
            if (TxtMessage.Selection.IsEmpty)
                return;

            TextSelection selection = TxtMessage.Selection;
            var currentValue = selection.GetPropertyValue(property);

            if (property == Inline.TextDecorationsProperty)
            {
                TextDecorationCollection currentDecorations = new TextDecorationCollection();
                if (currentValue != DependencyProperty.UnsetValue && currentValue is TextDecorationCollection existing)
                {
                    foreach (var d in existing) currentDecorations.Add(d);
                }
             
                TextDecoration target = ((TextDecorationCollection)value)[0];
                bool exists = false;
                TextDecoration found = null;

                foreach (var d in currentDecorations)
                {
                    if (d.Location == target.Location) { exists = true; found = d; break; }
                }

                if (exists) currentDecorations.Remove(found);
                else currentDecorations.Add(target);

                selection.ApplyPropertyValue(property, currentDecorations);
            }            
            else
            {
                if (currentValue != DependencyProperty.UnsetValue && currentValue.Equals(value))
                    selection.ApplyPropertyValue(property, normalValue);
                else
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

            if (property == Inline.TextDecorationsProperty)
            {
                TextDecorationCollection currentDecorations = new TextDecorationCollection();
                if (currentValue != DependencyProperty.UnsetValue && currentValue is TextDecorationCollection existing)
                {
                    foreach (var d in existing) currentDecorations.Add(d);
                }

                TextDecoration target = ((TextDecorationCollection)value)[0];
                bool exists = false;
                TextDecoration found = null;

                foreach (var d in currentDecorations)
                {
                    if (d.Location == target.Location) { exists = true; found = d; break; }
                }

                if (exists) currentDecorations.Remove(found);
                else currentDecorations.Add(target);

                selection.ApplyPropertyValue(property, currentDecorations);
            }
            else
            {
                if (currentValue != DependencyProperty.UnsetValue && currentValue.Equals(value))
                    selection.ApplyPropertyValue(property, normalValue);
                else
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

                if (!isValid) return;

                TxtErrorMessage.Text = string.Empty;
                await _viewModel.SubmitFeedbackAsync(subject, html, file);

                ShowDataGridPanel();                
                TxtSubject.Text = "";
                TxtMessage.Document.Blocks.Clear();
                this.ImagePath = "";

                await _viewModel.LoadFeedbackAsync();
                await RefreshFeedbackGrid();
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
            #region
            //BtnReplySubmit.IsEnabled = false;

            //int feedbackid = Convert.ToInt32(TxtFeedbackId.Text);
            //string rtfContent = "";
            //TextRange range = new TextRange(TxtReply.Document.ContentStart, TxtReply.Document.ContentEnd);

            //using (MemoryStream ms = new MemoryStream())
            //{
            //    range.Save(ms, DataFormats.Rtf);
            //    ms.Seek(0, SeekOrigin.Begin);
            //    using (StreamReader sr = new StreamReader(ms))
            //    {
            //        rtfContent = sr.ReadToEnd();
            //    }
            //}

            //string html = Rtf.ToHtml(rtfContent);
            //string file = this.ReplayImagePath;
            //var result = await _viewModel.SubmitFeedbackReplyAsync(feedbackid, html, file);

            //GroupBoxPanel.Visibility = Visibility.Collapsed;
            //ChatPanel.Visibility = Visibility.Visible;
            //TxtReply.Document.Blocks.Clear();
            //this.ReplayImagePath = string.Empty;

            //if (result != null && result.IsSuccess)
            //{
            //    if (!_viewModel.IsSocketConnected)
            //    {
            //        await _viewModel.GetFeedbackDetailsAsync(feedbackid);
            //        if (_viewModel.SelectedFeedbackDetails != null)
            //            await LoadChatPanel(_viewModel.SelectedFeedbackDetails);
            //    }
            //}
            //else if (!string.IsNullOrEmpty(_viewModel.ErrorMessage))
            //{              
            //    FileLogger.ApplicationLog(nameof(BtnReplySubmit_Click), _viewModel.ErrorMessage);
            //    BtnReplySubmit.IsEnabled = true;
            //}
            #endregion

            BtnReplySubmit.IsEnabled = false;

            int feedbackid = Convert.ToInt32(TxtFeedbackId.Text);
            
            string html = FlowDocumentToHtml(TxtReply.Document);

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
                FileLogger.ApplicationLog(nameof(BtnReplySubmit_Click), _viewModel.ErrorMessage);
                BtnReplySubmit.IsEnabled = true;
            }
        }
        private void TxtReply_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (BtnReplySubmit == null)
                return;

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
                ResizeMode = ResizeMode.NoResize,
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
                TxtMessage.Undo();
        }
        private void BtnRedo_Click(object sender, RoutedEventArgs e)
        {
            while (TxtMessage.CanRedo)
                TxtMessage.Redo();
        }
        private void BtnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Select Image File",
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp"
                };

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

        #endregion Events — New Feedback Formatting

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
                //selection.ApplyPropertyValue(TextElement.FontSizeProperty, 10.0);
            }
            else
            {
                selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Baseline);
                //selection.ApplyPropertyValue(TextElement.FontSizeProperty, 12.0);
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
                //selection.ApplyPropertyValue(TextElement.FontSizeProperty, 10.0);
            }
            else
            {
                selection.ApplyPropertyValue(Inline.BaselineAlignmentProperty, BaselineAlignment.Baseline);
                //selection.ApplyPropertyValue(TextElement.FontSizeProperty, 12.0);
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
                ResizeMode = ResizeMode.NoResize,
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
                TxtReply.Undo();
        }
        private void ButtonReplayRedo_Click(object sender, RoutedEventArgs e)
        {
            while (TxtReply.CanRedo)
                TxtReply.Redo();
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
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Title = "Select Image File",
                    Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp"
                };

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