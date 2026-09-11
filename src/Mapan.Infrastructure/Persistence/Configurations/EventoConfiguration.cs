using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class EventoConfiguration : IEntityTypeConfiguration<Evento>
{
    public void Configure(EntityTypeBuilder<Evento> builder)
    {
        builder.ToTable("evento", "auditoria", table =>
        {
        });
        builder.Property(e => e.EventoId).HasColumnName("evento_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.UsuarioId).HasColumnName("usuario_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.UsuarioEmpresaId).HasColumnName("usuario_empresa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasColumnType("character varying(100)").IsRequired(false).HasMaxLength(100);
        builder.Property(e => e.Origen).HasColumnName("origen").HasColumnType("character varying(60)").IsRequired(true).HasMaxLength(60).HasDefaultValueSql("'API'::character varying");
        builder.Property(e => e.EntidadTipo).HasColumnName("entidad_tipo").HasColumnType("character varying(120)").IsRequired(true).HasMaxLength(120);
        builder.Property(e => e.EntidadId).HasColumnName("entidad_id").HasColumnType("character varying(120)").IsRequired(false).HasMaxLength(120);
        builder.Property(e => e.Accion).HasColumnName("accion").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.DatosAnteriores).HasColumnName("datos_anteriores").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.DatosNuevos).HasColumnName("datos_nuevos").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.DetalleJson).HasColumnName("detalle_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.Exitoso).HasColumnName("exitoso").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.DireccionIp).HasColumnName("direccion_ip").HasColumnType("inet").IsRequired(false);
        builder.Property(e => e.UserAgent).HasColumnName("user_agent").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.FechaEvento).HasColumnName("fecha_evento").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.EventoId).HasName("evento_pkey");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).HasPrincipalKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_auditoria_empresa");
        builder.HasOne<Usuario>().WithMany().HasForeignKey(e => e.UsuarioId).HasPrincipalKey(e => e.UsuarioId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_auditoria_usuario");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => e.UsuarioEmpresaId).HasPrincipalKey(e => e.UsuarioEmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_auditoria_usuario_empresa");
        builder.HasIndex(e => e.CorrelationId).HasDatabaseName("ix_auditoria_correlation");
        builder.HasIndex(e => new { e.EmpresaId, e.FechaEvento }).HasDatabaseName("ix_auditoria_empresa_fecha").IsDescending(false, true);
        builder.HasIndex(e => new { e.EntidadTipo, e.EntidadId, e.FechaEvento }).HasDatabaseName("ix_auditoria_entidad").IsDescending(false, false, true);
    }
}
