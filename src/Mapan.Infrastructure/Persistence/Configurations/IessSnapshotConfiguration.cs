using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class IessSnapshotConfiguration : IEntityTypeConfiguration<IessSnapshot>
{
    public void Configure(EntityTypeBuilder<IessSnapshot> builder)
    {
        builder.ToTable("iess_snapshot", "integracion", table => { });
        builder.Property(e => e.IessSnapshotId).HasColumnName("iess_snapshot_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.EmpresaProveedorId).HasColumnName("empresa_proveedor_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ConsultaExternaId).HasColumnName("consulta_externa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.RelacionLaboralActiva).HasColumnName("relacion_laboral_activa").HasColumnType("boolean").IsRequired(false);
        builder.Property(e => e.EmpleadorRegistrado).HasColumnName("empleador_registrado").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.FechaAfiliacion).HasColumnName("fecha_afiliacion").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.AporteMensual).HasColumnName("aporte_mensual").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(30)").IsRequired(false).HasMaxLength(30);
        builder.Property(e => e.PayloadNormalizado).HasColumnName("payload_normalizado").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.IessSnapshotId).HasName("iess_snapshot_pkey");
        builder.HasOne<EmpresaProveedor>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.EmpresaProveedorId }).HasPrincipalKey(e => new { e.EmpresaId, e.EmpresaProveedorId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_iess_empresa_proveedor");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_iess_solicitud");
        builder.HasOne<ConsultaExterna>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId, e.ConsultaExternaId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId, e.ConsultaExternaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_iess_consulta");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId, e.FechaCreacion }).HasDatabaseName("ix_iess_snapshot_solicitud").IsDescending(false, false, true);
    }
}
