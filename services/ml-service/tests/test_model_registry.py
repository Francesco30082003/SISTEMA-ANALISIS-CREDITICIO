import json

import joblib
import numpy as np
import pytest
from sklearn.linear_model import LogisticRegression

from app.model_registry import FeatureIncompleteError, ModelNotLoadedError, ModelRegistry
from app.schemas import FeatureVector

FEATURES = ["ingreso_mensual", "gastos_mensuales", "monto_solicitado"]


def _train_toy_model():
    rng = np.random.default_rng(7)
    x = rng.normal(size=(200, len(FEATURES)))
    y = (x[:, 0] - x[:, 1] + 0.5 * x[:, 2] > 0).astype(int)
    model = LogisticRegression().fit(x, y)
    return model


def _write_bundle(tmp_path, *, umbral=0.5, bandas=False):
    model = _train_toy_model()
    joblib.dump(model, tmp_path / "model.joblib")
    metadata = {
        "modelo_version_id": "11111111-1111-1111-1111-111111111111",
        "feature_order": FEATURES,
        "umbral_decision": umbral,
    }
    if bandas:
        metadata["umbral_riesgo_bajo"] = 0.3
        metadata["umbral_riesgo_alto"] = 0.7
    (tmp_path / "metadata.json").write_text(json.dumps(metadata), encoding="utf-8")
    return metadata


def test_unconfigured_directory_stays_not_loaded():
    registry = ModelRegistry(None)
    registry.load()
    assert not registry.is_loaded
    with pytest.raises(ModelNotLoadedError):
        registry.predict(FeatureVector(ingreso_mensual=1))


def test_missing_bundle_files_stays_not_loaded(tmp_path):
    registry = ModelRegistry(tmp_path)
    registry.load()
    assert not registry.is_loaded


def test_loads_and_predicts_within_bounds(tmp_path):
    metadata = _write_bundle(tmp_path)
    registry = ModelRegistry(tmp_path)
    registry.load()
    assert registry.is_loaded
    assert str(registry.modelo_version_id) == metadata["modelo_version_id"]
    result = registry.predict(FeatureVector(ingreso_mensual=1500, gastos_mensuales=400, monto_solicitado=3000))
    assert 0 <= result.probabilidad <= 1
    assert result.clase in ("INCUMPLIMIENTO", "NO_INCUMPLIMIENTO")
    assert result.umbral == 0.5
    assert result.nivel is None
    for factor in result.factores:
        assert factor.direccion in ("AUMENTA_RIESGO", "REDUCE_RIESGO", "NEUTRO")


def test_risk_bands_only_when_both_configured(tmp_path):
    _write_bundle(tmp_path, bandas=True)
    registry = ModelRegistry(tmp_path)
    registry.load()
    result = registry.predict(FeatureVector(ingreso_mensual=1500, gastos_mensuales=400, monto_solicitado=3000))
    assert result.nivel in ("BAJO", "MEDIO", "ALTO")


def test_missing_feature_is_rejected_not_fabricated(tmp_path):
    _write_bundle(tmp_path)
    registry = ModelRegistry(tmp_path)
    registry.load()
    with pytest.raises(FeatureIncompleteError):
        registry.predict(FeatureVector(ingreso_mensual=1500))


def test_partial_risk_bands_are_rejected_at_load(tmp_path):
    model = _train_toy_model()
    joblib.dump(model, tmp_path / "model.joblib")
    metadata = {
        "modelo_version_id": "11111111-1111-1111-1111-111111111111",
        "feature_order": FEATURES,
        "umbral_decision": 0.5,
        "umbral_riesgo_bajo": 0.3,
    }
    (tmp_path / "metadata.json").write_text(json.dumps(metadata), encoding="utf-8")
    registry = ModelRegistry(tmp_path)
    with pytest.raises(ValueError):
        registry.load()
