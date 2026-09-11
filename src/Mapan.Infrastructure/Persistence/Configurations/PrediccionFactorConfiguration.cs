using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class PrediccionFactorConfiguration : IEntityTypeConfiguration<PrediccionFactor>
{
    public void Configure(EntityTypeBuilder<PrediccionFactor> builder)
    {
        builder.ToTable("prediccion_factor", "riesgo", table =>
        {
            table.HasCheckConstraint("ck_prediccion_factor_direccion", "((direccion)::text = ANY ((ARRAY['AUMENTA_RIESGO'::character varying, 'REDUCE_RIESGO'::character varying, 'NEUTRO'::character varying])::text[]))");
        });
        builder.Property(e => e.PrediccionFactorId).HasColumnName("prediccion_factor_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.PrediccionId).HasColumnName("prediccion_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.CodigoCaracteristica).HasColumnName("codigo_caracteristica").HasColumnType("character varying(120)").IsRequired(true).HasMaxLength(120);
        builder.Property(e => e.NombreCaracteristica).HasColumnName("nombre_caracteristica").HasColumnType("character varying(200)").IsRequired(false).HasMaxLength(200);
        builder.Property(e => e.ValorNumerico).HasColumnName("valor_numerico").HasColumnType("numeric(24,10)").IsRequired(false);
        builder.Property(e => e.ValorTexto).HasColumnName("valor_texto").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.Contribucion).HasColumnName("contribucion").HasColumnType("numeric(24,10)").IsRequired(true);
        builder.Property(e => e.Direccion).HasColumnName("direccion").HasColumnType("character varying(30)").IsRequired(true).HasMaxLength(30);
        builder.Property(e => e.PosicionImportancia).HasColumnName("posicion_importancia").HasColumnType("integer").IsRequired(false);
        builder.Property(e => e.FechaCreacion).HasColumnName("fecha_creacion").HasColumnType("timestamp with time zone").IsRequired(true).HasDefaultValueSql("now()");
        builder.HasOne<Prediccion>().WithMany().HasForeignKey(e => e.PrediccionId).HasPrincipalKey(e => e.PrediccionId).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_prediccion_factor_prediccion");
        builder.HasKey(e => e.PrediccionFactorId).HasName("prediccion_factor_pkey");
        builder.HasIndex(e => new { e.PrediccionId, e.CodigoCaracteristica }).IsUnique().HasDatabaseName("uq_prediccion_factor");
        builder.HasIndex(e => new { e.PrediccionId, e.PosicionImportancia }).HasDatabaseName("ix_prediccion_factor_prediccion");
    }
}
