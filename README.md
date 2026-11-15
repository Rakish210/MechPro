# MechPro

Offline-first desktop engineering workspace for mechanical building-services engineers.

## Product principles
- Core project data and engineering calculations work offline.
- Every calculation exposes inputs, units, assumptions, method, and references.
- Discipline modules are modular and independently testable.
- Cloud AI is optional and never replaces deterministic calculation logic.
- Prefer permissive-licensed dependencies; review all licenses before adoption.

## Planned stack
- Desktop: Tauri
- UI: React + TypeScript + Vite
- Engineering services: Python + FastAPI
- Local database: SQLite

## Repository layout
- backend/: Python API and engineering services
- frontend/: React desktop UI
- docs/: architecture and development plan

## Status
Foundation stage. See docs/ROADMAP.md and docs/ARCHITECTURE.md.

## License
No project license has been selected yet. Do not assume reuse permissions until a LICENSE file is added.

## Local development (initial scaffold)

Frontend:
```bash
cd frontend
npm install
npm run dev
```

Python API:
```bash
cd backend
python -m venv .venv
# Activate the virtual environment, then:
pip install -e ".[dev]"
uvicorn app.main:app --host 127.0.0.1 --port 8765
pytest
```

The Tauri desktop shell is configured in src-tauri. The first milestone does not yet package or supervise the Python API as a desktop sidecar; that integration is a follow-up task. The frontend also remains a scaffold and does not yet persist projects.
