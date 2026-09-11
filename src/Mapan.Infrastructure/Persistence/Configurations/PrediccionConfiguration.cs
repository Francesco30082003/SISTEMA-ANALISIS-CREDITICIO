using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class PrediccionConfiguration : IEntityTypeConfiguration<Prediccion>
{
    public void Configure(EntityTypeBuilder<Prediccion> builder)
    {
        builder.ToTable("prediccion", "riesgo", table =>
        {
            table.HasCheckConstraint("ck_prediccion_clase", "((clase_predicha)::text = ANY ((ARRAY['NO_INCUMPLIMIENTO'::character varying, 'INCUMPLIMIENTO'::character varying])::text[]))");
            table.HasCheckConstraint("ck_prediccion_nivel", "((nivel_riesgo IS NULL) OR ((nivel_riesgo)::text = ANY ((ARRAY['BAJO'::character varying, 'MEDIO'::character varying, 'ALTO'::character varying])::text[])))");
            table.HasCheckConstraint("ck_prediccion_probabilidad", "((probabilidad_incumplimiento >= (0)::numeric) AND (probabilidad_incumplimiento <= (1)::numeric))");
            table.HasCheckConstraint("ck_prediccion_umbral", "((umbral_utilizado >= (0)::numeric) AND (umbral_utilizado <= (1)::numeric))");
        });
        builder.Property(e => e.PrediccionId).HasColumnName("prediccion_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.AnalisisId).HasColumnName("analisis_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ModeloVersionId).HasColumnName("modelo_version_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ProbabilidadIncumplimiento).HasColumnName("probabilidad_incumplimiento").HasColumnType("numeric(12,10)").IsRequired(true);
        builder.Property(e => e.UmbralUtilizado).HasColumnName("umbral_utilizado").HasColumnType("numeric(12,10)").IsRequired(true);
        builder.Property(e => e.ClasePredicha).HasColumnName("clase_predicha").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30);
        builder.Property(e => e.NivelRiesgo).HasColumnName("nivel_riesgo").HasColumnType("character varying(20)").IsRequired(false).HasMaxLength(20);
        builder.Property(e => e.TiempoInferenciaMs).HasColumnName("tiempo_inferencia_ms").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.FechaPrediccion).HasColumnName("fecha_prediccion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Analisis>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.AnalisisId }).HasPrincipalKey(e => new { e.EmpresaId, e.AnalisisId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_prediccion_analisis");
        builder.HasOne<ModeloVersion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.ModeloVersionId }).HasPrincipalKey(e => new { e.EmpresaId, e.ModeloVersionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_prediccion_modelo_version");
        builder.HasKey(e => e.PrediccionId).HasName("prediccion_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.AnalisisId, e.ModeloVersionId }).IsUnique().HasDatabaseName("uq_prediccion_analisis_modelo");
        builder.HasIndex(e => new { e.EmpresaId, e.AnalisisId }).HasDatabaseName("ix_prediccion_analisis");
    }
}
