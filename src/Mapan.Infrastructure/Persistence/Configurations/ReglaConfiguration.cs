using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class ReglaConfiguration : IEntityTypeConfiguration<Regla>
{
    public void Configure(EntityTypeBuilder<Regla> builder)
    {
        builder.ToTable("regla", "politica", table =>
        {
            table.HasCheckConstraint("ck_regla_severidad", "((severidad)::text = ANY ((ARRAY['INFO'::character varying, 'VERDE'::character varying, 'AMARILLO'::character varying, 'ROJO'::character varying])::text[]))");
            table.HasCheckConstraint("ck_regla_etapa", "((etapa)::text = ANY ((ARRAY['PREEVALUACION'::character varying, 'ANALISIS'::character varying])::text[]))");
        });
        builder.Property(e => e.ReglaId).HasColumnName("regla_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.PoliticaVersionId).HasColumnName("politica_version_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Codigo).HasColumnName("codigo").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasColumnType("character varying(180)").IsRequired(true).HasMaxLength(180);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.Prioridad).HasColumnName("prioridad").HasColumnType("integer").IsRequired(true).HasDefaultValueSql("100");
        builder.Property(e => e.Severidad).HasColumnName("severidad").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'INFO'::character varying");
        builder.Property(e => e.Etapa).HasColumnName("etapa").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'ANALISIS'::character varying");
        builder.Property(e => e.CondicionJson).HasColumnName("condicion_json").HasColumnType("jsonb").IsRequired(true);
        builder.Property(e => e.AccionJson).HasColumnName("accion_json").HasColumnType("jsonb").IsRequired(true);
        builder.Property(e => e.Activa).HasColumnName("activa").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<PoliticaVersion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.PoliticaVersionId }).HasPrincipalKey(e => new { e.EmpresaId, e.PoliticaVersionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_regla_politica_version");
        builder.HasKey(e => e.ReglaId).HasName("regla_pkey");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.ReglaId }).HasName("uq_regla_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.PoliticaVersionId, e.Codigo }).IsUnique().HasDatabaseName("uq_regla_version_codigo");
        builder.HasIndex(e => new { e.EmpresaId, e.PoliticaVersionId, e.Prioridad }).HasDatabaseName("ix_regla_version_prioridad");
    }
}
