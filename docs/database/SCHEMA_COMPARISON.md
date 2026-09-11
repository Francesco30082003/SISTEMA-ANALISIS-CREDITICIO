# Comparación del esquema

Inspección directa de 48 tablas mediante catálogos pg_catalog, en transacción READ ONLY con rollback.

Fuente documental: ../PMCORP_MAPAN_MODELO_FISICO_COMPLETO_CODEX.sql. No se ejecutó DDL.

Comparación automatizada de tablas, columnas, tipos, nulabilidad y presencia de constraints nombradas. Las definiciones reales de PK/FK/UNIQUE/CHECK, defaults e índices están en actual-schema.json; este informe no afirma equivalencia semántica completa de expresiones SQL.

- credito.ingreso_periodo: constraint documentada ck_ingreso_periodo_monto_neto ausente.
- credito.ingreso_periodo: constraint documentada ck_ingreso_periodo_monto_bruto ausente.
- seguridad.permiso.fecha_creacion: documentada pero NO existe en PostgreSQL.
