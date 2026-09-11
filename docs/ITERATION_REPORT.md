# Entrega de continuación — 2026-09-06

## Implementado en esta iteración

- Auditoría en vivo de 48 tablas y configuración crediticia mediante transacciones READ ONLY.
- Inicio con CTA Nuevo análisis y enlaces directos a expedientes.
- Nuevo expediente guiado con cinco secciones: operación, documentos, datos económicos, validación y resumen.
- Captura/corrección de actividades, fuentes, períodos, gastos y obligaciones usando los endpoints existentes. Búsqueda de clientes en el alta.
- Carga múltiple con drag & drop, progreso por archivos procesados, errores individuales, descarga autenticada y creación de nuevas versiones. El progreso no representa porcentaje de bytes enviados.
- Consulta de extracción disponible e historial completo de validaciones manuales; sin OCR ficticio ni transferencia automática de valores al expediente económico.
- Administración de usuarios, roles y asignaciones con permisos reales, confirmación y lectura previa de asignaciones.
- Límites HTTP de carga alineados con Storage:MaxBytes.
- Arranque local sin dependencia de escritura en Windows Event Log ni claves DPAPI de otra cuenta. Producción mantiene HTTPS.
- Mapeo EF que conserva booleanos explícitos en INSERT, incluso ante DEFAULT true de PostgreSQL; sin cambio de esquema.

## Rutas frontend

| Ruta | Uso |
|---|---|
| /login | Autenticación y empresa |
| / | Bandeja de solicitudes recientes |
| /nuevo-analisis | Alta de solicitud y acceso al expediente |
| /solicitudes | Listado y borradores |
| /solicitudes/:id/expediente | Documentos, economía, validación y resumen |
| /clientes | Clientes |
| /productos | Productos |
| /administracion/seguridad | Usuarios, roles y permisos |

## Endpoints nuevos

- GET /api/documentos/{id}/revision
- POST /api/documentos/{id}/versiones (multipart: archivo)
- GET /api/seguridad/usuarios/{id}/roles
- GET /api/seguridad/roles/{id}/permisos

Se reutilizan /api/auth, /api/empresas, /api/organizacion, /api/clientes, /api/productos, /api/solicitudes, /api/solicitudes/{id}/expediente, /api/documentos y /api/seguridad. Los contratos y métodos completos están en /openapi/v1.json al ejecutar Development.

## Verificación

- dotnet restore y dotnet build: correctos, sin errores ni warnings de compilación.
- dotnet test: 46 pruebas correctas (22 Domain y 24 Application), incluida regresión de booleanos.
- npm.cmd install y npm.cmd run build: correctos. npm informó cuatro scripts de instalación pendientes de aprobación; el build no necesitó habilitarlos.
- FastAPI: 3 pruebas correctas, sin predicción fabricada.
- Arranque real de API en puerto temporal 5207: /health y /health/ready 200, /openapi/v1.json 200; consulta de clientes y revisión documental sin token 401. Proceso temporal detenido al finalizar.
- Sin prueba automatizada de navegador ni prueba transaccional PostgreSQL de carga/versionado. Las pruebas de aislamiento usan EF InMemory; el test físico contrasta los metadatos reales capturados.

## Pendientes y bloqueos

No se alcanzó el objetivo de dejar MAPAN terminado salvo ML.

La base real contiene una política, una versión, FACTOR_CAPACIDAD y una regla CUOTA_SUPERA_CAPACIDAD con resultado REVISION_ADICIONAL. No hay modelos ni rutas de aprobación. No se encontró resultado por defecto ni resolución de recomendaciones contradictorias. Continúan las decisiones documentadas sobre redondeo, mensualización, estados de obligaciones, transiciones y desembolso.

Todavía falta implementar administración de políticas/versiones/reglas, orquestador transaccional, snapshots y vectores persistidos, recomendación, resultado e informe, workflow, cartera/desempeño, consulta de auditoría e interfaces de integraciones/modelos. El catálogo de permisos de estos módulos también requiere configuración; su ausencia no implica necesidad de cambiar esquema.

No se cambiaron criterios crediticios, permisos en PostgreSQL ni datos de negocio para simular un flujo exitoso.

## Lo que falta específicamente para ML

