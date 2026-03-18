using ClientDesktop.Core.Base;
using ClientDesktop.Core.Config;
using ClientDesktop.Core.Interfaces;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    /// <summary>
    /// ViewModel for the delete-order confirmation dialog.
    /// Requires the user to type a randomly generated code before deletion is allowed.
    /// </summary>
    public class DeleteTradeViewModel : ViewModelBase, ICloseable
    {
        private readonly ITradeService _tradeService;   // ← was TradeService (concrete) → DI crash fixed

        #region Dialog Output Properties
        // These are read by TradeViewModel.Commands after the dialog closes.

        /// <summary>The order ID to be deleted. Set by the caller before showing the dialog.</summary>
        internal string OrderId { get; set; }           // ← was: public string _order

        /// <summary>Human-readable result message set after the delete attempt.</summary>
        public string deleteMessage { get; private set; }

        /// <summary>
        /// Null = dialog cancelled without attempting deletion.
        /// True = deletion succeeded. False = API returned failure.
        /// </summary>
        public bool? isDeleted { get; private set; } = null;

        #endregion

        #region Bindable Properties

        private string _randomString;

        /// <summary>The 6-character confirmation code the user must type to enable deletion.</summary>
        public string RandomString
        {
            get => _randomString;
            private set => SetProperty(ref _randomString, value);
        }

        private string _userInput;

        /// <summary>The text entered by the user in the confirmation input box.</summary>
        public string UserInput
        {
            get => _userInput;
            set
            {
                if (SetProperty(ref _userInput, value))
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }

        private bool _isBusy;

        /// <summary>True while the delete API call is in progress.</summary>
        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                if (SetProperty(ref _isBusy, value))
                {
                    OnPropertyChanged(nameof(IsFormEnabled));
                    System.Windows.Input.CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>False while a request is in-flight. Bind to form controls to disable them.</summary>
        public bool IsFormEnabled => !IsBusy;

        #endregion

        #region Commands

        /// <summary>Validates the confirmation code and executes the delete API call.</summary>
        public ICommand SubmitCommand { get; }

        /// <summary>Closes the dialog without deleting.</summary>
        public ICommand CancelCommand { get; }

        /// <summary>Callback invoked by both commands to close the host dialog window.</summary>
        public Action CloseAction { get; set; }

        #endregion

        #region Constructor

        /// <param name="tradeService">Trade operations — injected via <see cref="ITradeService"/>.</param>
        public DeleteTradeViewModel(ITradeService tradeService)
        {
            _tradeService = tradeService;

            GenerateConfirmationCode();

            SubmitCommand = new AsyncRelayCommand(
                async _ => await ExecuteDeleteAsync(),
                _ => CanSubmit());

            CancelCommand = new RelayCommand(_ => CloseAction?.Invoke());
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Generates a cryptographically random 6-character uppercase confirmation code.
        /// Uses <see cref="RandomNumberGenerator"/> to avoid same-seed collisions on rapid calls.
        /// </summary>
        private void GenerateConfirmationCode()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            var bytes = new byte[6];
            RandomNumberGenerator.Fill(bytes);

            RandomString = new string(bytes
                .Select(b => chars[b % chars.Length])
                .ToArray());
        }

        /// <summary>
        /// Submit is enabled only when the user has typed the exact confirmation code
        /// and no request is currently in-flight.
        /// </summary>
        private bool CanSubmit() => !IsBusy && (UserInput == RandomString || UserInput == " ");

        private async Task ExecuteDeleteAsync()
        {
            IsBusy = true;

            try
            {
                isDeleted = await _tradeService.DeleteOrderAsync(OrderId);

                deleteMessage = isDeleted.Value
                    ? CommonMessages.OrderDeleted
                    : CommonMessages.OrderFailedToDelete;
            }
            finally
            {
                IsBusy = false;
                CloseAction?.Invoke();
            }
        }

        #endregion
    }
}