using ClientDesktop.Core.Base;
using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Core.Models;
using ClientDesktop.Infrastructure.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace ClientDesktop.ViewModel
{
    public class InvoiceViewModel : ViewModelBase, ICloseable
    {
        // ─────────────────────────────────────────────────────────────
        // Dependencies
        // ─────────────────────────────────────────────────────────────
        private readonly SessionService _sessionService;
        private readonly InvoiceService _invoiceService;
        private readonly IPdfService _pdfService;          // ← NEW

        // ─────────────────────────────────────────────────────────────
        // Observable data
        // ─────────────────────────────────────────────────────────────
        private ObservableCollection<Invoicemodel> _invoiceData;

        public ObservableCollection<Invoicemodel> InvoiceDetails
        {
            get => _invoiceData;
            set { _invoiceData = value; OnPropertyChanged(); }
        }

        // ─────────────────────────────────────────────────────────────
        // PDF state – stored after every successful "Get Data" click
        // ─────────────────────────────────────────────────────────────

        /// <summary>Stores per-security DataTables ready for PDF rendering.</summary>
        private List<(string SecurityName, DataTable Table)> _securityPdfTables
            = new List<(string, DataTable)>();

        private DataTable _summaryPdfTable;
        private DataTable _carryPdfTable;
        private string _pdfTitle;
        private string _pdfSubTitle;

        // ─────────────────────────────────────────────────────────────
        // Constructor – IPdfService injected (register in DI container)
        // ─────────────────────────────────────────────────────────────
        public InvoiceViewModel(
            SessionService sessionService,
            InvoiceService invoiceService,
            IPdfService pdfService)           // ← NEW parameter
        {
            _sessionService = sessionService;
            _invoiceService = invoiceService;
            _pdfService = pdfService;
        }

        // ─────────────────────────────────────────────────────────────
        // Password verification
        // ─────────────────────────────────────────────────────────────
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

        // ─────────────────────────────────────────────────────────────
        // Load invoice data from API
        // ─────────────────────────────────────────────────────────────
        public async Task LoadInvoiceDetailAsync(string fromdate, string todate)
        {
            var result = await _invoiceService.InvoiceLoadData(fromdate, todate);

            if (result == null) return;

            InvoiceDetails = new ObservableCollection<Invoicemodel>(result);
        }

        // ─────────────────────────────────────────────────────────────
        // PDF – called from code-behind AFTER all DataTables are built
        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Stores all PDF-ready DataTables.
        /// Call this at the END of Btngetdata_Click, after all grids are populated.
        /// </summary>
        /// <param name="title">e.g. "UserId - Username"</param>
        /// <param name="subTitle">e.g. "From 01-01-2025 To 05-01-2025"</param>
        /// <param name="securityTables">Per-security: (name, DataTable with RowType column)</param>
        /// <param name="summaryTable">Summary DataTable — may be null if no summary rows</param>
        /// <param name="carryTable">Carry-forward DataTable — may be null if no carry rows</param>
        public void PreparePdfData(
            string title,
            string subTitle,
            List<(string SecurityName, DataTable Table)> securityTables,
            DataTable summaryTable,
            DataTable carryTable)
        {
            _pdfTitle = title;
            _pdfSubTitle = subTitle;
            _securityPdfTables = securityTables ?? new List<(string, DataTable)>();
            _summaryPdfTable = summaryTable;
            _carryPdfTable = carryTable;
        }

        /// <summary>
        /// Builds and saves the Invoice PDF.
        /// Call this from PdfExport_Click in the code-behind.
        /// </summary>
        public void BuildInvoicePdf()
        {
            // Guard – no data yet
            if (_securityPdfTables == null || _securityPdfTables.Count == 0)
            {
                MessageBox.Show("Please load invoice data before exporting.",
                                "No Data",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                return;
            }

            // Column alignments reused across security grids
            var securityAlignments = new Dictionary<string, PdfColumnAlignment>
            {
                ["BVol"] = PdfColumnAlignment.Right,
                ["SVol"] = PdfColumnAlignment.Right,
                ["Rate"] = PdfColumnAlignment.Right,
                ["Comm"] = PdfColumnAlignment.Right,
                ["Net"] = PdfColumnAlignment.Right
            };

            // ── 1. Clear previous state ───────────────────────────────
            _pdfService.Clear();

            // ── 2. Title & Sub-title ──────────────────────────────────
            _pdfService
                .AddTitle(_pdfTitle, fontSize: 18, centerAlign: true)
                .AddSubTitle(_pdfSubTitle, fontSize: 13, centerAlign: false);

            // ── 3. Security Grids ─────────────────────────────────────
            foreach (var (securityName, table) in _securityPdfTables)
            {
                _pdfService
                    .AddGrid(
                        dataTable: table,
                        gridTitle: securityName,
                        footerData: null,
                        columnAlignments: securityAlignments)
                    .AddSpacing();
            }

            // ── 4. Summary Grid (optional) ────────────────────────────
            if (_summaryPdfTable != null && _summaryPdfTable.Rows.Count > 0)
            {
                var summaryAlignments = new Dictionary<string, PdfColumnAlignment>
                {
                    ["M2M"] = PdfColumnAlignment.Right,
                    ["Comm"] = PdfColumnAlignment.Right,
                    ["Total"] = PdfColumnAlignment.Right
                };

                _pdfService
                    .AddTitle("SUMMARY", fontSize: 14, centerAlign: true)
                    .AddGrid(
                        dataTable: _summaryPdfTable,
                        gridTitle: null,
                        footerData: null,
                        columnAlignments: summaryAlignments)
                    .AddSpacing();
            }

            // ── 5. Carry-Forward Grid (optional) ──────────────────────
            if (_carryPdfTable != null && _carryPdfTable.Rows.Count > 0)
            {
                var carryAlignments = new Dictionary<string, PdfColumnAlignment>
                {
                    ["Type"] = PdfColumnAlignment.Right,
                    ["Quantity"] = PdfColumnAlignment.Right,
                    ["Net"] = PdfColumnAlignment.Right
                };

                _pdfService
                    .AddTitle("CARRY FORWARD", fontSize: 14, centerAlign: true)
                    .AddGrid(
                        dataTable: _carryPdfTable,
                        gridTitle: null,
                        footerData: null,
                        columnAlignments: carryAlignments)
                    .AddSpacing();
            }

            // ── 6. Build & Save PDF ───────────────────────────────────
            string baseFileName = _sessionService.UserId + "_Invoice";
            _pdfService.BuildPDF(baseFileName, landscape: false, autoFormat: true);
        }

        // ─────────────────────────────────────────────────────────────
        // INotifyPropertyChanged
        // ─────────────────────────────────────────────────────────────
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}