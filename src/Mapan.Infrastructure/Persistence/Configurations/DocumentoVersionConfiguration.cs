using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class DocumentoVersionConfiguration : IEntityTypeConfiguration<DocumentoVersion>
{
    public void Configure(EntityTypeBuilder<DocumentoVersion> builder)
    {
        builder.ToTable("documento_version", "documentos", table =>
        {
            table.HasCheckConstraint("ck_documento_version_numero", "(numero_version > 0)");
            table.HasCheckConstraint("ck_documento_version_origen", "((origen)::text = ANY ((ARRAY['CARGA_MANUAL'::character varying, 'API'::character varying, 'GENERADO'::character varying])::text[]))");
            table.HasCheckConstraint("ck_documento_version_tamano", "((tamano_bytes IS NULL) OR (tamano_bytes >= 0))");
        });
        builder.Property(e => e.DocumentoVersionId).HasColumnName("documento_version_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.DocumentoId).HasColumnName("documento_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.NumeroVersion).HasColumnName("numero_version").HasColumnType("integer").IsRequired(true);
        builder.Property(e => e.NombreArchivo).HasColumnName("nombre_archivo").HasColumnType("character varying(300)").IsRequired(true).HasMaxLength(300);
        builder.Property(e => e.MimeType).HasColumnName("mime_type").HasColumnType("character varying(120)").IsRequired(false).HasMaxLength(120);
        builder.Property(e => e.TamanoBytes).HasColumnName("tamano_bytes").HasColumnType("bigint").IsRequired(false);
        builder.Property(e => e.AlmacenamientoUri).HasColumnName("almacenamiento_uri").HasColumnType("text").IsRequired(true);
        builder.Property(e => e.HashSha256).HasColumnName("hash_sha256").HasColumnType("character varying(64)").IsRequired(false).HasMaxLength(64);
        builder.Property(e => e.Origen).HasColumnName("origen").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30).HasDefaultValueSql("'CARGA_MANUAL'::character varying");
        builder.Property(e => e.CreadoPorUsuarioEmpresaId).HasColumnName("creado_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.DocumentoVersionId).HasName("documento_version_pkey");
        builder.HasOne<Documento>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.DocumentoId }).HasPrincipalKey(e => new { e.EmpresaId, e.DocumentoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_documento_version_documento");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.CreadoPorUsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_documento_version_usuario");
        builder.HasIndex(e => new { e.EmpresaId, e.DocumentoId, e.NumeroVersion }).IsUnique().HasDatabaseName("uq_documento_version");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.DocumentoVersionId }).HasName("uq_documento_version_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.DocumentoId, e.NumeroVersion }).HasDatabaseName("ix_documento_version_documento").IsDescending(false, false, true);
    }
}
