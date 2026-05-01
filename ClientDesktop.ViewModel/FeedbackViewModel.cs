using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using ClientDesktop.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ClientDesktop.ViewModel
{
    public class FeedbackViewModel : ViewModelBase, ICloseable
    {
        #region Variables/Properties

        private readonly SessionService _sessionService;
        private readonly FeedbackService _FeedbackService;
        private ObservableCollection<FeedbackModel> _feedbackList = new ObservableCollection<FeedbackModel>();
        private readonly ISocketService _socketService;
        public ObservableCollection<FeedbackModel> FeedbackList
        {
            get => _feedbackList;
            set
            {
                _feedbackList = value;
                OnPropertyChanged(nameof(FeedbackList));
            }
        }
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }       
        public Action? OnFeedbackSubmittedSuccessfully { get; set; }

        public static Action<int>? OnRecordDeletedExternally;
        public Action? OnRequestClose { get; set; }        

        private FeedbackData? _selectedFeedbackDetails;
        public FeedbackData? SelectedFeedbackDetails
        {
            get => _selectedFeedbackDetails;
            set
            {
                _selectedFeedbackDetails = value;
                OnPropertyChanged(nameof(SelectedFeedbackDetails));            
            }
        }

        private int _currentFeedbackId = 0;
        public int CurrentFeedbackId
        {
            get => _currentFeedbackId;
            private set { _currentFeedbackId = value; OnPropertyChanged(nameof(CurrentFeedbackId)); }
        }

        public event Action<ChatList>? OnNewReplyReceived;

        #endregion Variables/Properties

        #region Constructor 

        public FeedbackViewModel(SessionService sessionService, FeedbackService feedbackService, ISocketService socketService)
        {
            _sessionService = sessionService;
            _FeedbackService = feedbackService;
            _socketService = socketService;
            FeedbackList = new ObservableCollection<FeedbackModel>();         
        }

        #endregion Constructor

        #region SocketLogic
        public void SubscribeToFeedbackSocket()
        {
            if (_socketService == null) return;          
            _socketService.OnFeedbackReplyReceived -= HandleSocketFeedbackReply;
            _socketService.OnFeedbackReplyReceived += HandleSocketFeedbackReply;
        }
        public void UnsubscribeFromFeedbackSocket()
        {
            if (_socketService == null) return;
            _socketService.OnFeedbackReplyReceived -= HandleSocketFeedbackReply;
        }
        private void HandleSocketFeedbackReply(FeedbackReplyData data)
        {           
            if (data == null || data.FeedbackId != CurrentFeedbackId)
                return;
            
            var chatItem = new ChatList
            {
                operatorId = data.OperatorId,
                feedbackId = data.FeedbackId,
                userName = data.UserName,
                userRole = data.UserRole,
                feedbackMessage = data.FeedbackMessage,
                isReply = data.IsReply,
                isRead = data.IsRead,
                filePath = data.FilePath ?? new List<string>(),
                createdOn = data.CreatedOn
            };           
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                OnNewReplyReceived?.Invoke(chatItem);
            });
        }
        public bool IsSocketConnected => _socketService?.IsConnected ?? false;

        #endregion SocketLogic

        #region Methods
        public async Task LoadFeedbackAsync()
        {
         
            if (!_sessionService.IsInternetAvailable)
                return;
            IsBusy = true;
            ErrorMessage = string.Empty;
            try
            {
                var response = await _FeedbackService.GetFeedbackListAsync();
                if (response != null && response.IsSuccess && response.Data != null)
                {
                    FeedbackList = new ObservableCollection<FeedbackModel>(response.Data);
                }
                else
                {
                    ErrorMessage = response?.Exception?.ToString() ?? "Failed to load feedback.";
                }
            }
            catch(Exception ex)
            {
                FileLogger.ApplicationLog(nameof(LoadFeedbackAsync),ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }       
        public async Task GetFeedbackDetailsAsync(int feedbackId)
        {
            if (!_sessionService.IsInternetAvailable)
                return;
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var data = await _FeedbackService.GetFeedbackDetailsAsync(feedbackId);

                if (data != null)
                {                    
                    SelectedFeedbackDetails = data;
                    CurrentFeedbackId = feedbackId;
                }
                else
                {                    
                    CurrentFeedbackId = 0;
                }
            }
            catch(Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GetFeedbackDetailsAsync), ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task<FeedbackResponse?> SubmitFeedbackAsync(string? feedbackSubject, string htmlMessage, string filePath)
        {
            if (!_sessionService.IsInternetAvailable) return null;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var response = await _FeedbackService.GenerateFeedbackAsync(feedbackSubject ?? string.Empty, htmlMessage, filePath);

                if (response != null && response.IsSuccess)
                    OnFeedbackSubmittedSuccessfully?.Invoke();
                else
                    ErrorMessage = response?.Exception ?? response?.SuccessMessage ?? "Failed to submit feedback.";

                return response;
            }
            catch(Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SubmitFeedbackAsync), ex.Message);
                return null;
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task<FeedbackReplyResponse?> SubmitFeedbackReplyAsync(int feedbackId, string feedbackMessage, string filePath)
        {
            if (!_sessionService.IsInternetAvailable) return null;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var response = await _FeedbackService.ReplyFeedbackAsync(feedbackId, feedbackMessage, filePath);

                if (response != null && response.IsSuccess)
                    OnFeedbackSubmittedSuccessfully?.Invoke();
                else
                    ErrorMessage = response?.SuccessMessage ?? "Failed to send reply.";

                return response;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(SubmitFeedbackReplyAsync), ex.Message);
                return null;
            }
            finally
            {
                IsBusy = false;
            }
        }
        public void ResetCurrentFeedback()
        {
            CurrentFeedbackId = 0;
            SelectedFeedbackDetails = null;
            UnsubscribeFromFeedbackSocket();
        }

        #endregion Methods
    }
}
