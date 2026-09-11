using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class SolicitudCreditoConfiguration : IEntityTypeConfiguration<SolicitudCredito>
{
    public void Configure(EntityTypeBuilder<SolicitudCredito> builder)
    {
        builder.ToTable("solicitud_credito", "credito", table =>
        {
            table.HasCheckConstraint("ck_solicitud_estado", "((estado)::text = ANY ((ARRAY['BORRADOR'::character varying, 'DOCUMENTACION'::character varying, 'VALIDACION'::character varying, 'ANALISIS'::character varying, 'REVISION'::character varying, 'APROBADA'::character varying, 'RECHAZADA'::character varying, 'CANCELADA'::character varying])::text[]))");
            table.HasCheckConstraint("ck_solicitud_monto", "(monto_solicitado > (0)::numeric)");
            table.HasCheckConstraint("ck_solicitud_plazo", "(plazo_solicitado_meses > 0)");
        });
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SucursalId).HasColumnName("sucursal_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ClienteId).HasColumnName("cliente_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.ProductoCreditoId).HasColumnName("producto_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.NumeroSolicitud).HasColumnName("numero_solicitud").HasColumnType("character varying(50)").IsRequired(true).HasMaxLength(50);
        builder.Property(e => e.MontoSolicitado).HasColumnName("monto_solicitado").HasColumnType("numeric(18,2)").IsRequired(true);
        builder.Property(e => e.PlazoSolicitadoMeses).HasColumnName("plazo_solicitado_meses").HasColumnType("integer").IsRequired(true);
        builder.Property(e => e.TasaInteresAnualPct).HasColumnName("tasa_interes_anual_pct").HasColumnType("numeric(9,4)").IsRequired(false);
        builder.Property(e => e.CapitalMensualEstimado).HasColumnName("capital_mensual_estimado").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.InteresMensualEstimado).HasColumnName("interes_mensual_estimado").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.CuotaEstimada).HasColumnName("cuota_estimada").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.InteresTotalEstimado).HasColumnName("interes_total_estimado").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.TotalAPagarEstimado).HasColumnName("total_a_pagar_estimado").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.DestinoCredito).HasColumnName("destino_credito").HasColumnType("character varying(500)").IsRequired(false).HasMaxLength(500);
        builder.Property(e => e.Estado).HasColumnName("estado").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30).HasDefaultValueSql("'BORRADOR'::character varying");
        builder.Property(e => e.CreadoPorUsuarioEmpresaId).HasColumnName("creado_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.Property(e => e.FechaEnvio).HasColumnName("fecha_envio").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.Property(e => e.FechaFinalizacion).HasColumnName("fecha_finalizacion").HasColumnType("timestamp with time zone").IsRequired(false);
        builder.HasOne<Cliente>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.ClienteId }).HasPrincipalKey(e => new { e.EmpresaId, e.ClienteId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_solicitud_cliente");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.CreadoPorUsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_solicitud_creado_por");
        builder.HasOne<Empresa>().WithMany().HasForeignKey(e => e.EmpresaId).HasPrincipalKey(e => e.EmpresaId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_solicitud_empresa");
        builder.HasOne<ProductoCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.ProductoCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.ProductoCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_solicitud_producto");
        builder.HasOne<Sucursal>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SucursalId }).HasPrincipalKey(e => new { e.EmpresaId, e.SucursalId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_solicitud_sucursal");
        builder.HasKey(e => e.SolicitudCreditoId).HasName("solicitud_credito_pkey");
        builder.HasAlternateKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasName("uq_solicitud_empresa_id");
        builder.HasIndex(e => new { e.EmpresaId, e.NumeroSolicitud }).IsUnique().HasDatabaseName("uq_solicitud_empresa_numero");
        builder.HasIndex(e => new { e.EmpresaId, e.ClienteId }).HasDatabaseName("ix_solicitud_cliente");
        builder.HasIndex(e => e.EmpresaId).HasDatabaseName("ix_solicitud_empresa");
        builder.HasIndex(e => new { e.EmpresaId, e.Estado }).HasDatabaseName("ix_solicitud_estado");
        builder.HasIndex(e => new { e.EmpresaId, e.FechaCreacion }).HasDatabaseName("ix_solicitud_fecha").IsDescending(false, true);
        builder.HasIndex(e => new { e.EmpresaId, e.ProductoCreditoId }).HasDatabaseName("ix_solicitud_producto");
    }
}
