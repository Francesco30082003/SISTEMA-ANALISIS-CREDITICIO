using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class AlertaConfiguration : IEntityTypeConfiguration<Alerta>
{
    public void Configure(EntityTypeBuilder<Alerta> builder)
    {
        builder.ToTable("alerta", "riesgo", table =>
        {
            table.HasCheckConstraint("ck_alerta_nivel", "((nivel)::text = ANY ((ARRAY['INFO'::character varying, 'VERDE'::character varying, 'AMARILLO'::character varying, 'ROJO'::character varying])::text[]))");
        });
        builder.Property(e => e.AlertaId).HasColumnName("alerta_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.AnalisisId).HasColumnName("analisis_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ReglaId).HasColumnName("regla_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.Codigo).HasColumnName("codigo").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.Nivel).HasColumnName("nivel").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20);
        builder.Property(e => e.Titulo).HasColumnName("titulo").HasColumnType("character varying(200)").IsRequired(true).HasMaxLength(200);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("text").IsRequired(true);
        builder.Property(e => e.Resuelta).HasColumnName("resuelta").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("false");
        builder.Property(e => e.ResueltaPorUsuarioEmpresaId).HasColumnName("resuelta_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.Property(e => e.FechaResolucion).HasColumnName("fecha_resolucion").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.HasKey(e => e.AlertaId).HasName("alerta_pkey");
        builder.HasOne<Analisis>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.AnalisisId }).HasPrincipalKey(e => new { e.EmpresaId, e.AnalisisId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_alerta_analisis");
        builder.HasOne<Regla>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.ReglaId }).HasPrincipalKey(e => new { e.EmpresaId, e.ReglaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_alerta_regla");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.ResueltaPorUsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_alerta_resuelta_por");
        builder.HasIndex(e => new { e.EmpresaId, e.AnalisisId, e.Nivel }).HasDatabaseName("ix_alerta_analisis");
    }
}
