using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class EmpresaConfiguration : IEntityTypeConfiguration<Empresa>
{
    public void Configure(EntityTypeBuilder<Empresa> builder)
    {
        builder.ToTable("empresa", "organizacion", table =>
        {
            table.HasCheckConstraint("ck_empresa_estado", "((estado)::text = ANY ((ARRAY['ACTIVA'::character varying, 'INACTIVA'::character varying, 'SUSPENDIDA'::character varying])::text[]))");
        });
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.Codigo).HasColumnName("codigo").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30);
        builder.Property(e => e.NombreLegal).HasColumnName("nombre_legal").HasColumnType("character varying(200)").IsRequired(true).HasMaxLength(200);
        builder.Property(e => e.NombreComercial).HasColumnName("nombre_comercial").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.TipoIdentificacionFiscal).HasColumnName("tipo_identificacion_fiscal").HasColumnType("character varying(20)").IsRequired(false).HasMaxLength(20);
        builder.Property(e => e.IdentificacionFiscal).HasColumnName("identificacion_fiscal").HasColumnType("character varying(30)").IsRequired(false).HasMaxLength(30);
        builder.Property(e => e.PaisCodigo).HasColumnName("pais_codigo").HasColumnType("character(2)").IsRequired(true).HasMaxLength(2).IsFixedLength().HasDefaultValueSql("'EC'::bpchar");
        builder.Property(e => e.MonedaCodigo).HasColumnName("moneda_codigo").HasColumnType("character(3)").IsRequired(true).HasMaxLength(3).IsFixedLength().HasDefaultValueSql("'USD'::bpchar");
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'ACTIVA'::character varying");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.Property(e => e.FechaActualizacion).HasColumnName("fecha_actualizacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.EmpresaId).HasName("empresa_pkey");
        builder.HasIndex(e => e.Codigo).IsUnique().HasDatabaseName("uq_empresa_codigo");
    }
}
