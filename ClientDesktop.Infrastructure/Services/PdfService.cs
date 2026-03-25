using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using iText.Layout.Properties;
using System;
using System.Collections.Generic;
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

        #region Constructor

        public PdfService()
        {
            _builder = new PDFBuilder();

            _builder.OnError += msg =>
                FileLogger.ApplicationLog(nameof(PdfService), $"PDF generation error: {msg}");
        }

        #endregion

        #region IPdfService Implementation

        /// <inheritdoc/>
        public IPdfService AddTitle(string title, int fontSize = 18, bool centerAlign = true)
        {
            _builder.AddTitle(title, fontSize, centerAlign);
            return this;
        }

        /// <inheritdoc/>
        public IPdfService AddSubTitle(string subTitle, int fontSize = 13, bool centerAlign = false)
        {
            _builder.AddSubTitle(subTitle, fontSize, centerAlign);
            return this;
        }

        /// <inheritdoc/>
        public IPdfService AddGrid(
            DataTable dataTable,
            string? gridTitle = null,
            Dictionary<string, string>? footerData = null,
            Dictionary<string, PdfColumnAlignment>? columnAlignments = null,
            bool repeatHeader = true)
        {
            // PdfService ka kaam: Core enum → iText7 enum conversion yahan hoga
            // ViewModel ko iText7 ka koi pata nahi — woh sirf PdfColumnAlignment.Right likhega
            var iTextAlignments = MapAlignments(columnAlignments);
            _builder.AddGrid(dataTable, gridTitle, footerData, iTextAlignments);
            return this;
        }

        /// <inheritdoc/>
        public IPdfService AddInfoSection(Dictionary<string, string> data, int columns = 2)
        {
            _builder.AddInfoSection(data, columns);
            return this;
        }

        /// <inheritdoc/>
        public IPdfService AddSpacing(float spacing = 10)
        {
            _builder.AddSpacing(spacing);
            return this;
        }

        /// <inheritdoc/>
        public IPdfService AddFooterNote(string note)
        {
            _builder.AddFooterNote(note);
            return this;
        }

        /// <inheritdoc/>
        public void BuildPDF(string baseFileName, bool landscape = true, bool autoFormat = true)
        {
            _builder.BuildPDF(baseFileName, landscape, autoFormat);
        }

        /// <inheritdoc/>
        public byte[] GeneratePdfBytes(bool landscape = true, bool autoFormat = true)
        {
            return _builder.GeneratePdfBytes(landscape, autoFormat);
        }

        /// <inheritdoc/>
        public IPdfService Clear()
        {
            _builder.Clear();
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
            add => _builder.OnPdfSaved += value;
            remove => _builder.OnPdfSaved -= value;
        }

        /// <summary>
        /// Subscribe to get notified when an error occurs during PDF generation.
        /// </summary>
        public event Action<string>? OnError
        {
            add => _builder.OnError += value;
            remove => _builder.OnError -= value;
        }

        #endregion

        #region Private Helpers

        /// <summary>
        /// Converts Core layer's PdfColumnAlignment enum to iText7's TextAlignment.
        /// This mapping exists ONLY in PdfService — nowhere else in the codebase.
        /// </summary>
        private static Dictionary<string, TextAlignment>? MapAlignments(
            Dictionary<string, PdfColumnAlignment>? source)
        {
            if (source is null or { Count: 0 }) return null;

            var result = new Dictionary<string, TextAlignment>(source.Count);
            foreach (var kvp in source)
            {
                result[kvp.Key] = kvp.Value switch
                {
                    PdfColumnAlignment.Right => TextAlignment.RIGHT,
                    PdfColumnAlignment.Center => TextAlignment.CENTER,
                    _ => TextAlignment.LEFT
                };
            }
            return result;
        }

        #endregion
    }
}