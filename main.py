"""MechPro desktop UI preview.

Run with: python main.py

This is a dependency-free Tkinter interface prototype. Demo projects are kept
in memory only; this file is not yet connected to the React/Tauri application
or a persistent database.
"""

from __future__ import annotations

import tkinter as tk
from tkinter import messagebox
from dataclasses import dataclass
from typing import Callable


COLORS = {
    "window": "#11131d", "sidebar": "#141621", "surface": "#171a26",
    "surface_alt": "#1d2030", "surface_hover": "#242838",
    "border": "#2a2e3c", "border_soft": "#222635", "text": "#e7e9f0",
    "muted": "#969aaa", "subtle": "#6e7385", "accent": "#737fe0",
    "accent_soft": "#292d4a", "green": "#53bd91", "yellow": "#e5b84b",
    "blue": "#63a6e6", "purple": "#a18ae8", "orange": "#db9860", "red": "#df777d",
}
FONT = "Segoe UI"


@dataclass
class Project:
    code: str
    name: str
    scope: str
    state: str
    due: str
    owner: str
    progress: int
    updated: str
    location: str = "Örnek proje"


DEMO_PROJECTS = [
    Project("MP-024", "Merkez Ofis Mekanik Tesisatı", "Mekanik · Koordinasyon", "Devam Ediyor", "19 Kas", "EA", 68, "Bugün 10:42", "Ankara · Örnek proje"),
    Project("MP-021", "Üretim Tesisi Yenileme", "HVAC · Yangın", "Devam Ediyor", "24 Kas", "MK", 46, "Dün 16:18"),
    Project("MP-019", "Konut Blokları", "Sıhhi Tesisat", "Planlama", "02 Ara", "AY", 22, "12 Kas"),
    Project("MP-018", "Veri Merkezi Altyapısı", "Mekanik Sistemler", "Planlama", "08 Ara", "SD", 12, "10 Kas"),
    Project("MP-012", "Sağlık Yapısı Projesi", "Mekanik Koordinasyon", "Tamamlandı", "Tamamlandı", "EA", 100, "06 Kas"),
]


