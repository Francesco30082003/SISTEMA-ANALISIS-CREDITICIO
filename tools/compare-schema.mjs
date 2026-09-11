import fs from 'node:fs';
const actual = JSON.parse(fs.readFileSync('docs/database/actual-schema.json','utf8'));
const sql = fs.readFileSync('docs/PMCORP_MAPAN_MODELO_FISICO_COMPLETO_CODEX.sql','utf8').replace(/--[^\n]*/g,'');
const diffs = [];
const documented = new Map();
for (const m of sql.matchAll(/CREATE TABLE\s+(\w+)\.(\w+)\s*\(/gi)) {
  let depth=1, quote=false, end=m.index+m[0].length;
  for (;end<sql.length && depth;end++) {
    if(sql[end]==="'") { if(quote && sql[end+1]==="'") {end++;continue;} quote=!quote; }
    if(!quote) { if(sql[end]==='(')depth++; if(sql[end]===')')depth--; }
  }
  const body=sql.slice(m.index+m[0].length,end-1);
  const parts=[]; let start=0;depth=0;quote=false;
  for(let i=0;i<body.length;i++) {
    if(body[i]==="'")quote=!quote;
    if(!quote) {if(body[i]==='(')depth++;if(body[i]===')')depth--;if(body[i]===','&&depth===0){parts.push(body.slice(start,i).trim());start=i+1;}}
  }
  parts.push(body.slice(start).trim());
  documented.set(`${m[1]}.${m[2]}`,parts);
}
const norm=t=>t.toLowerCase().replace(/\s+/g,' ').replace(/varchar/g,'character varying').replace(/\bchar\(/g,'character(').replace(/timestamptz/g,'timestamp with time zone').replace(/\bint\b/g,'integer').replace(/,\s+/g,',');
for(const table of actual){
  const name=`${table.Schema}.${table.Table}`, parts=documented.get(name);
  if(!parts){diffs.push(`${name}: tabla ausente en documentación.`);continue;}
  const columns=new Map(parts.filter(p=>! /^(CONSTRAINT|PRIMARY|UNIQUE|CHECK|FOREIGN)\b/i.test(p)).map(p=>[p.match(/^\w+/)?.[0],p]));
  for(const c of table.Columns){
    const doc=columns.get(c.name);
    if(!doc){diffs.push(`${name}.${c.name}: existe en PostgreSQL, ausente en documentación.`);continue;}
    const type=doc.match(/^\w+\s+([a-z]+(?:\s*\([^)]*\))?)/i)?.[1];
    if(norm(type)!==norm(c.type))diffs.push(`${name}.${c.name}: tipo documentado ${type}; real ${c.type}.`);
    const required=/NOT NULL|PRIMARY KEY/i.test(doc)||parts.some(p=>/^PRIMARY KEY/i.test(p)&&p.includes(c.name));
    if(required!==c.notNull)diffs.push(`${name}.${c.name}: nulabilidad distinta (NOT NULL real=${c.notNull}).`);
  }
  for(const nameColumn of columns.keys())if(!table.Columns.some(c=>c.name===nameColumn))diffs.push(`${name}.${nameColumn}: documentada pero NO existe en PostgreSQL.`);
  const docConstraints=parts.filter(p=>/^CONSTRAINT/i.test(p)).map(p=>p.match(/^CONSTRAINT\s+(\w+)/i)[1]);
  for(const c of docConstraints)if(!table.Constraints?.some(a=>a.name===c))diffs.push(`${name}: constraint documentada ${c} ausente.`);
}
for(const name of documented.keys())if(!actual.some(t=>`${t.Schema}.${t.Table}`===name))diffs.push(`${name}: tabla documentada ausente.`);
fs.writeFileSync('docs/database/SCHEMA_COMPARISON.md',`# Comparación del esquema\n\nInspección directa de ${actual.length} tablas mediante catálogos pg_catalog, en transacción READ ONLY con rollback.\n\nFuente documental: ../PMCORP_MAPAN_MODELO_FISICO_COMPLETO_CODEX.sql. No se ejecutó DDL.\n\nComparación automatizada de tablas, columnas, tipos, nulabilidad y presencia de constraints nombradas. Las definiciones reales de PK/FK/UNIQUE/CHECK, defaults e índices están en actual-schema.json; este informe no afirma equivalencia semántica completa de expresiones SQL.\n\n${diffs.map(d=>'- '+d).join('\n')||'Sin diferencias en los aspectos comparados.'}\n`);
console.log(diffs.join('\n')||'Sin diferencias en los aspectos comparados.');
