# MechPro Engineering Core

## Unit system

The backend owns a deterministic unit registry. Each unit belongs to one physical
dimension and maps to a canonical SI representation. Conversion is allowed only
within the same dimension.

Temperature uses an affine transform (scale and offset); other currently
registered dimensions use a multiplicative scale. Non-finite input values and
unknown or dimensionally incompatible units are rejected.

## Engineering calculation contract

Future calculation modules must:

- Keep deterministic calculation logic independent from UI and AI services.
- Declare input quantities with explicit units and validate dimensions.
- Return results with units, assumptions, warnings, and calculation metadata.
- Avoid silently choosing missing design criteria or code editions.
- Include reference standard identifiers and editions when a calculation relies
  on a standard.
- Have automated tests for nominal, boundary, and invalid input cases.

The unit registry is a conversion utility, not a substitute for discipline-specific
design rules or a validated engineering calculation.
