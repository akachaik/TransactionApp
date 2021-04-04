using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

            var validFileContentResult = _transactionService.ValidateFileContent(dataFile);
            if (validFileContentResult.Length > 0)
            {
                return BadRequest(validFileContentResult);
            }

            // Save to database
            var fileContents = _transactionService.ReadFileContents(dataFile);

            var validateMessage = _transactionService.AddTransactions(fileContents);

            if (!string.IsNullOrEmpty(validateMessage))
            {
                return BadRequest(validateMessage);
            }

            return Ok();
        }
    }
}
