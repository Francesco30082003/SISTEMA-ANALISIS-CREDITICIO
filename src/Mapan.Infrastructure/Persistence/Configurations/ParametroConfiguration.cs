using Mapan.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Mapan.Infrastructure.Persistence.Configurations;

public sealed class ParametroConfiguration : IEntityTypeConfiguration<Parametro>
{
    public void Configure(EntityTypeBuilder<Parametro> builder)
    {
        builder.ToTable("parametro", "politica", table =>
        {
            table.HasCheckConstraint("ck_parametro_tipo", "((tipo_dato)::text = ANY ((ARRAY['TEXTO'::character varying, 'NUMERO'::character varying, 'BOOLEANO'::character varying, 'FECHA'::character varying, 'JSON'::character varying])::text[]))");
        });
        builder.Property(e => e.ParametroId).HasColumnName("parametro_id").HasColumnType("uuid").IsRequired(true).HasDefaultValueSql("gen_random_uuid()");
        builder.Property(e => e.EmpresaId).HasColumnName("empresa_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.PoliticaVersionId).HasColumnName("politica_version_id").HasColumnType("uuid").IsRequired(true);
        builder.Property(e => e.Codigo).HasColumnName("codigo").HasColumnType("character varying(100)").IsRequired(true).HasMaxLength(100);
        builder.Property(e => e.Nombre).HasColumnName("nombre").HasColumnType("character varying(180)").IsRequired(true).HasMaxLength(180);
        builder.Property(e => e.TipoDato).HasColumnName("tipo_dato").HasColumnType("character varying(20)").IsRequired(true).HasMaxLength(20);
        builder.Property(e => e.ValorTexto).HasColumnName("valor_texto").HasColumnType("text").IsRequired(false);
        builder.Property(e => e.ValorNumerico).HasColumnName("valor_numerico").HasColumnType("numeric(24,8)").IsRequired(false);
        builder.Property(e => e.ValorBooleano).HasColumnName("valor_booleano").HasColumnType("boolean").IsRequired(false);
        builder.Property(e => e.ValorFecha).HasColumnName("valor_fecha").HasColumnType("date").IsRequired(false);
        builder.Property(e => e.ValorJson).HasColumnName("valor_json").HasColumnType("jsonb").IsRequired(false);
        builder.Property(e => e.Descripcion).HasColumnName("descripcion").HasColumnType("text").IsRequired(false);
        builder.HasOne<PoliticaVersion>().WithMany().HasForeignKey(e => new { e.EmpresaId, e.PoliticaVersionId }).HasPrincipalKey(e => new { e.EmpresaId, e.PoliticaVersionId }).OnDelete(DeleteBehavior.Restrict).HasConstraintName("fk_parametro_politica_version");
        builder.HasKey(e => e.ParametroId).HasName("parametro_pkey");
        builder.HasIndex(e => new { e.EmpresaId, e.PoliticaVersionId, e.Codigo }).IsUnique().HasDatabaseName("uq_parametro_version_codigo");
        builder.HasIndex(e => new { e.EmpresaId, e.PoliticaVersionId }).HasDatabaseName("ix_parametro_version");
    }
}
