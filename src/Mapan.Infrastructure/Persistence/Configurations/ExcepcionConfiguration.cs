using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class ExcepcionConfiguration : IEntityTypeConfiguration<Excepcion>
{
    public void Configure(EntityTypeBuilder<Excepcion> builder)
    {
        builder.ToTable("excepcion", "politica", table =>
        {
            table.HasCheckConstraint("ck_excepcion_estado", "((estado)::text = ANY ((ARRAY['SOLICITADA'::character varying, 'APROBADA'::character varying, 'RECHAZADA'::character varying])::text[]))");
        });
        builder.Property(e => e.ExcepcionId).HasColumnName("excepcion_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ReglaId).HasColumnName("regla_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.MotivoJustificacion).HasColumnName("motivo_justificacion").HasColumnType("text").IsRequired(true);
        builder.Property(e => e.Observacion).HasColumnName("observacion").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.Evidencia).HasColumnName("evidencia").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'SOLICITADA'::character varying");
        builder.Property(e => e.SolicitadaPorUsuarioEmpresaId).HasColumnName("solicitada_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.FechaSolicitud).HasColumnName("fecha_solicitud").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.Property(e => e.ResueltaPorUsuarioEmpresaId).HasColumnName("resuelta_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.FechaResolucion).HasColumnName("fecha_resolucion").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.ComentarioResolucion).HasColumnName("comentario_resolucion").HasColumnType("text").IsRequired(false);
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).HasPrincipalKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_excepcion_empresa");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_excepcion_solicitud");
        builder.HasOne<Regla>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.ReglaId }).HasPrincipalKey(e => new { e.EmpresaId, e.ReglaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_excepcion_regla");
        builder.HasKey(e => e.ExcepcionId).HasName("excepcion_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasDatabaseName("ix_excepcion_solicitud");
    }
}
