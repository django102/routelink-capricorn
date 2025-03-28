namespace FraudDetectionService.Models
{
    public class FraudDetectionResult
    {
        public bool IsFraudulent { get; set; }
        public decimal FraudScore { get; set; }
        public string Reason { get; set; }
    }

    public class TransactionEvaluationRequest
    {
        public string UserId { get; set; }
        public string TransactionType { get; set; }
        public decimal Amount { get; set; }
        public string Recipient { get; set; }
        public string Reference { get; set; }
    }
}