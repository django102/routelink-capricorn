using Microsoft.EntityFrameworkCore;
using TransactionService.Interfaces;
using TransactionService.Models;

namespace TransactionService.Repositories
{
    public class TransactionRepository : ITransactionRepository
    {
        private readonly TransactionDbContext _context;

        public TransactionRepository(TransactionDbContext context)
        {
            _context = context;
        }

        public async Task<Transaction> AddTransaction(Transaction transaction)
        {
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();
            return transaction;
        }

        public async Task<Transaction> GetTransactionById(Guid id)
        {
            return await _context.Transactions.FindAsync(id);
        }

        public async Task<Transaction> GetTransactionByIdempotencyKey(string idempotencyKey)
        {
            return await _context.Transactions
                .FirstOrDefaultAsync(t => t.IdempotencyKey == idempotencyKey);
        }

        public async Task<bool> IsDuplicateTransaction(string idempotencyKey)
        {
            if (string.IsNullOrEmpty(idempotencyKey))
                return false;

            return await _context.Transactions
                .AnyAsync(t => t.IdempotencyKey == idempotencyKey);
        }

        public async Task UpdateTransaction(Transaction transaction)
        {
            _context.Transactions.Update(transaction);
            await _context.SaveChangesAsync();
        }
    }
}