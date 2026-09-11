using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        builder.ToTable("usuario", "seguridad", table =>
        {
            table.HasCheckConstraint("ck_usuario_estado", "((estado)::text = ANY ((ARRAY['ACTIVO'::character varying, 'INACTIVO'::character varying, 'BLOQUEADO'::character varying])::text[]))");
        });
        builder.Property(e => e.UsuarioId).HasColumnName("usuario_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.NombreUsuario).HasColumnName("nombre_usuario").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.Correo).HasColumnName("correo").HasColumnType("character varying(200)").IsRequired(true).HasMaxLength(200);
        builder.Property(e => e.PasswordHash).HasColumnName("password_hash").HasColumnType("text").IsRequired(true);
        builder.Property(e => e.Nombres).HasColumnName("nombres").HasColumnType("character varying(120)").IsRequired(true).HasMaxLength(120);
        builder.Property(e => e.Apellidos).HasColumnName("apellidos").HasColumnType("character varying(120)").IsRequired(true).HasMaxLength(120);
        builder.Property(e => e.MfaHabilitado).HasColumnName("mfa_habilitado").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("false");
        builder.Property(e => e.IntentosFallidos).HasColumnName("intentos_fallidos").HasColumnType("integer").IsRequired(true).HasDefaultValueSql("0");
        builder.Property(e => e.BloqueadoHasta).HasColumnName("bloqueado_hasta").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.UltimoAcceso).HasColumnName("ultimo_acceso").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'ACTIVO'::character varying");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.Property(e => e.FechaActualizacion).HasColumnName("fecha_actualizacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasIndex(e => e.Correo).IsUnique().HasDatabaseName("uq_usuario_correo");
        builder.HasIndex(e => e.NombreUsuario).IsUnique().HasDatabaseName("uq_usuario_nombre");
        builder.HasKey(e => e.UsuarioId).HasName("usuario_pkey");
    }
}
