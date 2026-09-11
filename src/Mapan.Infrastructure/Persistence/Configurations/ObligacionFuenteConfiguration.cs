using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class ObligacionFuenteConfiguration : IEntityTypeConfiguration<ObligacionFuente>
{
    public void Configure(EntityTypeBuilder<ObligacionFuente> builder)
    {
        builder.ToTable("obligacion_fuente", "credito", table =>
        {
            table.HasCheckConstraint("ck_obligacion_fuente_cuota", "((cuota_reportada IS NULL) OR (cuota_reportada >= (0)::numeric))");
            table.HasCheckConstraint("ck_obligacion_fuente_mora", "((dias_mora_reportado IS NULL) OR (dias_mora_reportado >= 0))");
            table.HasCheckConstraint("ck_obligacion_fuente_saldo", "((saldo_reportado IS NULL) OR (saldo_reportado >= (0)::numeric))");
        });
        builder.Property(e => e.ObligacionFuenteId).HasColumnName("obligacion_fuente_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ObligacionId).HasColumnName("obligacion_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.TipoFuente).HasColumnName("tipo_fuente").HasColumnType("character varying(60)").IsRequired(true).HasMaxLength(60);
        builder.Property(e => e.BuroSnapshotId).HasColumnName("buro_snapshot_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.SolicitudDocumentoId).HasColumnName("solicitud_documento_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.ReferenciaFuente).HasColumnName("referencia_fuente").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.SaldoReportado).HasColumnName("saldo_reportado").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.CuotaReportada).HasColumnName("cuota_reportada").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.DiasMoraReportado).HasColumnName("dias_mora_reportado").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.FechaFuente).HasColumnName("fecha_fuente").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<BuroSnapshot>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.BuroSnapshotId }).HasPrincipalKey(e => new { e.EmpresaId, e.BuroSnapshotId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_obligacion_fuente_buro");
        builder.HasOne<SolicitudDocumento>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudDocumentoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudDocumentoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_obligacion_fuente_documento");
        builder.HasOne<Obligacion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.ObligacionId }).HasPrincipalKey(e => new { e.EmpresaId, e.ObligacionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_obligacion_fuente_obligacion");
        builder.HasKey(e => e.ObligacionFuenteId).HasName("obligacion_fuente_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.ObligacionId }).HasDatabaseName("ix_obligacion_fuente_obligacion");
    }
}
