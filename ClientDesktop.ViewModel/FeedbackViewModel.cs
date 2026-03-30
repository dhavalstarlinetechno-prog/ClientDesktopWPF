using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
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
        private readonly SessionService _sessionService;
        private readonly FeedbackService _FeedbackService;
        private ObservableCollection<FeedbackModel> _feedbackList;
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

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            set
            {
                _errorMessage = value;
                OnPropertyChanged(nameof(ErrorMessage));
            }
        }       
        public Action OnFeedbackSubmittedSuccessfully { get; set; }
        public static Action<int> OnRecordDeletedExternally;
        public Action OnRequestClose { get; set; }        

        private FeedbackData _selectedFeedbackDetails;
        public FeedbackData SelectedFeedbackDetails
        {
            get => _selectedFeedbackDetails;
            set
            {
                _selectedFeedbackDetails = value;
                OnPropertyChanged(nameof(SelectedFeedbackDetails));            
            }
        }
        public FeedbackViewModel(SessionService sessionService, FeedbackService feedbackService)
        {
            _sessionService = sessionService;
            _FeedbackService = feedbackService;
            FeedbackList = new ObservableCollection<FeedbackModel>();

            //OnRecordDeletedExternally = (id) => RemoveFeedbackFromGrid(id);
        }
        public async Task LoadFeedbackAsync()
        {
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
            finally
            {
                IsBusy = false;
            }
        }
        //public void RemoveFeedbackFromGrid(int feedbackId)
        //{           
        //    Application.Current.Dispatcher.Invoke(() =>
        //    {
        //        var itemToRemove = FeedbackList.FirstOrDefault(f => f.FeedbackId == feedbackId);
        //        if (itemToRemove != null)
        //        {
        //            FeedbackList.Remove(itemToRemove);
        //        }
        //    });
        //}
        public async Task GetFeedbackDetailsAsync(int feedbackId)
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var data = await _FeedbackService.GetFeedbackDetailsAsync(feedbackId);

                if (data != null)
                {
                    // Response bind ho gaya selected model pe
                    SelectedFeedbackDetails = data;
                }
                else
                {
                    ErrorMessage = "Failed to load feedback details.";
                }
            }
            finally
            {
                IsBusy = false;
            }
        }
        public async Task<FeedbackResponse> SubmitFeedbackAsync(string feedbackSubject,string htmlMessage, string filePath)
        {
            ErrorMessage = string.Empty;
        
            var response = await _FeedbackService.GenerateFeedbackAsync(feedbackSubject, htmlMessage, filePath);

            if (response != null && response.IsSuccess)
            {
                // Triggers View Code-Behind to Clear Grid/Fields
                OnFeedbackSubmittedSuccessfully?.Invoke();
            }
            else
            {
                ErrorMessage = response?.Exception ?? response?.SuccessMessage ?? "Failed to submit feedback.";
            }

            return response;
        }
        public async Task<FeedbackReplyResponse> SubmitFeedbackReplyAsync(int feedbackId, string feedbackMessage, string filePath)
        {
            IsBusy = true;
            ErrorMessage = string.Empty;
            try
            {
                var response = await _FeedbackService.ReplyFeedbackAsync(feedbackId, feedbackMessage, filePath);

                if (response != null && response.IsSuccess)
                {
                    OnFeedbackSubmittedSuccessfully?.Invoke();
                }
                else
                {
                    ErrorMessage = response?.SuccessMessage ?? "Failed to send reply.";
                }
                return response;
            }
            finally
            {
                IsBusy = false;
            }
        }

        //public void Close()
        //{
        //    OnRequestClose?.Invoke();
        //}
    }
}
