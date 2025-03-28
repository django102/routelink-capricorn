using FraudDetectionService.Models;
using FraudDetectionService.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FraudDetectionService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/fraud-detection")]
    public class FraudDetectionController : ControllerBase
    {
        private readonly IFraudDetectionService _fraudDetectionService;

        public FraudDetectionController(IFraudDetectionService fraudDetectionService)
        {
            _fraudDetectionService = fraudDetectionService;
        }

        [HttpPost("evaluate")]
        public async Task<IActionResult> EvaluateTransaction([FromBody] TransactionEvaluationRequest request)
        {
            var result = await _fraudDetectionService.EvaluateTransaction(request);
            return Ok(result);
        }
    }
}