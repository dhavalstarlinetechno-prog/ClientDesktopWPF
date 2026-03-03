using ClientDesktop.Core.Base;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ClientDesktop.ViewModel
{
    public class InvoiceViewModel : ViewModelBase, ICloseable
    {
        private readonly SessionService _sessionService;
        private readonly InvoiceService _invoiceService;
        //private Invoicemodel _invoiceData;
        private ObservableCollection<Invoicemodel> _invoiceData;


        public InvoiceViewModel(SessionService sessionService, InvoiceService invoiceService)
        {
            _sessionService = sessionService;
            _invoiceService = invoiceService;           
        }

        public ObservableCollection<Invoicemodel> InvoiceDetails
        {
            get => _invoiceData;
            set
            {
                _invoiceData = value;
                OnPropertyChanged();
            }
        }

        public async Task<bool> VerifyPasswordAsync(string password)
        {
            string clientId = _sessionService.UserId;
            string licenseId = _sessionService.LicenseId;

            var result = await _invoiceService
                .VerifyUserPasswordAsync(clientId, password, licenseId);

            if (!result.Success)
            {
                MessageBox.Show(result.ErrorMessage,
                                "Authentication Failed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        public async Task LoadInvoiceDetailAsync(string fromdate, string todate)
        {
            var result = await _invoiceService.InvoiceLoadData(fromdate, todate);

            if (result == null)
                return;

            InvoiceDetails = new ObservableCollection<Invoicemodel>(result);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
