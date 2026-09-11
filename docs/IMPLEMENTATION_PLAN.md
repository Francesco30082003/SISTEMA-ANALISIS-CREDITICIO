# MAPAN — auditoría de continuación, 2026-09-06

PostgreSQL inspeccionado en vivo (health/ready contra la BD real, sin DDL ni migraciones). Se conserva el trabajo previo sin commit. Esta revisión corrige la anterior: varias filas marcadas TODO/BLOCKED ya estaban implementadas en el código; se verificó cada una leyendo el código real, no solo los docs anteriores.

| Área | Estado | Evidencia / pendiente |
|---|---|---|
| Esquema y mapeos | DONE | Inspector real y prueba de columnas, tipos y nulabilidad (`PhysicalModelTests`) |
| Login, selección de empresa, JWT | DONE | Servicios, endpoints, guards y pruebas existentes |
| Aislamiento tenant | DONE (código); prueba PostgreSQL real pendiente | `SecurityAdministrationRepository.AssignRolesAsync` rechaza roles de otra empresa antes de escribir; `AuthenticationRepository` nunca concede permisos de un rol ajeno. Cubierto por `TenantIsolationTests` con EF InMemory; falta un test contra Postgres real porque el método usa `FOR UPDATE` |
| Clientes y productos | DONE | Backend y pantallas existentes |
| Solicitudes | DONE | Alta, borrador, documentación, envío a análisis y transiciones (`SolicitudTransitions`, testeado) |
| Captura económica | DONE | Backend y UI guiada de actividades, fuentes, períodos, gastos, obligaciones; mensualización exige mes calendario completo (`AnalysisDecisions.IsMonthly`) |
| Documentos | DONE (funcional); prueba PostgreSQL transaccional pendiente | Storage seguro, carga múltiple, descarga, versiones, revisión manual |
| OCR / buró | BLOCKED | Contratos disponibles; faltan proveedores reales y credenciales, fuera de alcance de MAPAN. Captura manual funcional |
| Motor financiero / DSL | DONE | Cálculo puro; 25 pruebas Domain |
| Políticas | DONE | CRUD completo de política/versión/parámetros/reglas con inmutabilidad tras uso (`PolicyRepository`), UI dedicada (`features/policies`) |
| Orquestador / snapshot / vector | DONE | `CreditAnalysisRepository.ExecuteAsync`: valida datos/documentos/moneda/obligaciones, calcula snapshot y vector, evalúa reglas, genera alertas, todo transaccional con bloqueo de fila y registro de estado ERROR si falla |
| Recomendación sin ML | DONE | `AnalysisDecisions.Recommend`: precedencia por severidad/prioridad, resultado por defecto, `RequiereRevisionHumana=true` siempre, nunca aprueba sola |
| Resultado / informe | DONE | Pantalla de análisis con capacidad, endeudamiento, obligaciones, alertas, reglas evaluadas, documentos, modelo/predicción y recomendación; informe HTML descargable con las mismas secciones |
| ML | PARTIAL | Orquestación, carga de artefacto joblib+SHAP y CRUD de `riesgo.modelo`/`modelo_version` con ciclo de vida completo, UI dedicada (`features/models`). Falta exclusivamente el artefacto entrenado con datos reales (ver más abajo) y la fila de permisos `Modelos:*` en la BD |
| Workflow | DONE | Enrutamiento por reglas o ruta predeterminada, pasos con roles/permisos/cantidad de aprobaciones, decisión (aprobar/rechazar/devolver/abstenerse) con historial (`WorkflowRepository`), bandeja e inbox en UI |
| Préstamos / desempeño | DONE (funcional, patrón genérico) | `OperationsRepository`: exige confirmación humana explícita, bloquea solicitudes rechazadas/canceladas y aprobaciones pendientes, registra cortes con mora 15/30/60/90 (columnas generadas por Postgres); UI en `/prestamos`. Implementado con el multiplexor genérico de Operaciones (como Integraciones/Rutas), no con clases dedicadas como Políticas/Modelos — funciona, sin DTOs tipados propios ni pruebas de repositorio dedicadas |
| Usuarios / roles | DONE | Backend y UI de creación, consulta y asignaciones; verificado el rechazo de asignación cruzada entre empresas |
| Auditoría | DONE | Consulta con filtros (empresa/usuario/fecha/entidad/acción/correlación), paginada, exclusivamente de lectura; `AuditWriter` no registra contraseñas/JWT/credenciales por construcción (solo tipo de entidad + id + acción) |
| UX | PARTIAL | Flujo funcional completo (inicio → solicitudes → expediente → análisis → alertas → recomendación → workflow → informe); no se rediseñó visualmente hacia el estilo "tarjeta/resumen" que pide la sección 16 — es trabajo de UI pendiente, no de funcionalidad |
| Pruebas de integración PostgreSQL real | DONE | Autorizado explícitamente por el usuario 2026-09-06 (escritura sintética con limpieza). `tests/Mapan.Application.Tests/PostgresIntegrationTests.cs`, 3 pruebas contra la BD real: (1) `AssignRolesAsync` rechaza rol de otra empresa con `FOR UPDATE` real y no deja fila parcial; (2) flujo integral completo cliente→solicitud→datos económicos→análisis→snapshot→vector→recomendación→workflow contra Postgres real, verificando además que sin modelo ML registrado no se fabrica predicción (`mlStatus=MODELO_PREDICTIVO_NO_CONFIGURADO`, `PrediccionId=null`); (3) verificación independiente de que no queda ningún residuo sintético (`ZZTEST-`) en 8 tablas tras ambas pruebas. Las tres pasan y se auto-omiten (sin fallar) si el entorno no tiene `ConnectionStrings:MapanDatabase` configurado |
| Validación base | DONE | `dotnet build`/`dotnet test`: 52 pruebas (25 Domain + 27 Application, incluidas las 3 de PostgreSQL real). `pytest services/ml-service/tests`: 9 pruebas. `npm.cmd install`/`npm.cmd run build`: correctos. API real arrancada en esta sesión: `/health` y `/health/ready` 200 contra Postgres real, `/openapi/v1.json` sin duplicar rutas de `/api/modelos`, endpoints protegidos devuelven 401 sin sesión |

