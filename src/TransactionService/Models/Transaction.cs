using System.ComponentModel.DataAnnotations;

namespace TransactionService.Models
{
    public class Transaction
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();
        
        [Required]
        public string UserId { get; set; }
        
        [Required]
        public string TransactionType { get; set; } // "Airtime", "Data", "TV"
        
        [Required]
        public string Provider { get; set; } // "Capricorn" or others
        
        [Required]
        public string Recipient { get; set; }
        
        [Required]
        public decimal Amount { get; set; }
        
        public string Reference { get; set; }
        
        [Required]
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        
        [Required]
        public string Status { get; set; } // "Pending", "Completed", "Failed"
        
        public string FailureReason { get; set; }
        
        // For idempotency
        public string IdempotencyKey { get; set; }
    }

    public class AirtimePurchaseRequest
    {
        [Required]
        public string PhoneNumber { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
        
        [Required]
        public string Provider { get; set; }
        
        public string IdempotencyKey { get; set; }
    }

    public class DataPurchaseRequest
    {
        [Required]
        public string PhoneNumber { get; set; }
        
        [Required]
        public string DataPlanId { get; set; }
        
        [Required]
        public string Provider { get; set; }
        
        public string IdempotencyKey { get; set; }
    }

    public class TvSubscriptionRequest
    {
        [Required]
        public string SmartCardNumber { get; set; }
        
        [Required]
        public string SubscriptionPlanId { get; set; }
        
        [Required]
        public string Provider { get; set; }
        
        public string IdempotencyKey { get; set; }
    }
}