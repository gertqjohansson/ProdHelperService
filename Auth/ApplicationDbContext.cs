using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ProdHelperService.Auth;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole, string>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Equipment> Equipment => Set<Equipment>();
    public DbSet<EquipmentTranslation> EquipmentTranslations => Set<EquipmentTranslation>();
    public DbSet<EquipmentCategory> EquipmentCategories => Set<EquipmentCategory>();
    public DbSet<EquipmentCategoryTranslation> EquipmentCategoryTranslations => Set<EquipmentCategoryTranslation>();
    public DbSet<Language> Languages => Set<Language>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<RefreshToken>(entity =>
        {
            entity.HasIndex(t => t.TokenHash).IsUnique();
            entity.HasIndex(t => t.UserId);
        });

        // Equipment, EquipmentTranslation and Language are pre-existing tables not
        // owned by our EF migrations - ExcludeFromMigrations keeps `dotnet ef
        // migrations add` from ever trying to create/alter/drop them.
        builder.Entity<Equipment>(entity =>
        {
            entity.ToTable("Equipment", t => t.ExcludeFromMigrations());
        });

        builder.Entity<EquipmentTranslation>(entity =>
        {
            entity.ToTable("EquipmentTranslation", t => t.ExcludeFromMigrations());
            entity.HasKey(t => new { t.EquipmentId, t.LanguageIsoCode });
        });

        builder.Entity<Language>(entity =>
        {
            entity.ToTable("Language", t => t.ExcludeFromMigrations());
            entity.HasKey(l => l.IsoCode);
        });

        builder.Entity<EquipmentCategory>(entity =>
        {
            entity.ToTable("EquipmentCategory", t => t.ExcludeFromMigrations());
        });

        builder.Entity<EquipmentCategoryTranslation>(entity =>
        {
            entity.ToTable("EquipmentCategoryTranslation", t => t.ExcludeFromMigrations());
            entity.HasKey(t => new { t.EquipmentCategoryId, t.LanguageIsoCode });
        });
    }
}
