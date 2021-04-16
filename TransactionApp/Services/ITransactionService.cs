using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using TransactionApp.Dtos;
using TransactionApp.Models;

namespace TransactionApp.Services
{
    public interface ITransactionService
    {
        bool ValidateFileExtension(IFormFile dataFile);

        bool ValidateFileSize(IFormFile dataFile);

        string[] ValidateFileContent(IEnumerable<TransactionDto> transactions, string fileExtension);

        (IEnumerable<TransactionDto> TransactionDtos, string FileExtension) ReadFileContents(IFormFile dataFile);

        string AddTransactions(IEnumerable<TransactionDto> transactions);

        List<TransactionResult> GetByCurrency(string currency);

        List<TransactionResult> GetByStatus(string status);

        List<TransactionResult> GetByDates(DateTime fromDate, DateTime toDate);

    }
}
