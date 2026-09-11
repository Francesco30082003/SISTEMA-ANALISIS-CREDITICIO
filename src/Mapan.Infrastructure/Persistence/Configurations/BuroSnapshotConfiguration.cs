using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class BuroSnapshotConfiguration : IEntityTypeConfiguration<BuroSnapshot>
{
    public void Configure(EntityTypeBuilder<BuroSnapshot> builder)
    {
        builder.ToTable("buro_snapshot", "integracion", table =>
        {
            table.HasCheckConstraint("ck_buro_origen", "((consulta_externa_id IS NOT NULL) OR (solicitud_documento_id IS NOT NULL))");
        });
        builder.Property(e => e.BuroSnapshotId).HasColumnName("buro_snapshot_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.EmpresaProveedorId).HasColumnName("empresa_proveedor_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ConsultaExternaId).HasColumnName("consulta_externa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.SolicitudDocumentoId).HasColumnName("solicitud_documento_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.FechaReporte).HasColumnName("fecha_reporte").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.ScoreBuro).HasColumnName("score_buro").HasColumnType("numeric(12,4)").IsRequired(false);
        builder.Property(e => e.DeudaTotal).HasColumnName("deuda_total").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.CuotaTotal).HasColumnName("cuota_total").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.CreditosActivos).HasColumnName("creditos_activos").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.MoraActualMaxDias).HasColumnName("mora_actual_max_dias").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.MoraHistoricaMaxDias).HasColumnName("mora_historica_max_dias").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.PayloadNormalizado).HasColumnName("payload_normalizado").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.BuroSnapshotId).HasName("buro_snapshot_pkey");
        builder.HasOne<ConsultaExterna>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId, e.ConsultaExternaId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId, e.ConsultaExternaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_buro_consulta");
        builder.HasOne<SolicitudDocumento>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudDocumentoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudDocumentoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_buro_documento");
        builder.HasOne<EmpresaProveedor>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.EmpresaProveedorId }).HasPrincipalKey(e => new { e.EmpresaId, e.EmpresaProveedorId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_buro_empresa_proveedor");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_buro_solicitud");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.BuroSnapshotId }).HasName("uq_buro_snapshot_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId, e.FechaCreacion }).HasDatabaseName("ix_buro_snapshot_solicitud").IsDescending(false, false, true);
    }
}
