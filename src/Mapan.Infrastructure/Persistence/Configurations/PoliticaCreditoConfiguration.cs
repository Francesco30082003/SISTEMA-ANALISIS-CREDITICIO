using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class PoliticaCreditoConfiguration : IEntityTypeConfiguration<PoliticaCredito>
{
    public void Configure(EntityTypeBuilder<PoliticaCredito> builder)
    {
        builder.ToTable("politica_credito", "politica", table =>
        {
            table.HasCheckConstraint("ck_politica_estado", "((estado)::text = ANY ((ARRAY['ACTIVA'::character varying, 'INACTIVA'::character varying])::text[]))");
        });
        builder.Property(e => e.PoliticaCreditoId).HasColumnName("politica_credito_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ProductoCreditoId).HasColumnName("producto_credito_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.Codigo).HasColumnName("codigo").HasColumnType("character varying(60)").IsRequired(true).HasMaxLength(60);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasColumnType("character varying(180)").IsRequired(true).HasMaxLength(180);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'ACTIVA'::character varying");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).HasPrincipalKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_politica_empresa");
        builder.HasOne<ProductoCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.ProductoCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.ProductoCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_politica_producto");
        builder.HasKey(e => e.PoliticaCreditoId).HasName("politica_credito_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.Codigo }).IsUnique().HasDatabaseName("uq_politica_empresa_codigo");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.PoliticaCreditoId }).HasName("uq_politica_empresa_id");
        builder.HasIndex(e => e.EmpresaId).HasDatabaseName("ix_politica_empresa");
        builder.HasIndex(e => new { e.EmpresaId, e.ProductoCreditoId }).HasDatabaseName("ix_politica_producto");
    }
}
