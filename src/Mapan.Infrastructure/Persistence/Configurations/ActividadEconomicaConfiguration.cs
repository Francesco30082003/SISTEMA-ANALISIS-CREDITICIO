using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class ActividadEconomicaConfiguration : IEntityTypeConfiguration<ActividadEconomica>
{
    public void Configure(EntityTypeBuilder<ActividadEconomica> builder)
    {
        builder.ToTable("actividad_economica", "credito", table =>
        {
        });
        builder.Property(e => e.ActividadEconomicaId).HasColumnName("actividad_economica_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.TipoActividad).HasColumnName("tipo_actividad").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30);
        builder.Property(e => e.EmpleadorNegocio).HasColumnName("empleador_negocio").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.CargoActividad).HasColumnName("cargo_actividad").HasColumnType("character varying(150)").IsRequired(false).HasMaxLength(150);
        builder.Property(e => e.FechaInicio).HasColumnName("fecha_inicio").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.Ruc).HasColumnName("ruc").HasColumnType("character varying(20)").IsRequired(false).HasMaxLength(20);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("character varying(300)").IsRequired(false).HasMaxLength(300);
        builder.Property(e => e.EsPrincipal).HasColumnName("es_principal").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.Verificada).HasColumnName("verificada").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("false");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.ActividadEconomicaId).HasName("actividad_economica_pkey");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_actividad_solicitud");
        builder.HasIndex(e => new { e.EmpresaId, e.ActividadEconomicaId }).IsUnique().HasDatabaseName("uq_actividad_empresa_id");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.SolicitudCreditoId, e.ActividadEconomicaId }).HasName("uq_actividad_solicitud_id");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasDatabaseName("ix_actividad_solicitud");
    }
}
