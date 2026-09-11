using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class UsuarioEmpresaRolConfiguration : IEntityTypeConfiguration<UsuarioEmpresaRol>
{
    public void Configure(EntityTypeBuilder<UsuarioEmpresaRol> builder)
    {
        builder.ToTable("usuario_empresa_rol", "seguridad", table =>
        {
        });
        builder.Property(e => e.UsuarioEmpresaId).HasColumnName("usuario_empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.RolId).HasColumnName("rol_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.FechaAsignacion).HasColumnName("fecha_asignacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Rol>().WithMany().HasForeignKey(e => e.RolId).HasPrincipalKey(e => e.RolId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_usuario_empresa_rol_rol");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => e.UsuarioEmpresaId).HasPrincipalKey(e => e.UsuarioEmpresaId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_usuario_empresa_rol_usuario");
        builder.HasKey(e => new { e.UsuarioEmpresaId, e.RolId }).HasName("usuario_empresa_rol_pkey");
    }
}
