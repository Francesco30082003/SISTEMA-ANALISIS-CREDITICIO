using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class SolicitudDocumentoConfiguration : IEntityTypeConfiguration<SolicitudDocumento>
{
    public void Configure(EntityTypeBuilder<SolicitudDocumento> builder)
    {
        builder.ToTable("solicitud_documento", "documentos", table =>
        {
            table.HasCheckConstraint("ck_solicitud_documento_estado", "((estado)::text = ANY ((ARRAY['PENDIENTE'::character varying, 'VALIDADO'::character varying, 'RECHAZADO'::character varying, 'REEMPLAZADO'::character varying])::text[]))");
        });
        builder.Property(e => e.SolicitudDocumentoId).HasColumnName("solicitud_documento_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.DocumentoVersionId).HasColumnName("documento_version_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.UsoDocumento).HasColumnName("uso_documento").HasColumnType("character varying(100)").IsRequired(false).HasMaxLength(100);
        builder.Property(e => e.Obligatorio).HasColumnName("obligatorio").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("false");
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30).HasDefaultValueSql("'PENDIENTE'::character varying");
        builder.Property(e => e.AsociadoPorUsuarioEmpresaId).HasColumnName("asociado_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.FechaAsociacion).HasColumnName("fecha_asociacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_solicitud_documento_solicitud");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.AsociadoPorUsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_solicitud_documento_usuario");
        builder.HasOne<DocumentoVersion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.DocumentoVersionId }).HasPrincipalKey(e => new { e.EmpresaId, e.DocumentoVersionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_solicitud_documento_version");
        builder.HasKey(e => e.SolicitudDocumentoId).HasName("solicitud_documento_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId, e.DocumentoVersionId }).IsUnique().HasDatabaseName("uq_solicitud_documento");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.SolicitudDocumentoId }).HasName("uq_solicitud_documento_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasDatabaseName("ix_solicitud_documento_solicitud");
    }
}
