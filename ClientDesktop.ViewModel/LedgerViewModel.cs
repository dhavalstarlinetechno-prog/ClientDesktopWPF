using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks; // Async tasks ke liye
using System.Windows;       // UI Dispatcher ke liye
using System.Windows.Data;

namespace ClientDesktop.ViewModel
{
    public class LedgerViewModel : INotifyPropertyChanged
    {
        private readonly LedgerService _ledgerService;
        private LedgerUserDetail _ledgerUser;
        public ObservableCollection<Ledgermodel> GridRows { get; set; }
        public LedgerViewModel()
        {
            _ledgerService = new LedgerService();
            GridRows = new ObservableCollection<Ledgermodel>();       
        }

        public LedgerUserDetail LedgerUser
        {
            get => _ledgerUser;
            set
            {
                _ledgerUser = value;
                OnPropertyChanged();
            }
        }

        public async Task<bool> VerifyPasswordAsync(string password)
        {
            string clientId = SessionManager.UserId;
            string licenseId = SessionManager.LicenseId;

            var result = await _ledgerService
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

        public async Task LoadLedgerUserDetailAsync()
        {
            var result = await _ledgerService.GetLedgerUserDetail();

            if (result == null)
                return;

            LedgerUser = result;
        }

        public async Task<(bool success, string error, LedgerData ledgerData)> LoadLedgerListAsync(string clientId, DateTime fromDate, DateTime toDate, string licenseId)
        {
            var (success, error, ledgerData) =
                await _ledgerService.GetLedgerListAsync(clientId, fromDate, toDate, licenseId);

            if (!success)
            {
                MessageBox.Show(error,
                                "Ledger Load Failed",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }

            return (success, error, ledgerData);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
