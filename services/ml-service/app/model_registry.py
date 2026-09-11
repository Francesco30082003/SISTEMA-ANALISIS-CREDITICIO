from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path
from typing import NamedTuple, Optional
from uuid import UUID

import joblib
import numpy as np

from .schemas import FeatureVector, PredictionFactor


class ModelNotLoadedError(RuntimeError):
    """Raised when /predict is called but no artifact bundle is configured."""


class FeatureIncompleteError(ValueError):
    """Raised when the request vector is missing a value the model requires."""

    def __init__(self, missing: list[str]):
        super().__init__(f"Faltan valores confirmados para: {', '.join(missing)}.")
        self.missing = missing


class PredictionResult(NamedTuple):
    probabilidad: float
    umbral: float
    clase: str
    nivel: Optional[str]
    factores: list[PredictionFactor]


@dataclass(frozen=True)
class _LoadedModel:
    modelo_version_id: UUID
    feature_order: list[str]
    umbral_decision: float
    umbral_riesgo_bajo: Optional[float]
    umbral_riesgo_alto: Optional[float]
    positive_index: int
    estimator: object
    explainer: object | None


class ModelRegistry:
    """Loads a joblib-exported scikit-learn/XGBoost/LightGBM estimator plus its
    metadata.json. Deliberately fails to an unconfigured state (never a guessed
    or fabricated prediction) whenever the bundle is absent or inconsistent.

    Expected bundle, under the configured directory:
      - model.joblib: an estimator exposing predict_proba(X).
      - metadata.json: {
          "modelo_version_id": "<uuid>",           # must match riesgo.modelo_version.modelo_version_id
          "feature_order": ["ingreso_mensual", ...],  # exact column order predict_proba expects
          "umbral_decision": 0.5,                   # classification threshold, defined by the trainer
          "positive_class": 1,                       # optional, defaults to 1 (label meaning INCUMPLIMIENTO)
          "umbral_riesgo_bajo": 0.3,                  # optional; omit both to leave nivel_riesgo null
          "umbral_riesgo_alto": 0.7
        }
    """

    def __init__(self, model_dir: str | Path | None):
        self._model_dir = Path(model_dir) if model_dir else None
        self._loaded: _LoadedModel | None = None

    def load(self) -> None:
        self._loaded = None
        if self._model_dir is None:
            return
        model_path = self._model_dir / "model.joblib"
        metadata_path = self._model_dir / "metadata.json"
        if not model_path.is_file() or not metadata_path.is_file():
            return
        metadata = json.loads(metadata_path.read_text(encoding="utf-8"))
        feature_order = metadata["feature_order"]
        if not isinstance(feature_order, list) or not feature_order or any(not isinstance(f, str) for f in feature_order):
            raise ValueError("metadata.json: feature_order debe ser una lista no vacía de nombres de variables.")
        umbral = float(metadata["umbral_decision"])
        if not (0 <= umbral <= 1):
            raise ValueError("metadata.json: umbral_decision debe estar entre 0 y 1.")
        bajo = metadata.get("umbral_riesgo_bajo")
        alto = metadata.get("umbral_riesgo_alto")
        if (bajo is None) != (alto is None):
            raise ValueError("metadata.json: define umbral_riesgo_bajo y umbral_riesgo_alto juntos, o ninguno.")
        estimator = joblib.load(model_path)
        if not hasattr(estimator, "predict_proba"):
            raise ValueError("El artefacto cargado no expone predict_proba; no es un clasificador binario probabilístico.")
        classes = list(getattr(estimator, "classes_", [0, 1]))
        positive_class = metadata.get("positive_class", 1)
        positive_index = classes.index(positive_class) if positive_class in classes else len(classes) - 1
        self._loaded = _LoadedModel(
            modelo_version_id=UUID(str(metadata["modelo_version_id"])),
            feature_order=feature_order,
            umbral_decision=umbral,
            umbral_riesgo_bajo=float(bajo) if bajo is not None else None,
            umbral_riesgo_alto=float(alto) if alto is not None else None,
            positive_index=positive_index,
            estimator=estimator,
            explainer=self._build_explainer(estimator),
        )

    @staticmethod
    def _build_explainer(estimator: object) -> object | None:
        # Best-effort only: an unsupported model type simply yields no factores,
        # it never blocks predictions or fabricates contributions.
        try:
            import shap

            return shap.TreeExplainer(estimator)
        except Exception:
            return None

    @property
    def is_loaded(self) -> bool:
        return self._loaded is not None

    @property
    def modelo_version_id(self) -> UUID:
        if self._loaded is None:
            raise ModelNotLoadedError()
        return self._loaded.modelo_version_id

    def _risk_level(self, m: _LoadedModel, probability: float) -> Optional[str]:
        if m.umbral_riesgo_bajo is None or m.umbral_riesgo_alto is None:
            return None
        if probability < m.umbral_riesgo_bajo:
            return "BAJO"
        if probability < m.umbral_riesgo_alto:
            return "MEDIO"
        return "ALTO"

    def predict(self, vector: FeatureVector) -> PredictionResult:
        if self._loaded is None:
            raise ModelNotLoadedError()
        m = self._loaded
        values = vector.model_dump()
        missing = [name for name in m.feature_order if values.get(name) is None]
        if missing:
            raise FeatureIncompleteError(missing)
        row = np.array([[float(values[name]) for name in m.feature_order]], dtype=float)
        proba = float(m.estimator.predict_proba(row)[0, m.positive_index])
        clase = "INCUMPLIMIENTO" if proba >= m.umbral_decision else "NO_INCUMPLIMIENTO"
        return PredictionResult(
            probabilidad=proba,
            umbral=m.umbral_decision,
            clase=clase,
            nivel=self._risk_level(m, proba),
            factores=self._explain(row, m),
        )

    @staticmethod
    def _explain(row: np.ndarray, m: _LoadedModel) -> list[PredictionFactor]:
        if m.explainer is None:
            return []
        try:
            raw = m.explainer.shap_values(row)
            if isinstance(raw, list):
                contributions = raw[m.positive_index][0]
            else:
                arr = np.asarray(raw)
                contributions = arr[0, :, m.positive_index] if arr.ndim == 3 else arr[0]
        except Exception:
            return []
        factors = []
        for name, contribution in zip(m.feature_order, contributions):
            value = float(contribution)
            direction = "AUMENTA_RIESGO" if value > 0 else "REDUCE_RIESGO" if value < 0 else "NEUTRO"
            factors.append(PredictionFactor(codigo_caracteristica=name, contribucion=value, direccion=direction))
        factors.sort(key=lambda f: abs(f.contribucion), reverse=True)
        return factors
