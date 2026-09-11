using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class DatoExtraidoConfiguration : IEntityTypeConfiguration<DatoExtraido>
{
    public void Configure(EntityTypeBuilder<DatoExtraido> builder)
    {
        builder.ToTable("dato_extraido", "documentos", table =>
        {
            table.HasCheckConstraint("ck_dato_extraido_confianza", "((confianza IS NULL) OR ((confianza >= (0)::numeric) AND (confianza <= (1)::numeric)))");
            table.HasCheckConstraint("ck_dato_extraido_pagina", "((pagina IS NULL) OR (pagina > 0))");
            table.HasCheckConstraint("ck_dato_extraido_tipo", "((tipo_dato)::text = ANY ((ARRAY['TEXTO'::character varying, 'NUMERO'::character varying, 'FECHA'::character varying, 'BOOLEANO'::character varying, 'JSON'::character varying])::text[]))");
        });
        builder.Property(e => e.DatoExtraidoId).HasColumnName("dato_extraido_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.DocumentoVersionId).HasColumnName("documento_version_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.CodigoCampo).HasColumnName("codigo_campo").HasColumnType("character varying(120)").IsRequired(true).HasMaxLength(120);
        builder.Property(e => e.NombreCampo).HasColumnName("nombre_campo").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.IndiceOcurrencia).HasColumnName("indice_ocurrencia").HasColumnType("integer").IsRequired(true).HasDefaultValueSql("1");
        builder.Property(e => e.TipoDato).HasColumnName("tipo_dato").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20);
        builder.Property(e => e.ValorOriginal).HasColumnName("valor_original").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.ValorTexto).HasColumnName("valor_texto").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.ValorNumerico).HasColumnName("valor_numerico").HasColumnType("numeric(24,8)").IsRequired(false);
        builder.Property(e => e.ValorFecha).HasColumnName("valor_fecha").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.ValorBooleano).HasColumnName("valor_booleano").HasColumnType("boolean").IsRequired(false);
        builder.Property(e => e.ValorJson).HasColumnName("valor_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.Pagina).HasColumnName("pagina").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.Confianza).HasColumnName("confianza").HasColumnType("numeric(8,6)").IsRequired(false);
        builder.Property(e => e.ModeloExtraccion).HasColumnName("modelo_extraccion").HasColumnType("character varying(150)").IsRequired(false).HasMaxLength(150);
        builder.Property(e => e.VersionExtractor).HasColumnName("version_extractor").HasColumnType("character varying(80)").IsRequired(false).HasMaxLength(80);
        builder.Property(e => e.UbicacionJson).HasColumnName("ubicacion_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.FechaExtraccion).HasColumnName("fecha_extraccion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.DatoExtraidoId).HasName("dato_extraido_pkey");
        builder.HasOne<DocumentoVersion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.DocumentoVersionId }).HasPrincipalKey(e => new { e.EmpresaId, e.DocumentoVersionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_dato_extraido_documento");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.DatoExtraidoId }).HasName("uq_dato_extraido_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.DocumentoVersionId, e.CodigoCampo, e.IndiceOcurrencia }).IsUnique().HasDatabaseName("uq_dato_extraido_ocurrencia");
        builder.HasIndex(e => new { e.EmpresaId, e.DocumentoVersionId }).HasDatabaseName("ix_dato_extraido_documento");
    }
}
