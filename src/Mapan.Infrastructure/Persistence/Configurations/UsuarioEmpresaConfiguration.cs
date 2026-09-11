using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class UsuarioEmpresaConfiguration : IEntityTypeConfiguration<UsuarioEmpresa>
{
    public void Configure(EntityTypeBuilder<UsuarioEmpresa> builder)
    {
        builder.ToTable("usuario_empresa", "seguridad", table =>
        {
            table.HasCheckConstraint("ck_usuario_empresa_estado", "((estado)::text = ANY ((ARRAY['ACTIVO'::character varying, 'INACTIVO'::character varying])::text[]))");
        });
        builder.Property(e => e.UsuarioEmpresaId).HasColumnName("usuario_empresa_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.UsuarioId).HasColumnName("usuario_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SucursalPredeterminadaId).HasColumnName("sucursal_predeterminada_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'ACTIVO'::character varying");
        builder.Property(e => e.FechaIngreso).HasColumnName("fecha_ingreso").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).HasPrincipalKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_usuario_empresa_empresa");
        builder.HasOne<Sucursal>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SucursalPredeterminadaId }).HasPrincipalKey(e => new { e.EmpresaId, e.SucursalId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_usuario_empresa_sucursal");
        builder.HasOne<Usuario>().WithMany().HasForeignKey(e => e.UsuarioId).HasPrincipalKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_usuario_empresa_usuario");
        builder.HasIndex(e => new { e.UsuarioId, e.EmpresaId }).IsUnique().HasDatabaseName("uq_usuario_empresa");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).HasName("uq_usuario_empresa_tenant");
        builder.HasKey(e => e.UsuarioEmpresaId).HasName("usuario_empresa_pkey");
        builder.HasIndex(e => e.EmpresaId).HasDatabaseName("ix_usuario_empresa_empresa");
        builder.HasIndex(e => e.UsuarioId).HasDatabaseName("ix_usuario_empresa_usuario");
    }
}
