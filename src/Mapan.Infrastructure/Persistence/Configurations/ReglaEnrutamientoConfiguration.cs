using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class ReglaEnrutamientoConfiguration : IEntityTypeConfiguration<ReglaEnrutamiento>
{
    public void Configure(EntityTypeBuilder<ReglaEnrutamiento> builder)
    {
        builder.ToTable("regla_enrutamiento", "flujo", table =>
        {
        });
        builder.Property(e => e.ReglaEnrutamientoId).HasColumnName("regla_enrutamiento_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.RutaAprobacionId).HasColumnName("ruta_aprobacion_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Codigo).HasColumnName("codigo").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasColumnType("character varying(200)").IsRequired(true).HasMaxLength(200);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.Prioridad).HasColumnName("prioridad").HasColumnType("integer").IsRequired(true).HasDefaultValueSql("100");
        builder.Property(e => e.CondicionJson).HasColumnName("condicion_json").HasColumnType("jsonb").IsRequired(true);
        builder.Property(e => e.Activa).HasColumnName("activa").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<RutaAprobacion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.RutaAprobacionId }).HasPrincipalKey(e => new { e.EmpresaId, e.RutaAprobacionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_regla_enrutamiento_ruta");
        builder.HasKey(e => e.ReglaEnrutamientoId).HasName("regla_enrutamiento_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.Codigo }).IsUnique().HasDatabaseName("uq_regla_enrutamiento_codigo");
        builder.HasIndex(e => new { e.EmpresaId, e.Prioridad }).HasDatabaseName("ix_regla_enrutamiento");
    }
}
