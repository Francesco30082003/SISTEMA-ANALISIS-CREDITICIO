using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class ObligacionConfiguration : IEntityTypeConfiguration<Obligacion>
{
    public void Configure(EntityTypeBuilder<Obligacion> builder)
    {
        builder.ToTable("obligacion", "credito", table =>
        {
            table.HasCheckConstraint("ck_obligacion_cuota", "((cuota_mensual IS NULL) OR (cuota_mensual >= (0)::numeric))");
            table.HasCheckConstraint("ck_obligacion_mora_actual", "(dias_mora_actual >= 0)");
            table.HasCheckConstraint("ck_obligacion_saldo", "((saldo_actual IS NULL) OR (saldo_actual >= (0)::numeric))");
        });
        builder.Property(e => e.ObligacionId).HasColumnName("obligacion_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Institucion).HasColumnName("institucion").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.TipoObligacion).HasColumnName("tipo_obligacion").HasColumnType("character varying(50)").IsRequired(false).HasMaxLength(50);
        builder.Property(e => e.NumeroOperacionMascara).HasColumnName("numero_operacion_mascara").HasColumnType("character varying(80)").IsRequired(false).HasMaxLength(80);
        builder.Property(e => e.MontoOriginal).HasColumnName("monto_original").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.SaldoActual).HasColumnName("saldo_actual").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.CuotaMensual).HasColumnName("cuota_mensual").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.DiasMoraActual).HasColumnName("dias_mora_actual").HasColumnType("integer").IsRequired(true).HasDefaultValueSql("0");
        builder.Property(e => e.MaxDiasMoraHistorico).HasColumnName("max_dias_mora_historico").HasColumnType("integer").IsRequired(true).HasDefaultValueSql("0");
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(30)").IsRequired(false).HasMaxLength(30);
        builder.Property(e => e.EsGarante).HasColumnName("es_garante").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("false");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_obligacion_solicitud");
        builder.HasKey(e => e.ObligacionId).HasName("obligacion_pkey");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.ObligacionId }).HasName("uq_obligacion_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasDatabaseName("ix_obligacion_solicitud");
    }
}
