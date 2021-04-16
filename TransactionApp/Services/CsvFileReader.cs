using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TransactionApp.Dtos;

namespace TransactionApp.Services
{
    public class CsvFileReader : IFileReader
    {
        public IEnumerable<TransactionDto> Read(IFormFile dataFile)
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
    }
}
