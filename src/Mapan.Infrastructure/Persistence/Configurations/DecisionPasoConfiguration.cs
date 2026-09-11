using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class DecisionPasoConfiguration : IEntityTypeConfiguration<DecisionPaso>
{
    public void Configure(EntityTypeBuilder<DecisionPaso> builder)
    {
        builder.ToTable("decision_paso", "flujo", table =>
        {
            table.HasCheckConstraint("ck_decision", "((decision)::text = ANY ((ARRAY['APROBAR'::character varying, 'RECHAZAR'::character varying, 'DEVOLVER'::character varying, 'ABSTENERSE'::character varying])::text[]))");
        });
        builder.Property(e => e.DecisionPasoId).HasColumnName("decision_paso_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.AprobacionPasoId).HasColumnName("aprobacion_paso_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.UsuarioEmpresaId).HasColumnName("usuario_empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Decision).HasColumnName("decision").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30);
        builder.Property(e => e.Comentario).HasColumnName("comentario").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.FechaDecision).HasColumnName("fecha_decision").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.DecisionPasoId).HasName("decision_paso_pkey");
        builder.HasOne<AprobacionPaso>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.AprobacionPasoId }).HasPrincipalKey(e => new { e.EmpresaId, e.AprobacionPasoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_decision_paso");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_decision_usuario");
        builder.HasIndex(e => new { e.EmpresaId, e.AprobacionPasoId, e.UsuarioEmpresaId }).IsUnique().HasDatabaseName("uq_decision_usuario_paso");
        builder.HasIndex(e => new { e.EmpresaId, e.AprobacionPasoId }).HasDatabaseName("ix_decision_paso");
    }
}
