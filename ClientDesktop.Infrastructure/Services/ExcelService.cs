using ClientDesktop.Core.Enums;
using ClientDesktop.Core.Interfaces;
using ClientDesktop.Infrastructure.Helpers;
using ClientDesktop.Infrastructure.Logger;
using System;
using System.Collections.Generic;
using System.Data;

namespace ClientDesktop.Infrastructure.Services
{
    /// <summary>
    /// Implements IExcelService by delegating to ExcelBuilder utility.
    /// This is the ONLY layer that knows about ExcelBuilder and ClosedXML.
    /// Registered as Transient in DI — each screen gets a fresh instance.
    ///
    /// Same pattern as PdfService → PDFBuilder.
    /// </summary>
    public class ExcelService : IExcelService
    {
        #region Fields

        private readonly ExcelBuilder _builder;

        #endregion

        #region Constructor

        public ExcelService()
        {
            _builder = new ExcelBuilder();

            _builder.OnError += msg =>
                FileLogger.ApplicationLog(nameof(ExcelService), $"Excel generation error: {msg}");
        }

        #endregion

        #region IExcelService Implementation

        /// <inheritdoc/>
        public IExcelService AddSheet(
            DataTable dataTable,
            string title,
            string sheetName = "Sheet1",
            Dictionary<string, ExcelColumnAlignment>? columnAlignments = null)
        {
            _builder.AddSheet(dataTable, title, sheetName, columnAlignments);
            return this;
        }

        /// <inheritdoc/>
        public void SaveExcel(string baseFileName)
        {
            _builder.SaveExcel(baseFileName);
        }

        /// <inheritdoc/>
        public byte[] GenerateExcelBytes()
        {
            return _builder.GenerateExcelBytes();
        }

        /// <inheritdoc/>
        public IExcelService Clear()
        {
            _builder.Clear();
            return this;
        }

        #endregion

        #region Event Passthrough

        /// <summary>
        /// Subscribe to get notified when Excel is saved (full file path returned).
        /// Usage in ViewModel: _excelService.OnExcelSaved += path => StatusMessage = $"Saved: {path}";
        /// </summary>
        public event Action<string>? OnExcelSaved
        {
            add => _builder.OnExcelSaved += value;
            remove => _builder.OnExcelSaved -= value;
        }

        /// <summary>
        /// Subscribe to get notified when an error occurs during Excel generation.
        /// </summary>
        public event Action<string>? OnError
        {
            add => _builder.OnError += value;
            remove => _builder.OnError -= value;
        }

        #endregion
    }
}