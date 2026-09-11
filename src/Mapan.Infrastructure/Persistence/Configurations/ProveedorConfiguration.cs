using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class ProveedorConfiguration : IEntityTypeConfiguration<Proveedor>
{
    public void Configure(EntityTypeBuilder<Proveedor> builder)
    {
        builder.ToTable("proveedor", "integracion", table =>
        {
        });
        builder.Property(e => e.ProveedorId).HasColumnName("proveedor_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Codigo).HasColumnName("codigo").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasColumnType("character varying(200)").IsRequired(true).HasMaxLength(200);
        builder.Property(e => e.Tipo).HasColumnName("tipo").HasColumnType("character varying(60)").IsRequired(true).HasMaxLength(60);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.Activo).HasColumnName("activo").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasIndex(e => e.Codigo).IsUnique().HasDatabaseName("proveedor_codigo_key");
        builder.HasKey(e => e.ProveedorId).HasName("proveedor_pkey");
    }
}
