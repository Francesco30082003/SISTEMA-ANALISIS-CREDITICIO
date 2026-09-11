using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class GastoConfiguration : IEntityTypeConfiguration<Gasto>
{
    public void Configure(EntityTypeBuilder<Gasto> builder)
    {
        builder.ToTable("gasto", "credito", table =>
        {
            table.HasCheckConstraint("ck_gasto_monto", "(monto_mensual >= (0)::numeric)");
        });
        builder.Property(e => e.GastoId).HasColumnName("gasto_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.SolicitudCreditoId).HasColumnName("solicitud_credito_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.TipoGasto).HasColumnName("tipo_gasto").HasColumnType("character varying(50)").IsRequired(true).HasMaxLength(50);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.MontoMensual).HasColumnName("monto_mensual").HasColumnType("numeric(18,2)").IsRequired(true);
        builder.Property(e => e.OrigenDato).HasColumnName("origen_dato").HasColumnType("character varying(30)").IsRequired(false).HasMaxLength(30);
        builder.Property(e => e.Declarado).HasColumnName("declarado").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("true");
        builder.Property(e => e.Verificado).HasColumnName("verificado").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("false");
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<SolicitudCredito>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasPrincipalKey(e => new { e.EmpresaId, e.SolicitudCreditoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_gasto_solicitud");
        builder.HasKey(e => e.GastoId).HasName("gasto_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.SolicitudCreditoId }).HasDatabaseName("ix_gasto_solicitud");
    }
}
