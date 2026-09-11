using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class DesempenoCreditoConfiguration : IEntityTypeConfiguration<DesempenoCredito>
{
    public void Configure(EntityTypeBuilder<DesempenoCredito> builder)
    {
        builder.ToTable("desempeno_credito", "credito", table =>
        {
            table.HasCheckConstraint("ck_desempeno_cuota", "(cuota_exigible >= (0)::numeric)");
            table.HasCheckConstraint("ck_desempeno_mora", "(dias_mora >= 0)");
            table.HasCheckConstraint("ck_desempeno_pago", "(monto_pagado_periodo >= (0)::numeric)");
            table.HasCheckConstraint("ck_desempeno_saldo", "(saldo_capital >= (0)::numeric)");
        });
        builder.Property(e => e.DesempenoCreditoId).HasColumnName("desempeno_credito_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.PrestamoId).HasColumnName("prestamo_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.FechaCorte).HasColumnName("fecha_corte").HasColumnType("date").IsRequired(true);
        builder.Property(e => e.SaldoCapital).HasColumnName("saldo_capital").HasColumnType("numeric(18,2)").IsRequired(true).HasDefaultValueSql("0");
        builder.Property(e => e.CuotaExigible).HasColumnName("cuota_exigible").HasColumnType("numeric(18,2)").IsRequired(true).HasDefaultValueSql("0");
        builder.Property(e => e.MontoPagadoPeriodo).HasColumnName("monto_pagado_periodo").HasColumnType("numeric(18,2)").IsRequired(true).HasDefaultValueSql("0");
        builder.Property(e => e.DiasMora).HasColumnName("dias_mora").HasColumnType("integer").IsRequired(true).HasDefaultValueSql("0");
        builder.Property(e => e.Mora15).HasColumnName("mora_15").HasColumnType("boolean").IsRequired(false).HasComputedColumnSql("(dias_mora >= 15)", stored: true);
        builder.Property(e => e.Mora30).HasColumnName("mora_30").HasColumnType("boolean").IsRequired(false).HasComputedColumnSql("(dias_mora >= 30)", stored: true);
        builder.Property(e => e.Mora60).HasColumnName("mora_60").HasColumnType("boolean").IsRequired(false).HasComputedColumnSql("(dias_mora >= 60)", stored: true);
        builder.Property(e => e.Mora90).HasColumnName("mora_90").HasColumnType("boolean").IsRequired(false).HasComputedColumnSql("(dias_mora >= 90)", stored: true);
        builder.Property(e => e.Refinanciado).HasColumnName("refinanciado").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("false");
        builder.Property(e => e.Reestructurado).HasColumnName("reestructurado").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("false");
        builder.Property(e => e.Castigado).HasColumnName("castigado").HasColumnType("boolean").IsRequired(true).HasDefaultValueSql("false");
        builder.Property(e => e.EstadoCartera).HasColumnName("estado_cartera").HasColumnType("character varying(60)").IsRequired(false).HasMaxLength(60);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasKey(e => e.DesempenoCreditoId).HasName("desempeno_credito_pkey");
        builder.HasOne<Prestamo>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.PrestamoId }).HasPrincipalKey(e => new { e.EmpresaId, e.PrestamoId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_desempeno_prestamo");
        builder.HasIndex(e => new { e.EmpresaId, e.PrestamoId, e.FechaCorte }).IsUnique().HasDatabaseName("uq_desempeno_periodo");
        builder.HasIndex(e => new { e.EmpresaId, e.PrestamoId, e.FechaCorte }).HasDatabaseName("ix_desempeno_prestamo_fecha").IsDescending(false, false, true);
    }
}
