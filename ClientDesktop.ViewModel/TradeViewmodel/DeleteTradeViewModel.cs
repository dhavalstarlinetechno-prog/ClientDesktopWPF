using ClientDesktop.Core.Base;
using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Services;
using DocumentFormat.OpenXml.Presentation;
using DocumentFormat.OpenXml.Wordprocessing;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class DeleteTradeViewModel : ViewModelBase, ICloseable
    {
        private readonly TradeService _tradeService;
        public string _order { get; set; }
        public string deleteMessage { get; set; }
        public Action CloseAction { get; set; }
        public bool? isDeleted { get; set; } = null;
        private string _randomString;
        public string RandomString
        {
            get => _randomString;
            set { _randomString = value; OnPropertyChanged(); }
        }

        private string _userInput;
        public string UserInput
        {
            get => _userInput;
            set
            {
                _userInput = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public ICommand SubmitCommand { get; }
        public ICommand CancelCommand { get; }

        public DeleteTradeViewModel(TradeService tradeService)
        {
            _tradeService = tradeService;
            GenerateRandomString();

            SubmitCommand = new AsyncRelayCommand(async (param) => await ExecuteSubmitAsync(), CanExecuteSubmit);
            CancelCommand = new RelayCommand((param) => CloseAction?.Invoke());
        }

        private void GenerateRandomString()
        {
            Random random = new Random();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            RandomString = new string(Enumerable.Range(0, 6)
                .Select(x => chars[random.Next(chars.Length)]).ToArray());
        }

        private bool CanExecuteSubmit(object param)
        {
            return !IsBusy && (UserInput == RandomString || UserInput == " ");
        }

        private async Task ExecuteSubmitAsync()
        {
            IsBusy = true;

            isDeleted = await _tradeService.DeleteOrderAsync(_order);

            IsBusy = false;

            if (isDeleted.Value)
            {
                deleteMessage = CommonMessages.OrderDeleted;
            }
            else
            {
                deleteMessage = CommonMessages.OrderFailedToDelete;
            }
            CloseAction?.Invoke();
        }


    }
}