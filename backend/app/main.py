"""MechPro local API entry point."""

from fastapi import FastAPI

app = FastAPI(
    title="MechPro Local API",
    description="Local-first engineering services for MechPro.",
    version="0.1.0",
)


@app.get("/health", tags=["system"])
async def health_check() -> dict[str, str]:
    """Return service status for desktop startup checks."""
    return {"status": "ok", "service": "mechpro-local-api"}
