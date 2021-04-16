using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using TransactionApp.Dtos;

namespace TransactionApp.Services
{
    public class XmlFileReader : IFileReader
    {
        public IEnumerable<TransactionDto> Read(IFormFile dataFile)
        {
            using (var memoryStream = new MemoryStream())
            {
                dataFile.CopyTo(memoryStream);
                memoryStream.Seek(0, SeekOrigin.Begin);
                var xElement = XElement.Load(memoryStream);

                var result = from transaction in xElement.Descendants("Transaction")
                             select new TransactionDto
                             {
                                 Id = (string)transaction.Attribute("id"),
                                 Amount = decimal.TryParse(transaction.Descendants("PaymentDetails").FirstOrDefault()?.Descendants("Amount").FirstOrDefault()?.Value, out var tempDate1) ? tempDate1 : default(decimal?),
                                 CurrencyCode = transaction.Descendants("PaymentDetails").FirstOrDefault()?.Descendants("CurrencyCode").FirstOrDefault()?.Value,
                                 Status = transaction.Descendants("Status").FirstOrDefault()?.Value,
                                 TransactionDate = DateTime.TryParseExact(transaction.Descendants("TransactionDate").FirstOrDefault()?.Value, "yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tempDate2) ? tempDate2 : default(DateTime?)
                             };

                return result;
            }
        }
    }
}
