using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class PermisoConfiguration : IEntityTypeConfiguration<Permiso>
{
    public void Configure(EntityTypeBuilder<Permiso> builder)
    {
        builder.ToTable("permiso", "seguridad", table =>
        {
        });
        builder.Property(e => e.PermisoId).HasColumnName("permiso_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Codigo).HasColumnName("codigo").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasColumnType("character varying(150)").IsRequired(true).HasMaxLength(150);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("character varying(300)").IsRequired(false).HasMaxLength(300);
        builder.HasKey(e => e.PermisoId).HasName("permiso_pkey");
        builder.HasIndex(e => e.Codigo).IsUnique().HasDatabaseName("uq_permiso_codigo");
    }
}
