using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class AprobacionConfiguration : IEntityTypeConfiguration<Aprobacion>
{
    public void Configure(EntityTypeBuilder<Aprobacion> builder)
    {
        builder.ToTable("aprobacion", "flujo", table =>
        {
            table.HasCheckConstraint("ck_aprobacion_estado", "((estado)::text = ANY ((ARRAY['PENDIENTE'::character varying, 'EN_CURSO'::character varying, 'APROBADA'::character varying, 'RECHAZADA'::character varying, 'DEVUELTA'::character varying, 'CANCELADA'::character varying])::text[]))");
        });
        builder.Property(e => e.AprobacionId).HasColumnName("aprobacion_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.RecomendacionId).HasColumnName("recomendacion_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.RutaAprobacionId).HasColumnName("ruta_aprobacion_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30).HasDefaultValueSql("'PENDIENTE'::character varying");
        builder.Property(e => e.CreadaPorUsuarioEmpresaId).HasColumnName("creada_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.FechaInicio).HasColumnName("fecha_inicio").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.Property(e => e.FechaFin).HasColumnName("fecha_fin").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.HasKey(e => e.AprobacionId).HasName("aprobacion_pkey");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.CreadaPorUsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_aprobacion_creada_por");
        builder.HasOne<Recomendacion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.RecomendacionId }).HasPrincipalKey(e => new { e.EmpresaId, e.RecomendacionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_aprobacion_recomendacion");
        builder.HasOne<RutaAprobacion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.RutaAprobacionId }).HasPrincipalKey(e => new { e.EmpresaId, e.RutaAprobacionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_aprobacion_ruta");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.AprobacionId }).HasName("uq_aprobacion_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.RecomendacionId }).HasDatabaseName("ix_aprobacion_recomendacion");
    }
}
