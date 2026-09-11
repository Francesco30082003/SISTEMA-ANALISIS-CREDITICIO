-- ============================================================================
-- PMCORP / MAPAN - MODELO FISICO COMPLETO (SOLO DDL)
-- Para entregar a Codex como contexto de arquitectura y mapeo.
-- PostgreSQL existente = FUENTE DE VERDAD.
-- NO usar este archivo para migrations automáticas ni EnsureCreated/EnsureDeleted.
-- Codex debe inspeccionar la BD real antes de mapear cada tabla.
-- ============================================================================

-- ============================================================================
-- PMCORP / MAPAN - NUCLEO PRINCIPAL COMPLETO PARA CONTEXTO DE CODEX
-- Revisado: 2026-09-05
-- IMPORTANTE:
--   1) La base PostgreSQL existente sigue siendo la FUENTE DE VERDAD.
--   2) Codex debe inspeccionar la BD real antes de crear mappings EF Core.
--   3) NO ejecutar migrations / EnsureCreated / EnsureDeleted.
--   4) Este archivo consolida el modelo acordado y corrige omisiones/sintaxis del TXT.
-- ============================================================================

--SCRIPTS database PMCORP
	--schemas microservicios
		CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE SCHEMA IF NOT EXISTS organizacion;

CREATE SCHEMA IF NOT EXISTS seguridad;

CREATE SCHEMA IF NOT EXISTS credito;

CREATE SCHEMA IF NOT EXISTS documentos;

CREATE SCHEMA IF NOT EXISTS politica;

CREATE SCHEMA IF NOT EXISTS riesgo;

CREATE SCHEMA IF NOT EXISTS flujo;

CREATE SCHEMA IF NOT EXISTS integracion;

CREATE SCHEMA IF NOT EXISTS auditoria;

--CORAZON TRANSACCIONAL 
			CREATE TABLE organizacion.empresa
		(
			empresa_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

			codigo VARCHAR(30) NOT NULL,
			nombre_legal VARCHAR(200) NOT NULL,
			nombre_comercial VARCHAR(200),

			tipo_identificacion_fiscal VARCHAR(20),
			identificacion_fiscal VARCHAR(30),

			pais_codigo CHAR(2) NOT NULL DEFAULT 'EC',
			moneda_codigo CHAR(3) NOT NULL DEFAULT 'USD',

			estado VARCHAR(20) NOT NULL DEFAULT 'ACTIVA',

			fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
			fecha_actualizacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

			CONSTRAINT uq_empresa_codigo
				UNIQUE (codigo),

			CONSTRAINT ck_empresa_estado
				CHECK (estado IN ('ACTIVA', 'INACTIVA', 'SUSPENDIDA'))
		);

--Relaciona EMPRESA 1 A N SUCURSALES
			CREATE TABLE organizacion.sucursal
		(
			sucursal_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

			empresa_id UUID NOT NULL,

			codigo VARCHAR(30) NOT NULL,
			nombre VARCHAR(150) NOT NULL,

			pais_codigo CHAR(2) NOT NULL DEFAULT 'EC',
			provincia VARCHAR(100),
			ciudad VARCHAR(100),

			direccion VARCHAR(300),

			estado VARCHAR(20) NOT NULL DEFAULT 'ACTIVA',

			fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

			CONSTRAINT fk_sucursal_empresa
				FOREIGN KEY (empresa_id)
				REFERENCES organizacion.empresa(empresa_id)
				ON DELETE RESTRICT,

			CONSTRAINT uq_sucursal_empresa_codigo
				UNIQUE (empresa_id, codigo),

			CONSTRAINT uq_sucursal_empresa_id
				UNIQUE (empresa_id, sucursal_id),

			CONSTRAINT ck_sucursal_estado
				CHECK (estado IN ('ACTIVA', 'INACTIVA'))
		);

-- Usuario sera global pertenece a varias empresas entonces N USUARIOS A N empresas
				CREATE TABLE seguridad.usuario
		(
			usuario_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

			nombre_usuario VARCHAR(100) NOT NULL,
			correo VARCHAR(200) NOT NULL,

			password_hash TEXT NOT NULL,

			nombres VARCHAR(120) NOT NULL,
			apellidos VARCHAR(120) NOT NULL,

			mfa_habilitado BOOLEAN NOT NULL DEFAULT FALSE,

			intentos_fallidos INTEGER NOT NULL DEFAULT 0,
			bloqueado_hasta TIMESTAMPTZ,

			ultimo_acceso TIMESTAMPTZ,

			estado VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',

			fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
			fecha_actualizacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

			CONSTRAINT uq_usuario_nombre
				UNIQUE (nombre_usuario),

			CONSTRAINT uq_usuario_correo
				UNIQUE (correo),

			CONSTRAINT ck_usuario_estado
				CHECK (estado IN ('ACTIVO', 'INACTIVO', 'BLOQUEADO'))
		);

-- TABLA INTERMEDIA AL SER N A N
		CREATE TABLE seguridad.usuario_empresa
		(
			usuario_empresa_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

			usuario_id UUID NOT NULL,
			empresa_id UUID NOT NULL,

			sucursal_predeterminada_id UUID,

			estado VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',

			fecha_ingreso TIMESTAMPTZ NOT NULL DEFAULT NOW(),

			CONSTRAINT fk_usuario_empresa_usuario
				FOREIGN KEY (usuario_id)
				REFERENCES seguridad.usuario(usuario_id)
				ON DELETE RESTRICT,

			CONSTRAINT fk_usuario_empresa_empresa
				FOREIGN KEY (empresa_id)
				REFERENCES organizacion.empresa(empresa_id)
				ON DELETE RESTRICT,

			CONSTRAINT fk_usuario_empresa_sucursal
				FOREIGN KEY (empresa_id, sucursal_predeterminada_id)
				REFERENCES organizacion.sucursal(empresa_id, sucursal_id)
				ON DELETE RESTRICT,

			CONSTRAINT uq_usuario_empresa
				UNIQUE (usuario_id, empresa_id),

			CONSTRAINT uq_usuario_empresa_tenant
				UNIQUE (empresa_id, usuario_empresa_id),

			CONSTRAINT ck_usuario_empresa_estado
				CHECK (estado IN ('ACTIVO', 'INACTIVO'))
		);

--roles se relaciona con empresas 1 empresa tiene N roles
		CREATE TABLE seguridad.rol
