using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class VectorCaracteristicasConfiguration : IEntityTypeConfiguration<VectorCaracteristicas>
{
    public void Configure(EntityTypeBuilder<VectorCaracteristicas> builder)
    {
        builder.ToTable("vector_caracteristicas", "riesgo", table =>
        {
        });
        builder.Property(e => e.VectorCaracteristicasId).HasColumnName("vector_caracteristicas_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.AnalisisId).HasColumnName("analisis_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.IngresoMensual).HasColumnName("ingreso_mensual").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.GastosMensuales).HasColumnName("gastos_mensuales").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.CuotasOtrasDeudas).HasColumnName("cuotas_otras_deudas").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.DeudaTotalActual).HasColumnName("deuda_total_actual").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.MontoSolicitado).HasColumnName("monto_solicitado").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.PlazoMeses).HasColumnName("plazo_meses").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.CuotaEstimada).HasColumnName("cuota_estimada").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.MaxDiasMoraHistorico).HasColumnName("max_dias_mora_historico").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.CreditosActivos).HasColumnName("creditos_activos").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.AntiguedadActividadMeses).HasColumnName("antiguedad_actividad_meses").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.EstabilidadIngresosScore).HasColumnName("estabilidad_ingresos_score").HasColumnType("numeric(12,8)").IsRequired(false);
        builder.Property(e => e.HistorialInternoScore).HasColumnName("historial_interno_score").HasColumnType("numeric(12,8)").IsRequired(false);
        builder.Property(e => e.IngresoDisponible).HasColumnName("ingreso_disponible").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.CapacidadNuevaCuota).HasColumnName("capacidad_nueva_cuota").HasColumnType("numeric(18,2)").IsRequired(false);
        builder.Property(e => e.DeudaSobreIngreso).HasColumnName("deuda_sobre_ingreso").HasColumnType("numeric(12,8)").IsRequired(false);
        builder.Property(e => e.CuotaSobreIngreso).HasColumnName("cuota_sobre_ingreso").HasColumnType("numeric(12,8)").IsRequired(false);
        builder.Property(e => e.MontoSobreIngreso).HasColumnName("monto_sobre_ingreso").HasColumnType("numeric(12,8)").IsRequired(false);
        builder.Property(e => e.VariablesAdicionales).HasColumnName("variables_adicionales").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.ScoreBuro).HasColumnName("score_buro").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.MoraActualMaxDiasBuro).HasColumnName("mora_actual_max_dias_buro").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.TieneProcesosJudiciales).HasColumnName("tiene_procesos_judiciales").HasColumnType("boolean").IsRequired(false);
        builder.Property(e => e.GravedadJudicial).HasColumnName("gravedad_judicial").HasColumnType("character varying(10)").IsRequired(false).HasMaxLength(10);
        builder.Property(e => e.DocumentosSospechosos).HasColumnName("documentos_sospechosos").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.VisitaRecomendacion).HasColumnName("visita_recomendacion").HasColumnType("character varying(30)").IsRequired(false).HasMaxLength(30);
        builder.Property(e => e.FechaSnapshot).HasColumnName("fecha_snapshot").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Analisis>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.AnalisisId }).HasPrincipalKey(e => new { e.EmpresaId, e.AnalisisId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_vector_analisis");
        builder.HasIndex(e => new { e.EmpresaId, e.AnalisisId }).IsUnique().HasDatabaseName("uq_vector_analisis");
        builder.HasKey(e => e.VectorCaracteristicasId).HasName("vector_caracteristicas_pkey");
    }
}
