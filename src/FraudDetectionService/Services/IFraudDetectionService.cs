using FraudDetectionService.Models;

namespace FraudDetectionService.Interfaces
{
    public interface IFraudDetectionService
    {
        Task<FraudDetectionResult> EvaluateTransaction(TransactionEvaluationRequest request);
    }
}