1. Dataset real y proceso de construcción con datos autorizados y trazables.
2. Definición formal de objetivo y scores aún no definidos; sin etiquetar arbitrariamente mal pagador.
3. Entrenamiento de candidatos.
4. Evaluación y selección del modelo.
5. Métricas reales y validación de su desempeño.
6. Artefacto entrenado (joblib) con `model.joblib` + `metadata.json` según el contrato de `services/ml-service/app/model_registry.py`.

Los puntos 1-6 son responsabilidad del proceso de entrenamiento externo (datos reales, definición de objetivo, entrenamiento, evaluación); MAPAN no los ejecuta ni los fabrica.

## Continuación — registro de modelos y carga de artefacto (2026-09-06)

- `services/ml-service` ya no devuelve 503 fijo: `ModelRegistry` carga `model.joblib` (scikit-learn/XGBoost/LightGBM, cualquier estimador con `predict_proba`) más `metadata.json` (orden de variables, `umbral_decision`, `positive_class` opcional, bandas de riesgo opcionales) desde el directorio de `MAPAN_ML_MODEL_DIR`. Sin ese directorio o sin ambos archivos, sigue en `NOT_CONFIGURED` — ningún valor fabricado. Explicabilidad vía SHAP (`TreeExplainer`) con degradación silenciosa a `factores: []` si el estimador no es soportado; nunca contribuciones inventadas. 9 pruebas nuevas/actualizadas en `services/ml-service/tests`.
- `riesgo.modelo` / `riesgo.modelo_version` ahora tienen CRUD completo: `Mapan.Application/Modelos`, `Mapan.Infrastructure/Persistence/Repositories/ModeloRepository.cs`, `Mapan.Api/Controllers/ModelosController.cs`. Ciclo de vida de versión con bloqueo de fila (`FOR UPDATE`): BORRADOR→ENTRENADO→VALIDADO→(PRODUCCION|SHADOW)→RETIRADO. Promover a PRODUCCION exige `artefacto_uri`, `umbral_decision` en [0,1] y el Modelo en ACTIVO; rechaza la promoción si ya hay otra versión PRODUCCION vigente en la empresa (evita `CONFIGURACION_ML_AMBIGUA` en el orquestador). El orquestador (`CreditAnalysisRepository.ExecuteAsync`) ya buscaba y llamaba automáticamente el modelo ACTIVO+PRODUCCION con `artefacto_uri`; no se tocó esa lógica.
- Pendiente de acción manual del usuario, no de código: insertar en `seguridad.permiso` los códigos `Modelos:Read` y `Modelos:Manage` (ya mapeados en `appsettings.json:Permissions:Modelos`) y asignarlos a un rol. Es una inserción de datos de catálogo, no DDL; MAPAN no la ejecuta por disciplina de no tocar la base real sin autorización explícita. Sin esa asignación, cualquier usuario recibe 403 al usar `/api/modelos`.
- `dotnet build`/`dotnet test` (46 pruebas) y `pytest services/ml-service/tests` (9 pruebas) verificados tras el cambio.

## Continuación — auditoría integral y cierre pre-ML (2026-09-06, segunda pasada)

Auditoría de código (no de docs) de los 20 puntos pedidos para "cierre integral antes del ML". Hallazgo principal: la mayoría de los módulos ya estaban completos (políticas, orquestador, recomendación, workflow, préstamos/desempeño, auditoría) — los documentos previos estaban desactualizados. Detalle en IMPLEMENTATION_PLAN.md, sección "ESTADO FINAL PRE-ML".

