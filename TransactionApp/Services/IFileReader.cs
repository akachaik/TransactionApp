using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using TransactionApp.Dtos;

namespace TransactionApp.Services
{
    public interface IFileReader
    {
        IEnumerable<TransactionDto> Read(IFormFile dataFile);
    }
}
