using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class RolPermisoConfiguration : IEntityTypeConfiguration<RolPermiso>
{
    public void Configure(EntityTypeBuilder<RolPermiso> builder)
    {
        builder.ToTable("rol_permiso", "seguridad", table =>
        {
        });
        builder.Property(e => e.RolId).HasColumnName("rol_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.PermisoId).HasColumnName("permiso_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.FechaAsignacion).HasColumnName("fecha_asignacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Permiso>().WithMany().HasForeignKey(e => e.PermisoId).HasPrincipalKey(e => e.PermisoId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_rol_permiso_permiso");
        builder.HasOne<Rol>().WithMany().HasForeignKey(e => e.RolId).HasPrincipalKey(e => e.RolId).OnDelete(DeleteBehavior.Cascade).HasConstraintName("fk_rol_permiso_rol");
        builder.HasKey(e => new { e.RolId, e.PermisoId }).HasName("rol_permiso_pkey");
    }
}
