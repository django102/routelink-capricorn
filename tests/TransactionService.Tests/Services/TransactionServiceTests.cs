using Moq;
using TransactionService.Interfaces;
using TransactionService.Models;
using TransactionService.Services;

namespace TransactionService.Tests.Services
{
    public class TransactionServiceTests
    {
        private readonly Mock<ITransactionRepository> _mockRepo;
        private readonly Mock<ICapricornService> _mockCapricorn;
        private readonly Mock<ICacheService> _mockCache;
        private readonly Services.TransactionService _service;

        public TransactionServiceTests()
        {
            _mockRepo = new Mock<ITransactionRepository>();
            _mockCapricorn = new Mock<ICapricornService>();
            _mockCache = new Mock<ICacheService>();
            
            _service = new Services.TransactionService(
                _mockRepo.Object,
                _mockCapricorn.Object,
                _mockCache.Object,
                null, // IMapper would need to be mocked or configured
                Mock.Of<ILogger<Services.TransactionService>>());
        }

        [Fact]
        public async Task PurchaseAirtime_ShouldPreventDuplicateTransactions()
        {
            // Arrange
            var request = new AirtimePurchaseRequest 
            { 
                PhoneNumber = "1234567890", 
                Amount = 100,
                IdempotencyKey = "test-key"
            };
            
            _mockRepo.Setup(x => x.IsDuplicateTransaction("test-key"))
                .ReturnsAsync(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _service.PurchaseAirtime(request, "user1"));
        }

        [Fact]
        public async Task PurchaseAirtime_ShouldCreateTransactionOnSuccess()
        {
            // Arrange
            var request = new AirtimePurchaseRequest 
            { 
                PhoneNumber = "1234567890", 
                Amount = 100 
            };
            
            var expectedTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                Status = "Completed"
            };
            
            _mockCapricorn.Setup(x => x.PurchaseAirtime(request))
                .ReturnsAsync(expectedTransaction);
                
            _mockRepo.Setup(x => x.AddTransaction(It.IsAny<Transaction>()))
                .ReturnsAsync((Transaction t) => t);

            // Act
            var result = await _service.PurchaseAirtime(request, "user1");

            // Assert
            Assert.Equal("Completed", result.Status);
            Assert.Equal("user1", result.UserId);
            _mockRepo.Verify(x => x.AddTransaction(It.IsAny<Transaction>()), Times.Once);
            _mockRepo.Verify(x => x.UpdateTransaction(It.IsAny<Transaction>()), Times.Once);
        }
    }
}