Cambios de código de esta pasada:
- **Bug corregido**: `ModelosController` (agregado en la pasada anterior) colisionaba de ruta con `GET/POST/PUT /api/modelos` del multiplexor genérico `OperacionesController`, que ya existía y no soportaba versiones de modelo. Se quitó "modelos" del multiplexor genérico (`OperationsService`, `OperationsRepository`, `OperacionesController`) y se le dio a Modelos su propia pantalla dedicada (`frontend/mapan-web/src/app/features/models/models.ts`), con gestión completa de versiones (crear, editar mientras no esté en uso, promover BORRADOR→ENTRENADO→VALIDADO→PRODUCCION/SHADOW→RETIRADO) — capacidad que no existía antes en ninguna UI. Verificado en vivo arrancando la API real: sin rutas duplicadas, 401 limpio en ambos endpoints.
- `credito.obligacion.estado`: catálogo VIGENTE/CANCELADA/CASTIGADA/REESTRUCTURADA confirmado por el usuario; `AnalysisDecisions.ActiveDebtCount` calcula `creditos_activos` (VIGENTE+REESTRUCTURADA); `CreditAnalysisRepository` rechaza estados fuera del catálogo. 3 pruebas nuevas (`ObligacionCatalogTests`).
- `estabilidad_ingresos_score`/`historial_interno_score`: confirmado por el usuario que permanecen NULL indefinidamente; sin cambio de código, documentado en BUSINESS_RULES.md.
- `AnalysisDetail`/`AnalysisReport`/pantalla de análisis: se agregaron obligaciones itemizadas, documentos asociados y reglas evaluadas — antes el informe y la pantalla no las mostraban.
- `BUSINESS_RULES.md`, `PENDING_DATABASE_CHANGES.md`, `LOCAL_RUN.md`, `IMPLEMENTATION_PLAN.md` corregidos para reflejar el código real en vez de un estado desactualizado (redondeo, moneda, transiciones, MFA, precedencia de recomendación, aislamiento de roles entre empresas ya estaban resueltos en código).
- Catálogo de permisos: consolidado en LOCAL_RUN.md el SQL con TODOS los códigos faltantes (Modelos, Politicas, Analisis, Alertas, Workflow, Prestamos, Auditoria, Integraciones), no solo Modelos. No ejecutado.

Verificado: `dotnet build`/`dotnet test` (49 pruebas), `pytest services/ml-service/tests` (9 pruebas), `npm.cmd install`/`npm.cmd run build`, y arranque real de la API contra PostgreSQL (`/health`, `/health/ready`, `/openapi/v1.json`).

## Continuación — pruebas de integración contra PostgreSQL real (2026-09-06, tercera pasada)

El usuario autorizó explícitamente escribir filas sintéticas temporales (prefijo `ZZTEST-`/`zztest`) en la base real, con limpieza explícita al final. Se agregó `tests/Mapan.Application.Tests/PostgresIntegrationTests.cs` (usa el mismo `UserSecretsId` que Mapan.Api; se auto-omite sin fallar si no hay `ConnectionStrings:MapanDatabase` configurado):

1. `AssignRolesAsync_RejectsCrossTenantRole_...`: crea dos empresas y un rol de cada una, confirma que asignar el rol ajeno lanza 403 con bloqueo `FOR UPDATE` real y no deja fila en `usuario_empresa_rol`, y que asignar el rol propio sí persiste.
2. `FullCreditFlow_ClienteToWorkflow_...`: monta una empresa, sucursal, cliente, producto, política+versión+FACTOR_CAPACIDAD, solicitud, fuente/período de ingreso, gasto, obligación VIGENTE y ruta de aprobación predeterminada; ejecuta `CreditAnalysisRepository.ExecuteAsync` contra Postgres real y verifica snapshot, vector (`creditos_activos=1`, scores en NULL), recomendación (`CAPACIDAD_COMPATIBLE`, revisión humana obligatoria, sin predicción fabricada — `mlStatus=MODELO_PREDICTIVO_NO_CONFIGURADO`), creación de la aprobación/paso de workflow y actualización de estado de la solicitud a REVISION.
3. `NoZzTestResidueRemainsInRealDatabase`: escaneo independiente por convención de nombre en 8 tablas, para detectar cualquier residuo de una corrida anterior que no se haya limpiado.

Las tres pasan de forma reproducible (corridas repetidas confirman limpieza completa). Total .NET: 52 pruebas (25 Domain + 27 Application).

## Continuación — informe en PDF y diálogos propios (2026-09-06, cuarta pasada)

