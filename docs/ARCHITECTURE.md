# MechPro – Initial Architecture

## Goals
1. Run the core desktop application without internet access.
2. Keep calculations deterministic, testable, and independent of the UI.
3. Keep discipline-specific features modular.
4. Treat cloud AI as optional, never as a calculation authority.

## Components
- Tauri: desktop window, native integration, packaging, and lifecycle.
- React + TypeScript: presentation and user workflows.
- Python services: calculations, validation, reports, and domain logic.
- SQLite: local-first project storage.

## Dependency direction
The UI calls application services through a local interface. Domain and calculation modules must not depend on React, Tauri, or network services.

Structure engineering logic as domain models and units, input validation, calculation methods, result models with assumptions and warnings, and tests using verified reference cases.

## Offline-first requirements
- Project creation, editing, and local persistence work offline.
- No mandatory login or cloud account for core functionality.
- AI, online catalogs, and update checks fail gracefully when offline.
- Project files remain local unless explicitly exported or synchronized.

## Engineering safety
Every result identifies its method, units, inputs, assumptions, and applicable standard/reference edition where known. Unverified formulas must be marked provisional and must not be presented as code-compliant design approval.

## Initial milestone
A thin vertical slice: desktop UI shell, local API health check, and documented project structure. No discipline calculation is production-ready in this milestone.
