using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class EmpresaProveedorConfiguration : IEntityTypeConfiguration<EmpresaProveedor>
{
    public void Configure(EntityTypeBuilder<EmpresaProveedor> builder)
    {
        builder.ToTable("empresa_proveedor", "integracion", table =>
        {
            table.HasCheckConstraint("ck_empresa_proveedor_ambiente", "((ambiente)::text = ANY ((ARRAY['PRUEBAS'::character varying, 'PRODUCCION'::character varying])::text[]))");
        });
        builder.Property(e => e.EmpresaProveedorId).HasColumnName("empresa_proveedor_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ProveedorId).HasColumnName("proveedor_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Ambiente).HasColumnName("ambiente").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30).HasDefaultValueSql("'PRUEBAS'::character varying");
        builder.Property(e => e.ConfiguracionJson).HasColumnName("configuracion_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.SecretoRef).HasColumnName("secreto_ref").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.Activo).HasColumnName("activo").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.EmpresaProveedorId).HasName("empresa_proveedor_pkey");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).HasPrincipalKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_empresa_proveedor_empresa");
        builder.HasOne<Proveedor>().WithMany().HasForeignKey(e => e.ProveedorId).HasPrincipalKey(e => e.ProveedorId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_empresa_proveedor_proveedor");
        builder.HasIndex(e => new { e.EmpresaId, e.ProveedorId }).IsUnique().HasDatabaseName("uq_empresa_proveedor");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.EmpresaProveedorId }).HasName("uq_empresa_proveedor_empresa_id");
    }
}
