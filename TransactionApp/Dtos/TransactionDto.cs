using System;

namespace TransactionApp.Dtos
{
    public class TransactionDto
    {
        public string Id { get; set; }
        public decimal? Amount { get; set; }
        public string CurrencyCode { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string Status { get; set; }
    }
}
