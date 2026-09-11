from decimal import Decimal
from uuid import UUID
from pydantic import BaseModel, ConfigDict, Field


class FeatureVector(BaseModel):
    model_config = ConfigDict(extra="forbid", allow_inf_nan=False)
    ingreso_mensual: Decimal | None = Field(default=None, ge=0)
    gastos_mensuales: Decimal | None = Field(default=None, ge=0)
    cuotas_otras_deudas: Decimal | None = Field(default=None, ge=0)
    deuda_total_actual: Decimal | None = Field(default=None, ge=0)
    monto_solicitado: Decimal | None = Field(default=None, gt=0)
    plazo_meses: int | None = Field(default=None, gt=0)
    cuota_estimada: Decimal | None = Field(default=None, ge=0)
    max_dias_mora_historico: int | None = Field(default=None, ge=0)
    creditos_activos: int | None = Field(default=None, ge=0)
    antiguedad_actividad_meses: int | None = Field(default=None, ge=0)
    estabilidad_ingresos_score: Decimal | None = None
    historial_interno_score: Decimal | None = None
    ingreso_disponible: Decimal | None = None
    capacidad_nueva_cuota: Decimal | None = None
    deuda_sobre_ingreso: Decimal | None = None
    cuota_sobre_ingreso: Decimal | None = None
    monto_sobre_ingreso: Decimal | None = None


class PredictRequest(BaseModel):
    model_config = ConfigDict(extra="forbid")
    analisis_id: UUID
    modelo_version_id: UUID
    vector: FeatureVector


class PredictionFactor(BaseModel):
    model_config = ConfigDict(extra="forbid", allow_inf_nan=False)
    codigo_caracteristica: str
    contribucion: Decimal
    direccion: str


class PredictResponse(BaseModel):
    model_config = ConfigDict(extra="forbid", allow_inf_nan=False)
    analisis_id: UUID
    modelo_version_id: UUID
    probabilidad_incumplimiento: Decimal = Field(ge=0, le=1)
    umbral_utilizado: Decimal = Field(ge=0, le=1)
    clase_predicha: str
    nivel_riesgo: str | None = None
    tiempo_inferencia_ms: int = Field(ge=0)
    factores: list[PredictionFactor] = Field(default_factory=list)
