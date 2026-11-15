"""Unit registry and deterministic conversion functions.

All conversions use a canonical SI representation. Temperature is affine;
all other supported dimensions use multiplicative factors.
"""

from __future__ import annotations

from dataclasses import dataclass
import math


@dataclass(frozen=True, slots=True)
class Unit:
    symbol: str
    dimension: str
    factor: float
    offset: float = 0.0
    label: str = ""


_UNITS = (
    Unit("m", "length", 1.0, label="metre"),
    Unit("mm", "length", 0.001, label="millimetre"),
    Unit("cm", "length", 0.01, label="centimetre"),
    Unit("km", "length", 1000.0, label="kilometre"),
    Unit("ft", "length", 0.3048, label="foot"),
    Unit("in", "length", 0.0254, label="inch"),
    Unit("m²", "area", 1.0, label="square metre"),
    Unit("mm²", "area", 1e-6, label="square millimetre"),
    Unit("cm²", "area", 1e-4, label="square centimetre"),
    Unit("ft²", "area", 0.09290304, label="square foot"),
    Unit("m³", "volume", 1.0, label="cubic metre"),
    Unit("L", "volume", 0.001, label="litre"),
    Unit("mL", "volume", 1e-6, label="millilitre"),
    Unit("m³/s", "flow_rate", 1.0, label="cubic metre per second"),
    Unit("m³/h", "flow_rate", 1 / 3600, label="cubic metre per hour"),
    Unit("L/s", "flow_rate", 0.001, label="litre per second"),
    Unit("L/min", "flow_rate", 1 / 60000, label="litre per minute"),
    Unit("L/h", "flow_rate", 1 / 3_600_000, label="litre per hour"),
    Unit("Pa", "pressure", 1.0, label="pascal"),
    Unit("kPa", "pressure", 1000.0, label="kilopascal"),
    Unit("MPa", "pressure", 1_000_000.0, label="megapascal"),
    Unit("bar", "pressure", 100_000.0, label="bar"),
    Unit("mbar", "pressure", 100.0, label="millibar"),
    Unit("psi", "pressure", 6894.757293168, label="pound per square inch"),
    Unit("°C", "temperature", 1.0, 273.15, "degree Celsius"),
    Unit("K", "temperature", 1.0, 0.0, "kelvin"),
    Unit("°F", "temperature", 5 / 9, 255.3722222222222, "degree Fahrenheit"),
    Unit("W", "power", 1.0, label="watt"),
    Unit("kW", "power", 1000.0, label="kilowatt"),
    Unit("MW", "power", 1_000_000.0, label="megawatt"),
    Unit("hp", "power", 745.6998715822702, label="mechanical horsepower"),
    Unit("J", "energy", 1.0, label="joule"),
    Unit("kJ", "energy", 1000.0, label="kilojoule"),
    Unit("kWh", "energy", 3_600_000.0, label="kilowatt-hour"),
    Unit("kg", "mass", 1.0, label="kilogram"),
    Unit("g", "mass", 0.001, label="gram"),
    Unit("kg/m³", "density", 1.0, label="kilogram per cubic metre"),
    Unit("g/cm³", "density", 1000.0, label="gram per cubic centimetre"),
    Unit("m/s", "velocity", 1.0, label="metre per second"),
    Unit("km/h", "velocity", 1 / 3.6, label="kilometre per hour"),
)

UNIT_REGISTRY: dict[str, Unit] = {unit.symbol: unit for unit in _UNITS}


class UnitConversionError(ValueError):
    """Raised when a unit conversion is invalid."""


def list_units() -> list[dict[str, str]]:
    """Return unit metadata in a stable, UI-friendly order."""
    return [
        {"symbol": unit.symbol, "dimension": unit.dimension, "label": unit.label}
        for unit in _UNITS
    ]


def convert(value: float, from_unit: str, to_unit: str) -> float:
    """Convert a finite numeric value between units of the same dimension."""
    if not math.isfinite(value):
        raise UnitConversionError("Value must be a finite number.")

    source = UNIT_REGISTRY.get(from_unit)
    target = UNIT_REGISTRY.get(to_unit)
    if source is None:
        raise UnitConversionError(f"Unsupported source unit: {from_unit}")
    if target is None:
        raise UnitConversionError(f"Unsupported target unit: {to_unit}")
    if source.dimension != target.dimension:
        raise UnitConversionError(
            f"Incompatible dimensions: {source.dimension} and {target.dimension}."
        )

    canonical_value = value * source.factor + source.offset
    return (canonical_value - target.offset) / target.factor
