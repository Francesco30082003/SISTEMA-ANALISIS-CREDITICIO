# Reglas de negocio y decisiones

Revisado: 2026-09-06. Esta versión corrige BUSINESS_RULES.md anterior, que describía como "bloqueado" varias reglas que el código ya resuelve. Cada punto indica dónde vive la implementación.

## Implementadas como cálculo puro

Promediar períodos dentro de cada fuente y sumar esos promedios. Restar gastos, multiplicar disponible por FACTOR_CAPACIDAD recibido de política. Ratios con ingreso cero son NULL (`CreditFinancialAnalysisEngine`).

DSL: comparaciones numéricas =, !=, >, >=, <, <=; igualdad/desigualdad de texto y booleanos; AND/OR mediante `{"operador":"AND","condiciones":[...]}`. Campo contra valor o comparar_con; campos ausentes/NULL, operadores desconocidos, propiedades desconocidas y JSON inválido producen error. Límites de tamaño, profundidad y nodos. Acción inicial GENERAR_ALERTA con resultado explícito (`RuleEngine`).

## Ya resueltas en código (antes descritas como bloqueadas)

- **Redondeo**: `AnalysisDecisions.Money` redondea importes a 2 decimales y `Ratio` a 8 decimales, ambos "away from zero", antes de evaluar cuota compatible (`AnalysisDecisions.Snapshot`).
- **Moneda**: MAPAN no convierte. `CreditAnalysisRepository.ExecuteAsync` rechaza el análisis si alguna fuente de ingreso está en una moneda distinta a la de la empresa.
- **Obligaciones con datos faltantes**: se rechaza el análisis si falta saldo o cuota confirmados; nunca se asumen cero.
- **Transiciones de aprobación/rechazo/devolución**: implementadas en `WorkflowRepository.DecideAsync`, con conteo de aprobaciones requeridas por paso y verificación de rol/permiso.
- **Precedencia de recomendaciones y resultado por defecto**: `AnalysisDecisions.Recommend` ordena por severidad y prioridad; si el grupo más severo tiene resultados distintos, usa REVISION_ADICIONAL; sin reglas aplicables, usa compatible/no compatible según capacidad de pago. Nunca aprueba automáticamente (`RequiereRevisionHumana=true` siempre).
- **Cuota estimada**: se calcula siempre en el backend y sustituye cualquier valor enviado por el cliente; nunca se exige ni se confía en un dato tecleado a mano. `CreditoFrancesCalculator` aplica amortización francesa de cuota fija (M = P·r·(1+r)^n / ((1+r)^n − 1), r = tasa anual del producto /100/12) y deriva la tasa de la tasa vigente configurada en `ProductoCredito`, no de un campo libre de la solicitud — así el producto define una única tasa oficial y la solicitud solo guarda el snapshot usado. `CapitalMensualEstimado`/`InteresMensualEstimado` reportan el desglose de la primera cuota, no un promedio: en amortización francesa el reparto capital/interés cambia cada mes aunque la cuota sea constante.
- **Requisitos documentales**: si la política define DOCUMENTOS_REQUERIDOS, se valida que estén adjuntos antes de completar el análisis.
- **Actividad principal para antigüedad**: si no hay exactamente una actividad marcada principal con fecha de inicio válida, `antiguedad_actividad_meses` queda NULL y se genera una alerta amarilla; no se infiere.
- **Investigación del cliente** (buró/Equifax, judicial, aval): `InvestigacionRepository.EjecutarAsync` consulta cada fuente por separado; si la cooperativa no activó un proveedor de esa categoría (`integracion.empresa_proveedor` por `Proveedor.Tipo`), simplemente no se registra consulta para esa fuente — nunca se fabrica un resultado. Si el proveedor está activo pero falla, la consulta queda en estado `ERROR` con el mensaje, y las demás fuentes se siguen consultando. Los proveedores hoy son mocks (`MockEquifaxProvider`, `MockJudicialProvider`, `MockAvalProvider`) reemplazables sin tocar el resto del sistema.
- **Eliminar un documento cargado por error**: nunca se borra el archivo ni la fila; `DocumentoService.VoidAsync` marca `Documento.Estado=ANULADO` (estado ya reservado para esto en el esquema) y queda auditado. Solo se permite mientras la solicitud sigue en `BORRADOR`/`DOCUMENTACION`.
- **Preevaluación** (antes de pedir documentos financieros): usa reglas `politica.regla` con `etapa=PREEVALUACION` (las de `etapa=ANALISIS`, el valor por defecto, siguen siendo las del análisis financiero de siempre — nunca se mezclan). Acciones posibles: `CONTINUAR`/`ALERTA` no bloquean; `REQUIERE_EXCEPCION` y `BLOQUEAR` sí, pero ambas pueden desbloquearse con una excepción aprobada (`politica.excepcion`) — la diferencia es solo de severidad/mensaje, no hay una autorización de nivel superior distinta todavía. Exige que la Investigación del Cliente ya se haya ejecutado; nunca inventa un resultado si faltan fuentes. Sin reglas de preevaluación configuradas, el resultado por defecto es `APTO`.
- **Excepciones**: aprobación de un solo nivel (`Excepciones:Aprobar`), no el flujo multinivel de `RutaAprobacion` — se puede evolucionar más adelante si una cooperativa lo pide explícitamente.
- **Carga de reportes de buró/Equifax/Aval** (Fase 5): un reporte cargado como documento se extrae con los mismos patrones (`documentos.dato_extraido`, campos `reporte_buro_*`) y, al confirmarse, termina en `integracion.buro_snapshot` igual que una consulta automática — `InvestigacionRepository` no distingue el origen al mostrar el resultado, solo `ConsultaExterna.ReferenciaExterna` (`CARGA_DOCUMENTO` vs `CONSULTA_API`) lo deja trazado. El snapshot se reconstruye completo cada vez que se confirma un campo del mismo documento (nunca queda a medias entre confirmaciones sucesivas).
- **MFA**: si el usuario tiene MFA habilitado, el login se rechaza (`ApplicationError.Configuration`); nunca se omite el segundo factor.
- **Vigencia de obligaciones / creditos_activos**: decidido 2026-09-06. `obligacion.estado` admite VIGENTE, CANCELADA, CASTIGADA, REESTRUCTURADA; VIGENTE y REESTRUCTURADA cuentan como crédito activo (`AnalysisDecisions.ActiveDebtCount`). El análisis rechaza obligaciones con un estado fuera de ese catálogo. Ver docs/PENDING_DATABASE_CHANGES.md para el CHECK constraint propuesto (no ejecutado).

## Decidido: permanecen sin calcular

- `estabilidad_ingresos_score` e `historial_interno_score`: decidido 2026-09-06 que quedan NULL indefinidamente en `vector_caracteristicas`. El modelo predictivo entrenado aparte puede traer su propio scoring interno sin depender de estas columnas; MAPAN no fabrica una fórmula. Si en el futuro se define una, se agrega en `AnalysisDecisions`.

## Bloqueos de negocio pendientes

No se identifican bloqueos adicionales sobre el flujo de análisis/aprobación en esta revisión. Lo que falta para ML es exclusivamente el artefacto entrenado con datos reales (ver ITERATION_REPORT.md, sección "Lo que falta específicamente para ML") y, aparte del flujo de crédito, los módulos aún TODO/PARTIAL/BLOCKED de IMPLEMENTATION_PLAN.md (préstamos/desempeño, auditoría con filtros, informe final) que no dependen de decisiones de negocio sino de implementación.
