using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class SnapshotFinancieroConfiguration : IEntityTypeConfiguration<SnapshotFinanciero>
{
    public void Configure(EntityTypeBuilder<SnapshotFinanciero> builder)
    {
        builder.ToTable("snapshot_financiero", "riesgo", table =>
        {
            table.HasCheckConstraint("ck_snapshot_cuotas", "(cuotas_actuales >= (0)::numeric)");
            table.HasCheckConstraint("ck_snapshot_deuda", "(deuda_total_actual >= (0)::numeric)");
            table.HasCheckConstraint("ck_snapshot_factor", "((factor_capacidad >= (0)::numeric) AND (factor_capacidad <= (1)::numeric))");
            table.HasCheckConstraint("ck_snapshot_gastos", "(gasto_total_mensual >= (0)::numeric)");
            table.HasCheckConstraint("ck_snapshot_ingresos", "(ingreso_total_mensual >= (0)::numeric)");
        });
        builder.Property(e => e.SnapshotFinancieroId).HasColumnName("snapshot_financiero_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.AnalisisId).HasColumnName("analisis_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.IngresoTotalMensual).HasColumnName("ingreso_total_mensual").HasColumnType("numeric(18,2)").IsRequired(true);
        builder.Property(e => e.GastoTotalMensual).HasColumnName("gasto_total_mensual").HasColumnType("numeric(18,2)").IsRequired(true);
        builder.Property(e => e.IngresoDisponible).HasColumnName("ingreso_disponible").HasColumnType("numeric(18,2)").IsRequired(true);
        builder.Property(e => e.FactorCapacidad).HasColumnName("factor_capacidad").HasColumnType("numeric(9,6)").IsRequired(true);
        builder.Property(e => e.CapacidadNuevaCuota).HasColumnName("capacidad_nueva_cuota").HasColumnType("numeric(18,2)").IsRequired(true);
        builder.Property(e => e.DeudaTotalActual).HasColumnName("deuda_total_actual").HasColumnType("numeric(18,2)").IsRequired(true).HasDefaultValueSql("0");
        builder.Property(e => e.CuotasActuales).HasColumnName("cuotas_actuales").HasColumnType("numeric(18,2)").IsRequired(true).HasDefaultValueSql("0");
        builder.Property(e => e.CuotaNuevaEstimada).HasColumnName("cuota_nueva_estimada").HasColumnType("numeric(18,2)").IsRequired(true);
        builder.Property(e => e.RatioEndeudamientoActual).HasColumnName("ratio_endeudamiento_actual").HasColumnType("numeric(12,8)").IsRequired(false);
        builder.Property(e => e.RatioEndeudamientoPost).HasColumnName("ratio_endeudamiento_post").HasColumnType("numeric(12,8)").IsRequired(false);
        builder.Property(e => e.CuotaCompatible).HasColumnName("cuota_compatible").HasColumnType("boolean").IsRequired(true);
        builder.Property(e => e.FechaCalculo).HasColumnName("fecha_calculo").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Analisis>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.AnalisisId }).HasPrincipalKey(e => new { e.EmpresaId, e.AnalisisId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_snapshot_analisis");
        builder.HasKey(e => e.SnapshotFinancieroId).HasName("snapshot_financiero_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.AnalisisId }).IsUnique().HasDatabaseName("uq_snapshot_analisis");
    }
}
