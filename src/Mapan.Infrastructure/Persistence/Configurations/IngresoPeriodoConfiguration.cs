using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class IngresoPeriodoConfiguration : IEntityTypeConfiguration<IngresoPeriodo>
{
    public void Configure(EntityTypeBuilder<IngresoPeriodo> builder)
    {
        builder.ToTable("ingreso_periodo", "credito", table =>
        {
            table.HasCheckConstraint("ck_ingreso_periodo_fechas", "(periodo_fin >= periodo_inicio)");
            table.HasCheckConstraint("ck_ingreso_periodo_monto", "(monto_neto >= (0)::numeric)");
        });
        builder.Property(e => e.IngresoPeriodoId).HasColumnName("ingreso_periodo_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.FuenteIngresoId).HasColumnName("fuente_ingreso_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.PeriodoInicio).HasColumnName("periodo_inicio").HasColumnType("date").IsRequired(true);
        builder.Property(e => e.PeriodoFin).HasColumnName("periodo_fin").HasColumnType("date").IsRequired(true);
        builder.Property(e => e.MontoBruto).HasColumnName("monto_bruto").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.MontoNeto).HasColumnName("monto_neto").HasColumnType("numeric(18,2)").IsRequired(true);
        builder.Property(e => e.OrigenDato).HasColumnName("origen_dato").HasColumnType("character varying(30)").IsRequired(false).HasMaxLength(30);
        builder.Property(e => e.Observacion).HasColumnName("observacion").HasColumnType("character varying(300)").IsRequired(false).HasMaxLength(300);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<FuenteIngreso>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.FuenteIngresoId }).HasPrincipalKey(e => new { e.EmpresaId, e.FuenteIngresoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_ingreso_periodo_fuente");
        builder.HasKey(e => e.IngresoPeriodoId).HasName("ingreso_periodo_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.FuenteIngresoId, e.PeriodoInicio }).HasDatabaseName("ix_ingreso_periodo_fuente");
    }
}
