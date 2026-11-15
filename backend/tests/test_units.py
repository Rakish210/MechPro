import math

import pytest

from app.domain.units import UnitConversionError, convert, list_units


@pytest.mark.parametrize(
    ("value", "source", "target", "expected"),
    [
        (1000, "mm", "m", 1),
        (1, "m³/h", "L/s", 1000 / 3600),
        (2, "bar", "kPa", 200),
        (1, "kWh", "J", 3_600_000),
        (0, "°C", "K", 273.15),
        (32, "°F", "°C", 0),
        (100, "km/h", "m/s", 100 / 3.6),
    ],
)
def test_convert(value: float, source: str, target: str, expected: float) -> None:
    assert convert(value, source, target) == pytest.approx(expected)


def test_same_unit_conversion_is_identity() -> None:
    assert convert(12.5, "L/s", "L/s") == pytest.approx(12.5)


@pytest.mark.parametrize("value", [math.inf, -math.inf, math.nan])
def test_non_finite_values_are_rejected(value: float) -> None:
    with pytest.raises(UnitConversionError, match="finite"):
        convert(value, "m", "mm")


def test_incompatible_dimensions_are_rejected() -> None:
    with pytest.raises(UnitConversionError, match="Incompatible dimensions"):
        convert(1, "m", "kg")


def test_unknown_units_are_rejected() -> None:
    with pytest.raises(UnitConversionError, match="Unsupported source unit"):
        convert(1, "unknown", "m")


def test_unit_catalog_contains_expected_dimensions() -> None:
    dimensions = {unit["dimension"] for unit in list_units()}
    assert {"length", "flow_rate", "pressure", "temperature", "power"} <= dimensions
