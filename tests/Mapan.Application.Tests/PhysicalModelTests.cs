using System.Text.Json;
using Mapan.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Mapan.Application.Tests;
public sealed class PhysicalModelTests
{
    [Fact]
    public void ExplicitFalseIsInsertedInsteadOfDatabaseTrueDefault()
    {
        using var db=new MapanDbContext(new DbContextOptionsBuilder<MapanDbContext>().UseNpgsql().Options);
        var property=db.Model.FindEntityType(typeof(Mapan.Domain.Entities.FuenteIngreso))!.FindProperty("EsRecurrente")!;
        Assert.Equal("true",property.GetDefaultValueSql());
        Assert.Equal(ValueGenerated.Never,property.ValueGenerated);
        Assert.Equal(PropertySaveBehavior.Save,property.GetBeforeSaveBehavior());
    }
    [Fact]
    public void EveryMappedColumnMatchesInspectedPostgresTypeAndNullability()
    {
        using var db=new MapanDbContext(new DbContextOptionsBuilder<MapanDbContext>().UseNpgsql().Options);
        using var doc=JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory,"actual-schema.json")));
        foreach(var table in doc.RootElement.EnumerateArray()) {
            var schema=table.GetProperty("Schema").GetString();var name=table.GetProperty("Table").GetString();
            var entity=Assert.Single(db.Model.GetEntityTypes(),e=>e.GetSchema()==schema&&e.GetTableName()==name);
            var store=StoreObjectIdentifier.Table(name!,schema);
            var columns=table.GetProperty("Columns").EnumerateArray().ToArray();
            Assert.Equal(columns.Length,entity.GetProperties().Count());
            foreach(var column in columns) {
                var property=Assert.Single(entity.GetProperties(),p=>p.GetColumnName(store)==column.GetProperty("name").GetString());
                Assert.Equal(column.GetProperty("type").GetString(),property.GetColumnType());
                Assert.Equal(!column.GetProperty("notNull").GetBoolean(),property.IsNullable);
            }
        }
    }
}
