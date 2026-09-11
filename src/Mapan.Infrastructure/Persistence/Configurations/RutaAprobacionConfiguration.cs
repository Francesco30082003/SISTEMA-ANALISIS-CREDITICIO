using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class RutaAprobacionConfiguration : IEntityTypeConfiguration<RutaAprobacion>
{
    public void Configure(EntityTypeBuilder<RutaAprobacion> builder)
    {
        builder.ToTable("ruta_aprobacion", "flujo", table =>
        {
        });
        builder.Property(e => e.RutaAprobacionId).HasColumnName("ruta_aprobacion_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ProductoCreditoId).HasColumnName("producto_credito_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.Codigo).HasColumnName("codigo").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasColumnType("character varying(200)").IsRequired(true).HasMaxLength(200);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.Prioridad).HasColumnName("prioridad").HasColumnType("integer").IsRequired(true).HasDefaultValueSql("100");
        builder.Property(e => e.EsPredeterminada).HasColumnName("es_predeterminada").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("false");
        builder.Property(e => e.Activa).HasColumnName("activa").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).HasPrincipalKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_ruta_empresa");
        builder.HasOne<ProductoCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.ProductoCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.ProductoCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_ruta_producto");
        builder.HasKey(e => e.RutaAprobacionId).HasName("ruta_aprobacion_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.Codigo }).IsUnique().HasDatabaseName("uq_ruta_empresa_codigo");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.RutaAprobacionId }).HasName("uq_ruta_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.ProductoCreditoId, e.Activa }).HasDatabaseName("ix_ruta_empresa_producto");
    }
}
