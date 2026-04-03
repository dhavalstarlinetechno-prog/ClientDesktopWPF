using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using iText.Layout.Properties;
using System.Data;

namespace ClientDesktop.Infrastructure.Services
{
    /// <summary>
    /// Implements IPdfService by delegating to the PDFBuilder utility.
    /// This is the ONLY layer that knows about PDFBuilder and iText7.
    /// Registered as Transient in DI so each screen gets a fresh, clean builder instance.
    /// </summary>
    public class PdfService : IPdfService
    {
        #region Fields

        private readonly PDFBuilder _builder;

        #endregion

        #region Styling Properties Implementation

        public float CellFontSize { get => _builder?.CellFontSize ?? 9f; set { if (_builder != null) _builder.CellFontSize = value; } }
        public float HeaderFontSize { get => _builder?.HeaderFontSize ?? 10f; set { if (_builder != null) _builder.HeaderFontSize = value; } }
        public float HeaderPadding { get => _builder?.HeaderPadding ?? 6f; set { if (_builder != null) _builder.HeaderPadding = value; } }
        public float CellPadding { get => _builder?.CellPadding ?? 5f; set { if (_builder != null) _builder.CellPadding = value; } }
        public bool ShowVerticalBorders { get => _builder?.ShowVerticalBorders ?? false; set { if (_builder != null) _builder.ShowVerticalBorders = value; } }
        public Dictionary<string, float> ColumnWidths { get => _builder?.ColumnWidths ?? new(); set { if (_builder != null) _builder.ColumnWidths = value; } }

        #endregion

        #region Constructor

        public PdfService()
        {
            try
            {
                _builder = new PDFBuilder();

                _builder.OnError += msg =>
                    FileLogger.ApplicationLog(nameof(PdfService), $"PDF generation error: {msg}");
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(PdfService), ex);
            }
        }

        #endregion

        #region IPdfService Implementation

        /// <inheritdoc/>
        public IPdfService AddTitle(string title, int fontSize = 18, bool centerAlign = true)
        {
            try
            {
                _builder?.AddTitle(title, fontSize, centerAlign);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(AddTitle), ex);
            }
            return this;
        }

        /// <inheritdoc/>
        public IPdfService AddSubTitle(string subTitle, int fontSize = 13, bool centerAlign = false)
        {
            try
            {
                _builder?.AddSubTitle(subTitle, fontSize, centerAlign);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(AddSubTitle), ex);
            }
            return this;
        }

        /// <inheritdoc/>
        public IPdfService AddGrid(
            DataTable dataTable,
            string? gridTitle = null,
            Dictionary<string, string>? footerData = null,
            Dictionary<string, EnumPdfColumnAlignment>? columnAlignments = null,
            bool repeatHeader = true)
        {
            try
            {
                var iTextAlignments = MapAlignments(columnAlignments);
                _builder?.AddGrid(dataTable, gridTitle, footerData, iTextAlignments, repeatHeader);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(AddGrid), ex);
            }
            return this;
        }

        /// <inheritdoc/>
        public IPdfService AddInfoSection(Dictionary<string, string> data, int columns = 2)
        {
            try
            {
                _builder?.AddInfoSection(data, columns);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(AddInfoSection), ex);
            }
            return this;
        }

        /// <inheritdoc/>
        public IPdfService AddSpacing(float spacing = 10)
        {
            try
            {
                _builder?.AddSpacing(spacing);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(AddSpacing), ex);
            }
            return this;
        }

        /// <inheritdoc/>
        public IPdfService AddFooterNote(string note)
        {
            try
            {
                _builder?.AddFooterNote(note);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(AddFooterNote), ex);
            }
            return this;
        }

        /// <inheritdoc/>
        public void BuildPDF(string baseFileName, bool landscape = true, bool autoFormat = true)
        {
            try
            {
                _builder?.BuildPDF(baseFileName, landscape, autoFormat);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(BuildPDF), ex);
            }
        }

        /// <inheritdoc/>
        public byte[] GeneratePdfBytes(bool landscape = true, bool autoFormat = true)
        {
            try
            {
                if (_builder == null) return Array.Empty<byte>();
                return _builder.GeneratePdfBytes(landscape, autoFormat);
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(GeneratePdfBytes), ex);
                return Array.Empty<byte>();
            }
        }

        /// <inheritdoc/>
        public IPdfService Clear()
        {
            try
            {
                _builder?.Clear();
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(Clear), ex);
            }
            return this;
        }

        #endregion

        #region Event Passthrough

        /// <summary>
        /// Subscribe to get notified when PDF is saved successfully (full file path returned).
        /// Usage in ViewModel: _pdfService.OnPdfSaved += path => StatusMessage = $"Saved: {path}";
        /// </summary>
        public event Action<string>? OnPdfSaved
        {
            add
            {
                try { if (_builder != null) _builder.OnPdfSaved += value; }
                catch (Exception ex) { FileLogger.ApplicationLog(nameof(OnPdfSaved), ex); }
            }
            remove
            {
                try { if (_builder != null) _builder.OnPdfSaved -= value; }
                catch (Exception ex) { FileLogger.ApplicationLog(nameof(OnPdfSaved), ex); }
            }
        }

        /// <summary>
        /// Subscribe to get notified when an error occurs during PDF generation.
        /// </summary>
        public event Action<string>? OnError
        {
            add
            {
                try { if (_builder != null) _builder.OnError += value; }
                catch (Exception ex) { FileLogger.ApplicationLog(nameof(OnError), ex); }
            }
            remove
            {
                try { if (_builder != null) _builder.OnError -= value; }
                catch (Exception ex) { FileLogger.ApplicationLog(nameof(OnError), ex); }
            }
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Converts Core layer's EnumPdfColumnAlignment enum to iText7's TextAlignment.
        /// This mapping exists ONLY in PdfService — nowhere else in the codebase.
        /// </summary>
        private static Dictionary<string, TextAlignment>? MapAlignments(
            Dictionary<string, EnumPdfColumnAlignment>? source)
        {
            try
            {
                if (source is null or { Count: 0 }) return null;

                var result = new Dictionary<string, TextAlignment>(source.Count);
                foreach (var kvp in source)
                {
                    result[kvp.Key] = kvp.Value switch
                    {
                        EnumPdfColumnAlignment.Right => TextAlignment.RIGHT,
                        EnumPdfColumnAlignment.Center => TextAlignment.CENTER,
                        _ => TextAlignment.LEFT
                    };
                }
                return result;
            }
            catch (Exception ex)
            {
                FileLogger.ApplicationLog(nameof(MapAlignments), ex);
                return null;
            }
        }

        #endregion
    }
}