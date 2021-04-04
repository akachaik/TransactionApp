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
using System.Threading.Tasks;
using TransactionApp.Dtos;

namespace TransactionApp.Services
{
    public interface ITransactionService
    {
        bool ValidateFileExtension(IFormFile dataFile);

        bool ValidateFileSize(IFormFile dataFile);

        string[] ValidateFileContent(IFormFile dataFile);

        List<TransactionDto> ReadFileContents(IFormFile dataFile);

    }

    public class TransactionService : ITransactionService
    {
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(ILogger<TransactionService> logger)
        {
            _logger = logger;
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
