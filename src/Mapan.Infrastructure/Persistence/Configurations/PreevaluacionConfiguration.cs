using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class PreevaluacionConfiguration : IEntityTypeConfiguration<Preevaluacion>
{
    public void Configure(EntityTypeBuilder<Preevaluacion> builder)
    {
        builder.ToTable("preevaluacion", "politica", table =>
        {
            table.HasCheckConstraint("ck_preevaluacion_resultado", "((resultado_preliminar)::text = ANY ((ARRAY['APTO'::character varying, 'REQUIERE_EXCEPCION'::character varying, 'NO_APTO'::character varying])::text[]))");
        });
        builder.Property(e => e.PreevaluacionId).HasColumnName("preevaluacion_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.PoliticaVersionId).HasColumnName("politica_version_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ResultadoPreliminar).HasColumnName("resultado_preliminar").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30);
        builder.Property(e => e.SeveridadMaxima).HasColumnName("severidad_maxima").HasColumnType("character varying(20)").IsRequired(false).HasMaxLength(20);
        builder.Property(e => e.DetalleJson).HasColumnName("detalle_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.EjecutadaPorUsuarioEmpresaId).HasColumnName("ejecutada_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.FechaEjecucion).HasColumnName("fecha_ejecucion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).HasPrincipalKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_preevaluacion_empresa");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_preevaluacion_solicitud");
        builder.HasOne<PoliticaVersion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.PoliticaVersionId }).HasPrincipalKey(e => new { e.EmpresaId, e.PoliticaVersionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_preevaluacion_politica_version");
        builder.HasKey(e => e.PreevaluacionId).HasName("preevaluacion_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasDatabaseName("ix_preevaluacion_solicitud");
    }
}
