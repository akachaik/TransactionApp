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

        public TransactionService(ILogger<TransactionService> logger, ApplicationDbContext applicationDbContext)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
        }

        public List<TransactionDto> ReadFileContents(IFormFile dataFile)
        {
            var result = new List<TransactionDto>();
            var fileExtension = Path.GetExtension(dataFile.FileName);

            if (fileExtension == ".csv")
            {
                result = ReadCsvContent(dataFile);
            }
            else if (fileExtension == ".xml")
            {
                result = ReadXmlContent(dataFile);
            }

            return result;
        }

        public string[] ValidateFileContent(IFormFile dataFile)
        {
            var result = new List<string>();
            var fileExtension = Path.GetExtension(dataFile.FileName);

            if (fileExtension == ".csv")
            {
                result = ValidateCsvContent(dataFile);
            }
            else if (fileExtension == ".xml")
            {
                result = ValidateXmlContent(dataFile);
            }

            return result.ToArray();
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

        public string AddTransactions(List<TransactionDto> transactions)
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

        private List<TransactionDto> ReadXmlContent(IFormFile dataFile)
        {
            throw new NotImplementedException();
        }

        private List<TransactionDto> ReadCsvContent(IFormFile dataFile)
        {
            var records = new List<TransactionDto>();
            using (var ms = new MemoryStream())
            {
                dataFile.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(ms))
                {
                    var conf = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        BadDataFound = null,
                        HasHeaderRecord = false,
                        TrimOptions = TrimOptions.Trim
                    };
                    var csvReader = new CsvReader(reader, conf);
                    var options = new TypeConverterOptions { Formats = new[] { "dd/MM/yyyy hh:mm:ss" } };
                    csvReader.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                    records = csvReader.GetRecords<TransactionDto>().ToList();
                }
            }

            return records;
        }

        private List<string> ValidateXmlContent(IFormFile dataFile)
        {
            throw new NotImplementedException();
        }

        private List<string> ValidateCsvContent(IFormFile dataFile)
        {
            var result = new List<string>();

            IEnumerable<TransactionDto> records = null;
            using (var ms = new MemoryStream())
            {
                dataFile.CopyTo(ms);
                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(ms))
                {
                    var conf = new CsvConfiguration(CultureInfo.InvariantCulture)
                    {
                        BadDataFound = null,
                        HasHeaderRecord = false,
                        TrimOptions = TrimOptions.Trim
                    };
                    var csvReader = new CsvReader(reader, conf);
                    var options = new TypeConverterOptions { Formats = new[] { "dd/MM/yyyy hh:mm:ss" } };
                    csvReader.Context.TypeConverterOptionsCache.AddOptions<DateTime?>(options);
                    records = csvReader.GetRecords<TransactionDto>().ToList();
                    result = ValidateRecords(records);
                }
            }

            return result;
        }

        private List<string> ValidateRecords(IEnumerable<TransactionDto> records)
        {
            var result = new List<string>();
            var allowedStatus = new[] { "Approved", "Failed", "Finished " };
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

                if (!allowedStatus.Contains(record.Status))
                {
                    recordResult += "Status is invalid";
                }

                if (!string.IsNullOrEmpty(recordResult))
                {
                    result.Add(recordResult);
                }
            }

            return result;
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
