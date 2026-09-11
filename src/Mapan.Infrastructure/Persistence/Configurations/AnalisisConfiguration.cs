using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class AnalisisConfiguration : IEntityTypeConfiguration<Analisis>
{
    public void Configure(EntityTypeBuilder<Analisis> builder)
    {
        builder.ToTable("analisis", "riesgo", table =>
        {
            table.HasCheckConstraint("ck_analisis_estado", "((estado)::text = ANY ((ARRAY['INICIADO'::character varying, 'COMPLETADO'::character varying, 'ERROR'::character varying, 'CANCELADO'::character varying])::text[]))");
            table.HasCheckConstraint("ck_analisis_tipo", "((tipo_analisis)::text = ANY ((ARRAY['REGLAS'::character varying, 'PREDICTIVO'::character varying, 'COMPLETO'::character varying])::text[]))");
        });
        builder.Property(e => e.AnalisisId).HasColumnName("analisis_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.PoliticaVersionId).HasColumnName("politica_version_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.NumeroEjecucion).HasColumnName("numero_ejecucion").HasColumnType("integer").IsRequired(true);
        builder.Property(e => e.TipoAnalisis).HasColumnName("tipo_analisis").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30).HasDefaultValueSql("'REGLAS'::character varying");
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30).HasDefaultValueSql("'INICIADO'::character varying");
        builder.Property(e => e.EjecutadoPorUsuarioEmpresaId).HasColumnName("ejecutado_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.IniciadoEn).HasColumnName("iniciado_en").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.Property(e => e.FinalizadoEn).HasColumnName("finalizado_en").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.HasKey(e => e.AnalisisId).HasName("analisis_pkey");
        builder.HasOne<PoliticaVersion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.PoliticaVersionId }).HasPrincipalKey(e => new { e.EmpresaId, e.PoliticaVersionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_analisis_politica_version");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_analisis_solicitud");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.EjecutadoPorUsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_analisis_usuario");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId, e.NumeroEjecucion }).IsUnique().HasDatabaseName("uq_analisis_ejecucion");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.AnalisisId }).HasName("uq_analisis_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.PoliticaVersionId }).HasDatabaseName("ix_analisis_politica");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId, e.NumeroEjecucion }).HasDatabaseName("ix_analisis_solicitud").IsDescending(false, false, true);
    }
}
