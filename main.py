"""MechPro local desktop preview.

Run from the repository root with: python main.py

The preview uses Tkinter and SQLite from the Python standard library.
Project records are stored locally in the user's MechPro data directory.
"""

from __future__ import annotations

import os
import sqlite3
import unicodedata
from datetime import datetime
from pathlib import Path
import tkinter as tk
from tkinter import messagebox

C = {
    "window": "#11131d", "sidebar": "#141621", "surface": "#171a26",
    "surface_alt": "#1d2030", "hover": "#242838", "border": "#2a2e3c",
    "border_soft": "#222635", "text": "#e7e9f0", "muted": "#969aaa",
    "subtle": "#6e7385", "accent": "#737fe0", "accent_soft": "#292d4a",
    "green": "#53bd91", "red": "#df777d",
}
FONT = "Segoe UI"


def data_directory() -> Path:
    """Return a per-user writable directory, with an override for testing."""
    override = os.environ.get("MECHPRO_DATA_DIR")
    if override:
        path = Path(override).expanduser()
    elif os.name == "nt":
        path = Path(os.environ.get("LOCALAPPDATA", Path.home())) / "MechPro"
    elif os.sys.platform == "darwin":
        path = Path.home() / "Library" / "Application Support" / "MechPro"
    else:
        path = Path(os.environ.get("XDG_DATA_HOME", Path.home() / ".local" / "share")) / "mechpro"
    path.mkdir(parents=True, exist_ok=True)
    return path


def normalized_name(name: str) -> str:
    """Normalize whitespace and Unicode case for duplicate-name checks."""
    return " ".join(unicodedata.normalize("NFKC", name).split()).casefold()


def timestamp() -> str:
    return datetime.now().astimezone().isoformat(timespec="minutes")


class ProjectStore:
    """Small SQLite repository for local project metadata."""

    def __init__(self, database_path: Path | None = None) -> None:
        self.database_path = database_path or data_directory() / "mechpro.db"
        self.database_path.parent.mkdir(parents=True, exist_ok=True)
        with self._connect() as connection:
            connection.execute(
                """
                CREATE TABLE IF NOT EXISTS projects (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    code TEXT NOT NULL UNIQUE,
                    name TEXT NOT NULL,
                    name_key TEXT NOT NULL UNIQUE,
                    description TEXT NOT NULL DEFAULT '',
                    created_at TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                )
                """
            )

    def _connect(self) -> sqlite3.Connection:
        connection = sqlite3.connect(self.database_path)
        connection.row_factory = sqlite3.Row
        return connection

    def list_projects(self) -> list[dict[str, object]]:
        with self._connect() as connection:
            rows = connection.execute(
                "SELECT * FROM projects ORDER BY updated_at DESC, id DESC"
            ).fetchall()
        return [dict(row) for row in rows]

    def get_project(self, project_id: int) -> dict[str, object] | None:
        with self._connect() as connection:
            row = connection.execute(
                "SELECT * FROM projects WHERE id = ?", (project_id,)
            ).fetchone()
        return dict(row) if row else None

    def create_project(self, name: str, description: str) -> int:
        clean_name = " ".join(name.split())
        name_key = normalized_name(clean_name)
        if not clean_name:
            raise ValueError("Proje adı boş bırakılamaz.")
        now = timestamp()
        try:
            with self._connect() as connection:
                cursor = connection.execute(
                    """
                    INSERT INTO projects (code, name, name_key, description, created_at, updated_at)
                    VALUES (?, ?, ?, ?, ?, ?)
                    """,
                    ("", clean_name, name_key, description.strip(), now, now),
                )
                project_id = int(cursor.lastrowid)
                code = f"MP-{project_id:04d}"
                connection.execute(
                    "UPDATE projects SET code = ? WHERE id = ?", (code, project_id)
                )
            return project_id
        except sqlite3.IntegrityError as exc:
            if "name_key" in str(exc).lower() or "unique constraint failed: projects.name_key" in str(exc).lower():
                raise ValueError("Bu isimde bir proje zaten kayıtlı. Farklı bir proje adı kullan.") from exc
            raise

    def update_project(self, project_id: int, name: str, description: str) -> None:
        clean_name = " ".join(name.split())
        name_key = normalized_name(clean_name)
        if not clean_name:
            raise ValueError("Proje adı boş bırakılamaz.")
        try:
            with self._connect() as connection:
                cursor = connection.execute(
                    """
                    UPDATE projects
                    SET name = ?, name_key = ?, description = ?, updated_at = ?
                    WHERE id = ?
                    """,
                    (clean_name, name_key, description.strip(), timestamp(), project_id),
                )
                if cursor.rowcount == 0:
                    raise ValueError("Düzenlenecek proje bulunamadı.")
        except sqlite3.IntegrityError as exc:
            if "name_key" in str(exc).lower() or "unique constraint failed: projects.name_key" in str(exc).lower():
                raise ValueError("Bu isimde başka bir proje zaten kayıtlı.") from exc
            raise

    def delete_project(self, project_id: int) -> None:
        with self._connect() as connection:
            connection.execute("DELETE FROM projects WHERE id = ?", (project_id,))