- El informe (`GET /api/analisis/{id}/informe`) ahora genera un **PDF real** con QuestPDF (licencia Community elegida por el usuario; sujeta a su límite de ingresos anuales, no verificado por MAPAN) en vez de HTML — mismas secciones (resumen, capacidad de pago, endeudamiento, obligaciones, documentos, reglas/alertas, modelo predictivo, recomendación). Se sirve con `Content-Disposition: inline`, así que el navegador lo previsualiza directamente. Frontend: el botón pasó de "Descargar informe" a "Ver informe (PDF)", abre en pestaña nueva manteniendo el token de autenticación (fetch autenticado + blob URL, no `window.open` directo al endpoint). 4 pruebas nuevas en `AnalysisReportTests.cs` verifican que el PDF se genera de verdad (bytes con firma `%PDF`) con y sin obligaciones/documentos/predicción, y que faltar snapshot o recomendación se reporta como error, no como PDF vacío.
- Se corrigió un bug real encontrado por el usuario probando la app: cambiar el estado de una versión de política (o de un modelo) devolvía `415 Unsupported Media Type`, porque Angular manda un `string` crudo como `Content-Type: text/plain` sin comillas JSON, y el backend espera `application/json`. Se agregó `ApiClient.postRaw()` (JSON.stringify + header explícito) y se corrigieron los 3 call sites afectados (`policies.ts`, `models.ts` ×2).
- Se reemplazaron los 9 usos de `confirm()`/`prompt()` nativos del navegador por un sistema de diálogo propio (`shared/dialog.ts`: `DialogService` + `mapan-dialog-host`, montado una vez en la raíz de la app) con el mismo lenguaje visual del resto de la aplicación.
- Se creó un rol `ADMIN_DEMO` en la base real (autorizado explícitamente por el usuario) con `Seguridad:Manage`, asignado a `analista.demo`, para desbloquear la autoadministración de permisos desde la propia UI.

## Continuación — autollenado de datos económicos desde documentos (2026-09-06, quinta pasada)

Extracción real (no OCR de imágenes, no fabricada) para PDF con texto digital: `TextPdfDocumentExtractionService` (PdfPig, MIT) reemplaza el stub `UnconfiguredDocumentExtractionService` para documentos. Reconstruye líneas por posición vertical de las palabras (page.Text de PdfPig no preserva saltos de línea — bug real encontrado y corregido con una prueba que lo detectó). Reconoce únicamente los campos registrados en `DocumentFieldPatterns` por `tipo_documento` (por ahora, `CERTIFICADO_INGRESOS`: nombre, identificación, empleador, cargo, fecha de ingreso, ingreso mensual, período desde/hasta); cualquier otro tipo o un PDF escaneado sin capa de texto se reporta honestamente como `NOT_CONFIGURED`/`NO_EXTRACTABLE_TEXT`, nunca se inventa un valor. Se dispara automáticamente (best-effort, no bloquea la carga) al subir o reemplazar un documento (`DocumentoRepository`).

Los valores extraídos quedan en `documentos.dato_extraido` como sugerencias; en el paso "Datos económicos" del expediente, cada campo relevante (empleador, cargo, fecha de inicio, período de ingreso, monto) muestra un botón "Usar de [documento]: [valor]" que solo prellena el input — el analista sigue guardando manualmente, y ese guardado sigue creando el registro real. Nada se transfiere sin que el analista lo confirme explícitamente. `credito.obligacion` queda deliberadamente fuera de este mecanismo: según indicó el usuario, las obligaciones se integrarán más adelante vía Equifax/Aval (`ICreditBureauProvider`, aún sin configurar).

Documento de prueba generado con QuestPDF en `docs/samples/certificado-ingresos-prueba.pdf` (texto digital real, no una imagen) para subir y probar el flujo completo. 6 pruebas nuevas (`DocumentExtractionPatternTests`) verifican, generando ese mismo documento en memoria, que cada patrón extrae el valor correcto. Total .NET: 58 pruebas.

## Continuación — autollenado real (no botón) y verificación de extremo a extremo (2026-09-06, sexta pasada)

El usuario reportó que la sugerencia "nunca apareció". Se agregó una prueba de extremo a extremo real (`UploadingASampleCertificateActuallyExtractsFieldsOnRealPostgresAndRealStorage`) que sube el documento de prueba a través de `DocumentoRepository.UploadAsync` usando el storage y Postgres reales (no aislado como las pruebas anteriores) — **esta prueba confirma que el motor de extracción sí funciona de punta a punta** (8 campos extraídos correctamente). El problema estaba en dos lugares:

