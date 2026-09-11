using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class DatoValidadoConfiguration : IEntityTypeConfiguration<DatoValidado>
{
    public void Configure(EntityTypeBuilder<DatoValidado> builder)
    {
        builder.ToTable("dato_validado", "documentos", table =>
        {
            table.HasCheckConstraint("ck_dato_validado_estado", "((estado)::text = ANY ((ARRAY['CONFIRMADO'::character varying, 'CORREGIDO'::character varying, 'AGREGADO'::character varying, 'DESCARTADO'::character varying])::text[]))");
            table.HasCheckConstraint("ck_dato_validado_tipo", "((tipo_dato)::text = ANY ((ARRAY['TEXTO'::character varying, 'NUMERO'::character varying, 'FECHA'::character varying, 'BOOLEANO'::character varying, 'JSON'::character varying])::text[]))");
        });
        builder.Property(e => e.DatoValidadoId).HasColumnName("dato_validado_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudDocumentoId).HasColumnName("solicitud_documento_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.DatoExtraidoId).HasColumnName("dato_extraido_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.CodigoCampo).HasColumnName("codigo_campo").HasColumnType("character varying(120)").IsRequired(true).HasMaxLength(120);
        builder.Property(e => e.NumeroRevision).HasColumnName("numero_revision").HasColumnType("integer").IsRequired(true).HasDefaultValueSql("1");
        builder.Property(e => e.TipoDato).HasColumnName("tipo_dato").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20);
        builder.Property(e => e.ValorTexto).HasColumnName("valor_texto").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.ValorNumerico).HasColumnName("valor_numerico").HasColumnType("numeric(24,8)").IsRequired(false);
        builder.Property(e => e.ValorFecha).HasColumnName("valor_fecha").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.ValorBooleano).HasColumnName("valor_booleano").HasColumnType("boolean").IsRequired(false);
        builder.Property(e => e.ValorJson).HasColumnName("valor_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30);
        builder.Property(e => e.Comentario).HasColumnName("comentario").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.ValidadoPorUsuarioEmpresaId).HasColumnName("validado_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.FechaValidacion).HasColumnName("fecha_validacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.DatoValidadoId).HasName("dato_validado_pkey");
        builder.HasOne<DatoExtraido>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.DatoExtraidoId }).HasPrincipalKey(e => new { e.EmpresaId, e.DatoExtraidoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_dato_validado_extraido");
        builder.HasOne<SolicitudDocumento>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudDocumentoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudDocumentoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_dato_validado_solicitud_documento");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.ValidadoPorUsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_dato_validado_usuario");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudDocumentoId, e.CodigoCampo, e.NumeroRevision }).IsUnique().HasDatabaseName("uq_dato_validado_revision");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudDocumentoId }).HasDatabaseName("ix_dato_validado_solicitud_documento");
    }
}
