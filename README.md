# MechPro

Offline-first workspace for mechanical design and engineering calculations.

## Product focus
- Mechanical building-services design and engineering calculations.
- No manufacturing workflow, production status, delivery target, progress tracking, or assigned-owner tracking.
- The navigation labels are provisional and will be reviewed incrementally.
- Engineering modules will be selected and added step by step based on feedback.
- Core project data and engineering calculations are intended to work offline.
- Optional cloud AI will not replace deterministic engineering calculations.

## Planned application stack
- Desktop shell: Tauri
- UI: React + TypeScript + Vite
- Engineering services: Python + FastAPI
- Local database: SQLite

## Repository layout
- `main.py`: standalone local Tkinter UI preview
- `backend/`: Python API and engineering services
- `frontend/`: React desktop UI
- `docs/`: architecture and development plan

## Run the local Python UI preview

From the repository root:

```bash
python main.py
```

The preview uses Tkinter and SQLite from Python's standard library. Projects are saved in the current user's MechPro data directory:

- Windows: `%LOCALAPPDATA%/MechPro/mechpro.db`
- macOS: `~/Library/Application Support/MechPro/mechpro.db`
- Linux: `~/.local/share/mechpro/mechpro.db` (or `$XDG_DATA_HOME/mechpro/mechpro.db`)

Set `MECHPRO_DATA_DIR` to override this location, for example when testing with a temporary database.

Project names are unique after Unicode normalization, whitespace normalization, and case-insensitive comparison. The project list supports view, edit, and delete. The Overview and Projects screens are intentionally different.

## Run the React UI scaffold

```bash
cd frontend
npm install
npm run dev
```

The React preview persists project metadata in browser local storage. It is currently a separate preview and is not yet synchronized with the Tkinter/SQLite preview or packaged as a connected Tauri application.

## Run the Python API

```bash
cd backend
python -m venv .venv
# Activate the virtual environment, then:
pip install -e ".[dev]"
uvicorn app.main:app --host 127.0.0.1 --port 8765
pytest
```

The Tauri shell is configured under `frontend/src-tauri`. Connecting the desktop shell, API service, and shared persistent project storage remains future work.

## License
No project license has been selected yet. Do not assume reuse permissions until a `LICENSE` file is added.
