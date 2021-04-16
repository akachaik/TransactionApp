using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using TransactionApp.Services;

namespace TransactionApp.Controllers
{
    public class TransactionController : Controller
    {
        private readonly ITransactionService _transactionService;
        public TransactionController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        [HttpPost]
        public IActionResult Import(IFormFile dataFile)
        {
            var isValidExtensions = _transactionService.ValidateFileExtension(dataFile);
            if (!isValidExtensions)
            {
                return BadRequest("Unknown format");
            }

            var isValidFileSize = _transactionService.ValidateFileSize(dataFile);

            if (!isValidFileSize)
            {
                return BadRequest("File Size is greather than 1 MB");
            }

            var result = _transactionService.ReadFileContents(dataFile);

            var validFileContentResult = _transactionService.ValidateFileContent(result.TransactionDtos, result.FileExtension);
            
            if (validFileContentResult.Length > 0)
            {
                return BadRequest(validFileContentResult);
            }

            // Save to database
            var validateMessage = _transactionService.AddTransactions(result.TransactionDtos);

            if (!string.IsNullOrEmpty(validateMessage))
            {
                return BadRequest(validateMessage);
            }

            return Ok();
        }

        [HttpGet]
        public IActionResult GetByCurrency(string currency)
        {
            var result = _transactionService.GetByCurrency(currency);

            return Ok(result);
        }

        [HttpGet]
        public IActionResult GetByStatus(string status)
        {
            var result = _transactionService.GetByStatus(status);

            return Ok(result);
        }

        [HttpGet]
        public IActionResult GetByDates(DateTime fromDate, DateTime toDate)
        {

            if (fromDate > toDate)
            {
                return BadRequest("FromDate is greater than toDate");
            }

            var result = _transactionService.GetByDates(fromDate, toDate);

            return Ok(result);
        }

    }
}
