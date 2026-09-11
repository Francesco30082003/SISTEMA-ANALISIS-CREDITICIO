using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class PoliticaVersionConfiguration : IEntityTypeConfiguration<PoliticaVersion>
{
    public void Configure(EntityTypeBuilder<PoliticaVersion> builder)
    {
        builder.ToTable("politica_version", "politica", table =>
        {
            table.HasCheckConstraint("ck_politica_version_estado", "((estado)::text = ANY ((ARRAY['BORRADOR'::character varying, 'VIGENTE'::character varying, 'INACTIVA'::character varying, 'ARCHIVADA'::character varying])::text[]))");
            table.HasCheckConstraint("ck_politica_version_fechas", "((vigente_hasta IS NULL) OR (vigente_hasta >= vigente_desde))");
        });
        builder.Property(e => e.PoliticaVersionId).HasColumnName("politica_version_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.PoliticaCreditoId).HasColumnName("politica_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.NumeroVersion).HasColumnName("numero_version").HasColumnType("integer").IsRequired(true);
        builder.Property(e => e.VigenteDesde).HasColumnName("vigente_desde").HasColumnType("timestamp with time zone").IsRequired(true);
        builder.Property(e => e.VigenteHasta).HasColumnName("vigente_hasta").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'BORRADOR'::character varying");
        builder.Property(e => e.CreadaPorUsuarioEmpresaId).HasColumnName("creada_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.AprobadaPorUsuarioEmpresaId).HasColumnName("aprobada_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.Property(e => e.FechaAprobacion).HasColumnName("fecha_aprobacion").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.AprobadaPorUsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_politica_version_aprobador");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.CreadaPorUsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_politica_version_creador");
        builder.HasOne<PoliticaCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.PoliticaCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.PoliticaCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_politica_version_politica");
        builder.HasKey(e => e.PoliticaVersionId).HasName("politica_version_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.PoliticaCreditoId, e.NumeroVersion }).IsUnique().HasDatabaseName("uq_politica_version");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.PoliticaVersionId }).HasName("uq_politica_version_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.PoliticaCreditoId, e.Estado, e.VigenteDesde }).HasDatabaseName("ix_politica_version_vigencia");
    }
}
