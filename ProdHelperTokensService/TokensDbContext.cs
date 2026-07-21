using Microsoft.EntityFrameworkCore;

namespace ProdHelperTokensService;

public class TokensDbContext(DbContextOptions<TokensDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<TokenUsageEntry> TokenUsageEntries => Set<TokenUsageEntry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Customer>(entity =>
        {
            entity.Property(c => c.CompanyName).HasMaxLength(200);
            entity.Property(c => c.ApiKey).HasMaxLength(100);
            entity.HasIndex(c => c.ApiKey).IsUnique();
        });

        builder.Entity<TokenUsageEntry>(entity =>
        {
            entity.HasIndex(e => e.CustomerId);
        });
    }
}
