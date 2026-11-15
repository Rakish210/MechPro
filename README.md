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
- `main.py`: standalone desktop UI preview (Tkinter)
- `backend/`: Python API and engineering services
- `frontend/`: React desktop UI
- `docs/`: architecture and development plan

## Run the desktop UI preview

The current visual prototype is a standalone Tkinter application and uses only Python's standard library.

```bash
python main.py
```

The preview uses sample projects stored in memory. Project data is not saved after the app closes. The preview is not yet wired to the React/Tauri desktop shell or the FastAPI service.

## Status
Foundation stage. The UI layout is an early prototype and will be refined incrementally based on feedback. See `docs/ROADMAP.md` and `docs/ARCHITECTURE.md`.

## License
No project license has been selected yet. Do not assume reuse permissions until a LICENSE file is added.

## Local development (React/Tauri scaffold)

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

The Tauri desktop shell is configured in `frontend/src-tauri`. Packaging and supervision of the Python API as a desktop sidecar, plus project persistence, remain follow-up work.