## Corrección importante sobre esta auditoría

La versión anterior de este documento (y de `ITERATION_REPORT.md`/`BUSINESS_RULES.md`) describía como TODO/BLOCKED varios módulos que, al leer el código, ya estaban implementados: políticas (backend y frontend completos), el orquestador de análisis completo, recomendación con precedencia y resultado por defecto, workflow completo, préstamos/desempeño funcionales, y auditoría con filtros. Los docs no se habían actualizado tras iteraciones previas de código. Esta revisión se basa en lectura directa del código, no en el estado de los documentos anteriores.

Un bug real se encontró y corrigió en esta sesión: el nuevo `ModelosController` (agregado para dar ciclo de vida a versiones de modelo) colisionaba de ruta con el multiplexor genérico `OperacionesController`, que ya exponía `GET/POST/PUT /api/modelos` sin soporte de versiones. Se eliminó "modelos" del multiplexor genérico y se le dio a Modelos su propia pantalla (`features/models`), igual que Políticas. Verificado en vivo: sin rutas duplicadas, sin `AmbiguousMatchException`.

## Bloqueos de autorización comprobados

El catálogo real de `seguridad.permiso` sigue teniendo solo 15 filas (Clientes, Productos, Solicitudes, Documentos, Seguridad:Manage). Los códigos `Modelos:*`, `Politicas:*`, `Analisis:*`, `Alertas:Read`, `Workflow:*`, `Prestamos:*`, `Auditoria:Read`, `Integraciones:*` están mapeados en `appsettings.json` y usados por el código, pero sin fila real en la BD ni asignación a un rol, todo usuario recibe 403 en esos módulos. Ver LOCAL_RUN.md para el SQL exacto (no ejecutado). Esto es una inserción de datos de catálogo, no un cambio de esquema.

## ESTADO FINAL PRE-ML

Flujo verificado end-to-end en código Y en una prueba automatizada contra PostgreSQL real (login → empresa → nueva solicitud → cliente → expediente → documentos → datos económicos → validación → requisitos documentales → ejecución del análisis → snapshot financiero → vector de características → evaluación de reglas → alertas → recomendación sin ML → predicción ML si existe modelo → revisión humana → workflow → decisión → informe final → si se aprueba, registro de préstamo → registro de desempeño): **DONE**, con una sola condición pendiente de acción del usuario (no de desarrollo):

1. Insertar las filas de catálogo de permisos faltantes y asignarlas a los roles correspondientes (SQL en LOCAL_RUN.md). Sin esto, los módulos afectados devuelven 403 en un entorno real aunque el código funcione.

### Qué queda exclusivamente para ML

1. Dataset real y proceso de construcción con datos autorizados y trazables — responsabilidad externa a MAPAN.
2. Definición del objetivo/etiqueta de entrenamiento — responsabilidad externa.
3. Entrenamiento, evaluación y selección del modelo — responsabilidad externa.
4. Exportar el modelo ganador como `model.joblib` + `metadata.json` según el contrato de `services/ml-service/app/model_registry.py` (documentado en LOCAL_RUN.md).
5. Configurar `MAPAN_ML_MODEL_DIR` en el servicio ML y registrar la versión en MAPAN (`/modelos`) hasta PRODUCCION.

MAPAN no entrena, no fabrica métricas ni probabilidades, y continúa funcionando con recomendación por reglas mientras no exista un modelo en PRODUCCION.

## Entrega y ejecución

Detalle verificable, endpoints, rutas y límites: ITERATION_REPORT.md. Instrucciones: LOCAL_RUN.md. Decisiones de negocio vigentes: BUSINESS_RULES.md.
