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

        string[] ValidateFileContent(IFormFile dataFile);

        List<TransactionDto> ReadFileContents(IFormFile dataFile);

        string AddTransactions(List<TransactionDto> transactions);

        List<TransactionResult> GetByCurrency(string currency);

        List<TransactionResult> GetByStatus(string status);

        List<TransactionResult> GetByDates(DateTime fromDate, DateTime toDate);

    }
}
