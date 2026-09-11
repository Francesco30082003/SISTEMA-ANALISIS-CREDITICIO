# Cambios de base pendientes

No se han ejecutado ni se proponen todavía cambios de esquema.

La tabla seguridad.usuario_empresa_rol tiene FK individuales a membresía y rol, pero no una FK compuesta que obligue a que ambos pertenezcan a la misma empresa. Verificado 2026-09-06: la aplicación ya comprueba esa coincidencia. `SecurityAdministrationRepository.AssignRolesAsync` (src/Mapan.Infrastructure/Security/SecurityAdministrationRepository.cs) rechaza con 403 si algún rolId no pertenece a la empresa activa y está ACTIVO, antes de escribir `usuario_empresa_rol`. `AuthenticationRepository.GetIdentityAsync` además nunca concede permisos de un rol de otra empresa aunque existiera el vínculo (cubierto por `TenantIsolationTests.RoleFromDifferentCompanyNeverGrantsPermissions`). Falta una prueba de integración contra PostgreSQL real para `AssignRolesAsync` específicamente, porque usa `FOR UPDATE` (no soportado por el proveedor InMemory usado en tests). No es necesario cambiar PostgreSQL para este control.

## Catálogo de estado para credito.obligacion (2026-09-06)

Decisión de negocio confirmada: obligacion.estado admite VIGENTE, CANCELADA, CASTIGADA, REESTRUCTURADA; VIGENTE y REESTRUCTURADA cuentan como crédito activo. La aplicación ya valida esta lista antes de ejecutar un análisis (`CreditAnalysisRepository.ExecuteAsync`) y calcula `vector_caracteristicas.creditos_activos` en consecuencia (`AnalysisDecisions.ActiveDebtCount`); no se requiere cambio de esquema para que el flujo funcione. Si se quiere reforzar la integridad a nivel de base, el CHECK equivalente sería:

```sql
ALTER TABLE credito.obligacion
ADD CONSTRAINT ck_obligacion_estado
CHECK (estado IN ('VIGENTE','CANCELADA','CASTIGADA','REESTRUCTURADA'));
```

No ejecutado. Es una propuesta documentada; aplicarla es decisión y acción del usuario sobre la base real.
