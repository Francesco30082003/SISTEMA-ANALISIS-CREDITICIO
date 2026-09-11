using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class DocumentoConfiguration : IEntityTypeConfiguration<Documento>
{
    public void Configure(EntityTypeBuilder<Documento> builder)
    {
        builder.ToTable("documento", "documentos", table =>
        {
            table.HasCheckConstraint("ck_documento_estado", "((estado)::text = ANY ((ARRAY['ACTIVO'::character varying, 'INACTIVO'::character varying, 'ANULADO'::character varying])::text[]))");
            table.HasCheckConstraint("ck_documento_estado_verificacion", "((estado_verificacion)::text = ANY ((ARRAY['PENDIENTE'::character varying, 'VERIFICADO'::character varying, 'SOSPECHOSO'::character varying, 'INCONSISTENTE'::character varying])::text[]))");
        });
        builder.Property(e => e.DocumentoId).HasColumnName("documento_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ClienteId).HasColumnName("cliente_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.TipoDocumento).HasColumnName("tipo_documento").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasColumnType("character varying(200)").IsRequired(true).HasMaxLength(200);
        builder.Property(e => e.NumeroDocumento).HasColumnName("numero_documento").HasColumnType("character varying(120)").IsRequired(false).HasMaxLength(120);
        builder.Property(e => e.Emisor).HasColumnName("emisor").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.FechaEmision).HasColumnName("fecha_emision").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.FechaVencimiento).HasColumnName("fecha_vencimiento").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30).HasDefaultValueSql("'ACTIVO'::character varying");
        builder.Property(e => e.EstadoVerificacion).HasColumnName("estado_verificacion").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20).HasDefaultValueSql("'PENDIENTE'::character varying");
        builder.Property(e => e.CreadoPorUsuarioEmpresaId).HasColumnName("creado_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(false);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.DocumentoId).HasName("documento_pkey");
        builder.HasOne<Cliente>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.ClienteId }).HasPrincipalKey(e => new { e.EmpresaId, e.ClienteId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_documento_cliente");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.CreadoPorUsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_documento_creado_por");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.DocumentoId }).HasName("uq_documento_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.ClienteId }).HasDatabaseName("ix_documento_cliente");
    }
}
