using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class ProductoCreditoConfiguration : IEntityTypeConfiguration<ProductoCredito>
{
    public void Configure(EntityTypeBuilder<ProductoCredito> builder)
    {
        builder.ToTable("producto_credito", "credito", table =>
        {
            table.HasCheckConstraint("ck_producto_montos", "((monto_minimo IS NULL) OR (monto_maximo IS NULL) OR (monto_maximo >= monto_minimo))");
            table.HasCheckConstraint("ck_producto_plazos", "((plazo_minimo_meses IS NULL) OR (plazo_maximo_meses IS NULL) OR (plazo_maximo_meses >= plazo_minimo_meses))");
            table.HasCheckConstraint("ck_producto_tasa", "((tasa_interes_anual_pct IS NULL) OR (tasa_interes_anual_pct >= (0)::numeric))");
            table.HasCheckConstraint("ck_producto_vigencia", "((vigente_desde IS NULL) OR (vigente_hasta IS NULL) OR (vigente_hasta >= vigente_desde))");
            table.HasCheckConstraint("ck_producto_categoria", "((categoria)::text = ANY ((ARRAY['MICROCREDITO'::character varying, 'COMERCIAL'::character varying, 'CONSUMO'::character varying, 'OTRO'::character varying])::text[]))");
        });
        builder.Property(e => e.ProductoCreditoId).HasColumnName("producto_credito_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Codigo).HasColumnName("codigo").HasColumnType("character varying(40)").IsRequired(true).HasMaxLength(40);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasColumnType("character varying(150)").IsRequired(true).HasMaxLength(150);
        builder.Property(e => e.Categoria).HasColumnName("categoria").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'OTRO'::character varying");
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("character varying(500)").IsRequired(false).HasMaxLength(500);
        builder.Property(e => e.MontoMinimo).HasColumnName("monto_minimo").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.MontoMaximo).HasColumnName("monto_maximo").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.PlazoMinimoMeses).HasColumnName("plazo_minimo_meses").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.PlazoMaximoMeses).HasColumnName("plazo_maximo_meses").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.MonedaCodigo).HasColumnName("moneda_codigo").HasColumnType("character(3)").IsRequired(true).HasMaxLength(3).IsFixedLength().HasDefaultValueSql("'USD'::bpchar");
        builder.Property(e => e.TasaInteresAnualPct).HasColumnName("tasa_interes_anual_pct").HasColumnType("numeric(9,4)").IsRequired(false);
        builder.Property(e => e.TipoTasa).HasColumnName("tipo_tasa").HasColumnType("character varying(20)").IsRequired(false).HasMaxLength(20).HasDefaultValueSql("'FIJA'::character varying");
        builder.Property(e => e.VigenteDesde).HasColumnName("vigente_desde").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.VigenteHasta).HasColumnName("vigente_hasta").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'ACTIVO'::character varying");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).HasPrincipalKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_producto_empresa");
        builder.HasKey(e => e.ProductoCreditoId).HasName("producto_credito_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.Codigo }).IsUnique().HasDatabaseName("uq_producto_empresa_codigo");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.ProductoCreditoId }).HasName("uq_producto_empresa_id");
        builder.HasIndex(e => e.EmpresaId).HasDatabaseName("ix_producto_empresa");
    }
}
