import time

from fastapi import FastAPI, HTTPException

from .config import MODEL_DIR
from .model_registry import FeatureIncompleteError, ModelRegistry
from .schemas import PredictRequest, PredictResponse

app = FastAPI(title="MAPAN ML", version="0.1.0")
registry = ModelRegistry(MODEL_DIR)
registry.load()


@app.get("/health")
def health():
    if registry.is_loaded:
        return {"status": "ok", "model_status": "CONFIGURED", "modelo_version_id": str(registry.modelo_version_id)}
    return {"status": "ok", "model_status": "NOT_CONFIGURED"}


@app.post("/predict", response_model=PredictResponse)
def predict(request: PredictRequest):
    if not registry.is_loaded:
        raise HTTPException(status_code=503, detail={
            "code": "MODEL_NOT_CONFIGURED",
            "message": "No existe un artefacto ML validado y configurado."
        })
    if request.modelo_version_id != registry.modelo_version_id:
        raise HTTPException(status_code=409, detail={
            "code": "MODEL_VERSION_MISMATCH",
            "message": "El modelo_version_id solicitado no coincide con el artefacto cargado en este servicio."
        })
    started = time.perf_counter()
    try:
        result = registry.predict(request.vector)
    except FeatureIncompleteError as error:
        raise HTTPException(status_code=422, detail={
            "code": "FEATURE_INCOMPLETE",
            "message": str(error)
        }) from error
    elapsed_ms = int((time.perf_counter() - started) * 1000)
    return PredictResponse(
        analisis_id=request.analisis_id,
        modelo_version_id=request.modelo_version_id,
        probabilidad_incumplimiento=result.probabilidad,
        umbral_utilizado=result.umbral,
        clase_predicha=result.clase,
        nivel_riesgo=result.nivel,
        tiempo_inferencia_ms=elapsed_ms,
        factores=result.factores,
    )
