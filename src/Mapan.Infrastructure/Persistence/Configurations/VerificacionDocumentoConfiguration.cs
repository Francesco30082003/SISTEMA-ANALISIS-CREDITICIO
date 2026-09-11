using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class VerificacionDocumentoConfiguration : IEntityTypeConfiguration<VerificacionDocumento>
{
    public void Configure(EntityTypeBuilder<VerificacionDocumento> builder)
    {
        builder.ToTable("verificacion", "documentos", table =>
        {
            table.HasCheckConstraint("ck_verificacion_resultado", "((resultado)::text = ANY ((ARRAY['VERIFICADO'::character varying, 'SOSPECHOSO'::character varying, 'INCONSISTENTE'::character varying])::text[]))");
        });
        builder.Property(e => e.VerificacionId).HasColumnName("verificacion_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.DocumentoId).HasColumnName("documento_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.EmpresaProveedorId).HasColumnName("empresa_proveedor_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.Resultado).HasColumnName("resultado").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20);
        builder.Property(e => e.ConfianzaPct).HasColumnName("confianza_pct").HasColumnType("numeric(5,2)").IsRequired(false);
        builder.Property(e => e.MotivosJson).HasColumnName("motivos_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.Observacion).HasColumnName("observacion").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.EjecutadaPorUsuarioEmpresaId).HasColumnName("ejecutada_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.VerificacionId).HasName("verificacion_pkey");
        builder.HasOne<Documento>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.DocumentoId }).HasPrincipalKey(e => new { e.EmpresaId, e.DocumentoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_verificacion_documento");
        builder.HasOne<EmpresaProveedor>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.EmpresaProveedorId }).HasPrincipalKey(e => new { e.EmpresaId, e.EmpresaProveedorId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_verificacion_empresa_proveedor");
        builder.HasIndex(e => new { e.EmpresaId, e.DocumentoId, e.FechaCreacion }).HasDatabaseName("ix_verificacion_documento").IsDescending(false, false, true);
    }
}
