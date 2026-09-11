using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class ModeloVersionConfiguration : IEntityTypeConfiguration<ModeloVersion>
{
    public void Configure(EntityTypeBuilder<ModeloVersion> builder)
    {
        builder.ToTable("modelo_version", "riesgo", table =>
        {
            table.HasCheckConstraint("ck_modelo_version_estado", "((estado)::text = ANY ((ARRAY['BORRADOR'::character varying, 'ENTRENADO'::character varying, 'VALIDADO'::character varying, 'PRODUCCION'::character varying, 'SHADOW'::character varying, 'RETIRADO'::character varying])::text[]))");
            table.HasCheckConstraint("ck_modelo_version_metricas", "(((roc_auc IS NULL) OR ((roc_auc >= (0)::numeric) AND (roc_auc <= (1)::numeric))) AND ((precision_score IS NULL) OR ((precision_score >= (0)::numeric) AND (precision_score <= (1)::numeric))) AND ((recall_score IS NULL) OR ((recall_score >= (0)::numeric) AND (recall_score <= (1)::numeric))) AND ((f1_score IS NULL) OR ((f1_score >= (0)::numeric) AND (f1_score <= (1)::numeric))) AND ((accuracy_score IS NULL) OR ((accuracy_score >= (0)::numeric) AND (accuracy_score <= (1)::numeric))))");
            table.HasCheckConstraint("ck_modelo_version_umbral", "((umbral_decision IS NULL) OR ((umbral_decision >= (0)::numeric) AND (umbral_decision <= (1)::numeric)))");
        });
        builder.Property(e => e.ModeloVersionId).HasColumnName("modelo_version_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ModeloId).HasColumnName("modelo_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.NumeroVersion).HasColumnName("numero_version").HasColumnType("integer").IsRequired(true);
        builder.Property(e => e.Algoritmo).HasColumnName("algoritmo").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.ArtefactoUri).HasColumnName("artefacto_uri").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.EsquemaCaracteristicas).HasColumnName("esquema_caracteristicas").HasColumnType("jsonb").IsRequired(true);
        builder.Property(e => e.Hiperparametros).HasColumnName("hiperparametros").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.FechaDatosDesde).HasColumnName("fecha_datos_desde").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.FechaDatosHasta).HasColumnName("fecha_datos_hasta").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.CantidadRegistros).HasColumnName("cantidad_registros").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.CantidadPositivos).HasColumnName("cantidad_positivos").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.CantidadNegativos).HasColumnName("cantidad_negativos").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.RocAuc).HasColumnName("roc_auc").HasColumnType("numeric(8,6)").IsRequired(false);
        builder.Property(e => e.PrecisionScore).HasColumnName("precision_score").HasColumnType("numeric(8,6)").IsRequired(false);
        builder.Property(e => e.RecallScore).HasColumnName("recall_score").HasColumnType("numeric(8,6)").IsRequired(false);
        builder.Property(e => e.F1Score).HasColumnName("f1_score").HasColumnType("numeric(8,6)").IsRequired(false);
        builder.Property(e => e.AccuracyScore).HasColumnName("accuracy_score").HasColumnType("numeric(8,6)").IsRequired(false);
        builder.Property(e => e.UmbralDecision).HasColumnName("umbral_decision").HasColumnType("numeric(8,6)").IsRequired(false);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'BORRADOR'::character varying");
        builder.Property(e => e.FechaEntrenamiento).HasColumnName("fecha_entrenamiento").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.FechaValidacion).HasColumnName("fecha_validacion").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Modelo>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.ModeloId }).HasPrincipalKey(e => new { e.EmpresaId, e.ModeloId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_modelo_version_modelo");
        builder.HasKey(e => e.ModeloVersionId).HasName("modelo_version_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.ModeloId, e.NumeroVersion }).IsUnique().HasDatabaseName("uq_modelo_version");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.ModeloVersionId }).HasName("uq_modelo_version_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.ModeloId, e.NumeroVersion }).HasDatabaseName("ix_modelo_version_modelo");
    }
}
