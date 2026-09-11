using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class RolConfiguration : IEntityTypeConfiguration<Rol>
{
    public void Configure(EntityTypeBuilder<Rol> builder)
    {
        builder.ToTable("rol", "seguridad", table =>
        {
        });
        builder.Property(e => e.RolId).HasColumnName("rol_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Codigo).HasColumnName("codigo").HasColumnType("character varying(50)").IsRequired(true).HasMaxLength(50);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("character varying(300)").IsRequired(false).HasMaxLength(300);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'ACTIVO'::character varying");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).HasPrincipalKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_rol_empresa");
        builder.HasKey(e => e.RolId).HasName("rol_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.Codigo }).IsUnique().HasDatabaseName("uq_rol_empresa_codigo");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.RolId }).HasName("uq_rol_empresa_id");
        builder.HasIndex(e => e.EmpresaId).HasDatabaseName("ix_rol_empresa");
    }
}