(
    rol_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    codigo VARCHAR(50) NOT NULL,
    nombre VARCHAR(100) NOT NULL,
    descripcion VARCHAR(300),

    estado VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_rol_empresa
        FOREIGN KEY (empresa_id)
        REFERENCES organizacion.empresa(empresa_id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_rol_empresa_codigo
        UNIQUE (empresa_id, codigo),

    CONSTRAINT uq_rol_empresa_id
        UNIQUE (empresa_id, rol_id)
);

-- PERMISOS GLOBALES DEL SISTEMA. LOS ROLES DE CADA EMPRESA SE ASOCIAN N:M A ESTOS PERMISOS.
CREATE TABLE seguridad.permiso
(
    permiso_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    codigo VARCHAR(100) NOT NULL,
    nombre VARCHAR(150) NOT NULL,
    descripcion VARCHAR(300),

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_permiso_codigo
        UNIQUE (codigo)
);

-- ROLES CON PERMISOS SON N A N 

CREATE TABLE seguridad.rol_permiso
(
    rol_id UUID NOT NULL,
    permiso_id UUID NOT NULL,

    fecha_asignacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    PRIMARY KEY (rol_id, permiso_id),

    CONSTRAINT fk_rol_permiso_rol
        FOREIGN KEY (rol_id)
        REFERENCES seguridad.rol(rol_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_rol_permiso_permiso
        FOREIGN KEY (permiso_id)
        REFERENCES seguridad.permiso(permiso_id)
        ON DELETE CASCADE
);

-- LA OTRA RELACION DEL USUARIO DE ESA EMPRESA UN ROL 

	CREATE TABLE seguridad.usuario_empresa_rol
(
    usuario_empresa_id UUID NOT NULL,
    rol_id UUID NOT NULL,

    fecha_asignacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    PRIMARY KEY (usuario_empresa_id, rol_id),

    CONSTRAINT fk_usuario_empresa_rol_usuario
        FOREIGN KEY (usuario_empresa_id)
        REFERENCES seguridad.usuario_empresa(usuario_empresa_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_usuario_empresa_rol_rol
        FOREIGN KEY (rol_id)
        REFERENCES seguridad.rol(rol_id)
        ON DELETE CASCADE
);

-- CLIENTE CON EMPRESAS UN CLINETE PUEDE ESTAR EN N EMPRESAS ENTONCES RELACIONAMOS CON EMPRESA  Y ASI NO DUPLICAMOS ENTRE empresas
CREATE TABLE credito.cliente
(
    cliente_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    tipo_persona VARCHAR(20) NOT NULL,

    tipo_identificacion VARCHAR(20) NOT NULL,
    numero_identificacion VARCHAR(30) NOT NULL,

    nombres VARCHAR(150),
    apellidos VARCHAR(150),

    razon_social VARCHAR(200),

    fecha_nacimiento DATE,

    telefono VARCHAR(30),
    correo VARCHAR(200),

    estado VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_actualizacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_cliente_empresa
        FOREIGN KEY (empresa_id)
        REFERENCES organizacion.empresa(empresa_id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_cliente_empresa_identificacion
        UNIQUE (
            empresa_id,
            tipo_identificacion,
            numero_identificacion
        ),

    CONSTRAINT uq_cliente_empresa_id
        UNIQUE (empresa_id, cliente_id),

    CONSTRAINT ck_cliente_tipo_persona
        CHECK (tipo_persona IN ('NATURAL', 'JURIDICA')),

    CONSTRAINT ck_cliente_estado
        CHECK (estado IN ('ACTIVO', 'INACTIVO'))
);

--Cada cooperativa o empresa puede definir sus productos 1 a NATIONAL
CREATE TABLE credito.producto_credito
(
    producto_credito_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    codigo VARCHAR(40) NOT NULL,
    nombre VARCHAR(150) NOT NULL,
    descripcion VARCHAR(500),

    monto_minimo NUMERIC(18,2),
    monto_maximo NUMERIC(18,2),

    plazo_minimo_meses INTEGER,
    plazo_maximo_meses INTEGER,

    moneda_codigo CHAR(3) NOT NULL DEFAULT 'USD',

    tasa_interes_anual_pct NUMERIC(9,4),
    tipo_tasa VARCHAR(20) DEFAULT 'FIJA',
    vigente_desde DATE,
    vigente_hasta DATE,

    estado VARCHAR(20) NOT NULL DEFAULT 'ACTIVO',

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_producto_empresa
        FOREIGN KEY (empresa_id)
        REFERENCES organizacion.empresa(empresa_id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_producto_empresa_codigo
        UNIQUE (empresa_id, codigo),

    CONSTRAINT uq_producto_empresa_id
        UNIQUE (empresa_id, producto_credito_id),

    CONSTRAINT ck_producto_montos
        CHECK (
            monto_minimo IS NULL
            OR monto_maximo IS NULL
            OR monto_maximo >= monto_minimo
        ),

    CONSTRAINT ck_producto_plazos
        CHECK (
            plazo_minimo_meses IS NULL
            OR plazo_maximo_meses IS NULL
            OR plazo_maximo_meses >= plazo_minimo_meses
        ),

    CONSTRAINT ck_producto_tasa
        CHECK (
            tasa_interes_anual_pct IS NULL
            OR tasa_interes_anual_pct >= 0
        ),

    CONSTRAINT ck_producto_vigencia
        CHECK (
            vigente_desde IS NULL
            OR vigente_hasta IS NULL
            OR vigente_hasta >= vigente_desde
        )
);

-- esta es la tabla central es el corazon ya que es el giro del negocio la solicitud del cliente 

CREATE TABLE credito.solicitud_credito
(
    solicitud_credito_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    sucursal_id UUID NOT NULL,
    cliente_id UUID NOT NULL,
    producto_credito_id UUID NOT NULL,

    numero_solicitud VARCHAR(50) NOT NULL,

    monto_solicitado NUMERIC(18,2) NOT NULL,
    plazo_solicitado_meses INTEGER NOT NULL,

    tasa_interes_anual_pct NUMERIC(9,4),

    capital_mensual_estimado NUMERIC(18,2),
    interes_mensual_estimado NUMERIC(18,2),
    cuota_estimada NUMERIC(18,2),
    interes_total_estimado NUMERIC(18,2),
    total_a_pagar_estimado NUMERIC(18,2),

    destino_credito VARCHAR(500),

    estado VARCHAR(30) NOT NULL DEFAULT 'BORRADOR',

    creado_por_usuario_empresa_id UUID NOT NULL,

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    fecha_envio TIMESTAMPTZ,
    fecha_finalizacion TIMESTAMPTZ,

    CONSTRAINT fk_solicitud_empresa
        FOREIGN KEY (empresa_id)
        REFERENCES organizacion.empresa(empresa_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_solicitud_sucursal
        FOREIGN KEY (empresa_id, sucursal_id)
        REFERENCES organizacion.sucursal(empresa_id, sucursal_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_solicitud_cliente
        FOREIGN KEY (empresa_id, cliente_id)
        REFERENCES credito.cliente(empresa_id, cliente_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_solicitud_producto
        FOREIGN KEY (empresa_id, producto_credito_id)
        REFERENCES credito.producto_credito(
            empresa_id,
            producto_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_solicitud_creado_por
        FOREIGN KEY (
            empresa_id,
            creado_por_usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa(
            empresa_id,
            usuario_empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_solicitud_empresa_numero
        UNIQUE (empresa_id, numero_solicitud),

    CONSTRAINT uq_solicitud_empresa_id
        UNIQUE (empresa_id, solicitud_credito_id),

    CONSTRAINT ck_solicitud_monto
        CHECK (monto_solicitado > 0),

    CONSTRAINT ck_solicitud_plazo
        CHECK (plazo_solicitado_meses > 0),

    CONSTRAINT ck_solicitud_estado
        CHECK (
            estado IN
            (
                'BORRADOR',
                'DOCUMENTACION',
                'VALIDACION',
                'ANALISIS',
                'REVISION',
                'APROBADA',
                'RECHAZADA',
                'CANCELADA'
            )
        )
);

--indices

CREATE INDEX ix_sucursal_empresa
ON organizacion.sucursal(empresa_id);

CREATE INDEX ix_usuario_empresa_empresa
ON seguridad.usuario_empresa(empresa_id);

CREATE INDEX ix_usuario_empresa_usuario
ON seguridad.usuario_empresa(usuario_id);

CREATE INDEX ix_rol_empresa
ON seguridad.rol(empresa_id);

CREATE INDEX ix_cliente_empresa
ON credito.cliente(empresa_id);

CREATE INDEX ix_cliente_identificacion
ON credito.cliente(
    empresa_id,
    numero_identificacion
);

CREATE INDEX ix_producto_empresa
ON credito.producto_credito(empresa_id);

CREATE INDEX ix_solicitud_empresa
ON credito.solicitud_credito(empresa_id);

CREATE INDEX ix_solicitud_cliente
ON credito.solicitud_credito(
    empresa_id,
    cliente_id
);

CREATE INDEX ix_solicitud_producto
ON credito.solicitud_credito(
    empresa_id,
    producto_credito_id
);

CREATE INDEX ix_solicitud_estado
ON credito.solicitud_credito(
    empresa_id,
    estado
);

CREATE INDEX ix_solicitud_fecha
ON credito.solicitud_credito(
    empresa_id,
    fecha_creacion DESC
);

---part 2 negocio 
CREATE TABLE credito.actividad_economica
(
    actividad_economica_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,
    solicitud_credito_id UUID NOT NULL,

    tipo_actividad VARCHAR(30) NOT NULL,

    empleador_negocio VARCHAR(200),
    cargo_actividad VARCHAR(150),

    fecha_inicio DATE,

    ruc VARCHAR(20),

    descripcion VARCHAR(300),

    es_principal BOOLEAN NOT NULL DEFAULT TRUE,
    verificada BOOLEAN NOT NULL DEFAULT FALSE,

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_actividad_solicitud
        FOREIGN KEY (
            empresa_id,
            solicitud_credito_id
        )
        REFERENCES credito.solicitud_credito(
            empresa_id,
            solicitud_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_actividad_empresa_id
        UNIQUE (
            empresa_id,
            actividad_economica_id
        ),

    CONSTRAINT uq_actividad_solicitud_id
        UNIQUE (
            empresa_id,
            solicitud_credito_id,
            actividad_economica_id
        )
);

-- fuente de ingrso se relacion acon la actividad economica 
CREATE TABLE credito.fuente_ingreso
(
    fuente_ingreso_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,
    solicitud_credito_id UUID NOT NULL,

    actividad_economica_id UUID,

    tipo_ingreso VARCHAR(50) NOT NULL,

    descripcion VARCHAR(200),

    moneda_codigo CHAR(3) NOT NULL DEFAULT 'USD',

    es_recurrente BOOLEAN NOT NULL DEFAULT TRUE,

    declarado BOOLEAN NOT NULL DEFAULT TRUE,
    verificado BOOLEAN NOT NULL DEFAULT FALSE,

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_fuente_ingreso_solicitud
        FOREIGN KEY (
            empresa_id,
            solicitud_credito_id
        )
        REFERENCES credito.solicitud_credito(
            empresa_id,
            solicitud_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_fuente_ingreso_actividad
        FOREIGN KEY (
            empresa_id,
            solicitud_credito_id,
            actividad_economica_id
        )
        REFERENCES credito.actividad_economica(
            empresa_id,
            solicitud_credito_id,
            actividad_economica_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_fuente_ingreso_empresa_id
        UNIQUE (
            empresa_id,
            fuente_ingreso_id
        )
);

-- PERIODOS DE INGRESO. PERMITE GUARDAR 3, 6 O N MESES SIN CREAR COLUMNAS FIJAS POR MES.
CREATE TABLE credito.ingreso_periodo
(
    ingreso_periodo_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,
    fuente_ingreso_id UUID NOT NULL,

    periodo_inicio DATE NOT NULL,
    periodo_fin DATE NOT NULL,

    monto_bruto NUMERIC(18,2),
    monto_neto NUMERIC(18,2) NOT NULL,

    origen_dato VARCHAR(30),
    observacion VARCHAR(300),

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_ingreso_periodo_fuente
        FOREIGN KEY (
            empresa_id,
            fuente_ingreso_id
        )
        REFERENCES credito.fuente_ingreso(
            empresa_id,
            fuente_ingreso_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT ck_ingreso_periodo_fechas
        CHECK (periodo_fin >= periodo_inicio),

    CONSTRAINT ck_ingreso_periodo_monto_neto
        CHECK (monto_neto >= 0),

    CONSTRAINT ck_ingreso_periodo_monto_bruto
        CHECK (monto_bruto IS NULL OR monto_bruto >= 0)
);

-- cada solicitud e un cliente de uina surcusalde una empresa tiene diferentes gastos 

CREATE TABLE credito.gasto
(
    gasto_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,
    solicitud_credito_id UUID NOT NULL,

    tipo_gasto VARCHAR(50) NOT NULL,

    descripcion VARCHAR(200),

    monto_mensual NUMERIC(18,2) NOT NULL,

    origen_dato VARCHAR(30),

    declarado BOOLEAN NOT NULL DEFAULT TRUE,
    verificado BOOLEAN NOT NULL DEFAULT FALSE,

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_gasto_solicitud
        FOREIGN KEY (
            empresa_id,
            solicitud_credito_id
        )
        REFERENCES credito.solicitud_credito(
            empresa_id,
            solicitud_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT ck_gasto_monto
        CHECK (
            monto_mensual >= 0
        )
);

-- obligaciones que tiene cada cliente  se relacionada la solicitud que se empleo

CREATE TABLE credito.obligacion
(
    obligacion_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,
    solicitud_credito_id UUID NOT NULL,

    institucion VARCHAR(200),

    tipo_obligacion VARCHAR(50),

    numero_operacion_mascara VARCHAR(80),

    monto_original NUMERIC(18,2),

    saldo_actual NUMERIC(18,2),

    cuota_mensual NUMERIC(18,2),

    dias_mora_actual INTEGER NOT NULL DEFAULT 0,

    max_dias_mora_historico INTEGER NOT NULL DEFAULT 0,

    estado VARCHAR(30),

    es_garante BOOLEAN NOT NULL DEFAULT FALSE,

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_obligacion_solicitud
        FOREIGN KEY (
            empresa_id,
            solicitud_credito_id
        )
        REFERENCES credito.solicitud_credito(
            empresa_id,
            solicitud_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_obligacion_empresa_id
        UNIQUE (
            empresa_id,
            obligacion_id
        ),

    CONSTRAINT ck_obligacion_saldo
        CHECK (
            saldo_actual IS NULL
            OR saldo_actual >= 0
        ),

    CONSTRAINT ck_obligacion_cuota
        CHECK (
            cuota_mensual IS NULL
            OR cuota_mensual >= 0
        ),

    CONSTRAINT ck_obligacion_mora_actual
        CHECK (
            dias_mora_actual >= 0
        )
);

--INIDICES 
CREATE INDEX ix_actividad_solicitud
ON credito.actividad_economica
(
    empresa_id,
    solicitud_credito_id
);

CREATE INDEX ix_fuente_ingreso_solicitud
ON credito.fuente_ingreso
(
    empresa_id,
    solicitud_credito_id
);

CREATE INDEX ix_fuente_ingreso_actividad
ON credito.fuente_ingreso
(
    empresa_id,
    actividad_economica_id
);

CREATE INDEX ix_ingreso_periodo_fuente
ON credito.ingreso_periodo
(
    empresa_id,
    fuente_ingreso_id,
    periodo_inicio
);

CREATE INDEX ix_gasto_solicitud
ON credito.gasto
(
    empresa_id,
    solicitud_credito_id
);

CREATE INDEX ix_obligacion_solicitud
ON credito.obligacion
(
    empresa_id,
    solicitud_credito_id
);

--PARTE 3 POLITICAZ Y PARAEMTROS PARA AJUSTAR LA BASE A N EMPRESAS COOPERATIVAS BANCOS 

CREATE TABLE politica.politica_credito
(
    politica_credito_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    producto_credito_id UUID,

    codigo VARCHAR(60) NOT NULL,

    nombre VARCHAR(180) NOT NULL,

    descripcion TEXT,

    estado VARCHAR(20) NOT NULL DEFAULT 'ACTIVA',

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_politica_empresa
        FOREIGN KEY (empresa_id)
        REFERENCES organizacion.empresa(empresa_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_politica_producto
        FOREIGN KEY (
            empresa_id,
            producto_credito_id
        )
        REFERENCES credito.producto_credito(
            empresa_id,
            producto_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_politica_empresa_codigo
        UNIQUE (
            empresa_id,
            codigo
        ),

    CONSTRAINT uq_politica_empresa_id
        UNIQUE (
            empresa_id,
            politica_credito_id
        ),

    CONSTRAINT ck_politica_estado
        CHECK (
            estado IN (
                'ACTIVA',
                'INACTIVA'
            )
        )
);

-- POLITICA VERSION POR SEGURIDAD AUDITORIAS TRAZABILIDAD
CREATE TABLE politica.politica_version
(
    politica_version_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    politica_credito_id UUID NOT NULL,

    numero_version INTEGER NOT NULL,

    vigente_desde TIMESTAMPTZ NOT NULL,

    vigente_hasta TIMESTAMPTZ,

    estado VARCHAR(20) NOT NULL DEFAULT 'BORRADOR',

    creada_por_usuario_empresa_id UUID NOT NULL,

    aprobada_por_usuario_empresa_id UUID,

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    fecha_aprobacion TIMESTAMPTZ,

    CONSTRAINT fk_politica_version_politica
        FOREIGN KEY (
            empresa_id,
            politica_credito_id
        )
        REFERENCES politica.politica_credito(
            empresa_id,
            politica_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_politica_version_creador
        FOREIGN KEY (
            empresa_id,
            creada_por_usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa(
            empresa_id,
            usuario_empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_politica_version_aprobador
        FOREIGN KEY (
            empresa_id,
            aprobada_por_usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa(
            empresa_id,
            usuario_empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_politica_version
        UNIQUE (
            empresa_id,
            politica_credito_id,
            numero_version
        ),

    CONSTRAINT uq_politica_version_empresa_id
        UNIQUE (
            empresa_id,
            politica_version_id
        ),

    CONSTRAINT ck_politica_version_estado
        CHECK (
            estado IN (
                'BORRADOR',
                'VIGENTE',
                'INACTIVA',
                'ARCHIVADA'
            )
        ),

    CONSTRAINT ck_politica_version_fechas
        CHECK (
            vigente_hasta IS NULL
            OR vigente_hasta >= vigente_desde
        )
);

--PARAMETROS

CREATE TABLE politica.parametro
(
    parametro_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    politica_version_id UUID NOT NULL,

    codigo VARCHAR(100) NOT NULL,

    nombre VARCHAR(180) NOT NULL,

    tipo_dato VARCHAR(20) NOT NULL,

    valor_texto TEXT,

    valor_numerico NUMERIC(24,8),

    valor_booleano BOOLEAN,

    valor_fecha DATE,

    valor_json JSONB,

    descripcion TEXT,

    CONSTRAINT fk_parametro_politica_version
        FOREIGN KEY (
            empresa_id,
            politica_version_id
        )
        REFERENCES politica.politica_version(
            empresa_id,
            politica_version_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_parametro_version_codigo
        UNIQUE (
            empresa_id,
            politica_version_id,
            codigo
        ),

    CONSTRAINT ck_parametro_tipo
        CHECK (
            tipo_dato IN (
                'TEXTO',
                'NUMERO',
                'BOOLEANO',
                'FECHA',
                'JSON'
            )
        )
);

-- REGLAS SON DIREFERENTES A LOS PARAMETROS YA QUE MORA GENERE ALERTA ROJA ES REGLA PERO QUE SE DEBE DAR 0.500 CTVS EXTRAS ES UN  PARAMETRO DE EMPRESA

CREATE TABLE politica.regla
(
    regla_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    politica_version_id UUID NOT NULL,

    codigo VARCHAR(100) NOT NULL,

    nombre VARCHAR(180) NOT NULL,

    descripcion TEXT,

    prioridad INTEGER NOT NULL DEFAULT 100,

    severidad VARCHAR(20) NOT NULL DEFAULT 'INFO',

    etapa VARCHAR(20) NOT NULL DEFAULT 'ANALISIS',

    condicion_json JSONB NOT NULL,

    accion_json JSONB NOT NULL,

    activa BOOLEAN NOT NULL DEFAULT TRUE,

    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_regla_politica_version
        FOREIGN KEY (
            empresa_id,
            politica_version_id
        )
        REFERENCES politica.politica_version(
            empresa_id,
            politica_version_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_regla_empresa_id
        UNIQUE (
            empresa_id,
            regla_id
        ),

    CONSTRAINT uq_regla_version_codigo
        UNIQUE (
            empresa_id,
            politica_version_id,
            codigo
        ),

    CONSTRAINT ck_regla_severidad
        CHECK (
            severidad IN (
                'INFO',
                'VERDE',
                'AMARILLO',
                'ROJO'
            )
        ),

    CONSTRAINT ck_regla_etapa
        CHECK (
            etapa IN (
                'PREEVALUACION',
                'ANALISIS'
            )
        )
);

-- Fase 4: histórico de resultados de la preevaluación (buró/judicial/aval antes de pedir documentos).
CREATE TABLE politica.preevaluacion
(
    preevaluacion_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id UUID NOT NULL,
    solicitud_credito_id UUID NOT NULL,
    politica_version_id UUID NOT NULL,
    resultado_preliminar VARCHAR(30) NOT NULL,
    severidad_maxima VARCHAR(20),
    detalle_json JSONB,
    ejecutada_por_usuario_empresa_id UUID NOT NULL,
    fecha_ejecucion TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    CONSTRAINT ck_preevaluacion_resultado CHECK (resultado_preliminar IN ('APTO','REQUIERE_EXCEPCION','NO_APTO')),
    CONSTRAINT fk_preevaluacion_empresa FOREIGN KEY (empresa_id) REFERENCES organizacion.empresa(empresa_id) ON DELETE RESTRICT,
    CONSTRAINT fk_preevaluacion_solicitud FOREIGN KEY (empresa_id,solicitud_credito_id) REFERENCES credito.solicitud_credito(empresa_id,solicitud_credito_id) ON DELETE RESTRICT,
    CONSTRAINT fk_preevaluacion_politica_version FOREIGN KEY (empresa_id,politica_version_id) REFERENCES politica.politica_version(empresa_id,politica_version_id) ON DELETE RESTRICT
);

-- Fase 4: excepciones a política — motivo, observación, evidencia, quién solicita/resuelve, con auditoría.
CREATE TABLE politica.excepcion
(
    excepcion_id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    empresa_id UUID NOT NULL,
    solicitud_credito_id UUID NOT NULL,
    regla_id UUID,
    motivo_justificacion TEXT NOT NULL,
    observacion TEXT,
    evidencia TEXT,
    estado VARCHAR(20) NOT NULL DEFAULT 'SOLICITADA',
    solicitada_por_usuario_empresa_id UUID NOT NULL,
    fecha_solicitud TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    resuelta_por_usuario_empresa_id UUID,
    fecha_resolucion TIMESTAMPTZ,
    comentario_resolucion TEXT,
    CONSTRAINT ck_excepcion_estado CHECK (estado IN ('SOLICITADA','APROBADA','RECHAZADA')),
    CONSTRAINT fk_excepcion_empresa FOREIGN KEY (empresa_id) REFERENCES organizacion.empresa(empresa_id) ON DELETE RESTRICT,
    CONSTRAINT fk_excepcion_solicitud FOREIGN KEY (empresa_id,solicitud_credito_id) REFERENCES credito.solicitud_credito(empresa_id,solicitud_credito_id) ON DELETE RESTRICT,
    CONSTRAINT fk_excepcion_regla FOREIGN KEY (empresa_id,regla_id) REFERENCES politica.regla(empresa_id,regla_id) ON DELETE RESTRICT
);

--INIDICES

CREATE INDEX ix_politica_empresa
ON politica.politica_credito
(
    empresa_id
);

CREATE INDEX ix_politica_producto
ON politica.politica_credito
(
    empresa_id,
    producto_credito_id
);

CREATE INDEX ix_politica_version_vigencia
ON politica.politica_version
(
    empresa_id,
    politica_credito_id,
    estado,
    vigente_desde
);

CREATE INDEX ix_parametro_version
ON politica.parametro
(
    empresa_id,
    politica_version_id
);

CREATE INDEX ix_regla_version_prioridad
ON politica.regla
(
    empresa_id,
    politica_version_id,
    prioridad
);

--Parte 3 giuardar el analisis ques e realiza 

	CREATE TABLE riesgo.analisis
(
    analisis_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    solicitud_credito_id UUID NOT NULL,

    politica_version_id UUID NOT NULL,

    numero_ejecucion INTEGER NOT NULL,

    tipo_analisis VARCHAR(30)
        NOT NULL
        DEFAULT 'REGLAS',

    estado VARCHAR(30)
        NOT NULL
        DEFAULT 'INICIADO',

    ejecutado_por_usuario_empresa_id UUID NOT NULL,

    iniciado_en TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    finalizado_en TIMESTAMPTZ,

    CONSTRAINT fk_analisis_solicitud
        FOREIGN KEY
        (
            empresa_id,
            solicitud_credito_id
        )
        REFERENCES credito.solicitud_credito
        (
            empresa_id,
            solicitud_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_analisis_politica_version
        FOREIGN KEY
        (
            empresa_id,
            politica_version_id
        )
        REFERENCES politica.politica_version
        (
            empresa_id,
            politica_version_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_analisis_usuario
        FOREIGN KEY
        (
            empresa_id,
            ejecutado_por_usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa
        (
            empresa_id,
            usuario_empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_analisis_ejecucion
        UNIQUE
        (
            empresa_id,
            solicitud_credito_id,
            numero_ejecucion
        ),

    CONSTRAINT uq_analisis_empresa_id
        UNIQUE
        (
            empresa_id,
            analisis_id
        ),

    CONSTRAINT ck_analisis_tipo
        CHECK
        (
            tipo_analisis IN
            (
                'REGLAS',
                'PREDICTIVO',
                'COMPLETO'
            )
        ),

    CONSTRAINT ck_analisis_estado
        CHECK
        (
            estado IN
            (
                'INICIADO',
                'COMPLETADO',
                'ERROR',
                'CANCELADO'
            )
        )
);

CREATE TABLE riesgo.snapshot_financiero
(
    snapshot_financiero_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    analisis_id UUID NOT NULL,

    ingreso_total_mensual NUMERIC(18,2)
        NOT NULL,

    gasto_total_mensual NUMERIC(18,2)
        NOT NULL,

    ingreso_disponible NUMERIC(18,2)
        NOT NULL,

    factor_capacidad NUMERIC(9,6)
        NOT NULL,

    capacidad_nueva_cuota NUMERIC(18,2)
        NOT NULL,

    deuda_total_actual NUMERIC(18,2)
        NOT NULL
        DEFAULT 0,

    cuotas_actuales NUMERIC(18,2)
        NOT NULL
        DEFAULT 0,

    cuota_nueva_estimada NUMERIC(18,2)
        NOT NULL,

    ratio_endeudamiento_actual NUMERIC(12,8),

    ratio_endeudamiento_post NUMERIC(12,8),

    cuota_compatible BOOLEAN
        NOT NULL,

    fecha_calculo TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_snapshot_analisis
        FOREIGN KEY
        (
            empresa_id,
            analisis_id
        )
        REFERENCES riesgo.analisis
        (
            empresa_id,
            analisis_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_snapshot_analisis
        UNIQUE
        (
            empresa_id,
            analisis_id
        ),

    CONSTRAINT ck_snapshot_factor
        CHECK
        (
            factor_capacidad >= 0
            AND factor_capacidad <= 1
        ),

    CONSTRAINT ck_snapshot_ingresos
        CHECK
        (
            ingreso_total_mensual >= 0
        ),

    CONSTRAINT ck_snapshot_gastos
        CHECK
        (
            gasto_total_mensual >= 0
        ),

    CONSTRAINT ck_snapshot_deuda
        CHECK
        (
            deuda_total_actual >= 0
        ),

    CONSTRAINT ck_snapshot_cuotas
        CHECK
        (
            cuotas_actuales >= 0
        )
);

CREATE INDEX ix_analisis_solicitud
ON riesgo.analisis
(
    empresa_id,
    solicitud_credito_id,
    numero_ejecucion DESC
);

CREATE INDEX ix_analisis_politica
ON riesgo.analisis
(
    empresa_id,
    politica_version_id
);

---riesgo de alerta se relaciona con el analisis por que una soliciutd se puede analizar varias veces 

CREATE TABLE riesgo.alerta
(
    alerta_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    analisis_id UUID NOT NULL,

    regla_id UUID,

    codigo VARCHAR(100) NOT NULL,

    nivel VARCHAR(20) NOT NULL,

    titulo VARCHAR(200) NOT NULL,

    descripcion TEXT NOT NULL,

    resuelta BOOLEAN
        NOT NULL
        DEFAULT FALSE,

    resuelta_por_usuario_empresa_id UUID,

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    fecha_resolucion TIMESTAMPTZ,

    CONSTRAINT fk_alerta_analisis
        FOREIGN KEY
        (
            empresa_id,
            analisis_id
        )
        REFERENCES riesgo.analisis
        (
            empresa_id,
            analisis_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_alerta_regla
        FOREIGN KEY
        (
            empresa_id,
            regla_id
        )
        REFERENCES politica.regla
        (
            empresa_id,
            regla_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_alerta_resuelta_por
        FOREIGN KEY
        (
            empresa_id,
            resuelta_por_usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa
        (
            empresa_id,
            usuario_empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT ck_alerta_nivel
        CHECK
        (
            nivel IN
            (
                'INFO',
                'VERDE',
                'AMARILLO',
                'ROJO'
            )
        )
);

CREATE INDEX ix_alerta_analisis
ON riesgo.alerta
(
    empresa_id,
    analisis_id,
    nivel
);

--CREAMOS LA BASE QUE ALMACENA LAS VARIABLES QUE VAMOS USAR PARA NUESTRO MODEL

CREATE TABLE riesgo.vector_caracteristicas
(
    vector_caracteristicas_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    analisis_id UUID NOT NULL,

    -- 12 variables base

    ingreso_mensual NUMERIC(18,2),

    gastos_mensuales NUMERIC(18,2),

    cuotas_otras_deudas NUMERIC(18,2),

    deuda_total_actual NUMERIC(18,2),

    monto_solicitado NUMERIC(18,2),

    plazo_meses INTEGER,

    cuota_estimada NUMERIC(18,2),

    max_dias_mora_historico INTEGER,

    creditos_activos INTEGER,

    antiguedad_actividad_meses INTEGER,

    estabilidad_ingresos_score NUMERIC(12,8),

    historial_interno_score NUMERIC(12,8),

    -- Variables derivadas

    ingreso_disponible NUMERIC(18,2),

    capacidad_nueva_cuota NUMERIC(18,2),

    deuda_sobre_ingreso NUMERIC(12,8),

    cuota_sobre_ingreso NUMERIC(12,8),

    monto_sobre_ingreso NUMERIC(12,8),

    variables_adicionales JSONB,

    fecha_snapshot TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_vector_analisis
        FOREIGN KEY
        (
            empresa_id,
            analisis_id
        )
        REFERENCES riesgo.analisis
        (
            empresa_id,
            analisis_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_vector_analisis
        UNIQUE
        (
            empresa_id,
            analisis_id
        )
);

CREATE TABLE riesgo.modelo
(
    modelo_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    codigo VARCHAR(100) NOT NULL,

    nombre VARCHAR(200) NOT NULL,

    descripcion TEXT,

    objetivo VARCHAR(200) NOT NULL,

    estado VARCHAR(20)
        NOT NULL
        DEFAULT 'BORRADOR',

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_modelo_empresa
        FOREIGN KEY (empresa_id)
        REFERENCES organizacion.empresa(empresa_id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_modelo_empresa_codigo
        UNIQUE
        (
            empresa_id,
            codigo
        ),

    CONSTRAINT uq_modelo_empresa_id
        UNIQUE
        (
            empresa_id,
            modelo_id
        ),

    CONSTRAINT ck_modelo_estado
        CHECK
        (
            estado IN
            (
                'BORRADOR',
                'ACTIVO',
                'INACTIVO',
                'ARCHIVADO'
            )
        )
);

CREATE TABLE riesgo.modelo_version
(
    modelo_version_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,
    modelo_id UUID NOT NULL,
    numero_version INTEGER NOT NULL,
    algoritmo VARCHAR(100) NOT NULL,
    descripcion TEXT,

    artefacto_uri TEXT,
    esquema_caracteristicas JSONB NOT NULL,
    hiperparametros JSONB,

    fecha_datos_desde DATE,
    fecha_datos_hasta DATE,
    cantidad_registros INTEGER,
    cantidad_positivos INTEGER,
    cantidad_negativos INTEGER,

    roc_auc NUMERIC(8,6),
    precision_score NUMERIC(8,6),
    recall_score NUMERIC(8,6),
    f1_score NUMERIC(8,6),
    accuracy_score NUMERIC(8,6),

    umbral_decision NUMERIC(8,6),

    estado VARCHAR(20)
        NOT NULL
        DEFAULT 'BORRADOR',

    fecha_entrenamiento TIMESTAMPTZ,
    fecha_validacion TIMESTAMPTZ,
    fecha_creacion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_modelo_version_modelo
        FOREIGN KEY (
            empresa_id,
            modelo_id
        )
        REFERENCES riesgo.modelo(
            empresa_id,
            modelo_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_modelo_version
        UNIQUE (
            empresa_id,
            modelo_id,
            numero_version
        ),

    CONSTRAINT uq_modelo_version_empresa_id
        UNIQUE (
            empresa_id,
            modelo_version_id
        ),

    CONSTRAINT ck_modelo_version_estado
        CHECK (
            estado IN (
                'BORRADOR',
                'ENTRENADO',
                'VALIDADO',
                'PRODUCCION',
                'SHADOW',
                'RETIRADO'
            )
        ),

    CONSTRAINT ck_modelo_version_metricas
        CHECK (
            (roc_auc IS NULL OR roc_auc BETWEEN 0 AND 1)
            AND (precision_score IS NULL OR precision_score BETWEEN 0 AND 1)
            AND (recall_score IS NULL OR recall_score BETWEEN 0 AND 1)
            AND (f1_score IS NULL OR f1_score BETWEEN 0 AND 1)
            AND (accuracy_score IS NULL OR accuracy_score BETWEEN 0 AND 1)
        ),

    CONSTRAINT ck_modelo_version_umbral
        CHECK (
            umbral_decision IS NULL
            OR umbral_decision BETWEEN 0 AND 1
        )
);

CREATE TABLE riesgo.prediccion
(
    prediccion_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,
    analisis_id UUID NOT NULL,
    modelo_version_id UUID NOT NULL,

    probabilidad_incumplimiento NUMERIC(12,10) NOT NULL,
    umbral_utilizado NUMERIC(12,10) NOT NULL,
    clase_predicha VARCHAR(30) NOT NULL,
    nivel_riesgo VARCHAR(20),
    tiempo_inferencia_ms INTEGER,
    fecha_prediccion TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_prediccion_analisis
        FOREIGN KEY (
            empresa_id,
            analisis_id
        )
        REFERENCES riesgo.analisis(
            empresa_id,
            analisis_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_prediccion_modelo_version
        FOREIGN KEY (
            empresa_id,
            modelo_version_id
        )
        REFERENCES riesgo.modelo_version(
            empresa_id,
            modelo_version_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_prediccion_analisis_modelo
        UNIQUE (
            empresa_id,
            analisis_id,
            modelo_version_id
        ),

    CONSTRAINT ck_prediccion_probabilidad
        CHECK (probabilidad_incumplimiento BETWEEN 0 AND 1),

    CONSTRAINT ck_prediccion_umbral
        CHECK (umbral_utilizado BETWEEN 0 AND 1),

    CONSTRAINT ck_prediccion_clase
        CHECK (
            clase_predicha IN (
                'NO_INCUMPLIMIENTO',
                'INCUMPLIMIENTO'
            )
        ),

    CONSTRAINT ck_prediccion_nivel
        CHECK (
            nivel_riesgo IS NULL
            OR nivel_riesgo IN (
                'BAJO',
                'MEDIO',
                'ALTO'
            )
        )
);

CREATE TABLE riesgo.prediccion_factor
(
    prediccion_factor_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    prediccion_id UUID NOT NULL,

    codigo_caracteristica VARCHAR(120) NOT NULL,

    nombre_caracteristica VARCHAR(200),

    valor_numerico NUMERIC(24,10),

    valor_texto TEXT,

    contribucion NUMERIC(24,10) NOT NULL,

    direccion VARCHAR(30) NOT NULL,

    posicion_importancia INTEGER,

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_prediccion_factor_prediccion
        FOREIGN KEY
        (
            prediccion_id
        )
        REFERENCES riesgo.prediccion
        (
            prediccion_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_prediccion_factor
        UNIQUE
        (
            prediccion_id,
            codigo_caracteristica
        ),

    CONSTRAINT ck_prediccion_factor_direccion
        CHECK
        (
            direccion IN
            (
                'AUMENTA_RIESGO',
                'REDUCE_RIESGO',
                'NEUTRO'
            )
        )
);

CREATE TABLE riesgo.recomendacion
(
    recomendacion_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    analisis_id UUID NOT NULL,

    prediccion_id UUID,

    codigo_recomendacion VARCHAR(60)
        NOT NULL,

    nivel_riesgo_final VARCHAR(30),

    cumple_capacidad BOOLEAN,

    severidad_maxima_reglas VARCHAR(20),

    probabilidad_incumplimiento NUMERIC(12,10),

    requiere_revision_humana BOOLEAN
        NOT NULL
        DEFAULT TRUE,

    resumen TEXT,

    detalle_json JSONB,

    fecha_generacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_recomendacion_analisis
        FOREIGN KEY
        (
            empresa_id,
            analisis_id
        )
        REFERENCES riesgo.analisis
        (
            empresa_id,
            analisis_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_recomendacion_prediccion
        FOREIGN KEY
        (
            prediccion_id
        )
        REFERENCES riesgo.prediccion
        (
            prediccion_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_recomendacion_analisis
        UNIQUE
        (
            empresa_id,
            analisis_id
        ),

    CONSTRAINT ck_recomendacion_probabilidad
        CHECK
        (
            probabilidad_incumplimiento IS NULL
            OR probabilidad_incumplimiento
               BETWEEN 0 AND 1
        )
);

CREATE INDEX ix_recomendacion_analisis
ON riesgo.recomendacion
(
    empresa_id,
    analisis_id
);

CREATE INDEX ix_prediccion_factor_prediccion
ON riesgo.prediccion_factor
(
    prediccion_id,
    posicion_importancia
);

CREATE INDEX ix_modelo_empresa
ON riesgo.modelo
(
    empresa_id
);

CREATE INDEX ix_modelo_version_modelo
ON riesgo.modelo_version
(
    empresa_id,
    modelo_id,
    numero_version
);

CREATE INDEX ix_prediccion_analisis
ON riesgo.prediccion
(
    empresa_id,
    analisis_id
);

CREATE TABLE flujo.ruta_aprobacion
(
    ruta_aprobacion_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    producto_credito_id UUID,

    codigo VARCHAR(100) NOT NULL,

    nombre VARCHAR(200) NOT NULL,

    descripcion TEXT,

    prioridad INTEGER
        NOT NULL
        DEFAULT 100,

    es_predeterminada BOOLEAN
        NOT NULL
        DEFAULT FALSE,

    activa BOOLEAN
        NOT NULL
        DEFAULT TRUE,

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_ruta_empresa
        FOREIGN KEY (empresa_id)
        REFERENCES organizacion.empresa(empresa_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_ruta_producto
        FOREIGN KEY
        (
            empresa_id,
            producto_credito_id
        )
        REFERENCES credito.producto_credito
        (
            empresa_id,
            producto_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_ruta_empresa_codigo
        UNIQUE
        (
            empresa_id,
            codigo
        ),

    CONSTRAINT uq_ruta_empresa_id
        UNIQUE
        (
            empresa_id,
            ruta_aprobacion_id
        )
);

CREATE TABLE flujo.ruta_aprobacion_paso
(
    ruta_aprobacion_paso_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    ruta_aprobacion_id UUID NOT NULL,

    orden INTEGER NOT NULL,

    nombre VARCHAR(200) NOT NULL,

    rol_id UUID NOT NULL,

    obligatorio BOOLEAN
        NOT NULL
        DEFAULT TRUE,

    cantidad_aprobaciones_requeridas INTEGER
        NOT NULL
        DEFAULT 1,

    permite_aprobar BOOLEAN
        NOT NULL
        DEFAULT TRUE,

    permite_rechazar BOOLEAN
        NOT NULL
        DEFAULT TRUE,

    permite_devolver BOOLEAN
        NOT NULL
        DEFAULT TRUE,

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_ruta_paso_ruta
        FOREIGN KEY
        (
            empresa_id,
            ruta_aprobacion_id
        )
        REFERENCES flujo.ruta_aprobacion
        (
            empresa_id,
            ruta_aprobacion_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_ruta_paso_rol
        FOREIGN KEY
        (
            empresa_id,
            rol_id
        )
        REFERENCES seguridad.rol
        (
            empresa_id,
            rol_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_ruta_paso_orden
        UNIQUE
        (
            empresa_id,
            ruta_aprobacion_id,
            orden
        ),

    CONSTRAINT uq_ruta_paso_empresa_id
        UNIQUE
        (
            empresa_id,
            ruta_aprobacion_paso_id
        ),

    CONSTRAINT ck_ruta_paso_orden
        CHECK (orden > 0),

    CONSTRAINT ck_ruta_paso_cantidad
        CHECK (cantidad_aprobaciones_requeridas > 0)
);

CREATE TABLE flujo.regla_enrutamiento
(
    regla_enrutamiento_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    ruta_aprobacion_id UUID NOT NULL,

    codigo VARCHAR(100) NOT NULL,

    nombre VARCHAR(200) NOT NULL,

    descripcion TEXT,

    prioridad INTEGER
        NOT NULL
        DEFAULT 100,

    condicion_json JSONB NOT NULL,

    activa BOOLEAN
        NOT NULL
        DEFAULT TRUE,

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_regla_enrutamiento_ruta
        FOREIGN KEY
        (
            empresa_id,
            ruta_aprobacion_id
        )
        REFERENCES flujo.ruta_aprobacion
        (
            empresa_id,
            ruta_aprobacion_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_regla_enrutamiento_codigo
        UNIQUE
        (
            empresa_id,
            codigo
        )
);

ALTER TABLE riesgo.recomendacion
ADD CONSTRAINT uq_recomendacion_empresa_id
UNIQUE
(
    empresa_id,
    recomendacion_id
);

CREATE TABLE flujo.aprobacion
(
    aprobacion_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    recomendacion_id UUID NOT NULL,

    ruta_aprobacion_id UUID NOT NULL,

    estado VARCHAR(30)
        NOT NULL
        DEFAULT 'PENDIENTE',

    creada_por_usuario_empresa_id UUID,

    fecha_inicio TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    fecha_fin TIMESTAMPTZ,

    CONSTRAINT fk_aprobacion_recomendacion
        FOREIGN KEY
        (
            empresa_id,
            recomendacion_id
        )
        REFERENCES riesgo.recomendacion
        (
            empresa_id,
            recomendacion_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_aprobacion_ruta
        FOREIGN KEY
        (
            empresa_id,
            ruta_aprobacion_id
        )
        REFERENCES flujo.ruta_aprobacion
        (
            empresa_id,
            ruta_aprobacion_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_aprobacion_creada_por
        FOREIGN KEY
        (
            empresa_id,
            creada_por_usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa
        (
            empresa_id,
            usuario_empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_aprobacion_empresa_id
        UNIQUE
        (
            empresa_id,
            aprobacion_id
        ),

    CONSTRAINT ck_aprobacion_estado
        CHECK
        (
            estado IN
            (
                'PENDIENTE',
                'EN_CURSO',
                'APROBADA',
                'RECHAZADA',
                'DEVUELTA',
                'CANCELADA'
            )
        )
);

CREATE TABLE flujo.aprobacion_paso
(
    aprobacion_paso_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    aprobacion_id UUID NOT NULL,

    ruta_aprobacion_paso_id UUID NOT NULL,

    orden INTEGER NOT NULL,

    nombre_paso VARCHAR(200) NOT NULL,

    rol_id UUID NOT NULL,

    cantidad_aprobaciones_requeridas INTEGER
        NOT NULL
        DEFAULT 1,

    estado VARCHAR(30)
        NOT NULL
        DEFAULT 'PENDIENTE',

    fecha_habilitacion TIMESTAMPTZ,

    fecha_completado TIMESTAMPTZ,

    CONSTRAINT fk_aprobacion_paso_aprobacion
        FOREIGN KEY
        (
            empresa_id,
            aprobacion_id
        )
        REFERENCES flujo.aprobacion
        (
            empresa_id,
            aprobacion_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_aprobacion_paso_config
        FOREIGN KEY
        (
            empresa_id,
            ruta_aprobacion_paso_id
        )
        REFERENCES flujo.ruta_aprobacion_paso
        (
            empresa_id,
            ruta_aprobacion_paso_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_aprobacion_paso_rol
        FOREIGN KEY
        (
            empresa_id,
            rol_id
        )
        REFERENCES seguridad.rol
        (
            empresa_id,
            rol_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_aprobacion_paso_orden
        UNIQUE
        (
            empresa_id,
            aprobacion_id,
            orden
        ),

    CONSTRAINT uq_aprobacion_paso_empresa_id
        UNIQUE
        (
            empresa_id,
            aprobacion_paso_id
        ),

    CONSTRAINT ck_aprobacion_paso_estado
        CHECK
        (
            estado IN
            (
                'PENDIENTE',
                'ACTIVO',
                'APROBADO',
                'RECHAZADO',
                'DEVUELTO',
                'OMITIDO'
            )
        )
);

CREATE TABLE flujo.decision_paso
(
    decision_paso_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    aprobacion_paso_id UUID NOT NULL,

    usuario_empresa_id UUID NOT NULL,

    decision VARCHAR(30) NOT NULL,

    comentario TEXT,

    fecha_decision TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_decision_paso
        FOREIGN KEY
        (
            empresa_id,
            aprobacion_paso_id
        )
        REFERENCES flujo.aprobacion_paso
        (
            empresa_id,
            aprobacion_paso_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_decision_usuario
        FOREIGN KEY
        (
            empresa_id,
            usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa
        (
            empresa_id,
            usuario_empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_decision_usuario_paso
        UNIQUE
        (
            empresa_id,
            aprobacion_paso_id,
            usuario_empresa_id
        ),

    CONSTRAINT ck_decision
        CHECK
        (
            decision IN
            (
                'APROBAR',
                'RECHAZAR',
                'DEVOLVER',
                'ABSTENERSE'
            )
        )
);

CREATE INDEX ix_ruta_empresa_producto
ON flujo.ruta_aprobacion
(
    empresa_id,
    producto_credito_id,
    activa
);

CREATE INDEX ix_regla_enrutamiento
ON flujo.regla_enrutamiento
(
    empresa_id,
    prioridad
);

CREATE INDEX ix_aprobacion_recomendacion
ON flujo.aprobacion
(
    empresa_id,
    recomendacion_id
);

CREATE INDEX ix_aprobacion_paso_estado
ON flujo.aprobacion_paso
(
    empresa_id,
    aprobacion_id,
    estado
);

CREATE INDEX ix_decision_paso
ON flujo.decision_paso
(
    empresa_id,
    aprobacion_paso_id
);

CREATE TABLE credito.prestamo
(
    prestamo_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    solicitud_credito_id UUID NOT NULL,

    numero_prestamo VARCHAR(80) NOT NULL,

    monto_desembolsado NUMERIC(18,2) NOT NULL,

    fecha_desembolso DATE NOT NULL,

    plazo_meses INTEGER NOT NULL,

    tasa_interes_anual_pct NUMERIC(9,6),

    cuota_pactada NUMERIC(18,2),

    fecha_vencimiento DATE,

    estado VARCHAR(30)
        NOT NULL
        DEFAULT 'VIGENTE',

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_prestamo_solicitud
        FOREIGN KEY
        (
            empresa_id,
            solicitud_credito_id
        )
        REFERENCES credito.solicitud_credito
        (
            empresa_id,
            solicitud_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_prestamo_solicitud
        UNIQUE
        (
            empresa_id,
            solicitud_credito_id
        ),

    CONSTRAINT uq_prestamo_numero
        UNIQUE
        (
            empresa_id,
            numero_prestamo
        ),

    CONSTRAINT uq_prestamo_empresa_id
        UNIQUE
        (
            empresa_id,
            prestamo_id
        ),

    CONSTRAINT ck_prestamo_monto
        CHECK
        (
            monto_desembolsado > 0
        ),

    CONSTRAINT ck_prestamo_plazo
        CHECK
        (
            plazo_meses > 0
        ),

    CONSTRAINT ck_prestamo_estado
        CHECK
        (
            estado IN
            (
                'VIGENTE',
                'LIQUIDADO',
                'VENCIDO',
                'REESTRUCTURADO',
                'CASTIGADO',
                'CANCELADO'
            )
        )
);

CREATE TABLE credito.desempeno_credito
(
    desempeno_credito_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    prestamo_id UUID NOT NULL,

    fecha_corte DATE NOT NULL,

    saldo_capital NUMERIC(18,2)
        NOT NULL
        DEFAULT 0,

    cuota_exigible NUMERIC(18,2)
        NOT NULL
        DEFAULT 0,

    monto_pagado_periodo NUMERIC(18,2)
        NOT NULL
        DEFAULT 0,

    dias_mora INTEGER
        NOT NULL
        DEFAULT 0,

    mora_15 BOOLEAN
        GENERATED ALWAYS AS
        (dias_mora >= 15)
        STORED,

    mora_30 BOOLEAN
        GENERATED ALWAYS AS
        (dias_mora >= 30)
        STORED,

    mora_60 BOOLEAN
        GENERATED ALWAYS AS
        (dias_mora >= 60)
        STORED,

    mora_90 BOOLEAN
        GENERATED ALWAYS AS
        (dias_mora >= 90)
        STORED,

    refinanciado BOOLEAN
        NOT NULL
        DEFAULT FALSE,

    reestructurado BOOLEAN
        NOT NULL
        DEFAULT FALSE,

    castigado BOOLEAN
        NOT NULL
        DEFAULT FALSE,

    estado_cartera VARCHAR(60),

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_desempeno_prestamo
        FOREIGN KEY
        (
            empresa_id,
            prestamo_id
        )
        REFERENCES credito.prestamo
        (
            empresa_id,
            prestamo_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_desempeno_periodo
        UNIQUE
        (
            empresa_id,
            prestamo_id,
            fecha_corte
        ),

    CONSTRAINT ck_desempeno_saldo
        CHECK
        (
            saldo_capital >= 0
        ),

    CONSTRAINT ck_desempeno_cuota
        CHECK
        (
            cuota_exigible >= 0
        ),

    CONSTRAINT ck_desempeno_pago
        CHECK
        (
            monto_pagado_periodo >= 0
        ),

    CONSTRAINT ck_desempeno_mora
        CHECK
        (
            dias_mora >= 0
        )
);

CREATE TABLE documentos.documento
(
    documento_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    cliente_id UUID NOT NULL,

    tipo_documento VARCHAR(100) NOT NULL,

    nombre VARCHAR(200) NOT NULL,

    numero_documento VARCHAR(120),

    emisor VARCHAR(200),

    fecha_emision DATE,

    fecha_vencimiento DATE,

    estado VARCHAR(30)
        NOT NULL
        DEFAULT 'ACTIVO',

    creado_por_usuario_empresa_id UUID,

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_documento_cliente
        FOREIGN KEY
        (
            empresa_id,
            cliente_id
        )
        REFERENCES credito.cliente
        (
            empresa_id,
            cliente_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_documento_creado_por
        FOREIGN KEY
        (
            empresa_id,
            creado_por_usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa
        (
            empresa_id,
            usuario_empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_documento_empresa_id
        UNIQUE
        (
            empresa_id,
            documento_id
        ),

    CONSTRAINT ck_documento_estado
        CHECK
        (
            estado IN
            (
                'ACTIVO',
                'INACTIVO',
                'ANULADO'
            )
        )
);

CREATE TABLE documentos.documento_version
(
    documento_version_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    documento_id UUID NOT NULL,

    numero_version INTEGER NOT NULL,

    nombre_archivo VARCHAR(300) NOT NULL,

    mime_type VARCHAR(120),

    tamano_bytes BIGINT,

    almacenamiento_uri TEXT NOT NULL,

    hash_sha256 VARCHAR(64),

    origen VARCHAR(30)
        NOT NULL
        DEFAULT 'CARGA_MANUAL',

    creado_por_usuario_empresa_id UUID,

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_documento_version_documento
        FOREIGN KEY
        (
            empresa_id,
            documento_id
        )
        REFERENCES documentos.documento
        (
            empresa_id,
            documento_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_documento_version_usuario
        FOREIGN KEY
        (
            empresa_id,
            creado_por_usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa
        (
            empresa_id,
            usuario_empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_documento_version
        UNIQUE
        (
            empresa_id,
            documento_id,
            numero_version
        ),

    CONSTRAINT uq_documento_version_empresa_id
        UNIQUE
        (
            empresa_id,
            documento_version_id
        ),

    CONSTRAINT ck_documento_version_numero
        CHECK
        (
            numero_version > 0
        ),

    CONSTRAINT ck_documento_version_tamano
        CHECK
        (
            tamano_bytes IS NULL
            OR tamano_bytes >= 0
        ),

    CONSTRAINT ck_documento_version_origen
        CHECK
        (
            origen IN
            (
                'CARGA_MANUAL',
                'API',
                'GENERADO'
            )
        )
);

CREATE TABLE documentos.solicitud_documento
(
    solicitud_documento_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    solicitud_credito_id UUID NOT NULL,

    documento_version_id UUID NOT NULL,

    uso_documento VARCHAR(100),

    obligatorio BOOLEAN
        NOT NULL
        DEFAULT FALSE,

    estado VARCHAR(30)
        NOT NULL
        DEFAULT 'PENDIENTE',

    asociado_por_usuario_empresa_id UUID,

    fecha_asociacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_solicitud_documento_solicitud
        FOREIGN KEY
        (
            empresa_id,
            solicitud_credito_id
        )
        REFERENCES credito.solicitud_credito
        (
            empresa_id,
            solicitud_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_solicitud_documento_version
        FOREIGN KEY
        (
            empresa_id,
            documento_version_id
        )
        REFERENCES documentos.documento_version
        (
            empresa_id,
            documento_version_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_solicitud_documento_usuario
        FOREIGN KEY
        (
            empresa_id,
            asociado_por_usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa
        (
            empresa_id,
            usuario_empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_solicitud_documento
        UNIQUE
        (
            empresa_id,
            solicitud_credito_id,
            documento_version_id
        ),

    CONSTRAINT uq_solicitud_documento_empresa_id
        UNIQUE
        (
            empresa_id,
            solicitud_documento_id
        ),

    CONSTRAINT ck_solicitud_documento_estado
        CHECK
        (
            estado IN
            (
                'PENDIENTE',
                'VALIDADO',
                'RECHAZADO',
                'REEMPLAZADO'
            )
        )
);

CREATE TABLE documentos.dato_extraido
(
    dato_extraido_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    documento_version_id UUID NOT NULL,

    codigo_campo VARCHAR(120) NOT NULL,

    nombre_campo VARCHAR(200),

    indice_ocurrencia INTEGER
        NOT NULL
        DEFAULT 1,

    tipo_dato VARCHAR(20)
        NOT NULL,

    valor_original TEXT,

    valor_texto TEXT,

    valor_numerico NUMERIC(24,8),

    valor_fecha DATE,

    valor_booleano BOOLEAN,

    valor_json JSONB,

    pagina INTEGER,

    confianza NUMERIC(8,6),

    modelo_extraccion VARCHAR(150),

    version_extractor VARCHAR(80),

    ubicacion_json JSONB,

    fecha_extraccion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_dato_extraido_documento
        FOREIGN KEY
        (
            empresa_id,
            documento_version_id
        )
        REFERENCES documentos.documento_version
        (
            empresa_id,
            documento_version_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_dato_extraido_empresa_id
        UNIQUE
        (
            empresa_id,
            dato_extraido_id
        ),

    CONSTRAINT uq_dato_extraido_ocurrencia
        UNIQUE
        (
            empresa_id,
            documento_version_id,
            codigo_campo,
            indice_ocurrencia
        ),

    CONSTRAINT ck_dato_extraido_tipo
        CHECK
        (
            tipo_dato IN
            (
                'TEXTO',
                'NUMERO',
                'FECHA',
                'BOOLEANO',
                'JSON'
            )
        ),

    CONSTRAINT ck_dato_extraido_confianza
        CHECK
        (
            confianza IS NULL
            OR confianza BETWEEN 0 AND 1
        ),

    CONSTRAINT ck_dato_extraido_pagina
        CHECK
        (
            pagina IS NULL
            OR pagina > 0
        )
);

CREATE TABLE documentos.dato_validado
(
    dato_validado_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    solicitud_documento_id UUID NOT NULL,

    dato_extraido_id UUID,

    codigo_campo VARCHAR(120) NOT NULL,

    numero_revision INTEGER
        NOT NULL
        DEFAULT 1,

    tipo_dato VARCHAR(20) NOT NULL,

    valor_texto TEXT,

    valor_numerico NUMERIC(24,8),

    valor_fecha DATE,

    valor_booleano BOOLEAN,

    valor_json JSONB,

    estado VARCHAR(30) NOT NULL,

    comentario TEXT,

    validado_por_usuario_empresa_id UUID NOT NULL,

    fecha_validacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_dato_validado_solicitud_documento
        FOREIGN KEY
        (
            empresa_id,
            solicitud_documento_id
        )
        REFERENCES documentos.solicitud_documento
        (
            empresa_id,
            solicitud_documento_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_dato_validado_extraido
        FOREIGN KEY
        (
            empresa_id,
            dato_extraido_id
        )
        REFERENCES documentos.dato_extraido
        (
            empresa_id,
            dato_extraido_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_dato_validado_usuario
        FOREIGN KEY
        (
            empresa_id,
            validado_por_usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa
        (
            empresa_id,
            usuario_empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_dato_validado_revision
        UNIQUE
        (
            empresa_id,
            solicitud_documento_id,
            codigo_campo,
            numero_revision
        ),

    CONSTRAINT ck_dato_validado_tipo
        CHECK
        (
            tipo_dato IN
            (
                'TEXTO',
                'NUMERO',
                'FECHA',
                'BOOLEANO',
                'JSON'
            )
        ),

    CONSTRAINT ck_dato_validado_estado
        CHECK
        (
            estado IN
            (
                'CONFIRMADO',
                'CORREGIDO',
                'AGREGADO',
                'DESCARTADO'
            )
        )
);

CREATE TABLE integracion.proveedor
(
    proveedor_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    codigo VARCHAR(100)
        NOT NULL
        UNIQUE,

    nombre VARCHAR(200)
        NOT NULL,

    tipo VARCHAR(60)
        NOT NULL,

    descripcion TEXT,

    activo BOOLEAN
        NOT NULL
        DEFAULT TRUE,

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW()
);

CREATE TABLE integracion.empresa_proveedor
(
    empresa_proveedor_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    proveedor_id UUID NOT NULL,

    ambiente VARCHAR(30)
        NOT NULL
        DEFAULT 'PRUEBAS',

    configuracion_json JSONB,

    secreto_ref TEXT,

    activo BOOLEAN
        NOT NULL
        DEFAULT TRUE,

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_empresa_proveedor_empresa
        FOREIGN KEY (empresa_id)
        REFERENCES organizacion.empresa(empresa_id)
        ON DELETE RESTRICT,

    CONSTRAINT fk_empresa_proveedor_proveedor
        FOREIGN KEY (proveedor_id)
        REFERENCES integracion.proveedor(proveedor_id)
        ON DELETE RESTRICT,

    CONSTRAINT uq_empresa_proveedor
        UNIQUE
        (
            empresa_id,
            proveedor_id
        ),

    CONSTRAINT uq_empresa_proveedor_empresa_id
        UNIQUE
        (
            empresa_id,
            empresa_proveedor_id
        ),

    CONSTRAINT ck_empresa_proveedor_ambiente
        CHECK
        (
            ambiente IN
            (
                'PRUEBAS',
                'PRODUCCION'
            )
        )
);

CREATE TABLE integracion.consulta_externa
(
    consulta_externa_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    solicitud_credito_id UUID NOT NULL,

    empresa_proveedor_id UUID NOT NULL,

    tipo_consulta VARCHAR(100) NOT NULL,

    estado VARCHAR(30)
        NOT NULL
        DEFAULT 'SOLICITADA',

    referencia_externa VARCHAR(200),

    solicitud_resumen_json JSONB,

    respuesta_resumen_json JSONB,

    archivo_respuesta_uri TEXT,

    mensaje_error TEXT,

    iniciada_por_usuario_empresa_id UUID,

    fecha_inicio TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    fecha_fin TIMESTAMPTZ,

    CONSTRAINT fk_consulta_solicitud
        FOREIGN KEY
        (
            empresa_id,
            solicitud_credito_id
        )
        REFERENCES credito.solicitud_credito
        (
            empresa_id,
            solicitud_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_consulta_empresa_proveedor
        FOREIGN KEY
        (
            empresa_id,
            empresa_proveedor_id
        )
        REFERENCES integracion.empresa_proveedor
        (
            empresa_id,
            empresa_proveedor_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_consulta_usuario
        FOREIGN KEY
        (
            empresa_id,
            iniciada_por_usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa
        (
            empresa_id,
            usuario_empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_consulta_empresa_id
        UNIQUE
        (
            empresa_id,
            consulta_externa_id
        ),

    CONSTRAINT uq_consulta_solicitud_id
        UNIQUE
        (
            empresa_id,
            solicitud_credito_id,
            consulta_externa_id
        ),

    CONSTRAINT ck_consulta_estado
        CHECK
        (
            estado IN
            (
                'SOLICITADA',
                'PROCESANDO',
                'COMPLETADA',
                'ERROR',
                'CANCELADA'
            )
        )
);

CREATE TABLE integracion.buro_snapshot
(
    buro_snapshot_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    solicitud_credito_id UUID NOT NULL,

    empresa_proveedor_id UUID NOT NULL,

    consulta_externa_id UUID,

    solicitud_documento_id UUID,

    fecha_reporte DATE,

    score_buro NUMERIC(12,4),

    deuda_total NUMERIC(18,2),

    cuota_total NUMERIC(18,2),

    creditos_activos INTEGER,

    mora_actual_max_dias INTEGER,

    mora_historica_max_dias INTEGER,

    payload_normalizado JSONB,

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_buro_solicitud
        FOREIGN KEY
        (
            empresa_id,
            solicitud_credito_id
        )
        REFERENCES credito.solicitud_credito
        (
            empresa_id,
            solicitud_credito_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_buro_empresa_proveedor
        FOREIGN KEY
        (
            empresa_id,
            empresa_proveedor_id
        )
        REFERENCES integracion.empresa_proveedor
        (
            empresa_id,
            empresa_proveedor_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_buro_consulta
        FOREIGN KEY
        (
            empresa_id,
            solicitud_credito_id,
            consulta_externa_id
        )
        REFERENCES integracion.consulta_externa
        (
            empresa_id,
            solicitud_credito_id,
            consulta_externa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_buro_documento
        FOREIGN KEY
        (
            empresa_id,
            solicitud_documento_id
        )
        REFERENCES documentos.solicitud_documento
        (
            empresa_id,
            solicitud_documento_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT uq_buro_snapshot_empresa_id
        UNIQUE
        (
            empresa_id,
            buro_snapshot_id
        ),

    CONSTRAINT ck_buro_origen
        CHECK
        (
            consulta_externa_id IS NOT NULL
            OR solicitud_documento_id IS NOT NULL
        )
);

CREATE TABLE credito.obligacion_fuente
(
    obligacion_fuente_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID NOT NULL,

    obligacion_id UUID NOT NULL,

    tipo_fuente VARCHAR(60) NOT NULL,

    buro_snapshot_id UUID,

    solicitud_documento_id UUID,

    referencia_fuente VARCHAR(200),

    saldo_reportado NUMERIC(18,2),

    cuota_reportada NUMERIC(18,2),

    dias_mora_reportado INTEGER,

    fecha_fuente DATE,

    fecha_creacion TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_obligacion_fuente_obligacion
        FOREIGN KEY
        (
            empresa_id,
            obligacion_id
        )
        REFERENCES credito.obligacion
        (
            empresa_id,
            obligacion_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_obligacion_fuente_buro
        FOREIGN KEY
        (
            empresa_id,
            buro_snapshot_id
        )
        REFERENCES integracion.buro_snapshot
        (
            empresa_id,
            buro_snapshot_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_obligacion_fuente_documento
        FOREIGN KEY
        (
            empresa_id,
            solicitud_documento_id
        )
        REFERENCES documentos.solicitud_documento
        (
            empresa_id,
            solicitud_documento_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT ck_obligacion_fuente_saldo
        CHECK
        (
            saldo_reportado IS NULL
            OR saldo_reportado >= 0
        ),

    CONSTRAINT ck_obligacion_fuente_cuota
        CHECK
        (
            cuota_reportada IS NULL
            OR cuota_reportada >= 0
        ),

    CONSTRAINT ck_obligacion_fuente_mora
        CHECK
        (
            dias_mora_reportado IS NULL
            OR dias_mora_reportado >= 0
        )
);

CREATE TABLE auditoria.evento
(
    evento_id UUID
        PRIMARY KEY
        DEFAULT gen_random_uuid(),

    empresa_id UUID,

    usuario_id UUID,

    usuario_empresa_id UUID,

    correlation_id VARCHAR(100),

    origen VARCHAR(60)
        NOT NULL
        DEFAULT 'API',

    entidad_tipo VARCHAR(120) NOT NULL,

    entidad_id VARCHAR(120),

    accion VARCHAR(100) NOT NULL,

    datos_anteriores JSONB,

    datos_nuevos JSONB,

    detalle_json JSONB,

    exitoso BOOLEAN
        NOT NULL
        DEFAULT TRUE,

    direccion_ip INET,

    user_agent TEXT,

    fecha_evento TIMESTAMPTZ
        NOT NULL
        DEFAULT NOW(),

    CONSTRAINT fk_auditoria_empresa
        FOREIGN KEY
        (
            empresa_id
        )
        REFERENCES organizacion.empresa
        (
            empresa_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_auditoria_usuario
        FOREIGN KEY
        (
            usuario_id
        )
        REFERENCES seguridad.usuario
        (
            usuario_id
        )
        ON DELETE RESTRICT,

    CONSTRAINT fk_auditoria_usuario_empresa
        FOREIGN KEY
        (
            usuario_empresa_id
        )
        REFERENCES seguridad.usuario_empresa
        (
            usuario_empresa_id
        )
        ON DELETE RESTRICT
);

CREATE INDEX ix_prestamo_empresa_estado
ON credito.prestamo
(
    empresa_id,
    estado
);

CREATE INDEX ix_desempeno_prestamo_fecha
ON credito.desempeno_credito
(
    empresa_id,
    prestamo_id,
    fecha_corte DESC
);

CREATE INDEX ix_documento_cliente
ON documentos.documento
(
    empresa_id,
    cliente_id
);

CREATE INDEX ix_documento_version_documento
ON documentos.documento_version
(
    empresa_id,
    documento_id,
    numero_version DESC
);

CREATE INDEX ix_solicitud_documento_solicitud
ON documentos.solicitud_documento
(
    empresa_id,
    solicitud_credito_id
);

CREATE INDEX ix_dato_extraido_documento
ON documentos.dato_extraido
(
    empresa_id,
    documento_version_id
);

CREATE INDEX ix_dato_validado_solicitud_documento
ON documentos.dato_validado
(
    empresa_id,
    solicitud_documento_id
);

CREATE INDEX ix_consulta_externa_solicitud
ON integracion.consulta_externa
(
    empresa_id,
    solicitud_credito_id,
    fecha_inicio DESC
);

CREATE INDEX ix_buro_snapshot_solicitud
ON integracion.buro_snapshot
(
    empresa_id,
    solicitud_credito_id,
    fecha_creacion DESC
);

CREATE INDEX ix_obligacion_fuente_obligacion
ON credito.obligacion_fuente
(
    empresa_id,
    obligacion_id
);

CREATE INDEX ix_auditoria_empresa_fecha
ON auditoria.evento
(
    empresa_id,
    fecha_evento DESC
);

CREATE INDEX ix_auditoria_entidad
ON auditoria.evento
(
    entidad_tipo,
    entidad_id,
    fecha_evento DESC
);

CREATE INDEX ix_auditoria_correlation
ON auditoria.evento
(
    correlation_id
);
