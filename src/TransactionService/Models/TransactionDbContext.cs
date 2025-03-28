using Microsoft.EntityFrameworkCore;

namespace TransactionService.Models
{
    public class TransactionDbContext : DbContext
    {
        public TransactionDbContext(DbContextOptions<TransactionDbContext> options) : base(options)
        {
        }

        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasIndex(t => t.IdempotencyKey).IsUnique();
                entity.HasIndex(t => t.UserId);
                entity.HasIndex(t => t.TransactionDate);
                entity.HasIndex(t => t.Status);
            });
        }
    }
}