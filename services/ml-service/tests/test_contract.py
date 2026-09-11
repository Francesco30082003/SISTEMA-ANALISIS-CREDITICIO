import asyncio
import httpx
from app.main import app

class Client:
    def get(self, path):
        return asyncio.run(self.request("GET", path))

    def post(self, path, json):
        return asyncio.run(self.request("POST", path, json=json))

    async def request(self, method, path, **kwargs):
        async with httpx.AsyncClient(transport=httpx.ASGITransport(app=app), base_url="http://test") as client:
            return await client.request(method, path, **kwargs)

client = Client()


def test_health_reports_unconfigured_model():
    assert client.get("/health").json()["model_status"] == "NOT_CONFIGURED"


def test_predict_never_fabricates_probability():
    response = client.post("/predict", json={
        "analisis_id": "00000000-0000-0000-0000-000000000001",
        "modelo_version_id": "00000000-0000-0000-0000-000000000002",
        "vector": {"ingreso_mensual": 1200},
    })
    assert response.status_code == 503
    assert response.json()["detail"]["code"] == "MODEL_NOT_CONFIGURED"
    assert "probabilidad_incumplimiento" not in response.json()


def test_unknown_feature_is_rejected():
    response = client.post("/predict", json={
        "analisis_id": "00000000-0000-0000-0000-000000000001",
        "modelo_version_id": "00000000-0000-0000-0000-000000000002",
        "vector": {"inventada": 1},
    })
    assert response.status_code == 422
