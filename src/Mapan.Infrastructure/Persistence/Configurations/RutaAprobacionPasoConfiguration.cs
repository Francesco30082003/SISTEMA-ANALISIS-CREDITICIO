using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class RutaAprobacionPasoConfiguration : IEntityTypeConfiguration<RutaAprobacionPaso>
{
    public void Configure(EntityTypeBuilder<RutaAprobacionPaso> builder)
    {
        builder.ToTable("ruta_aprobacion_paso", "flujo", table =>
        {
            table.HasCheckConstraint("ck_ruta_paso_cantidad", "(cantidad_aprobaciones_requeridas > 0)");
            table.HasCheckConstraint("ck_ruta_paso_orden", "(orden > 0)");
        });
        builder.Property(e => e.RutaAprobacionPasoId).HasColumnName("ruta_aprobacion_paso_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.RutaAprobacionId).HasColumnName("ruta_aprobacion_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Orden).HasColumnName("orden").HasColumnType("integer").IsRequired(true);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasColumnType("character varying(200)").IsRequired(true).HasMaxLength(200);
        builder.Property(e => e.RolId).HasColumnName("rol_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Obligatorio).HasColumnName("obligatorio").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.CantidadAprobacionesRequeridas).HasColumnName("cantidad_aprobaciones_requeridas").HasColumnType("integer").IsRequired(true).HasDefaultValueSql("1");
        builder.Property(e => e.PermiteAprobar).HasColumnName("permite_aprobar").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.PermiteRechazar).HasColumnName("permite_rechazar").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.PermiteDevolver).HasColumnName("permite_devolver").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Rol>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.RolId }).HasPrincipalKey(e => new { e.EmpresaId, e.RolId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_ruta_paso_rol");
        builder.HasOne<RutaAprobacion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.RutaAprobacionId }).HasPrincipalKey(e => new { e.EmpresaId, e.RutaAprobacionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_ruta_paso_ruta");
        builder.HasKey(e => e.RutaAprobacionPasoId).HasName("ruta_aprobacion_paso_pkey");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.RutaAprobacionPasoId }).HasName("uq_ruta_paso_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.RutaAprobacionId, e.Orden }).IsUnique().HasDatabaseName("uq_ruta_paso_orden");
    }
}
