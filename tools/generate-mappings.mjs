import fs from 'node:fs';
const tables=JSON.parse(fs.readFileSync('docs/database/actual-schema.json','utf8'));
const selected=new Set(process.argv.slice(2));
const pascal=s=>s.split('_').map(p=>p[0].toUpperCase()+p.slice(1)).join('');
const literal=s=>JSON.stringify(s);
const names=s=>s.split(',').map(x=>pascal(x.trim()));
const selector=cols=>cols.length===1?`e => e.${cols[0]}`:`e => new { ${cols.map(c=>'e.'+c).join(', ')} }`;
const write=(path,text)=>{fs.mkdirSync(path.slice(0,path.lastIndexOf('/')),{recursive:true});fs.writeFileSync(path,text);};
for(const t of tables.filter(t=>selected.has(t.Schema+'.'+t.Table))) {
 const cls=pascal(t.Table);
 let entity=`namespace Mapan.Domain.Entities;\n\npublic sealed class ${cls}\n{\n`;
 let mapping=`using Mapan.Domain.Entities;\nusing Microsoft.EntityFrameworkCore;\nusing Microsoft.EntityFrameworkCore.Metadata.Builders;\n\nnamespace Mapan.Infrastructure.Persistence.Configurations;\n\npublic sealed class ${cls}Configuration : IEntityTypeConfiguration<${cls}>\n{\n    public void Configure(EntityTypeBuilder<${cls}> builder)\n    {\n        builder.ToTable(${literal(t.Table)}, ${literal(t.Schema)}, table =>\n        {\n`;
 for(const c of t.Constraints??[])if(c.type==='c')mapping+=`            table.HasCheckConstraint(${literal(c.name)}, ${literal(c.definition.replace(/^CHECK \(/,'').slice(0,-1))});\n`;
 mapping+='        });\n';
 for(const c of t.Columns){
  const type=c.type.startsWith('character')||['text','jsonb'].includes(c.type)?'string':c.type==='uuid'?'Guid':c.type==='timestamp with time zone'?'DateTimeOffset':c.type==='date'?'DateOnly':c.type.startsWith('numeric')?'decimal':c.type==='integer'?'int':c.type==='bigint'?'long':c.type==='boolean'?'bool':c.type==='inet'?'System.Net.IPAddress':null;
  if(!type)throw Error('Unsupported '+c.type);
  entity+=`    public ${type==='string'&&c.notNull?'required ':''}${type}${c.notNull?'':'?'} ${pascal(c.name)} { get; set; }\n`;
  mapping+=`        builder.Property(e => e.${pascal(c.name)}).HasColumnName(${literal(c.name)}).HasColumnType(${literal(c.type)}).IsRequired(${c.notNull})`;
  if(c.type.startsWith('character')) {mapping+=`.HasMaxLength(${c.type.match(/\((\d+)\)/)[1]})`;if(c.type.startsWith('character('))mapping+='.IsFixedLength()';}
  if(c.generated)mapping+=`.HasComputedColumnSql(${literal(c.default)}, stored: true)`;
  else if(c.default)mapping+=`.HasDefaultValueSql(${literal(c.default)})`;
  mapping+=';\n';
 }
 for(const c of t.Constraints??[]){
  if(['p','u'].includes(c.type)){
    const rawCols=c.definition.match(/\(([^)]+)\)/)[1];const cols=names(rawCols);
    const principal=tables.some(other=>other.Constraints?.some(f=>f.type==='f'&&f.definition.includes(`REFERENCES ${t.Schema}.${t.Table}(${rawCols})`)));
    mapping+=c.type==='u'&&!principal
      ?`        builder.HasIndex(${selector(cols)}).IsUnique().HasDatabaseName(${literal(c.name)});\n`
      :`        builder.${c.type==='p'?'HasKey':'HasAlternateKey'}(${selector(cols)}).HasName(${literal(c.name)});\n`;
  }
  if(c.type==='f'){
   const m=c.definition.match(/FOREIGN KEY \(([^)]+)\) REFERENCES (\w+)\.(\w+)\(([^)]+)\)(?: ON DELETE (\w+))?/);
   if(!m)throw Error(c.definition);
   if(!selected.has(m[2]+'.'+m[3])&&m[3]!=='empresa')continue;
   mapping+=`        builder.HasOne<${pascal(m[3])}>().WithMany().HasForeignKey(${selector(names(m[1]))}).HasPrincipalKey(${selector(names(m[4]))}).OnDelete(DeleteBehavior.${m[5]==='CASCADE'?'Cascade':'Restrict'}).HasConstraintName(${literal(c.name)});\n`;
  }
 }
 for(const i of t.Indexes??[]){
   if(t.Constraints?.some(c=>c.name===i.name))continue;
   const m=i.definition.match(/USING btree \(([^)]+)\)$/);if(!m)continue;
   const parts=m[1].split(',').map(x=>x.trim());
   mapping+=`        builder.HasIndex(${selector(parts.map(p=>pascal(p.replace(/ DESC| ASC/g,''))))}).HasDatabaseName(${literal(i.name)})`;
   if(i.definition.startsWith('CREATE UNIQUE'))mapping+='.IsUnique()';
   if(parts.some(p=>p.endsWith(' DESC')))mapping+=`.IsDescending(${parts.map(p=>p.endsWith(' DESC')).join(', ')})`;
   mapping+=';\n';
 }
 write(`src/Mapan.Domain/Entities/${cls}.cs`,entity+'}\n');
 write(`src/Mapan.Infrastructure/Persistence/Configurations/${cls}Configuration.cs`,mapping+'    }\n}\n');
 console.log(t.Schema+'.'+t.Table);
}
