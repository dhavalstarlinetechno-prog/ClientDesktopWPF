using ClientDesktop.Core.Base;
using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Logger;
using ClientDesktop.Infrastructure.Services;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class DeleteFeedbackViewModel : ViewModelBase, ICloseable
    {
        #region Variables/Properties

        private readonly SessionService _sessionService;
        private readonly FeedbackService _FeedbackService;
        public int FeedbackId { get; set; }
        public string? deleteMessage { get; private set; }
        public bool? isDeleted { get; private set; } = null;

        private string? _randomString;
        public string? RandomString
        {
            get => _randomString;
            private set => SetProperty(ref _randomString, value);
        }

        private string? _userInput;
        public string? UserInput
        {
            get => _userInput;
            set
            {
                if (SetProperty(ref _userInput, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsFormEnabled));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }
        public bool IsFormEnabled => !IsBusy;
        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }
        public new Action? CloseAction { get; set; }

        #endregion Variables/Properties

        #region Constructor
        public DeleteFeedbackViewModel(SessionService sessionService, FeedbackService feedbackService)
        {
            _sessionService = sessionService;
            _FeedbackService = feedbackService;

            GenerateConfirmationCode();

            SubmitCommand = new AsyncRelayCommand(
                 async _ => await ExecuteDeleteAsync(),
                 _ => CanSubmit());

            CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
        }

        #endregion Constructor

        #region Methods
        private void GenerateConfirmationCode()
        {
            if (!_sessionService.IsInternetAvailable)
                return;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var bytes = new byte[6];
            RandomNumberGenerator.Fill(bytes);

            RandomString = new string(bytes
                .Select(b => chars[b % chars.Length])
                .ToArray());
        }
        private bool CanSubmit()
        {
            if (!_sessionService.IsInternetAvailable)
                return false;
            if (IsBusy) return false;
            return UserInput == RandomString || UserInput == " ";
        }
        private async Task ExecuteDeleteAsync()
        {
            if (!_sessionService.IsInternetAvailable)
                return;
            IsBusy = true;

            try
            {
                var response = await _FeedbackService.DeleteFeedbackAsync(FeedbackId);
                isDeleted = response.IsSuccess;

                deleteMessage = isDeleted.Value
                    ? CommonMessages.DeleteFeedbackValidation
                    : CommonMessages.FailedDeleteFeedbackValidation;

                if (isDeleted == true)
                {
                    FeedbackViewModel.OnRecordDeletedExternally?.Invoke(FeedbackId);
                }
            }
            catch(Exception ex)
            {
                FileLogger.ApplicationLog(nameof(ExecuteDeleteAsync),ex.Message);
            }
            finally
            {
                IsBusy = false;
                CloseAction?.Invoke();
            }
        }
        
        #endregion Methods
    }
}