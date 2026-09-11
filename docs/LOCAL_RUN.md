# Ejecutar MAPAN localmente

## Requisitos

.NET SDK 10, Node/npm compatibles con el Angular instalado, PostgreSQL existente accesible y Python con las dependencias de services/ml-service/requirements.txt. No ejecutar el SQL documental, migrations, EnsureCreated ni comandos DDL.

La conexión existente se obtiene de User Secrets de Mapan.Api o de ConnectionStrings__MapanDatabase. El inspector comparte el mismo UserSecretsId. No imprimir ni versionar secretos.

Configurar en User Secrets o entorno:

- ConnectionStrings:MapanDatabase: conexión autorizada a PostgreSQL existente.
- Jwt:SigningKey: secreto aleatorio de al menos 32 bytes.
- Cors:AllowedOrigins:0: http://localhost:4200.
- Storage:RootPath: directorio de archivos accesible por la cuenta de la API.
- Storage:MaxBytes: límite por archivo, por defecto 20971520; máximo soportado 104857600.
- Storage:AllowedMimeTypes: application/pdf, image/png, image/jpeg por defecto.
- Ml:BaseUrl: http://localhost:8000 para el conector futuro; no habilita un modelo.

Los permisos de appsettings.json corresponden al catálogo inspeccionado. Usar un usuario y membresía existentes; no hay contraseña de demostración creada por esta iteración.

## API (desde la raíz)

```powershell
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Mapan.Api --launch-profile http
```

API: http://localhost:5199. Salud: /health. PostgreSQL: /health/ready. OpenAPI en Development: /openapi/v1.json.

Development admite HTTP local, usa claves Data Protection en memoria y no usa el Event Log de Windows. JWT sigue utilizando Jwt:SigningKey. Producción conserva redirección HTTPS y Data Protection persistente predeterminado; configurar sus claves según el despliegue.

## Frontend (otra terminal)

```powershell
cd frontend/mapan-web
npm.cmd install
npm.cmd run build
npm.cmd start
```

Abrir http://localhost:4200. public/config.json apunta a http://localhost:5199/api. Usar npm.cmd en PowerShell cuando la política de ejecución impida cargar npm.ps1.

## Contrato ML opcional (otra terminal, desde la raíz)

```powershell
python -m venv services/ml-service/.venv
services/ml-service/.venv/Scripts/python.exe -m pip install -r services/ml-service/requirements.txt
cd services/ml-service
.venv/Scripts/python.exe -m uvicorn app.main:app --host 127.0.0.1 --port 8000
```

GET /health informa modelo no configurado. POST /predict devuelve 503 con MODEL_NOT_CONFIGURED mientras no exista un artefacto real configurado. Es deliberado: no existe modelo ficticio.

Para servir un modelo real entrenado aparte (scikit-learn/XGBoost/LightGBM), exportarlo con `joblib.dump` y colocar en un directorio:

- `model.joblib`: el estimador entrenado, debe exponer `predict_proba`.
- `metadata.json`: `{"modelo_version_id":"<uuid igual al de riesgo.modelo_version>","feature_order":["ingreso_mensual", "..."],"umbral_decision":0.5,"positive_class":1,"umbral_riesgo_bajo":0.3,"umbral_riesgo_alto":0.7}`. `feature_order` debe coincidir exactamente con las columnas que el modelo espera, en ese orden. `positive_class` y las bandas de riesgo son opcionales; sin bandas, `nivel_riesgo` queda `null`.

Configurar `MAPAN_ML_MODEL_DIR` apuntando a ese directorio antes de levantar uvicorn. Sin esa variable o sin ambos archivos, el servicio sigue en NOT_CONFIGURED. Registrar la versión correspondiente en MAPAN (`POST /api/modelos`, `POST /api/modelos/{id}/versiones`, `POST /api/modelos/versiones/{id}/estado` hasta PRODUCCION) requiere el permiso Modelos:Manage; ver nota de catálogo de permisos más abajo.

Pruebas del contrato, desde la raíz:

```powershell
services/ml-service/.venv/Scripts/python.exe -m pytest services/ml-service/tests -q -p no:cacheprovider
```

## Recorrido disponible

Login → empresa → Nuevo análisis → crear solicitud → Abrir expediente → documentos → captura/corrección económica → validación manual → resumen.

