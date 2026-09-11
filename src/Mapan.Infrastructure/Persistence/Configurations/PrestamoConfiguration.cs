using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class PrestamoConfiguration : IEntityTypeConfiguration<Prestamo>
{
    public void Configure(EntityTypeBuilder<Prestamo> builder)
    {
        builder.ToTable("prestamo", "credito", table =>
        {
            table.HasCheckConstraint("ck_prestamo_estado", "((estado)::text = ANY ((ARRAY['VIGENTE'::character varying, 'LIQUIDADO'::character varying, 'VENCIDO'::character varying, 'REESTRUCTURADO'::character varying, 'CASTIGADO'::character varying, 'CANCELADO'::character varying])::text[]))");
            table.HasCheckConstraint("ck_prestamo_monto", "(monto_desembolsado > (0)::numeric)");
            table.HasCheckConstraint("ck_prestamo_plazo", "(plazo_meses > 0)");
        });
        builder.Property(e => e.PrestamoId).HasColumnName("prestamo_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.NumeroPrestamo).HasColumnName("numero_prestamo").HasColumnType("character varying(80)").IsRequired(true).HasMaxLength(80);
        builder.Property(e => e.MontoDesembolsado).HasColumnName("monto_desembolsado").HasColumnType("numeric(18,2)").IsRequired(true);
        builder.Property(e => e.FechaDesembolso).HasColumnName("fecha_desembolso").HasColumnType("date").IsRequired(true);
        builder.Property(e => e.PlazoMeses).HasColumnName("plazo_meses").HasColumnType("integer").IsRequired(true);
        builder.Property(e => e.TasaInteresAnualPct).HasColumnName("tasa_interes_anual_pct").HasColumnType("numeric(9,6)").IsRequired(false);
        builder.Property(e => e.CuotaPactada).HasColumnName("cuota_pactada").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30).HasDefaultValueSql("'VIGENTE'::character varying");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_prestamo_solicitud");
        builder.HasKey(e => e.PrestamoId).HasName("prestamo_pkey");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.PrestamoId }).HasName("uq_prestamo_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.NumeroPrestamo }).IsUnique().HasDatabaseName("uq_prestamo_numero");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId }).IsUnique().HasDatabaseName("uq_prestamo_solicitud");
        builder.HasIndex(e => new { e.EmpresaId, e.Estado }).HasDatabaseName("ix_prestamo_empresa_estado");
    }
}
