using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class FuenteIngresoConfiguration : IEntityTypeConfiguration<FuenteIngreso>
{
    public void Configure(EntityTypeBuilder<FuenteIngreso> builder)
    {
        builder.ToTable("fuente_ingreso", "credito", table =>
        {
        });
        builder.Property(e => e.FuenteIngresoId).HasColumnName("fuente_ingreso_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ActividadEconomicaId).HasColumnName("actividad_economica_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.TipoIngreso).HasColumnName("tipo_ingreso").HasColumnType("character varying(50)").IsRequired(true).HasMaxLength(50);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.MonedaCodigo).HasColumnName("moneda_codigo").HasColumnType("character(3)").IsRequired(true).HasMaxLength(3).IsFixedLength().HasDefaultValueSql("'USD'::bpchar");
        builder.Property(e => e.EsRecurrente).HasColumnName("es_recurrente").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.Declarado).HasColumnName("declarado").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.Verificado).HasColumnName("verificado").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("false");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<ActividadEconomica>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId, e.ActividadEconomicaId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId, e.ActividadEconomicaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_fuente_ingreso_actividad");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_fuente_ingreso_solicitud");
        builder.HasKey(e => e.FuenteIngresoId).HasName("fuente_ingreso_pkey");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.FuenteIngresoId }).HasName("uq_fuente_ingreso_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.ActividadEconomicaId }).HasDatabaseName("ix_fuente_ingreso_actividad");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasDatabaseName("ix_fuente_ingreso_solicitud");
    }
}