1. `DocumentFieldPatterns.ByTipoDocumento` exigía coincidencia exacta de mayúsculas/espacios con `CERTIFICADO_INGRESOS`. Se agregó `DocumentFieldPatterns.Find(tipoDocumento)`, que normaliza mayúsculas/espacios/guiones antes de comparar.
2. El frontend requería que el analista hiciera clic en "Usar sugerencia" — el usuario pidió autollenado real. Se cambió `workspace.ts`: en cuanto se cargan las sugerencias (`loadSuggestions`), se aplican automáticamente a los campos vacíos correspondientes (`applySuggestions`); el campo muestra "Prellenado automáticamente desde... verifica antes de guardar" en vez de un botón. El analista sigue siendo quien guarda (doble check), pero ya no tiene que aceptar sugerencia por sugerencia. También se dejó de silenciar errores de `/documentos/{id}/revision` en el frontend (antes un fallo ahí era invisible).

Se corrigió además un bug de orden de limpieza en la propia prueba de integración (borraba `usuario_empresa` antes que `auditoria.evento`, violando la FK) — dejó un residuo sintético temporal que se limpió manualmente antes de continuar.

## Continuación — extracción independiente del tipo de documento, obligaciones incluidas (2026-09-06, séptima pasada)

El usuario pidió que cualquier tipo de archivo/documento (no solo "certificado de ingresos") pueda autollenar, incluidas obligaciones (como híbrido futuro con Equifax/Aval). `DocumentFieldPatterns` dejó de estar indexado por `tipo_documento` (el texto libre del analista no es confiable): ahora `DocumentFieldPatterns.All` se prueba contra cualquier PDF con texto digital, sin importar qué haya escrito el analista como tipo. Se agregaron 5 patrones nuevos para obligaciones (institución, saldo, cuota, días de mora, estado — mismo catálogo VIGENTE/CANCELADA/CASTIGADA/REESTRUCTURADA decidido antes). El frontend conecta estos campos al mismo mecanismo de autollenado ya construido para ingresos.

Segundo documento de prueba: `docs/samples/certificado-deuda-prueba.pdf`. 3 pruebas nuevas confirman que un documento de ingresos no dispara campos de obligación y viceversa, y que un documento sin ninguna etiqueta reconocida no extrae nada (nunca se fabrica). Total .NET: 60 pruebas.

Pendiente, no construido en esta pasada: reconocimiento de imágenes reales (fotos/escaneos sin capa de texto) — requiere un motor OCR (Tesseract local o una API de nube) y es una decisión de alcance/costo que se dejó en manos del usuario.

## Continuación — OCR real con Tesseract para fotos/imágenes (2026-09-06, octava pasada)

El usuario eligió Tesseract local (gratuito, sin credenciales, corre en el propio servidor). Se agregó soporte real para `image/png` e `image/jpeg`: `TextPdfDocumentExtractionService` ahora rutea por tipo MIME — PDF con texto sigue usando PdfPig (exacto); una imagen se procesa con Tesseract (paquete `Tesseract` 5.2.0, binarios nativos win-x64/x86 incluidos) usando datos entrenados en español (`tessdata/spa.traineddata`, Apache 2.0, descargado de `tesseract-ocr/tessdata_fast`). El mismo catálogo `DocumentFieldPatterns.All` se aplica sobre el texto que salga de cualquiera de los dos caminos.

Verificado con OCR real (no simulado): se dibuja texto sobre un bitmap en memoria (sin ninguna capa de texto de PDF de por medio), se guarda como PNG, se procesa con el mismo motor Tesseract que usa el servicio, y se confirma que el texto reconocido coincide con los patrones. Se generó además `docs/samples/certificado-ingresos-foto-prueba.png` para subir y probar desde la UI.

**Limitaciones honestas, no resueltas y no ocultas:**
- La precisión de OCR sobre una foto real (borrosa, inclinada, con mala luz) es menor que la extracción exacta de un PDF con texto — es una limitación inherente de OCR, no de esta implementación.
- Un PDF escaneado (imagen dentro de un PDF, sin capa de texto) **todavía no se procesa** — solo se cubrió `image/png`/`image/jpeg` directos. Rasterizar páginas de PDF a imagen para luego aplicarles OCR requeriría una librería de renderizado de PDF adicional; no se construyó en esta pasada.
- Los binarios nativos de Tesseract incluidos por el paquete NuGet son para Windows (x64/x86). Si el despliegue final es en Linux, hace falta el binario nativo de Tesseract para esa plataforma — no verificado en esta sesión (el entorno de desarrollo es Windows).
- La confianza de OCR queda `NULL` en `dato_extraido.confianza` (nunca se inventa un número de certeza).

Total .NET: 61 pruebas.

Ejecución local: LOCAL_RUN.md.

