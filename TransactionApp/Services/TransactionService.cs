using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TransactionApp.Dtos;
using TransactionApp.Models;

namespace TransactionApp.Services
{

    public class TransactionService : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly IFileReaderResolver _fileReaderResolver;

        public TransactionService(ILogger<TransactionService> logger, ApplicationDbContext applicationDbContext, IFileReaderResolver fileReaderResolver)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _fileReaderResolver = fileReaderResolver;
        }

        public (IEnumerable<TransactionDto> TransactionDtos, string FileExtension) ReadFileContents(IFormFile dataFile)
        {
            var fileExtension = Path.GetExtension(dataFile.FileName);
            var fileReader = _fileReaderResolver.Resolve(fileExtension);

            return (fileReader.Read(dataFile), fileExtension);
        }

        public string[] ValidateFileContent(IEnumerable<TransactionDto> records, string fileExtension)
        {
            var result = new List<string>();
            var allowedStatuses = GetAllowedStatusesByFileExtension(fileExtension);
            foreach (var record in records)
            {
                var recordResult = string.Empty;
                if (string.IsNullOrEmpty(record.Id))
                {
                    recordResult += "Id is empty";
                }

                if (!string.IsNullOrEmpty(record.Id) && record.Id.Length > 50)
                {
                    recordResult += "Id is greater than 50 characters";
                }

                if (!record.TransactionDate.HasValue)
                {
                    recordResult += "TransactionDate is empty";
                }

                if (!record.Amount.HasValue)
                {
                    recordResult += "Amount is empty";
                }

                if (string.IsNullOrEmpty(record.CurrencyCode))
                {
                    recordResult += "CurrencyCode is empty";
                }

                if (!ValidateCurrencyCode(record.CurrencyCode))
                {
                    recordResult += "CurrencyCode is invalid";
                }

                if (string.IsNullOrEmpty(record.Status))
                {
                    recordResult += "Status is empty";
                }

                if (!allowedStatuses.Contains(record.Status))
                {
                    recordResult += "Status is invalid";
                }

                if (!string.IsNullOrEmpty(recordResult))
                {
                    result.Add(recordResult);
                }
            }

            return result.ToArray();
        }

        private string[] GetAllowedStatusesByFileExtension(string fileExtension)
        {
            var csvAllowedStatuses = new[] { "Approved", "Failed", "Finished" };
            var xmlAllowedStatuses = new[] { "Approved", "Rejected", "Done" };
            switch (fileExtension)
            {
                case ".csv":
                    return csvAllowedStatuses;
                case ".xml":
                    return xmlAllowedStatuses;
                default:
                    throw new NotSupportedException($"{fileExtension} is not supported");
            }

        }

        public bool ValidateFileExtension(IFormFile dataFile)
        {
            var allowedExtensions = new[] { ".csv", ".xml" };
            var extension = Path.GetExtension(dataFile.FileName);

            if (!allowedExtensions.Contains(extension))
            {
                return false;
            }

            return true;
        }

        public bool ValidateFileSize(IFormFile dataFile)
        {
            var fileSizeLimit = 1 * 1024 * 1024; // 1 MB
            if (dataFile.Length > fileSizeLimit)
            {
                return false;
            }

            return true;
        }

        public string AddTransactions(IEnumerable<TransactionDto> transactions)
        {
            var result = string.Empty;

            try
            {
                foreach (var item in transactions)
                {
                    _applicationDbContext.Transactions.Add(new Transaction
                    {
                        Id = item.Id,
                        Amount = item.Amount.Value,
                        CurrencyCode = item.CurrencyCode,
                        Status = item.Status,
                        TransactionDate = item.TransactionDate.Value
                    });
                }

                _applicationDbContext.SaveChanges();
                return result;

            }
            catch (Exception exception)
            {

                _logger.LogWarning(exception, "Unable to save transaction");
                result = "Unable to save transaction";

            }

            return result;
        }

        public List<TransactionResult> GetByCurrency(string currency)
        {
            var transactions = _applicationDbContext.Transactions
                            .Where(t => t.CurrencyCode == currency)
                            .ToList();
            var result = transactions.Select(a => new TransactionResult
            {
                Id = a.Id,
                Status = MapStatus(a.Status),
                Payment = $"{a.Amount} {a.CurrencyCode}"
            })
                .ToList();

            return result;
        }

        public List<TransactionResult> GetByStatus(string status)
        {
            var transactions = _applicationDbContext.Transactions
                            .Where(t => t.Status == status)
                            .ToList();
            var result = transactions.Select(a => new TransactionResult
            {
                Id = a.Id,
                Status = MapStatus(a.Status),
                Payment = $"{a.Amount} {a.CurrencyCode}"
            })
                .ToList();

            return result;
        }

        public List<TransactionResult> GetByDates(DateTime fromDate, DateTime toDate)
        {
            var fromDatreTimeSpan = new TimeSpan(0, 0, 0, 0, 0);
            fromDate = fromDate.Date + fromDatreTimeSpan;

            var toDateTimeSpan = new TimeSpan(0, 23, 59, 59, 999);
            toDate = toDate.Date + toDateTimeSpan;

            var transactions = _applicationDbContext.Transactions
                .Where(t => t.TransactionDate >= fromDate && t.TransactionDate <= toDate)
                .ToList();
            var result = transactions.Select(a => new TransactionResult
            {
                Id = a.Id,
                Status = MapStatus(a.Status),
                Payment = $"{a.Amount} {a.CurrencyCode}"
            })
                .ToList();

            return result;

        }

        private string MapStatus(string status)
        {
            switch (status)
            {
                case "Approved":
                    return "A";
                case "Failed":
                case "Rejected":
                    return "R";
                case "Finished":
                case "Done":
                    return "D";
                default:
                    return string.Empty;
            }
        }

        private bool ValidateCurrencyCode(string currencyCode)
        {
            var regionalInfo = CultureInfo
               .GetCultures(CultureTypes.AllCultures)
               .Where(c => !c.IsNeutralCulture)
               .Select(culture =>
               {
                   try
                   {
                       return new RegionInfo(culture.Name);
                   }
                   catch
                   {
                       return null;
                   }
               })
               .Where(ri => ri != null && ri.ISOCurrencySymbol == currencyCode)
               .FirstOrDefault();

            return regionalInfo != null;

        }
    }
}
