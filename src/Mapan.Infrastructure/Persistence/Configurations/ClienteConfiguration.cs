using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class ClienteConfiguration : IEntityTypeConfiguration<Cliente>
{
    public void Configure(EntityTypeBuilder<Cliente> builder)
    {
        builder.ToTable("cliente", "credito", table =>
        {
            table.HasCheckConstraint("ck_cliente_estado", "((estado)::text = ANY ((ARRAY['ACTIVO'::character varying, 'INACTIVO'::character varying])::text[]))");
            table.HasCheckConstraint("ck_cliente_tipo_persona", "((tipo_persona)::text = ANY ((ARRAY['NATURAL'::character varying, 'JURIDICA'::character varying])::text[]))");
        });
        builder.Property(e => e.ClienteId).HasColumnName("cliente_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.TipoPersona).HasColumnName("tipo_persona").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20);
        builder.Property(e => e.TipoIdentificacion).HasColumnName("tipo_identificacion").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20);
        builder.Property(e => e.NumeroIdentificacion).HasColumnName("numero_identificacion").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30);
        builder.Property(e => e.Nombres).HasColumnName("nombres").HasColumnType("character varying(150)").IsRequired(false).HasMaxLength(150);
        builder.Property(e => e.Apellidos).HasColumnName("apellidos").HasColumnType("character varying(150)").IsRequired(false).HasMaxLength(150);
        builder.Property(e => e.RazonSocial).HasColumnName("razon_social").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.FechaNacimiento).HasColumnName("fecha_nacimiento").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.Telefono).HasColumnName("telefono").HasColumnType("character varying(30)").IsRequired(false).HasMaxLength(30);
        builder.Property(e => e.Correo).HasColumnName("correo").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'ACTIVO'::character varying");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.Property(e => e.FechaActualizacion).HasColumnName("fecha_actualizacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.ClienteId).HasName("cliente_pkey");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).HasPrincipalKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_cliente_empresa");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.ClienteId }).HasName("uq_cliente_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.TipoIdentificacion, e.NumeroIdentificacion }).IsUnique().HasDatabaseName("uq_cliente_empresa_identificacion");
        builder.HasIndex(e => e.EmpresaId).HasDatabaseName("ix_cliente_empresa");
        builder.HasIndex(e => new { e.EmpresaId, e.NumeroIdentificacion }).HasDatabaseName("ix_cliente_identificacion");
    }
}
