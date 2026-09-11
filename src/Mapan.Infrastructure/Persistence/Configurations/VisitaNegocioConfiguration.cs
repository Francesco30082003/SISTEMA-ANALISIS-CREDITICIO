using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class VisitaNegocioConfiguration : IEntityTypeConfiguration<VisitaNegocio>
{
    public void Configure(EntityTypeBuilder<VisitaNegocio> builder)
    {
        builder.ToTable("visita_negocio", "credito", table =>
        {
            table.HasCheckConstraint("ck_visita_negocio_recomendacion", "((recomendacion)::text = ANY ((ARRAY['FAVORABLE'::character varying, 'FAVORABLE_CON_OBSERVACIONES'::character varying, 'DESFAVORABLE'::character varying])::text[]))");
        });
        builder.Property(e => e.VisitaNegocioId).HasColumnName("visita_negocio_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.FechaVisita).HasColumnName("fecha_visita").HasColumnType("date").IsRequired(true);
        builder.Property(e => e.DireccionObservada).HasColumnName("direccion_observada").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.Latitud).HasColumnName("latitud").HasColumnType("numeric(9,6)").IsRequired(false);
        builder.Property(e => e.Longitud).HasColumnName("longitud").HasColumnType("numeric(9,6)").IsRequired(false);
        builder.Property(e => e.NegocioExiste).HasColumnName("negocio_existe").HasColumnType("boolean").IsRequired(true);
        builder.Property(e => e.TipoNegocioObservado).HasColumnName("tipo_negocio_observado").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.TiempoFuncionamientoObservado).HasColumnName("tiempo_funcionamiento_observado").HasColumnType("character varying(100)").IsRequired(false).HasMaxLength(100);
        builder.Property(e => e.NumeroEmpleadosObservado).HasColumnName("numero_empleados_observado").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.InventarioEstimado).HasColumnName("inventario_estimado").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.IngresoMensualEstimadoObservado).HasColumnName("ingreso_mensual_estimado_observado").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.Observaciones).HasColumnName("observaciones").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.Recomendacion).HasColumnName("recomendacion").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30);
        builder.Property(e => e.RealizadaPorUsuarioEmpresaId).HasColumnName("realizada_por_usuario_empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.VisitaNegocioId).HasName("visita_negocio_pkey");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_visita_negocio_solicitud");
        builder.HasOne<UsuarioEmpresa>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.RealizadaPorUsuarioEmpresaId }).HasPrincipalKey(e => new { e.EmpresaId, e.UsuarioEmpresaId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_visita_negocio_usuario");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId, e.FechaCreacion }).HasDatabaseName("ix_visita_negocio_solicitud").IsDescending(false, false, true);
    }
}
