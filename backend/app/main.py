"""MechPro local API entry point."""

from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from app.domain.units import UnitConversionError, convert, list_units

app = FastAPI(
    title="MechPro Local API",
    description="Local-first engineering services for MechPro.",
    version="0.2.0",
)


class UnitConversionRequest(BaseModel):
    value: float = Field(description="Numeric value to convert.")
    from_unit: str = Field(min_length=1, description="Source unit symbol.")
    to_unit: str = Field(min_length=1, description="Target unit symbol.")


@app.get("/health", tags=["system"])
async def health_check() -> dict[str, str]:
    """Return service status for desktop startup checks."""
    return {"status": "ok", "service": "mechpro-local-api"}


@app.get("/api/v1/units", tags=["engineering"])
async def get_units() -> dict[str, list[dict[str, str]]]:
    """Return the supported engineering unit catalog."""
    return {"units": list_units()}


@app.post("/api/v1/units/convert", tags=["engineering"])
async def convert_unit(request: UnitConversionRequest) -> dict[str, float | str]:
    """Convert a value between compatible physical units."""
    try:
        result = convert(request.value, request.from_unit, request.to_unit)
    except UnitConversionError as exc:
        raise HTTPException(status_code=422, detail=str(exc)) from exc

    return {
        "value": request.value,
        "from_unit": request.from_unit,
        "to_unit": request.to_unit,
        "result": result,
    }
