using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Services;
using ClientDesktop.ViewModel;
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
        }

        #endregion Constructor

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
        * {{ margin: 0; padding: 0; box-sizing: border-box; }}
        html, body {{
            word-wrap: break-word;
            overflow-wrap: break-word;
            white-space: pre-wrap;
            overflow: hidden;
            background: white;
            width: 100%;
        }}
        p {{ margin: 0; padding: 0; word-break: break-word; }}
    </style>
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
            messageLayout.Children.Add(new TextBlock
            {
                Text = msgTime.ToString("dd/MM/yy HH:mm") + " ✔✔",
                FontSize = 13,
                Foreground = Brushes.Gray,
                Margin = new Thickness(0),
                HorizontalAlignment = HorizontalAlignment.Left
            });

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
                    return;
               
                foreach (var chat in feedback.ChatList)
                {
                    var listItem = await CreateChatListBoxItemAsync(chat);
                    ChatPanel.Items.Add(listItem);
                }               
                await Task.Delay(300);
                ScrollChatToBottom();
            }
            catch { }
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

                    await LoadChatPanel(feedback);

                    ShowReplyPanel();
                    GroupBoxPanel.Visibility = Visibility.Collapsed;
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
                if (string.IsNullOrEmpty(subject))
                {
                    TxtSubject.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 102, 102));
                    isValid = false;
                }
                if (string.IsNullOrEmpty(plainText))
                {
                    TxtMessage.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 102, 102));
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