class MechProApp:
    NAV_GROUPS = [
        ("ÇALIŞMA ALANI", [
            ("⌂", "Genel Bakış"),
            ("▤", "Projeler"),
            ("∑", "Hesaplamalar"),
            ("▦", "Kütüphane"),
            ("▧", "Raporlar"),
        ]),
        ("GÖRÜNÜMLER", [
            ("◷", "Son Çalışmalar"),
            ("◇", "Yıldızlılar"),
            ("▱", "Arşiv"),
        ]),
    ]

    def __init__(self, root: tk.Tk, store: ProjectStore | None = None) -> None:
        self.root = root
        self.store = store or ProjectStore()
        self.root.title("MechPro — Mekanik Tasarım ve Hesaplama")
        self.root.geometry("1440x900")
        self.root.minsize(1080, 680)
        self.root.configure(bg=C["window"])
        self.current_page = "Genel Bakış"
        self.search_var = tk.StringVar()
        self.search_var.trace_add("write", lambda *_: self.render_page() if self.current_page == "Projeler" else None)
        self._build_shell()
        self.render_page()

    def label(self, parent: tk.Widget, text: str, *, fg: str | None = None,
              size: int = 10, weight: str = "normal", bg: str | None = None,
              **kwargs: object) -> tk.Label:
        return tk.Label(
            parent, text=text, fg=fg or C["text"], bg=bg or C["surface"],
            font=(FONT, size, weight), **kwargs
        )

    def button(self, parent: tk.Widget, text: str, command, *, primary: bool = False,
               compact: bool = False) -> tk.Button:
        bg = C["accent"] if primary else C["surface_alt"]
        return tk.Button(
            parent, text=text, command=command, bg=bg,
            fg="#ffffff" if primary else C["text"],
            activebackground=C["hover"], activeforeground=C["text"],
            relief="flat", bd=0, padx=10 if compact else 13,
            pady=5 if compact else 8, font=(FONT, 8 if compact else 9, "bold" if primary else "normal"),
            cursor="hand2", highlightthickness=1,
            highlightbackground=C["border"], highlightcolor=C["accent"],
        )

    def _build_shell(self) -> None:
        self.root.grid_rowconfigure(0, weight=1)
        self.root.grid_columnconfigure(1, weight=1)
        sidebar = tk.Frame(self.root, bg=C["sidebar"], width=235,
                           highlightthickness=1, highlightbackground=C["border_soft"])
        sidebar.grid(row=0, column=0, sticky="nsew")
        sidebar.grid_propagate(False)
        self.sidebar = sidebar
        self.workspace = tk.Frame(self.root, bg=C["surface"])
        self.workspace.grid(row=0, column=1, sticky="nsew")
        self.workspace.grid_rowconfigure(2, weight=1)
        self.workspace.grid_columnconfigure(0, weight=1)
        self._build_sidebar()
        self._build_topbar()
        self.toolbar = tk.Frame(self.workspace, bg=C["surface"], height=49,
                                highlightthickness=1, highlightbackground=C["border_soft"])
        self.toolbar.grid(row=1, column=0, sticky="ew")
        self.toolbar.grid_propagate(False)
        self.body = tk.Frame(self.workspace, bg=C["surface"])
        self.body.grid(row=2, column=0, sticky="nsew")
        self.body.grid_rowconfigure(0, weight=1)
        self.body.grid_columnconfigure(0, weight=1)

    def _build_sidebar(self) -> None:
        top = tk.Frame(self.sidebar, bg=C["sidebar"])
        top.pack(fill="x", padx=17, pady=(18, 29))
        mark = tk.Canvas(top, width=34, height=34, bg=C["sidebar"], highlightthickness=0)
        mark.pack(side="left", padx=(0, 10))
        mark.create_rectangle(1, 1, 33, 33, fill=C["accent_soft"], outline=C["accent"])
        mark.create_text(17, 17, text="M", fill="#d4d7ff", font=(FONT, 15, "bold"))
        brand = tk.Frame(top, bg=C["sidebar"])
        brand.pack(side="left", anchor="center")
        self.label(brand, "MechPro", bg=C["sidebar"], size=14, weight="bold").pack(anchor="w")
        self.label(brand, "DESIGN & ENGINEERING", bg=C["sidebar"], fg=C["subtle"], size=7).pack(anchor="w", pady=(2, 0))

        self.nav_buttons: dict[str, tk.Frame] = {}
        for heading, items in self.NAV_GROUPS:
            self.label(self.sidebar, heading, bg=C["sidebar"], fg=C["subtle"],
                       size=7, weight="bold").pack(anchor="w", padx=21, pady=(7, 9))
            for icon, title in items:
                self._nav_item(icon, title)
        self.label(self.sidebar, "ÇALIŞMA MODÜLLERİ", bg=C["sidebar"], fg=C["subtle"],
                   size=7, weight="bold").pack(anchor="w", padx=21, pady=(24, 9))
        module_hint = tk.Frame(self.sidebar, bg=C["sidebar"])
        module_hint.pack(fill="x", padx=21)
        self.label(module_hint, "Bu alan daha sonra modüllerle doldurulacak.",
                   bg=C["sidebar"], fg=C["subtle"], size=8, wraplength=180,
                   justify="left").pack(anchor="w")

        bottom = tk.Frame(self.sidebar, bg=C["sidebar"])
        bottom.pack(side="bottom", fill="x", padx=13, pady=14)
        tk.Frame(bottom, height=1, bg=C["border"]).pack(fill="x", pady=(0, 12))
        local = tk.Frame(bottom, bg="#1c2b2a", highlightthickness=1, highlightbackground="#294642")
        local.pack(fill="x", pady=(0, 9))
        self.label(local, "●", fg=C["green"], bg="#1c2b2a", size=9).pack(side="left", padx=(10, 7), pady=10)
        status = tk.Frame(local, bg="#1c2b2a")
        status.pack(side="left", pady=7)
        self.label(status, "Yerel çalışma", bg="#1c2b2a", fg="#c1ddd2", size=9, weight="bold").pack(anchor="w")
        self.label(status, "Veriler bu bilgisayarda saklanır", bg="#1c2b2a", fg="#85a59b", size=7).pack(anchor="w", pady=(2, 0))
        self._nav_item("⚙", "Ayarlar")
        self.label(bottom, "MECHPRO · UI PREVIEW 0.3", bg=C["sidebar"], fg=C["subtle"], size=7).pack(anchor="w", padx=10, pady=(8, 0))

    def _nav_item(self, icon: str, title: str) -> None:
        row = tk.Frame(self.sidebar, bg=C["sidebar"], cursor="hand2")
        row.pack(fill="x", padx=10, pady=2)
        self.nav_buttons[title] = row
        icon_label = self.label(row, icon, bg=C["sidebar"], fg=C["muted"], size=13, width=2)
        icon_label.pack(side="left", padx=(9, 10), pady=8)
        text_label = self.label(row, title, bg=C["sidebar"], fg="#b5b8c5", size=9)
        text_label.pack(side="left", pady=8)
        for widget in (row, icon_label, text_label):
            widget.bind("<Button-1>", lambda _event, page=title: self.navigate(page))
            widget.bind("<Enter>", lambda _event, target=row: target.configure(bg=C["hover"]))
            widget.bind("<Leave>", lambda _event, target=row, name=title: target.configure(
                bg=C["accent_soft"] if self.current_page == name else C["sidebar"]
            ))

    def _highlight_nav(self) -> None:
        for name, row in self.nav_buttons.items():
            color = C["accent_soft"] if self.current_page == name else C["sidebar"]
            row.configure(bg=color)
            for widget in row.winfo_children():
                widget.configure(bg=color, fg="#c5caff" if self.current_page == name else C["muted"] if widget == row.winfo_children()[0] else "#b5b8c5")

    def _build_topbar(self) -> None:
        bar = tk.Frame(self.workspace, bg=C["surface"], height=58,
                       highlightthickness=1, highlightbackground=C["border_soft"])
        bar.grid(row=0, column=0, sticky="ew")
        bar.grid_propagate(False)
        left = tk.Frame(bar, bg=C["surface"])
        left.pack(side="left", fill="y", padx=22)
        self.label(left, "◈", bg=C["surface"], fg=C["accent"], size=15).pack(side="left", padx=(0, 9))
        self.label(left, "MechPro Workspace", bg=C["surface"], size=10, weight="bold").pack(side="left")
        self.label(left, "⌄", bg=C["surface"], fg=C["muted"], size=11).pack(side="left", padx=(8, 12))
        self.label(left, "☆", bg=C["surface"], fg=C["muted"], size=15).pack(side="left")
        right = tk.Frame(bar, bg=C["surface"])
        right.pack(side="right", fill="y", padx=18)
        self.button(right, "✧  Güncellemeler",
                    lambda: self.notice("Güncellemeler", "Bu önizlemede güncelleme bağlantısı etkin değil.")).pack(side="left", padx=(0, 9), pady=12)
        avatar = tk.Canvas(right, width=29, height=29, bg=C["surface"], highlightthickness=0)
        avatar.pack(side="left", pady=13)
        avatar.create_oval(1, 1, 28, 28, fill="#343953", outline="#59618a")
        avatar.create_text(14, 14, text="MP", fill="#d6d9ff", font=(FONT, 8, "bold"))

    def _build_toolbar(self) -> None:
        for child in self.toolbar.winfo_children():
            child.destroy()
        left = tk.Frame(self.toolbar, bg=C["surface"])
        left.pack(side="left", padx=21, fill="y")
        self.label(left, self.current_page.upper(), bg=C["surface_alt"],
                   fg="#c4c9d8", size=8, weight="bold").pack(side="left", pady=9, ipadx=8, ipady=5)
        self.label(left, "  ·  Tasarım ve hesaplama çalışma alanı",
                   bg=C["surface"], fg=C["muted"], size=8).pack(side="left")
        if self.current_page == "Projeler":
            right = tk.Frame(self.toolbar, bg=C["surface"])
            right.pack(side="right", padx=20, fill="y")
            entry = tk.Entry(
                right, textvariable=self.search_var, width=24, bg=C["surface_alt"],
                fg=C["text"], insertbackground=C["text"], relief="flat", font=(FONT, 9),
                bd=0, highlightthickness=1, highlightbackground=C["border"],
                highlightcolor=C["accent"],
            )
            entry.pack(side="left", pady=9, ipady=6, padx=(0, 8))
            entry.insert(0, "")
            self.button(right, "⌕  Ara", lambda: self.search_var.set(self.search_var.get())).pack(side="left", pady=8)
        elif self.current_page == "Genel Bakış":
            self.label(self.toolbar, "ÇEVRİMDIŞI · YEREL VERİ", bg=C["surface"],
                       fg=C["green"], size=8, weight="bold").pack(side="right", padx=22)

    def navigate(self, page: str) -> None:
        self.current_page = page
        self._highlight_nav()
        self._build_toolbar()
        self.render_page()

    def render_page(self) -> None:
        for child in self.body.winfo_children():
            child.destroy()
        if self.current_page == "Genel Bakış":
            self.render_overview()
        elif self.current_page == "Projeler":
            self.render_projects()
        else:
            self.render_placeholder(self.current_page)

    def _page_container(self) -> tk.Frame:
        page = tk.Frame(self.body, bg=C["surface"])
        page.pack(fill="both", expand=True, padx=30, pady=25)
        return page

    def render_overview(self) -> None:
        page = self._page_container()
        projects = self.store.list_projects()
        self.label(page, "MEKANİK TASARIM VE HESAPLAMA", bg=C["surface"],
                   fg=C["accent"], size=8, weight="bold").pack(anchor="w", pady=(0, 10))
        self.label(page, "Çalışma alanına hoş geldin.", bg=C["surface"],
                   size=23, weight="bold").pack(anchor="w")
        self.label(page, "Mekanik tesisat projelerini ve mühendislik çalışmalarını tek yerde düzenle.",
                   bg=C["surface"], fg=C["muted"], size=10).pack(anchor="w", pady=(7, 24))

        cards = tk.Frame(page, bg=C["surface"])
        cards.pack(fill="x")
        self._overview_card(cards, "KAYITLI PROJELER", str(len(projects)),
                            "Yerel proje arşivindeki toplam kayıt", "▤", 0)
        self._overview_card(cards, "ÇALIŞMA ALANI", "Tasarım",
                            "İmalat ve ilerleme takibi içermez", "⌘", 1)
        self._overview_card(cards, "VERİ DEPOLAMA", "Yerel",
                            "Projeler bu bilgisayarda saklanır", "◉", 2)

        section = tk.Frame(page, bg=C["surface"])
        section.pack(fill="x", pady=(32, 12))
        self.label(section, "Son açılan projeler", bg=C["surface"], size=14, weight="bold").pack(side="left")
        self.button(section, "Projeleri görüntüle  →",
                    lambda: self.navigate("Projeler"), compact=True).pack(side="right")

        recent = tk.Frame(page, bg=C["surface_alt"], highlightthickness=1,
                          highlightbackground=C["border"])
        recent.pack(fill="x")
        if not projects:
            self.label(recent, "Henüz kayıtlı proje yok.",
                       bg=C["surface_alt"], fg=C["muted"], size=10).pack(anchor="w", padx=17, pady=(17, 4))
            self.label(recent, "İlk projen oluşturulduğunda burada görünecek. Proje eklemek için Projeler sekmesine geç.",
                       bg=C["surface_alt"], fg=C["subtle"], size=9, wraplength=700,
                       justify="left").pack(anchor="w", padx=17, pady=(0, 17))
        else:
            for project in projects[:4]:
                self._overview_project_row(recent, project)

        info = tk.Frame(page, bg="#1b1b2a", highlightthickness=1,
                        highlightbackground="#39344f")
        info.pack(fill="x", pady=(21, 0))
        self.label(info, "✦", bg="#1b1b2a", fg="#b5a6ed", size=16).pack(side="left", padx=(15, 11), pady=14)
        info_text = tk.Frame(info, bg="#1b1b2a")
        info_text.pack(side="left", pady=12)
        self.label(info_text, "MechPro tasarım odaklı geliştiriliyor",
                   bg="#1b1b2a", fg="#e3def7", size=10, weight="bold").pack(anchor="w")
        self.label(info_text, "Hesaplama ve mühendislik modüllerini ihtiyaçlara göre sırayla ekleyeceğiz.",
                   bg="#1b1b2a", fg="#aaa2c2", size=8).pack(anchor="w", pady=(4, 0))

    def _overview_card(self, parent: tk.Widget, title: str, value: str,
                       description: str, icon: str, column: int) -> None:
        card = tk.Frame(parent, bg=C["surface_alt"], highlightthickness=1,
                        highlightbackground=C["border"])
        card.grid(row=0, column=column, sticky="nsew", padx=(0 if column == 0 else 9, 0))
        parent.grid_columnconfigure(column, weight=1)
        self.label(card, icon, bg=C["surface_alt"], fg=C["accent"], size=16).pack(anchor="w", padx=15, pady=(13, 8))
        self.label(card, title, bg=C["surface_alt"], fg=C["subtle"], size=7, weight="bold").pack(anchor="w", padx=15)
        self.label(card, value, bg=C["surface_alt"], size=18, weight="bold").pack(anchor="w", padx=15, pady=(6, 2))
        self.label(card, description, bg=C["surface_alt"], fg=C["muted"], size=8).pack(anchor="w", padx=15, pady=(0, 14))

    def _overview_project_row(self, parent: tk.Widget, project: dict[str, object]) -> None:
        row = tk.Frame(parent, bg=C["surface_alt"], cursor="hand2")
        row.pack(fill="x", padx=13, pady=3)
        mark = tk.Label(row, text="MP", bg=C["accent_soft"], fg="#cbd0ff",
                        font=(FONT, 8, "bold"), padx=8, pady=7)
        mark.pack(side="left", padx=(3, 11), pady=7)
        name_area = tk.Frame(row, bg=C["surface_alt"])
        name_area.pack(side="left", fill="x", expand=True, pady=7)
        self.label(name_area, str(project["name"]), bg=C["surface_alt"], size=9, weight="bold").pack(anchor="w")
        self.label(name_area, str(project["code"]), bg=C["surface_alt"], fg=C["subtle"], size=7).pack(anchor="w", pady=(3, 0))
        self.label(row, "Düzenlendi  " + self._format_date(str(project["updated_at"])),
                   bg=C["surface_alt"], fg=C["muted"], size=8).pack(side="left", padx=12)
        for widget in (row, mark, name_area, *name_area.winfo_children()):
            widget.bind("<Button-1>", lambda _event, pid=int(project["id"]): self.view_project(pid))

    def render_projects(self) -> None:
        page = self._page_container()
        heading = tk.Frame(page, bg=C["surface"])
        heading.pack(fill="x", pady=(0, 18))
        text_area = tk.Frame(heading, bg=C["surface"])
        text_area.pack(side="left")
        self.label(text_area, "Projeler", bg=C["surface"], size=21, weight="bold").pack(anchor="w")
        self.label(text_area, "Mekanik tasarım ve hesaplama çalışmalarını yönet.",
                   bg=C["surface"], fg=C["muted"], size=9).pack(anchor="w", pady=(6, 0))
        self.button(heading, "+  Yeni proje", self.new_project, primary=True).pack(side="right", pady=(3, 0))

        table = tk.Frame(page, bg=C["surface_alt"], highlightthickness=1,
                         highlightbackground=C["border"])
        table.pack(fill="both", expand=True)
        header = tk.Frame(table, bg=C["surface_alt"])
        header.pack(fill="x", padx=12, pady=(12, 7))
        for title, width in (("KOD", 11), ("PROJE ADI", 39), ("AÇIKLAMA", 31), ("SON DÜZENLEME", 17), ("İŞLEMLER", 25)):
            self.label(header, title, bg=C["surface_alt"], fg=C["subtle"],
                       size=7, weight="bold", width=width, anchor="w").pack(side="left", padx=(4, 0))
        tk.Frame(table, bg=C["border"], height=1).pack(fill="x")

        projects = self.store.list_projects()
        query = self.search_var.get().strip().casefold()
        if query:
            projects = [
                project for project in projects
                if query in str(project["name"]).casefold()
                or query in str(project["code"]).casefold()
                or query in str(project["description"]).casefold()
            ]
        if not projects:
            empty = tk.Frame(table, bg=C["surface_alt"])
            empty.pack(fill="both", expand=True, pady=70)
            self.label(empty, "Henüz proje bulunamadı" if not query else "Aramana uygun proje yok",
                       bg=C["surface_alt"], size=12, weight="bold").pack()
            self.label(empty, "Yeni proje ekleyerek tasarım çalışmalarını burada düzenleyebilirsin.",
                       bg=C["surface_alt"], fg=C["muted"], size=9).pack(pady=(7, 0))
        else:
            for project in projects:
                self._project_row(table, project)

        footer = tk.Frame(page, bg=C["surface"])
        footer.pack(fill="x", pady=(10, 0))
        self.label(footer, f"{len(projects)} proje",
                   bg=C["surface"], fg=C["muted"], size=8).pack(side="left")
        self.label(footer, "Proje verileri yerel SQLite veritabanında saklanır.",
                   bg=C["surface"], fg=C["subtle"], size=8).pack(side="right")

    def _project_row(self, parent: tk.Widget, project: dict[str, object]) -> None:
        row = tk.Frame(parent, bg=C["surface_alt"])
        row.pack(fill="x", padx=8, pady=2)
        self.label(row, str(project["code"]), bg=C["surface_alt"], fg=C["muted"],
                   size=8, width=11, anchor="w").pack(side="left", padx=(8, 0), pady=14)
        self.label(row, str(project["name"]), bg=C["surface_alt"],
                   size=9, weight="bold", width=39, anchor="w").pack(side="left", padx=(4, 0))
        description = str(project["description"]) or "—"
        self.label(row, description[:42] + ("…" if len(description) > 42 else ""),
                   bg=C["surface_alt"], fg=C["muted"], size=8,
                   width=31, anchor="w").pack(side="left", padx=(4, 0))
        self.label(row, self._format_date(str(project["updated_at"])),
                   bg=C["surface_alt"], fg=C["muted"], size=8,
                   width=17, anchor="w").pack(side="left", padx=(4, 0))
        actions = tk.Frame(row, bg=C["surface_alt"])
        actions.pack(side="left", padx=(1, 3))
        project_id = int(project["id"])
        self.button(actions, "Görüntüle", lambda pid=project_id: self.view_project(pid), compact=True).pack(side="left", padx=2)
        self.button(actions, "Düzenle", lambda pid=project_id: self.edit_project(pid), compact=True).pack(side="left", padx=2)
        self.button(actions, "Sil", lambda pid=project_id: self.delete_project(pid), compact=True).pack(side="left", padx=2)

    def view_project(self, project_id: int) -> None:
        project = self.store.get_project(project_id)
        if project is None:
            self.notice("Proje bulunamadı", "Bu proje artık mevcut değil.")
            self.navigate("Projeler")
            return
        for child in self.body.winfo_children():
            child.destroy()
        page = self._page_container()
        self.button(page, "←  Projelere dön", lambda: self.navigate("Projeler"), compact=True).pack(anchor="w", pady=(0, 22))
        self.label(page, "PROJE ÇALIŞMA ALANI", bg=C["surface"], fg=C["accent"],
                   size=8, weight="bold").pack(anchor="w")
        self.label(page, str(project["name"]), bg=C["surface"],
                   size=22, weight="bold", wraplength=900, justify="left").pack(anchor="w", pady=(9, 5))
        self.label(page, f'{project["code"]}  ·  Oluşturulma: {self._format_date(str(project["created_at"]))}',
                   bg=C["surface"], fg=C["muted"], size=9).pack(anchor="w", pady=(0, 18))
        card = tk.Frame(page, bg=C["surface_alt"], highlightthickness=1,
                        highlightbackground=C["border"])
        card.pack(fill="x", pady=(0, 14))
        self.label(card, "PROJE AÇIKLAMASI", bg=C["surface_alt"],
                   fg=C["subtle"], size=7, weight="bold").pack(anchor="w", padx=17, pady=(15, 6))
        description = str(project["description"]) or "Bu proje için henüz açıklama eklenmemiş."
        self.label(card, description, bg=C["surface_alt"], fg=C["text"],
                   size=9, wraplength=850, justify="left").pack(anchor="w", padx=17, pady=(0, 16))
        workspace = tk.Frame(page, bg=C["surface"], highlightthickness=1,
                             highlightbackground=C["border"])
        workspace.pack(fill="both", expand=True)
        self.label(workspace, "Tasarım ve hesaplama çalışma alanı",
                   bg=C["surface"], size=13, weight="bold").pack(anchor="w", padx=18, pady=(18, 7))
        self.label(workspace, "Bu proje alanını ihtiyaç duyduğumuz çizim, hesaplama ve mühendislik modülleriyle adım adım oluşturacağız.",
                   bg=C["surface"], fg=C["muted"], size=9, wraplength=850,
                   justify="left").pack(anchor="w", padx=18)
        actions = tk.Frame(page, bg=C["surface"])
        actions.pack(fill="x", pady=(14, 0))
        self.button(actions, "Düzenle", lambda: self.edit_project(project_id)).pack(side="left")
        self.button(actions, "Projeyi sil", lambda: self.delete_project(project_id)).pack(side="left", padx=8)

    def render_placeholder(self, page_name: str) -> None:
        page = self._page_container()
        self.label(page, "ÇALIŞMA ALANI", bg=C["surface"], fg=C["subtle"],
                   size=8, weight="bold").pack(anchor="w")
        self.label(page, page_name, bg=C["surface"], size=22, weight="bold").pack(anchor="w", pady=(11, 8))
        self.label(page, "Bu menü şimdilik yer tutucu olarak bırakıldı.",
                   bg=C["surface"], fg=C["muted"], size=10).pack(anchor="w")
        self.label(page, "İçeriğini ve kullanılacak modülleri ihtiyaçlara göre daha sonra belirleyeceğiz.",
                   bg=C["surface"], fg=C["subtle"], size=9).pack(anchor="w", pady=(7, 0))

    def new_project(self) -> None:
        self.project_dialog()

    def edit_project(self, project_id: int) -> None:
        project = self.store.get_project(project_id)
        if project:
            self.project_dialog(project)

    def project_dialog(self, project: dict[str, object] | None = None) -> None:
        editing = project is not None
        dialog = tk.Toplevel(self.root)
        dialog.title("Projeyi düzenle" if editing else "Yeni proje")
        dialog.configure(bg=C["surface"])
        dialog.resizable(False, False)
        dialog.transient(self.root)
        dialog.grab_set()
        dialog.geometry("500x350" if not editing else "500x365")
        dialog.update_idletasks()
        x = self.root.winfo_rootx() + (self.root.winfo_width() - 500) // 2
        y = self.root.winfo_rooty() + (self.root.winfo_height() - dialog.winfo_height()) // 2
        dialog.geometry(f"+{x}+{y}")

        panel = tk.Frame(dialog, bg=C["surface"])
        panel.pack(fill="both", expand=True, padx=25, pady=23)
        self.label(panel, "Projeyi düzenle" if editing else "Yeni proje oluştur",
                   bg=C["surface"], size=16, weight="bold").pack(anchor="w")
        self.label(panel, "Proje bilgilerini gir. Proje adı benzersiz olmalıdır.",
                   bg=C["surface"], fg=C["muted"], size=9).pack(anchor="w", pady=(5, 18))

        self.label(panel, "PROJE ADI *", bg=C["surface"], fg=C["subtle"],
                   size=7, weight="bold").pack(anchor="w", pady=(0, 6))
        name_var = tk.StringVar(value=str(project["name"]) if editing else "")
        name_entry = tk.Entry(panel, textvariable=name_var, bg=C["surface_alt"],
                              fg=C["text"], insertbackground=C["text"], relief="flat",
                              font=(FONT, 10), highlightthickness=1,
                              highlightbackground=C["border"], highlightcolor=C["accent"])
        name_entry.pack(fill="x", ipady=9)
        name_entry.focus_set()

        self.label(panel, "AÇIKLAMA", bg=C["surface"], fg=C["subtle"],
                   size=7, weight="bold").pack(anchor="w", pady=(15, 6))
        desc = tk.Text(panel, height=4, wrap="word", bg=C["surface_alt"], fg=C["text"],
                       insertbackground=C["text"], relief="flat", font=(FONT, 9),
                       highlightthickness=1, highlightbackground=C["border"],
                       highlightcolor=C["accent"])
        desc.pack(fill="x")
        if editing:
            desc.insert("1.0", str(project["description"]))

        actions = tk.Frame(panel, bg=C["surface"])
        actions.pack(fill="x", side="bottom", pady=(17, 0))
        self.button(actions, "Vazgeç", dialog.destroy).pack(side="right", padx=(8, 0))

        def save() -> None:
            try:
                if editing:
                    self.store.update_project(int(project["id"]), name_var.get(), desc.get("1.0", "end").strip())
                    project_id = int(project["id"])
                else:
                    project_id = self.store.create_project(name_var.get(), desc.get("1.0", "end").strip())
            except ValueError as exc:
                messagebox.showwarning("Proje kaydedilemedi", str(exc), parent=dialog)
                name_entry.focus_set()
                return
            except sqlite3.Error as exc:
                messagebox.showerror("Veritabanı hatası", f"Proje kaydedilemedi:\n{exc}", parent=dialog)
                return
            dialog.destroy()
            self.search_var.set("")
            self.current_page = "Projeler"
            self._highlight_nav()
            self._build_toolbar()
            self.view_project(project_id) if editing else self.navigate("Projeler")

        self.button(actions, "Değişiklikleri kaydet" if editing else "Projeyi oluştur",
                    save, primary=True).pack(side="right")
        name_entry.bind("<Return>", lambda _event: save())

    def delete_project(self, project_id: int) -> None:
        project = self.store.get_project(project_id)
        if project is None:
            self.navigate("Projeler")
            return
        confirmed = messagebox.askyesno(
            "Projeyi sil",
            f'"{project["name"]}" projesini silmek istediğine emin misin?\n\nBu işlem geri alınamaz.',
            parent=self.root,
        )
        if confirmed:
            self.store.delete_project(project_id)
            self.navigate("Projeler")

    @staticmethod
    def _format_date(value: str) -> str:
        try:
            parsed = datetime.fromisoformat(value)
            return parsed.strftime("%d.%m.%Y  %H:%M")
        except ValueError:
            return value

    def notice(self, title: str, message: str) -> None:
        messagebox.showinfo(title, message, parent=self.root)


def main() -> None:
    root = tk.Tk()
    MechProApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
