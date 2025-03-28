using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TransactionService.Interfaces;
using TransactionService.Models;

namespace TransactionService.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ILogger<TransactionController> _logger;

        public TransactionController(
            ITransactionService transactionService,
            ILogger<TransactionController> logger)
        {
            _transactionService = transactionService;
            _logger = logger;
        }

        [HttpPost("airtime/purchase")]
        public async Task<IActionResult> PurchaseAirtime([FromBody] AirtimePurchaseRequest request)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var transaction = await _transactionService.PurchaseAirtime(request, userId);
                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing airtime purchase");
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("data/purchase")]
        public async Task<IActionResult> PurchaseData([FromBody] DataPurchaseRequest request)
        {
            // Similar to PurchaseAirtime
        }

        [HttpPost("tv/subscribe")]
        public async Task<IActionResult> SubscribeTv([FromBody] TvSubscriptionRequest request)
        {
            // Similar to PurchaseAirtime
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransaction(Guid id)
        {
            try
            {
                var userId = User.FindFirst("sub")?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var transaction = await _transactionService.GetTransactionById(id, userId);
                return Ok(transaction);
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving transaction");
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}