La creación inicial requiere cliente existente; se puede buscar por identificación y acceder a Clientes para crearlo. La captura económica requiere BORRADOR. No enviar a DOCUMENTACION hasta completar dicha captura. Las versiones documentales conservan las asociaciones y revisiones anteriores; no declaran automáticamente completado un requisito documental.

Corregido 2026-09-06: la ejecución de análisis SÍ está conectada de punta a punta, no deshabilitada. El paso "Resumen" del expediente llama a `POST /api/solicitudes/{id}/analisis` (permiso Analisis:Execute), que ejecuta `CreditAnalysisRepository.ExecuteAsync`: valida datos/documentos/monedas/obligaciones, calcula snapshot financiero y vector de características, evalúa reglas, genera alertas, calcula la recomendación sin ML, llama al modelo PRODUCCION si existe (o deja ML como no configurado si no), crea el flujo de aprobación aplicable y navega a `/analisis/{id}` con el resultado completo. Desde ahí se descarga el informe HTML (`GET /api/analisis/{id}/informe`). Ver IMPLEMENTATION_PLAN.md, sección "ESTADO FINAL PRE-ML", para el detalle verificado módulo por módulo.

Administración → Usuarios, roles y permisos permite crear usuarios/roles y editar asignaciones, con Seguridad:Manage. La API verifica permisos vigentes en cada petición; la navegación se actualiza con una nueva sesión.

## Catálogo de permisos pendiente (2026-09-06)

`appsettings.json:Permissions` ya mapea las operaciones de todos los módulos, pero el catálogo real en `seguridad.permiso` sólo tiene 15 filas (ver docs/database/permission-codes.json): las de Clientes, Documentos, Productos, Seguridad:Manage y Solicitudes. Los siguientes códigos existen en el código pero NO como filas reales, así que cualquier usuario recibe 403 al usar esos módulos hasta que se inserten y se asignen a un rol: `Modelos:Read`, `Modelos:Manage`, `Politicas:Read`, `Politicas:Manage`, `Analisis:Read`, `Analisis:Execute`, `Alertas:Read`, `Workflow:Read`, `Workflow:Manage`, `Prestamos:Read`, `Prestamos:Manage`, `Auditoria:Read`, `Integraciones:Read`, `Integraciones:Manage`.

Esto es una inserción de datos de catálogo (no un cambio de esquema); MAPAN no la ejecuta automáticamente sobre la base real. Ejemplo, ajustando el rol destino:

```sql
INSERT INTO seguridad.permiso (codigo, nombre) VALUES
  ('Modelos:Read', 'Consultar modelos predictivos'),
  ('Modelos:Manage', 'Administrar modelos predictivos'),
  ('Politicas:Read', 'Consultar políticas de crédito'),
  ('Politicas:Manage', 'Administrar políticas de crédito'),
  ('Analisis:Read', 'Consultar análisis y su historial'),
  ('Analisis:Execute', 'Ejecutar el análisis de una solicitud'),
  ('Alertas:Read', 'Consultar y resolver alertas de análisis'),
  ('Workflow:Read', 'Consultar aprobaciones y flujos'),
  ('Workflow:Manage', 'Configurar rutas de aprobación'),
  ('Prestamos:Read', 'Consultar préstamos y desempeño'),
  ('Prestamos:Manage', 'Registrar préstamos y cortes de desempeño'),
  ('Auditoria:Read', 'Consultar el registro de auditoría'),
  ('Integraciones:Read', 'Consultar integraciones configuradas'),
  ('Integraciones:Manage', 'Configurar integraciones externas');

INSERT INTO seguridad.rol_permiso (rol_id, permiso_id)
SELECT r.rol_id, p.permiso_id
FROM seguridad.rol r, seguridad.permiso p
WHERE r.rol_id = '<rol-id-destino>' AND p.codigo IN (
  'Modelos:Read','Modelos:Manage','Politicas:Read','Politicas:Manage','Analisis:Read','Analisis:Execute',
  'Alertas:Read','Workflow:Read','Workflow:Manage','Prestamos:Read','Prestamos:Manage','Auditoria:Read',
  'Integraciones:Read','Integraciones:Manage'
);
```

No ejecutado por MAPAN. Ajustar la lista de códigos por rol según a quién corresponda cada autoridad (por ejemplo, un rol "Analista" no debería recibir Workflow:Manage ni Politicas:Manage).
