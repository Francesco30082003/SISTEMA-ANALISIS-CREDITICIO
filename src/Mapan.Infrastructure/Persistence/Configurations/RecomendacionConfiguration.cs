using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class RecomendacionConfiguration : IEntityTypeConfiguration<Recomendacion>
{
    public void Configure(EntityTypeBuilder<Recomendacion> builder)
    {
        builder.ToTable("recomendacion", "riesgo", table =>
        {
            table.HasCheckConstraint("ck_recomendacion_probabilidad", "((probabilidad_incumplimiento IS NULL) OR ((probabilidad_incumplimiento >= (0)::numeric) AND (probabilidad_incumplimiento <= (1)::numeric)))");
        });
        builder.Property(e => e.RecomendacionId).HasColumnName("recomendacion_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.AnalisisId).HasColumnName("analisis_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.PrediccionId).HasColumnName("prediccion_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.CodigoRecomendacion).HasColumnName("codigo_recomendacion").HasColumnType("character varying(60)").IsRequired(true).HasMaxLength(60);
        builder.Property(e => e.NivelRiesgoFinal).HasColumnName("nivel_riesgo_final").HasColumnType("character varying(30)").IsRequired(false).HasMaxLength(30);
        builder.Property(e => e.CumpleCapacidad).HasColumnName("cumple_capacidad").HasColumnType("boolean").IsRequired(false);
        builder.Property(e => e.SeveridadMaximaReglas).HasColumnName("severidad_maxima_reglas").HasColumnType("character varying(20)").IsRequired(false).HasMaxLength(20);
        builder.Property(e => e.ProbabilidadIncumplimiento).HasColumnName("probabilidad_incumplimiento").HasColumnType("numeric(12,10)").IsRequired(false);
        builder.Property(e => e.RequiereRevisionHumana).HasColumnName("requiere_revision_humana").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.Resumen).HasColumnName("resumen").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.DetalleJson).HasColumnName("detalle_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.FechaGeneracion).HasColumnName("fecha_generacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Analisis>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.AnalisisId }).HasPrincipalKey(e => new { e.EmpresaId, e.AnalisisId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_recomendacion_analisis");
        builder.HasOne<Prediccion>().WithMany().HasForeignKey(e => e.PrediccionId).HasPrincipalKey(e => e.PrediccionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_recomendacion_prediccion");
        builder.HasKey(e => e.RecomendacionId).HasName("recomendacion_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.AnalisisId }).IsUnique().HasDatabaseName("uq_recomendacion_analisis");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.RecomendacionId }).HasName("uq_recomendacion_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.AnalisisId }).HasDatabaseName("ix_recomendacion_analisis");
    }
}
