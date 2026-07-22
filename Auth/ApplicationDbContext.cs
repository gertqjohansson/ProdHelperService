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
    public DbSet<EquipmentUpload> EquipmentUploads => Set<EquipmentUpload>();
    public DbSet<EquipmentLink> EquipmentLinks => Set<EquipmentLink>();
    public DbSet<EquipmentLog> EquipmentLogs => Set<EquipmentLog>();
    public DbSet<ShiftScheduleVersion> ShiftScheduleVersions => Set<ShiftScheduleVersion>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<ActionLog> ActionLogs => Set<ActionLog>();

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

        // EquipmentUploads is the first table this app fully owns for the Equipment domain - no
        // ExcludeFromMigrations. No navigation/FK to Equipment (that table is excluded from
        // migrations above; a migrated FK against an unmigrated table is untested in this
        // codebase), so "equipment must exist" is enforced in EquipmentUploadController instead.
        builder.Entity<EquipmentUpload>(entity =>
        {
            entity.HasIndex(x => new { x.EquipmentId, x.FileName }).IsUnique();
        });

        // EquipmentLinks is fully EF-owned too (no ExcludeFromMigrations). No uniqueness
        // constraint - unlike uploads there's no "overwrite" concept, so multiple links can share
        // the same path. No navigation/FK to Equipment, same reasoning as EquipmentUpload above.
        builder.Entity<EquipmentLink>(entity =>
        {
            entity.HasIndex(x => x.EquipmentId);
        });

        // EquipmentLogs is fully EF-owned too. Same reasoning as EquipmentLink for the lack of a
        // navigation/FK to Equipment and the non-unique EquipmentId index.
        builder.Entity<EquipmentLog>(entity =>
        {
            entity.HasIndex(x => x.EquipmentId);
        });

        // ShiftScheduleVersion is a pre-existing table the user created manually (not owned by our
        // EF migrations) - same ExcludeFromMigrations treatment as Equipment/Language/etc. above.
        // No navigation/FK to Equipment, same reasoning as EquipmentLog/EquipmentLink.
        builder.Entity<ShiftScheduleVersion>(entity =>
        {
            entity.ToTable("ShiftScheduleVersion", t => t.ExcludeFromMigrations());
        });

        // ErrorLog is another pre-existing table, same ExcludeFromMigrations treatment as
        // Equipment/Language/etc. above.
        builder.Entity<ErrorLog>(entity =>
        {
            entity.ToTable("ErrorLog", t => t.ExcludeFromMigrations());
            entity.Property(e => e.Section).HasMaxLength(200);
        });

        // ActionLog is another pre-existing table, same ExcludeFromMigrations treatment.
        builder.Entity<ActionLog>(entity =>
        {
            entity.ToTable("ActionLog", t => t.ExcludeFromMigrations());
            entity.Property(e => e.ActionType).HasMaxLength(20);
            entity.Property(e => e.MadeByUser).HasMaxLength(100);
            entity.Property(e => e.Section).HasMaxLength(100);
        });
    }
}
