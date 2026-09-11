using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class SucursalConfiguration : IEntityTypeConfiguration<Sucursal>
{
    public void Configure(EntityTypeBuilder<Sucursal> builder)
    {
        builder.ToTable("sucursal", "organizacion", table =>
        {
            table.HasCheckConstraint("ck_sucursal_estado", "((estado)::text = ANY ((ARRAY['ACTIVA'::character varying, 'INACTIVA'::character varying])::text[]))");
        });
        builder.Property(e => e.SucursalId).HasColumnName("sucursal_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Codigo).HasColumnName("codigo").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasColumnType("character varying(150)").IsRequired(true).HasMaxLength(150);
        builder.Property(e => e.PaisCodigo).HasColumnName("pais_codigo").HasColumnType("character(2)").IsRequired(true).HasMaxLength(2).IsFixedLength().HasDefaultValueSql("'EC'::bpchar");
        builder.Property(e => e.Provincia).HasColumnName("provincia").HasColumnType("character varying(100)").IsRequired(false).HasMaxLength(100);
        builder.Property(e => e.Ciudad).HasColumnName("ciudad").HasColumnType("character varying(100)").IsRequired(false).HasMaxLength(100);
        builder.Property(e => e.Direccion).HasColumnName("direccion").HasColumnType("character varying(300)").IsRequired(false).HasMaxLength(300);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'ACTIVA'::character varying");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).HasPrincipalKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_sucursal_empresa");
        builder.HasKey(e => e.SucursalId).HasName("sucursal_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.Codigo }).IsUnique().HasDatabaseName("uq_sucursal_empresa_codigo");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.SucursalId }).HasName("uq_sucursal_empresa_id");
        builder.HasIndex(e => e.EmpresaId).HasDatabaseName("ix_sucursal_empresa");
    }
}
