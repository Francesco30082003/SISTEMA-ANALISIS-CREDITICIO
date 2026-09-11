using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class ConsultaExternaConfiguration : IEntityTypeConfiguration<ConsultaExterna>
{
    public void Configure(EntityTypeBuilder<ConsultaExterna> builder)
    {
        builder.ToTable("consulta_externa", "integracion", table =>
        {
            table.HasCheckConstraint("ck_consulta_estado", "((estado)::text = ANY ((ARRAY['SOLICITADA'::character varying, 'PROCESANDO'::character varying, 'COMPLETADA'::character varying, 'ERROR'::character varying, 'CANCELADA'::character varying])::text[]))");
        });
        builder.Property(e => e.ConsultaExternaId).HasColumnName("consulta_externa_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.EmpresaProveedorId).HasColumnName("empresa_proveedor_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.TipoConsulta).HasColumnName("tipo_consulta").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30).HasDefaultValueSql("'SOLICITADA'::character varying");
        builder.Property(e => e.ReferenciaExterna).HasColumnName("referencia_externa").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.SolicitudResumenJson).HasColumnName("solicitud_resumen_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.RespuestaResumenJson).HasColumnName("respuesta_resumen_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.ArchivoRespuestaUri).HasColumnName("archivo_respuesta_uri").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.MensajeError).HasColumnName("mensaje_error").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.IniciadaPorUsuarioEmpresaId).HasColumnName("iniciada_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.FechaInicio).HasColumnName("fecha_inicio").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.Property(e => e.FechaFin).HasColumnName("fecha_fin").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.HasKey(e => e.ConsultaExternaId).HasName("consulta_externa_pkey");
        builder.HasOne<EmpresaProveedor>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.EmpresaProveedorId }).HasPrincipalKey(e => new { e.EmpresaId, e.EmpresaProveedorId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_consulta_empresa_proveedor");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_consulta_solicitud");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.IniciadaPorUsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_consulta_usuario");
        builder.HasIndex(e => new { e.EmpresaId, e.ConsultaExternaId }).IsUnique().HasDatabaseName("uq_consulta_empresa_id");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.SolicitudCreditoId, e.ConsultaExternaId }).HasName("uq_consulta_solicitud_id");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId, e.FechaInicio }).HasDatabaseName("ix_consulta_externa_solicitud").IsDescending(false, false, true);
    }
}
