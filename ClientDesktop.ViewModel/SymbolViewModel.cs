using ClientDesktop.Core.Base;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
using DocumentFormat.OpenXml.Drawing.Charts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace ClientDesktop.ViewModel
{
    public class SymbolViewModel : ViewModelBase
    {
        private readonly SessionService _sessionService;
        private readonly SymbolService _symbolService;

        public Action CloseAction { get; set; }
        public ICommand CloseCommand { get; }

        private string _symbolName;
        public string SymbolName
        {
            get => _symbolName;
            set => SetProperty(ref _symbolName, value);
        }

        private ObservableCollection<Folder> _folders;
        public ObservableCollection<Folder> Folders
        {
            get { return _folders; }
            set
            {
                _folders = value;
                OnPropertyChanged(nameof(Folders));
            }
        }
        private ObservableCollection<SubSymbolModel> _subSymbols;

        private ObservableCollection<SymbolModel> _symbolData;

        public ObservableCollection<SubSymbolModel> SubSymbols
        {
            get => _subSymbols;
            set { _subSymbols = value; OnPropertyChanged(nameof(SubSymbols)); }
        }
        public ObservableCollection<SubSymbolModel> Loadsymbolsbyroute
        {
            get => _subSymbols;
            set { _subSymbols = value; OnPropertyChanged(nameof(Loadsymbolsbyroute)); }
        }
        public ObservableCollection<SubSymbolModel> Loaddolorsymbols
        {
            get => _subSymbols;
            set { _subSymbols = value; OnPropertyChanged(nameof(Loaddolorsymbols)); }
        }

        public ObservableCollection<SymbolModel> SymbolData
        {
            get => _symbolData;
            set { _symbolData = value; OnPropertyChanged(nameof(SymbolData)); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public SymbolViewModel(SessionService sessionService, SymbolService symbolService)
        {
            _sessionService = sessionService;
            _symbolService = symbolService;
            Folders = new ObservableCollection<Folder>();
            SubSymbols = new ObservableCollection<SubSymbolModel>();
            SymbolData = new ObservableCollection<SymbolModel>();
            CloseCommand = new RelayCommand(_ => CloseAction?.Invoke());
        }

        public async Task<string> LoadSymbolsAsync()
        {
            try
            {
                IsBusy = true;

            
                var result = await _symbolService.GetSymbolsAsync();

                Folders.Clear();
                if (result != null && result.Data != null)
                {
                    foreach (var item in result.Data)
                    {
                        Folders.Add(item);
                    }
                }
                // Return the serialized result so Window_Loaded can use it
                return Newtonsoft.Json.JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading symbols: " + ex.Message);
                return string.Empty; // Return empty string on error to satisfy the return type
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<string> LoadSubSymbolsAsync()
        {
            try
            {
                var result = await _symbolService.GetSubSymbolsAsync();
                SubSymbols.Clear();
                if (result?.Data != null)
                {
                    foreach (var item in result.Data)
                    {
                        SubSymbols.Add(item);
                    }
                }

                // Return the serialized result
                return Newtonsoft.Json.JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading sub-symbols: " + ex.Message);
                return string.Empty; // Return empty string on error
            }
        }

        public async Task<string> Loadsymbolsbyrouteforclient(int routeId)
        {
            try
            {
                IsBusy = true;

                // 1. Call the service method we created earlier
                var result = await _symbolService.Getsymbolsbyrouteforclient(routeId);

                // 2. Clear existing data
                Loadsymbolsbyroute.Clear();

                if (result != null && result.Data != null)
                {
                    // 3. Fill the ObservableCollection for the UI
                    foreach (var item in result.Data)
                    {
                        Loadsymbolsbyroute.Add(item);
                    }
                }

                // 4. Return the serialized JSON as per your other methods' pattern
                return Newtonsoft.Json.JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading symbols by route: " + ex.Message);
                return string.Empty;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<string> LoadDolorSignTree(string symbolId)
        {
            try
            {
                IsBusy = true;

                // 1. Call the service method we created earlier
                var result = await _symbolService.GetDolorSignTree(symbolId);

                // 2. Clear existing data
                Loaddolorsymbols.Clear();

                if (result != null && result.Data != null)
                {
                    // 3. Fill the ObservableCollection for the UI
                    foreach (var item in result.Data)
                    {
                        Loaddolorsymbols.Add(item);
                    }
                }

                // 4. Return the serialized JSON as per your other methods' pattern
                return Newtonsoft.Json.JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading symbols by route: " + ex.Message);
                return string.Empty;
            }
            finally
            {
                IsBusy = false;
            }
        }

        public async Task<string> LoadSymbolDetailsAsync(int symbolId)
        {
            try
            {
                IsBusy = true;

                // 1. Call the service method we created earlier
                var result = await _symbolService.GetSymbolDetailsAsync(symbolId);

                if (result != null && result != null)
                {
                    SymbolData.Clear();
                    SymbolData.Add(result);
                }

                // 4. Return the serialized JSON as per your other methods' pattern
                return Newtonsoft.Json.JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading symbols by route: " + ex.Message);
                return string.Empty;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
