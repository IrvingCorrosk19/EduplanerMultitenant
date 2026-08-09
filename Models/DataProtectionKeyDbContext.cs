using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SchoolManager.Models
{
    /// <summary>
    /// DbContext dedicado exclusivamente al almacenamiento persistente de Data Protection Keys.
    /// Contexto separado del SchoolDbContext multi-tenant para evitar:
    ///  - Conflictos con Global Query Filters de tenant
    ///  - Errores de TenantProvider cuando no hay HttpContext (arranque de la app)
    ///  - Contaminación del modelo principal de EF Core
    /// </summary>
    public class DataProtectionKeyDbContext : DbContext, IDataProtectionKeyContext
    {
        public DataProtectionKeyDbContext(DbContextOptions<DataProtectionKeyDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Tabla donde ASP.NET Core almacena las claves de cifrado de cookies y antiforgery tokens.
        /// Persiste entre reinicios del contenedor en Render.com (elimina la necesidad de [IgnoreAntiforgeryToken]).
        /// </summary>
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DataProtectionKey>(entity =>
            {
                entity.ToTable("data_protection_keys");
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id"); // PostgreSQL stores column as lowercase; Npgsql quotes identifiers so must map explicitly
                entity.Property(e => e.FriendlyName).HasColumnName("friendly_name");
                entity.Property(e => e.Xml).HasColumnName("xml").HasColumnType("text");
            });
        }
    }
}