class MechProApp:
    def __init__(self, root: tk.Tk) -> None:
        self.root = root
        self.root.title("MechPro — Mechanical Engineering Workspace")
        self.root.geometry("1540x940")
        self.root.minsize(1180, 720)
        self.root.configure(bg=COLORS["window"])
        self.projects = [Project(**vars(p)) for p in DEMO_PROJECTS]
        self.selected_code = self.projects[0].code
        self.current_page = "Projeler"
        self.search_var = tk.StringVar()
        self.search_var.trace_add("write", lambda *_: self.render_project_list())
        self._build_shell()
        self.render_project_list()
        self.show_details(self.selected_code)

    def label(self, parent: tk.Widget, text: str, *, fg: str | None = None,
              size: int = 10, weight: str = "normal", bg: str | None = None,
              **kwargs: object) -> tk.Label:
        return tk.Label(parent, text=text, fg=fg or COLORS["text"],
                        bg=bg or COLORS["surface"], font=(FONT, size, weight), **kwargs)

    def button(self, parent: tk.Widget, text: str, command: Callable[[], None],
               *, primary: bool = False) -> tk.Button:
        bg = COLORS["accent"] if primary else COLORS["surface_alt"]
        button = tk.Button(
            parent, text=text, command=command, bg=bg,
            fg="#ffffff" if primary else COLORS["text"],
            activebackground=COLORS["surface_hover"], activeforeground=COLORS["text"],
            relief="flat", bd=0, padx=13, pady=8,
            font=(FONT, 9, "bold" if primary else "normal"),
            cursor="hand2", highlightthickness=1,
            highlightbackground=COLORS["border"], highlightcolor=COLORS["border"],
        )
        return button

    def _build_shell(self) -> None:
        self.root.grid_rowconfigure(0, weight=1)
        self.root.grid_columnconfigure(1, weight=1)
        self.sidebar = tk.Frame(self.root, bg=COLORS["sidebar"], width=246,
                                highlightthickness=1, highlightbackground=COLORS["border_soft"])
        self.sidebar.grid(row=0, column=0, sticky="nsew")
        self.sidebar.grid_propagate(False)
        self.workspace = tk.Frame(self.root, bg=COLORS["surface"])
        self.workspace.grid(row=0, column=1, sticky="nsew")
        self.workspace.grid_rowconfigure(2, weight=1)
        self.workspace.grid_columnconfigure(0, weight=1)
        self._build_sidebar()
        self._build_topbar()
        self._build_toolbar()
        self.body = tk.Frame(self.workspace, bg=COLORS["surface"])
        self.body.grid(row=2, column=0, sticky="nsew")
        self.body.grid_rowconfigure(0, weight=1)
        self.body.grid_columnconfigure(0, weight=1)

    def _build_sidebar(self) -> None:
        top = tk.Frame(self.sidebar, bg=COLORS["sidebar"])
        top.pack(fill="x", padx=16, pady=(14, 18))
        dots = tk.Frame(top, bg=COLORS["sidebar"])
        dots.pack(anchor="w", pady=(0, 17))
        for color in ("#ef6a67", "#e4b34c", "#55b77d"):
            dot = tk.Canvas(dots, width=12, height=12, bg=COLORS["sidebar"], highlightthickness=0)
            dot.pack(side="left", padx=(0, 7))
            dot.create_oval(1, 1, 11, 11, fill=color, outline=color)
        brand = tk.Frame(top, bg=COLORS["sidebar"])
        brand.pack(fill="x")
        mark = tk.Canvas(brand, width=32, height=32, bg=COLORS["sidebar"], highlightthickness=0)
        mark.pack(side="left", padx=(0, 10))
        mark.create_rectangle(1, 1, 31, 31, fill=COLORS["accent_soft"], outline=COLORS["accent"])
        mark.create_text(16, 16, text="M", fill="#cbd0ff", font=(FONT, 15, "bold"))
        brand_text = tk.Frame(brand, bg=COLORS["sidebar"])
        brand_text.pack(side="left", anchor="center")
        self.label(brand_text, "MechPro", bg=COLORS["sidebar"], size=14, weight="bold").pack(anchor="w")
        self.label(brand_text, "ENGINEERING WORKSPACE", bg=COLORS["sidebar"], fg=COLORS["subtle"], size=7).pack(anchor="w", pady=(1, 0))

        self.button(self.sidebar, "+   Yeni proje", self.new_project, primary=True).pack(fill="x", padx=14, pady=(0, 18))
        self._nav_heading("ÇALIŞMA ALANI")
        self._nav_item("⌂", "Genel Bakış")
        self._nav_item("▤", "Projeler", selected=True)
        self._nav_item("∑", "Hesaplamalar")
        self._nav_item("▦", "Kütüphane")
        self._nav_item("▧", "Raporlar")
        self._nav_heading("GÖRÜNÜMLER", top_gap=23)
        self._nav_item("◷", "Son Çalışmalar")
        self._nav_item("◇", "Yıldızlılar")
        self._nav_item("▱", "Arşiv")
        self._nav_heading("ÇALIŞMA MODÜLLERİ", top_gap=23)
        note = tk.Frame(self.sidebar, bg=COLORS["sidebar"])
        note.pack(fill="x", padx=20, pady=(0, 4))
        self.label(note, "Modül yapısını birlikte\nşekillendireceğiz.", bg=COLORS["sidebar"],
                   fg=COLORS["subtle"], size=9, justify="left").pack(anchor="w")

        bottom = tk.Frame(self.sidebar, bg=COLORS["sidebar"])
        bottom.pack(side="bottom", fill="x", padx=13, pady=14)
        tk.Frame(bottom, height=1, bg=COLORS["border"]).pack(fill="x", pady=(0, 12))
        local = tk.Frame(bottom, bg="#1c2b2a", highlightthickness=1, highlightbackground="#294642")
        local.pack(fill="x", pady=(0, 9))
        self.label(local, "●", fg=COLORS["green"], bg="#1c2b2a", size=9).pack(side="left", padx=(10, 7), pady=10)
        local_text = tk.Frame(local, bg="#1c2b2a")
        local_text.pack(side="left", pady=7)
        self.label(local_text, "Yerel çalışma", bg="#1c2b2a", fg="#c1ddd2", size=9, weight="bold").pack(anchor="w")
        self.label(local_text, "Çevrimdışı arayüz önizlemesi", bg="#1c2b2a", fg="#85a59b", size=7).pack(anchor="w", pady=(2, 0))
        self._nav_item("⚙", "Ayarlar", parent=bottom)
        self.label(bottom, "MECHPRO  ·  UI PREVIEW 0.2", bg=COLORS["sidebar"], fg=COLORS["subtle"], size=7).pack(anchor="w", padx=10, pady=(8, 0))

    def _nav_heading(self, text: str, top_gap: int = 0) -> None:
        frame = tk.Frame(self.sidebar, bg=COLORS["sidebar"])
        frame.pack(fill="x", padx=22, pady=(top_gap, 7))
        self.label(frame, text, bg=COLORS["sidebar"], fg=COLORS["subtle"], size=7, weight="bold").pack(anchor="w")

    def _nav_item(self, icon: str, text: str, selected: bool = False,
                  parent: tk.Widget | None = None) -> None:
        container = parent or self.sidebar
        bg = COLORS["accent_soft"] if selected else COLORS["sidebar"]
        fg = "#c5caff" if selected else "#b5b8c5"
        row = tk.Frame(container, bg=bg, cursor="hand2")
        row.pack(fill="x", padx=10, pady=2)
        icon_label = self.label(row, icon, bg=bg, fg=fg, size=13, width=2)
        icon_label.pack(side="left", padx=(9, 10), pady=8)
        text_label = self.label(row, text, bg=bg, fg=fg, size=9, weight="bold" if selected else "normal")
        text_label.pack(side="left", pady=8)
        for widget in (row, icon_label, text_label):
            widget.bind("<Button-1>", lambda _e, page=text: self.navigate(page))
            widget.bind("<Enter>", lambda _e, r=row, p=selected: r.configure(bg=COLORS["surface_hover"] if not p else COLORS["accent_soft"]))
            widget.bind("<Leave>", lambda _e, r=row, b=bg: r.configure(bg=b))

    def _build_topbar(self) -> None:
        bar = tk.Frame(self.workspace, bg=COLORS["surface"], height=58,
                       highlightthickness=1, highlightbackground=COLORS["border_soft"])
        bar.grid(row=0, column=0, sticky="ew")
        bar.grid_propagate(False)
        left = tk.Frame(bar, bg=COLORS["surface"])
        left.pack(side="left", fill="y", padx=23)
        self.label(left, "◈", bg=COLORS["surface"], fg=COLORS["accent"], size=15).pack(side="left", padx=(0, 9))
        self.label(left, "MechPro Workspace", bg=COLORS["surface"], size=10, weight="bold").pack(side="left")
        self.label(left, "⌄", bg=COLORS["surface"], fg=COLORS["muted"], size=11).pack(side="left", padx=(8, 12))
        self.label(left, "☆", bg=COLORS["surface"], fg=COLORS["muted"], size=15).pack(side="left")
        right = tk.Frame(bar, bg=COLORS["surface"])
        right.pack(side="right", fill="y", padx=18)
        self.button(right, "✧  Güncellemeler", lambda: self.show_notice("Güncellemeler", "Bu önizlemede güncelleme bağlantısı henüz etkin değil.")).pack(side="left", padx=(0, 9), pady=12)
        self.button(right, "♧", lambda: self.show_notice("Bildirimler", "Şimdilik yeni bildirimin yok.")).pack(side="left", pady=12)
        self.label(right, "│", bg=COLORS["surface"], fg=COLORS["border"], size=15).pack(side="left", padx=10)
        avatar = tk.Canvas(right, width=29, height=29, bg=COLORS["surface"], highlightthickness=0)
        avatar.pack(side="left", pady=13)
        avatar.create_oval(1, 1, 28, 28, fill="#343953", outline="#59618a")
        avatar.create_text(14, 14, text="MP", fill="#d6d9ff", font=(FONT, 8, "bold"))

    def _build_toolbar(self) -> None:
        toolbar = tk.Frame(self.workspace, bg=COLORS["surface"], height=52,
                           highlightthickness=1, highlightbackground=COLORS["border_soft"])
        toolbar.grid(row=1, column=0, sticky="ew")
        toolbar.grid_propagate(False)
        left = tk.Frame(toolbar, bg=COLORS["surface"])
        left.pack(side="left", padx=22, fill="y")
        self.label(left, "▣  TÜM PROJELER", bg=COLORS["surface_alt"], fg="#c4c9d8", size=8, weight="bold").pack(side="left", pady=10, ipadx=8, ipady=5)
        self.label(left, "  ·  ", bg=COLORS["surface"], fg=COLORS["subtle"]).pack(side="left")
        self.label(left, "Çalışma alanı", bg=COLORS["surface"], fg=COLORS["muted"], size=8).pack(side="left")
        right = tk.Frame(toolbar, bg=COLORS["surface"])
        right.pack(side="right", padx=20, fill="y")
        self.search_entry = tk.Entry(
            right, textvariable=self.search_var, width=23, bg=COLORS["surface_alt"],
            fg=COLORS["text"], insertbackground=COLORS["text"], relief="flat",
            font=(FONT, 9), bd=0, highlightthickness=1,
            highlightbackground=COLORS["border"], highlightcolor=COLORS["accent"])
        self.search_entry.pack(side="left", pady=10, ipady=6, padx=(0, 8))
        self.button(right, "☷  Filtre", lambda: self.show_notice("Filtre", "Filtre seçeneklerini sonraki adımda birlikte belirleyebiliriz.")).pack(side="left", pady=9, padx=(0, 8))
        self.button(right, "▤  Görünüm", lambda: self.show_notice("Görünüm", "Liste görünümü şu anki önizleme düzenidir.")).pack(side="left", pady=9)

    def navigate(self, page: str) -> None:
        self.current_page = page
        if page in ("Projeler", "Genel Bakış"):
            self.render_project_list()
            self.show_details(self.selected_code)
            return
        for child in self.body.winfo_children():
            child.destroy()
        placeholder = tk.Frame(self.body, bg=COLORS["surface"])
        placeholder.pack(fill="both", expand=True, padx=55, pady=55)
        self.label(placeholder, "ÇALIŞMA ALANI", bg=COLORS["surface"], fg=COLORS["subtle"], size=8, weight="bold").pack(anchor="w")
        self.label(placeholder, page, bg=COLORS["surface"], size=23, weight="bold").pack(anchor="w", pady=(12, 8))
        self.label(placeholder, "Bu ekranın içeriğini ve işlevlerini birlikte, adım adım belirleyeceğiz.",
                   bg=COLORS["surface"], fg=COLORS["muted"], size=10).pack(anchor="w")
        self.label(placeholder, "Şimdilik herhangi bir modül veya iş akışı sabitlenmedi.",
                   bg=COLORS["surface"], fg=COLORS["subtle"], size=9).pack(anchor="w", pady=(8, 0))

    def render_project_list(self) -> None:
        if not hasattr(self, "body"):
            return
        for child in self.body.winfo_children():
            child.destroy()
        container = tk.Frame(self.body, bg=COLORS["surface"])
        container.pack(fill="both", expand=True)
        container.grid_rowconfigure(0, weight=1)
        container.grid_columnconfigure(0, weight=1)
        list_panel = tk.Frame(container, bg=COLORS["surface"])
        list_panel.grid(row=0, column=0, sticky="nsew")
        list_panel.grid_rowconfigure(1, weight=1)
        list_panel.grid_columnconfigure(0, weight=1)
        title = tk.Frame(list_panel, bg=COLORS["surface"])
        title.grid(row=0, column=0, sticky="ew", padx=28, pady=(22, 17))
        title.grid_columnconfigure(0, weight=1)
        self.label(title, "Projeler", bg=COLORS["surface"], size=19, weight="bold").grid(row=0, column=0, sticky="w")
        self.label(title, "Mekanik mühendislik çalışmalarının genel görünümü", bg=COLORS["surface"], fg=COLORS["muted"], size=9).grid(row=1, column=0, sticky="w", pady=(5, 0))
        self.button(title, "+  Yeni proje", self.new_project, primary=True).grid(row=0, column=1, rowspan=2, sticky="e")

        table = tk.Frame(list_panel, bg=COLORS["surface"])
        table.grid(row=1, column=0, sticky="nsew", padx=20, pady=(0, 15))
        table.grid_columnconfigure(0, weight=1)
        self._table_header(table)
        query = self.search_var.get().strip().casefold()
        row_index = 1
        visible_count = 0
        for state in ("Devam Ediyor", "Planlama", "Tamamlandı"):
            items = [p for p in self.projects if p.state == state and
                     (not query or query in " ".join((p.code, p.name, p.scope, p.owner)).casefold())]
            if not items:
                continue
            self._group_header(table, state, len(items), row_index)
            row_index += 1
            for project in items:
                self._project_row(table, project, row_index)
                row_index += 1
                visible_count += 1
        if visible_count == 0:
            self.label(table, "Aramana uygun proje bulunamadı.", bg=COLORS["surface"], fg=COLORS["muted"], size=10).grid(row=1, column=0, sticky="w", padx=16, pady=24)
        footer = tk.Frame(list_panel, bg=COLORS["surface"], highlightthickness=1, highlightbackground=COLORS["border_soft"])
        footer.grid(row=2, column=0, sticky="ew", padx=20, pady=(0, 13))
        self.label(footer, f"{len(self.projects)} örnek proje", bg=COLORS["surface"], fg=COLORS["muted"], size=8).pack(side="left", padx=10, pady=9)
        self.label(footer, "Bu veriler yalnızca arayüz önizlemesi içindir", bg=COLORS["surface"], fg=COLORS["subtle"], size=8).pack(side="right", padx=10, pady=9)
        self._build_inspector(container)

    def _table_header(self, parent: tk.Widget) -> None:
        header = tk.Frame(parent, bg=COLORS["surface"])
        header.grid(row=0, column=0, sticky="ew")
        for col, label in enumerate(("PROJE", "KAPSAM", "DURUM", "HEDEF", "İLERLEME", "SORUMLU")):
            self.label(header, label, bg=COLORS["surface"], fg=COLORS["subtle"], size=7, weight="bold").grid(row=0, column=col, sticky="w", padx=(8, 8), pady=(0, 9))
        for col, weight in enumerate((0, 1, 0, 0, 0, 0)):
            header.grid_columnconfigure(col, weight=weight)

    def _group_header(self, parent: tk.Widget, state: str, count: int, row: int) -> None:
        bg = COLORS["surface_alt"]
        frame = tk.Frame(parent, bg=bg, height=37)
        frame.grid(row=row, column=0, sticky="ew", pady=(7, 0))
        frame.grid_propagate(False)
        color = {"Devam Ediyor": COLORS["yellow"], "Planlama": COLORS["purple"], "Tamamlandı": COLORS["accent"]}.get(state, COLORS["muted"])
        symbol = {"Devam Ediyor": "◉", "Planlama": "◌", "Tamamlandı": "●"}.get(state, "○")
        self.label(frame, symbol, bg=bg, fg=color, size=11).pack(side="left", padx=(12, 8))
        self.label(frame, state, bg=bg, size=9, weight="bold").pack(side="left")
        self.label(frame, str(count), bg=bg, fg=COLORS["muted"], size=8).pack(side="left", padx=(8, 0))

    def _project_row(self, parent: tk.Widget, project: Project, row: int) -> None:
        selected = project.code == self.selected_code
        bg = COLORS["surface_hover"] if selected else COLORS["surface"]
        frame = tk.Frame(parent, bg=bg, height=57, cursor="hand2",
                         highlightthickness=1,
                         highlightbackground=COLORS["accent"] if selected else COLORS["border_soft"])
        frame.grid(row=row, column=0, sticky="ew")
        frame.grid_propagate(False)
        frame.grid_columnconfigure(1, weight=1)
        frame.grid_columnconfigure(4, minsize=78)
        self.label(frame, project.code, bg=bg, fg=COLORS["muted"], size=8).grid(row=0, column=0, sticky="w", padx=(12, 10), pady=16)
        name_box = tk.Frame(frame, bg=bg)
        name_box.grid(row=0, column=1, sticky="w", pady=8)
        self.label(name_box, project.name, bg=bg, size=9, weight="bold" if selected else "normal").pack(anchor="w")
        self.label(name_box, project.updated, bg=bg, fg=COLORS["subtle"], size=7).pack(anchor="w", pady=(3, 0))
        self.label(frame, project.scope, bg=bg, fg=COLORS["muted"], size=8).grid(row=0, column=2, sticky="w", padx=(5, 10))
        state_color = {"Devam Ediyor": COLORS["yellow"], "Planlama": COLORS["purple"], "Tamamlandı": COLORS["green"]}[project.state]
        self.label(frame, "●  " + project.state, bg=COLORS["surface_alt"], fg=state_color, size=7).grid(row=0, column=3, sticky="w", padx=(0, 10), ipadx=7, ipady=5)
        self.label(frame, project.due, bg=bg, fg=COLORS["muted"], size=8).grid(row=0, column=4, sticky="w", padx=(3, 4))
        progress = tk.Frame(frame, bg=bg)
        progress.grid(row=0, column=5, sticky="ew", padx=(0, 7))
        self.label(progress, f"{project.progress}%", bg=bg, fg=COLORS["muted"], size=8).grid(row=0, column=0, sticky="w")
        bar = tk.Canvas(progress, width=64, height=5, bg=bg, highlightthickness=0)
        bar.grid(row=1, column=0, sticky="w", pady=(4, 0))
        bar.create_rectangle(0, 0, 64, 5, fill=COLORS["border"], outline="")
        bar.create_rectangle(0, 0, int(64 * project.progress / 100), 5, fill=COLORS["accent"], outline="")
        avatar = tk.Canvas(frame, width=26, height=26, bg=bg, highlightthickness=0)
        avatar.grid(row=0, column=6, padx=(2, 12))
        avatar.create_oval(1, 1, 25, 25, fill="#343953", outline="#525b82")
        avatar.create_text(13, 13, text=project.owner, fill="#d8dbff", font=(FONT, 7, "bold"))
        for widget in (frame, name_box, *name_box.winfo_children()):
            widget.bind("<Button-1>", lambda _e, key=project.code: self.select_project(key))

    def _build_inspector(self, parent: tk.Widget) -> None:
        inspector = tk.Frame(parent, bg=COLORS["sidebar"], width=420,
                             highlightthickness=1, highlightbackground=COLORS["border"])
        inspector.grid(row=0, column=1, sticky="nsew")
        inspector.grid_propagate(False)
        self.inspector = inspector
        self._render_inspector()

    def _render_inspector(self) -> None:
        if not hasattr(self, "inspector"):
            return
        for child in self.inspector.winfo_children():
            child.destroy()
        project = next((p for p in self.projects if p.code == self.selected_code), None)
        if project is None:
            return
        top = tk.Frame(self.inspector, bg=COLORS["sidebar"])
        top.pack(fill="x", padx=22, pady=(22, 19))
        mark = tk.Canvas(top, width=39, height=39, bg=COLORS["sidebar"], highlightthickness=0)
        mark.pack(anchor="w", pady=(0, 14))
        mark.create_rectangle(1, 1, 38, 38, fill=COLORS["accent_soft"], outline=COLORS["accent"])
        mark.create_text(19, 19, text="MP", fill="#d7d9ff", font=(FONT, 9, "bold"))
        self.label(top, project.name, bg=COLORS["sidebar"], size=11, weight="bold", wraplength=370, justify="left").pack(anchor="w")
        self.label(top, f"{project.code}  ·  {project.location}", bg=COLORS["sidebar"], fg=COLORS["muted"], size=8).pack(anchor="w", pady=(6, 0))
        self._separator(self.inspector)
        props = tk.Frame(self.inspector, bg=COLORS["sidebar"])
        props.pack(fill="x", padx=22, pady=16)
        self.label(props, "PROJE ÖZELLİKLERİ", bg=COLORS["sidebar"], fg=COLORS["subtle"], size=7, weight="bold").pack(anchor="w", pady=(0, 14))
        self._property(props, "Durum", project.state, self._state_color(project.state))
        self._property(props, "Proje kodu", project.code)
        self._property(props, "Kapsam", project.scope)
        self._property(props, "Sorumlu", f"{project.owner}  ·  MechPro kullanıcısı")
        self._property(props, "Hedef tarih", project.due)
        self._property(props, "Son güncelleme", project.updated)
        self._separator(self.inspector)
        progress_panel = tk.Frame(self.inspector, bg=COLORS["sidebar"])
        progress_panel.pack(fill="x", padx=22, pady=17)
        heading = tk.Frame(progress_panel, bg=COLORS["sidebar"])
        heading.pack(fill="x")
        self.label(heading, "İLERLEME", bg=COLORS["sidebar"], fg=COLORS["subtle"], size=7, weight="bold").pack(side="left")
        self.label(heading, f"{project.progress}%", bg=COLORS["sidebar"], size=10, weight="bold").pack(side="right")
        self._progress_chart(progress_panel, project.progress)
        self.label(progress_panel, "Örnek ilerleme grafiği", bg=COLORS["sidebar"], fg=COLORS["subtle"], size=7).pack(anchor="w", pady=(6, 0))
        self._separator(self.inspector)
        actions = tk.Frame(self.inspector, bg=COLORS["sidebar"])
        actions.pack(fill="x", padx=22, pady=16)
        self.label(actions, "HIZLI İŞLEMLER", bg=COLORS["sidebar"], fg=COLORS["subtle"], size=7, weight="bold").pack(anchor="w", pady=(0, 11))
        self.button(actions, "↗  Projeyi aç", lambda: self.show_notice("Proje", "Proje çalışma ekranını sonraki adımda birlikte tasarlayacağız.")).pack(fill="x", pady=(0, 7))
        self.button(actions, "✎  Proje bilgilerini düzenle", lambda: self.show_notice("Düzenleme", "Proje düzenleme akışını henüz tanımlamadık.")).pack(fill="x")

    def _separator(self, parent: tk.Widget) -> None:
        tk.Frame(parent, bg=COLORS["border"], height=1).pack(fill="x")

    def _property(self, parent: tk.Widget, name: str, value: str, color: str | None = None) -> None:
        row = tk.Frame(parent, bg=COLORS["sidebar"])
        row.pack(fill="x", pady=5)
        self.label(row, name, bg=COLORS["sidebar"], fg=COLORS["muted"], size=8).pack(side="left")
        self.label(row, value, bg=COLORS["sidebar"], fg=color or COLORS["text"], size=8).pack(side="right")

    def _progress_chart(self, parent: tk.Widget, progress: int) -> None:
        chart = tk.Canvas(parent, height=118, bg=COLORS["sidebar"], highlightthickness=0)
        chart.pack(fill="x", pady=(8, 0))
        chart.update_idletasks()
        width = max(chart.winfo_width(), 250)
        for y in (20, 48, 76, 104):
            chart.create_line(0, y, width, y, fill=COLORS["border_soft"], dash=(2, 4))
        points_a = [(4, 101), (45, 90), (86, 92), (126, 65), (166, 70), (207, 38), (width - 4, 23)]
        points_b = [(4, 105), (45, 96), (86, 84), (126, 87), (166, 64), (207, 57), (width - 4, max(16, 110 - progress))]
        for points, color, dash in ((points_a, COLORS["accent"], (3, 3)), (points_b, COLORS["yellow"], ())):
            chart.create_line(*[v for point in points for v in point], fill=color, width=2, smooth=True, dash=dash)
            x, y = points[-1]
            chart.create_oval(x - 3, y - 3, x + 3, y + 3, fill=color, outline=color)
        self.label(parent, "Başlangıç                                      Güncel", bg=COLORS["sidebar"], fg=COLORS["subtle"], size=7).pack(fill="x")

    def _state_color(self, state: str) -> str:
        return {"Devam Ediyor": COLORS["yellow"], "Planlama": COLORS["purple"], "Tamamlandı": COLORS["green"]}.get(state, COLORS["muted"])

    def select_project(self, code: str) -> None:
        self.selected_code = code
        self.render_project_list()
        self.show_details(code)

    def show_details(self, code: str) -> None:
        self.selected_code = code
        self._render_inspector()

    def new_project(self) -> None:
        dialog = tk.Toplevel(self.root)
        dialog.title("Yeni proje · MechPro")
        dialog.configure(bg=COLORS["surface"])
        dialog.resizable(False, False)
        dialog.transient(self.root)
        dialog.grab_set()
        dialog.geometry("440x260")
        dialog.update_idletasks()
        x = self.root.winfo_rootx() + (self.root.winfo_width() - 440) // 2
        y = self.root.winfo_rooty() + (self.root.winfo_height() - 260) // 2
        dialog.geometry(f"440x260+{x}+{y}")
        panel = tk.Frame(dialog, bg=COLORS["surface"])
        panel.pack(fill="both", expand=True, padx=25, pady=23)
        self.label(panel, "Yeni proje oluştur", bg=COLORS["surface"], size=15, weight="bold").pack(anchor="w")
        self.label(panel, "Bu önizlemede proje bilgileri yalnızca bellekte tutulur.", bg=COLORS["surface"], fg=COLORS["muted"], size=8).pack(anchor="w", pady=(5, 17))
        self.label(panel, "PROJE ADI", bg=COLORS["surface"], fg=COLORS["subtle"], size=7, weight="bold").pack(anchor="w", pady=(0, 5))
        name_var = tk.StringVar()
        entry = tk.Entry(panel, textvariable=name_var, bg=COLORS["surface_alt"], fg=COLORS["text"],
                         insertbackground=COLORS["text"], relief="flat", font=(FONT, 10),
                         highlightthickness=1, highlightbackground=COLORS["border"],
                         highlightcolor=COLORS["accent"])
        entry.pack(fill="x", ipady=9)
        entry.focus_set()
        actions = tk.Frame(panel, bg=COLORS["surface"])
        actions.pack(fill="x", side="bottom")
        self.button(actions, "Vazgeç", dialog.destroy).pack(side="right", padx=(8, 0))

        def create() -> None:
            name = name_var.get().strip()
            if not name:
                messagebox.showwarning("Proje adı gerekli", "Lütfen bir proje adı gir.", parent=dialog)
                entry.focus_set()
                return
            number = max((int(p.code.split("-")[-1]) for p in self.projects), default=0) + 1
            project = Project(f"MP-{number:03d}", name, "Yeni çalışma", "Planlama", "Belirlenecek", "MP", 0, "Az önce", "Yeni proje")
            self.projects.insert(0, project)
            self.selected_code = project.code
            dialog.destroy()
            self.search_var.set("")
            self.navigate("Projeler")

        self.button(actions, "Projeyi oluştur", create, primary=True).pack(side="right")
        entry.bind("<Return>", lambda _e: create())

    def show_notice(self, title: str, message: str) -> None:
        messagebox.showinfo(title, message, parent=self.root)


def main() -> None:
    root = tk.Tk()
    MechProApp(root)
    root.mainloop()


if __name__ == "__main__":
    main()
