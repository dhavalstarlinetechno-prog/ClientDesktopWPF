using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace ClientDesktop.ViewModel
{
    public class BanScriptViewModel : INotifyPropertyChanged
    {
        private readonly BanScriptService _banScriptService;

        public ObservableCollection<BanscriptGridRow> GridRows { get; set; }

        private bool _isDataLoaded;

        public bool IsDataLoaded
        {
            get => _isDataLoaded;
            set
            {
                _isDataLoaded = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsNoDataVisible));
            }
        }
        public bool IsNoDataVisible => IsDataLoaded && GridRows.Count == 0;

        public BanScriptViewModel()
        {
            _banScriptService = new BanScriptService();
            GridRows = new ObservableCollection<BanscriptGridRow>();
            BanScipt_data();
        }
        //public async void BanScipt_data()
        //{
        //    try
        //    {
        //        var bansctiptTask = _banScriptService.GetBanScript();
        //        await Task.WhenAll(bansctiptTask);

        //        var banResult = await bansctiptTask;
        //        Application.Current.Dispatcher.Invoke(() =>
        //        {
        //            GridRows.Clear();
        //            var BanscriptList = banResult.BanScripts ?? new List<BanScripts>();

        //            foreach (var ban in BanscriptList)
        //            {             
        //                GridRows.Add(new BanscriptGridRow
        //                {                           
        //                    Symbol = ban.SymbolDisplayName,                           
        //                });
        //            }
        //            IsDataLoaded = true;
        //        });
        //    }
        //    catch (Exception ex)
        //    {
        //        IsDataLoaded = true;
        //        Console.WriteLine("Data Load Error: " + ex.Message);
        //    }
        //}

        public async Task BanScipt_data()
        {
            try
            {
                IsDataLoaded = false;
                GridRows.Clear();

                var banResult = await _banScriptService.GetBanScript();

                var banscriptList = banResult?.BanScripts ?? new List<BanScripts>();

                foreach (var ban in banscriptList)
                {
                    GridRows.Add(new BanscriptGridRow
                    {
                        Symbol = ban.SymbolDisplayName
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Data Load Error: " + ex.Message);
            }
            finally
            {
                IsDataLoaded = true; // 🔑 important
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
