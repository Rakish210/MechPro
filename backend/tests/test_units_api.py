from fastapi.testclient import TestClient

from app.main import app

client = TestClient(app)


def test_unit_catalog_endpoint_returns_units() -> None:
    response = client.get("/api/v1/units")

    assert response.status_code == 200
    units = response.json()["units"]
    assert any(unit["symbol"] == "m³/h" for unit in units)
    assert any(unit["symbol"] == "kPa" for unit in units)


def test_unit_conversion_endpoint_returns_result() -> None:
    response = client.post(
        "/api/v1/units/convert",
        json={"value": 1, "from_unit": "bar", "to_unit": "kPa"},
    )

    assert response.status_code == 200
    assert response.json()["result"] == 100


def test_unit_conversion_endpoint_rejects_incompatible_dimensions() -> None:
    response = client.post(
        "/api/v1/units/convert",
        json={"value": 1, "from_unit": "m", "to_unit": "kg"},
    )

    assert response.status_code == 422
    assert "Incompatible dimensions" in response.json()["detail"]
