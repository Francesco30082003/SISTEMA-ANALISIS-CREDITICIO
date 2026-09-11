using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class AprobacionPasoConfiguration : IEntityTypeConfiguration<AprobacionPaso>
{
    public void Configure(EntityTypeBuilder<AprobacionPaso> builder)
    {
        builder.ToTable("aprobacion_paso", "flujo", table =>
        {
            table.HasCheckConstraint("ck_aprobacion_paso_estado", "((estado)::text = ANY ((ARRAY['PENDIENTE'::character varying, 'ACTIVO'::character varying, 'APROBADO'::character varying, 'RECHAZADO'::character varying, 'DEVUELTO'::character varying, 'OMITIDO'::character varying])::text[]))");
        });
        builder.Property(e => e.AprobacionPasoId).HasColumnName("aprobacion_paso_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.AprobacionId).HasColumnName("aprobacion_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.RutaAprobacionPasoId).HasColumnName("ruta_aprobacion_paso_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Orden).HasColumnName("orden").HasColumnType("integer").IsRequired(true);
        builder.Property(e => e.NombrePaso).HasColumnName("nombre_paso").HasColumnType("character varying(200)").IsRequired(true).HasMaxLength(200);
        builder.Property(e => e.RolId).HasColumnName("rol_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.CantidadAprobacionesRequeridas).HasColumnName("cantidad_aprobaciones_requeridas").HasColumnType("integer").IsRequired(true).HasDefaultValueSql("1");
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30).HasDefaultValueSql("'PENDIENTE'::character varying");
        builder.Property(e => e.FechaHabilitacion).HasColumnName("fecha_habilitacion").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.FechaCompletado).HasColumnName("fecha_completado").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.HasKey(e => e.AprobacionPasoId).HasName("aprobacion_paso_pkey");
        builder.HasOne<Aprobacion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.AprobacionId }).HasPrincipalKey(e => new { e.EmpresaId, e.AprobacionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_aprobacion_paso_aprobacion");
        builder.HasOne<RutaAprobacionPaso>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.RutaAprobacionPasoId }).HasPrincipalKey(e => new { e.EmpresaId, e.RutaAprobacionPasoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_aprobacion_paso_config");
        builder.HasOne<Rol>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.RolId }).HasPrincipalKey(e => new { e.EmpresaId, e.RolId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_aprobacion_paso_rol");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.AprobacionPasoId }).HasName("uq_aprobacion_paso_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.AprobacionId, e.Orden }).IsUnique().HasDatabaseName("uq_aprobacion_paso_orden");
        builder.HasIndex(e => new { e.EmpresaId, e.AprobacionId, e.Estado }).HasDatabaseName("ix_aprobacion_paso_estado");
    }
}
