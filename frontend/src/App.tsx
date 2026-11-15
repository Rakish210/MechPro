import { useMemo, useState, type FormEvent } from "react";
import {
  Activity, Archive, ArrowUpRight, Bell, Bookmark, CalendarDays, Calculator,
  ChevronDown, ChevronRight, Clock3, FileText, FolderKanban, LayoutDashboard,
  Link2, ListFilter, MoreHorizontal, Plus, Search, Settings, SlidersHorizontal,
  Sparkles, Star, X,
} from "lucide-react";

type ProjectStatus = "Devam Ediyor" | "Planlama" | "Tamamlandı";
type Project = {
  id: string; name: string; scope: string; status: ProjectStatus;
  due: string; owner: string; progress: number; updated: string; location: string;
};

const initialProjects: Project[] = [
  { id: "MP-024", name: "Merkez Ofis Mekanik Tesisatı", scope: "Mekanik · Koordinasyon", status: "Devam Ediyor", due: "19 Kas", owner: "EA", progress: 68, updated: "Bugün 10:42", location: "Ankara · Örnek proje" },
  { id: "MP-021", name: "Üretim Tesisi Yenileme", scope: "HVAC · Yangın", status: "Devam Ediyor", due: "24 Kas", owner: "MK", progress: 46, updated: "Dün 16:18", location: "Örnek proje" },
  { id: "MP-019", name: "Konut Blokları", scope: "Sıhhi Tesisat", status: "Planlama", due: "02 Ara", owner: "AY", progress: 22, updated: "12 Kas", location: "Örnek proje" },
  { id: "MP-018", name: "Veri Merkezi Altyapısı", scope: "Mekanik Sistemler", status: "Planlama", due: "08 Ara", owner: "SD", progress: 12, updated: "10 Kas", location: "Örnek proje" },
  { id: "MP-012", name: "Sağlık Yapısı Projesi", scope: "Mekanik Koordinasyon", status: "Tamamlandı", due: "Tamamlandı", owner: "EA", progress: 100, updated: "06 Kas", location: "Örnek proje" },
];
const statusOrder: ProjectStatus[] = ["Devam Ediyor", "Planlama", "Tamamlandı"];
const stateClass = (status: ProjectStatus) =>
  status === "Devam Ediyor" ? "in-progress" : status === "Planlama" ? "planning" : "done";

function App() {
  const [projects, setProjects] = useState(initialProjects);
  const [selectedId, setSelectedId] = useState("MP-024");
  const [activeNav, setActiveNav] = useState("Projeler");
  const [query, setQuery] = useState("");
  const [showCreate, setShowCreate] = useState(false);
  const [newProjectName, setNewProjectName] = useState("");
  const [showInspector, setShowInspector] = useState(true);
  const [expandedSection, setExpandedSection] = useState<string | null>(null);
  const selectedProject = projects.find((project) => project.id === selectedId) ?? projects[0];

  const filteredProjects = useMemo(() => {
    const value = query.trim().toLocaleLowerCase("tr-TR");
    if (!value) return projects;
    return projects.filter((project) =>
      [project.id, project.name, project.scope, project.owner]
        .some((field) => field.toLocaleLowerCase("tr-TR").includes(value)),
    );
  }, [projects, query]);

  const createProject = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const name = newProjectName.trim();
    if (!name) return;
    const nextNumber = Math.max(0, ...projects.map((project) => Number(project.id.slice(3)))) + 1;
    const project: Project = {
      id: "MP-" + String(nextNumber).padStart(3, "0"),
      name, scope: "Yeni çalışma", status: "Planlama", due: "Belirlenecek",
      owner: "MP", progress: 0, updated: "Az önce", location: "Yeni proje",
    };
    setProjects((current) => [project, ...current]);
    setSelectedId(project.id);
    setNewProjectName("");
    setShowCreate(false);
    setActiveNav("Projeler");
  };

  const navItems = [
    { label: "Genel Bakış", icon: LayoutDashboard },
    { label: "Projeler", icon: FolderKanban },
    { label: "Hesaplamalar", icon: Calculator },
    { label: "Kütüphane", icon: FileText },
    { label: "Raporlar", icon: Activity },
  ];

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="window-controls"><i /><i /><i /></div>
        <div className="workspace-switcher">
          <div className="brand-mark">M</div>
          <div className="brand-copy"><strong>MechPro</strong><span>ENGINEERING WORKSPACE</span></div>
          <ChevronDown size={14} className="workspace-chevron" />
        </div>
        <button className="new-project-button" onClick={() => setShowCreate(true)}><Plus size={16} /> Yeni proje</button>
        <div className="sidebar-heading">ÇALIŞMA ALANI</div>
        <nav className="sidebar-nav">
          {navItems.map(({ label, icon: Icon }) => (
            <button key={label} className={"nav-item " + (activeNav === label ? "active" : "")} onClick={() => setActiveNav(label)}>
              <Icon size={16} strokeWidth={1.8} /><span>{label}</span>
            </button>
          ))}
        </nav>
        <div className="sidebar-heading secondary-heading">GÖRÜNÜMLER</div>
        <nav className="sidebar-nav">
          <button className="nav-item" onClick={() => setActiveNav("Son Çalışmalar")}><Clock3 size={16} /><span>Son Çalışmalar</span></button>
          <button className="nav-item" onClick={() => setActiveNav("Yıldızlılar")}><Bookmark size={16} /><span>Yıldızlılar</span></button>
          <button className="nav-item" onClick={() => setActiveNav("Arşiv")}><Archive size={16} /><span>Arşiv</span></button>
        </nav>
        <div className="sidebar-heading secondary-heading">ÇALIŞMA MODÜLLERİ</div>
        <div className="module-placeholder"><div className="module-placeholder-icon"><Sparkles size={15} /></div><span>Modül yapısını birlikte şekillendireceğiz.</span></div>
        <div className="sidebar-bottom">
          <div className="offline-card"><span className="online-dot" /><div><strong>Yerel çalışma</strong><small>Çevrimdışı arayüz önizlemesi</small></div></div>
          <button className="nav-item settings-item" onClick={() => setActiveNav("Ayarlar")}><Settings size={16} /><span>Ayarlar</span></button>
          <div className="sidebar-version">MECHPRO · PREVIEW 0.2</div>
        </div>
      </aside>

      <main className="main-area">
        <header className="topbar">
          <div className="breadcrumb">
            <div className="mini-brand">M</div><strong>MechPro Workspace</strong><ChevronDown size={14} /><Star size={15} className="topbar-star" />
          </div>
          <div className="topbar-actions">
            <button className="topbar-button" onClick={() => window.alert("Bu önizlemede güncelleme bağlantısı henüz etkin değil.")}><Sparkles size={14} /> Güncellemeler</button>
            <button className="icon-button" aria-label="Bildirimler" onClick={() => window.alert("Şimdilik yeni bildirimin yok.")}><Bell size={16} /></button>
            <span className="topbar-separator" /><div className="user-avatar">MP</div>
          </div>
        </header>
        <div className="view-toolbar">
          <div className="view-context">
            <span className="context-chip"><span className="context-symbol">⌘</span> Mekanik Mühendislik</span>
            <button className="filter-button" onClick={() => setQuery(query ? "" : "MP-")}><ListFilter size={14} /> Filtre</button>
          </div>
          <div className="view-actions">
            <label className="search-box"><Search size={14} /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Projelerde ara..." /></label>
            <button className="view-toggle active" aria-label="Liste görünümü"><ListFilter size={15} /></button>
            <button className="view-toggle" aria-label="Izgara görünümü" onClick={() => window.alert("Izgara görünümünü sonraki adımda tasarlayabiliriz.")}>▦</button>
            <button className="view-menu" onClick={() => setShowInspector((value) => !value)}><SlidersHorizontal size={14} /> {showInspector ? "Paneli gizle" : "Paneli göster"}</button>
          </div>
        </div>

        {activeNav === "Projeler" || activeNav === "Genel Bakış" ? (
          <div className={"content-layout " + (showInspector ? "" : "without-inspector")}>
            <section className="project-board">
              <div className="board-heading">
                <div><h1>{activeNav}</h1><p>Mekanik mühendislik çalışmalarının genel görünümü</p></div>
                <button className="primary-button" onClick={() => setShowCreate(true)}><Plus size={15} /> Yeni proje</button>
              </div>
              <div className="table-head"><span>PROJE</span><span>KAPSAM</span><span>DURUM</span><span>HEDEF</span><span>İLERLEME</span><span>SORUMLU</span></div>
              {statusOrder.map((status) => {
                const items = filteredProjects.filter((project) => project.status === status);
                if (!items.length) return null;
                return (
                  <section className="project-group" key={status}>
                    <button className="group-heading" onClick={() => setExpandedSection(expandedSection === status ? null : status)}>
                      <span className={"group-indicator " + stateClass(status)}>{status === "Tamamlandı" ? "✓" : "◌"}</span>
                      <strong>{status}</strong><small>{items.length}</small><span className="group-spacer" /><Plus size={15} />
                    </button>
                    {expandedSection !== status && items.map((project) => (
                      <button key={project.id} className={"project-row " + (selectedId === project.id ? "selected" : "")} onClick={() => setSelectedId(project.id)}>
                        <span className="project-identity"><small>{project.id}</small><span><strong>{project.name}</strong><em>{project.updated}</em></span></span>
                        <span className="scope-cell">{project.scope}</span>
                        <span><em className={"status-pill " + stateClass(project.status)}><i />{project.status}</em></span>
                        <span className="date-cell"><CalendarDays size={13} />{project.due}</span>
                        <span className="progress-cell"><b>{project.progress}%</b><i><span style={{ width: project.progress + "%" }} /></i></span>
                        <span className="owner-avatar">{project.owner}</span>
                      </button>
                    ))}
                  </section>
                );
              })}
              {!filteredProjects.length && <div className="no-results">Aramana uygun proje bulunamadı.</div>}
              <div className="board-footer"><span>{filteredProjects.length} proje gösteriliyor</span><span>Örnek veriler · Kalıcı kayıt henüz etkin değil</span></div>
            </section>

            {showInspector && selectedProject && (
              <aside className="inspector">
                <div className="inspector-project">
                  <div className="inspector-mark">MP</div>
                  <h2>{selectedProject.name}</h2>
                  <p>{selectedProject.id} <span>·</span> {selectedProject.location}</p>
                </div>
                <div className="inspector-section">
                  <h3>PROJE ÖZELLİKLERİ</h3>
                  <div className="property-row"><span>Durum</span><strong className={stateClass(selectedProject.status)}>{selectedProject.status}</strong></div>
                  <div className="property-row"><span>Proje kodu</span><strong>{selectedProject.id}</strong></div>
                  <div className="property-row"><span>Kapsam</span><strong>{selectedProject.scope}</strong></div>
                  <div className="property-row"><span>Sorumlu</span><strong>{selectedProject.owner} · Kullanıcı</strong></div>
                  <div className="property-row"><span>Hedef tarih</span><strong>{selectedProject.due}</strong></div>
                  <div className="property-row"><span>Son güncelleme</span><strong>{selectedProject.updated}</strong></div>
                </div>
                <div className="inspector-section progress-section">
                  <div className="section-title-line"><h3>İLERLEME</h3><strong>{selectedProject.progress}%</strong></div>
                  <svg className="progress-chart" viewBox="0 0 300 124" preserveAspectRatio="none" role="img" aria-label="Örnek proje ilerleme grafiği">
                    {[20, 48, 76, 104].map((y) => <line key={y} x1="0" y1={y} x2="300" y2={y} className="chart-grid" />)}
                    <path d="M2 104 C42 96 55 94 87 77 S128 82 151 61 S198 58 225 36 S268 28 298 18" className="chart-line violet-line" />
                    <path d="M2 108 C35 99 55 85 86 92 S126 79 151 70 S193 65 225 58 S266 61 298 50" className="chart-line yellow-line" />
                    <circle cx="298" cy="18" r="3" className="chart-point-violet" />
                    <circle cx="298" cy="50" r="3" className="chart-point-yellow" />
                  </svg>
                  <div className="chart-labels"><span>Başlangıç</span><span>Güncel</span></div>
                  <small className="chart-caption">Örnek ilerleme grafiği</small>
                </div>
                <div className="inspector-links">
                  <button onClick={() => setExpandedSection(expandedSection === "documents" ? null : "documents")}><span><FileText size={14} /> Belgeler</span><ChevronRight size={14} /></button>
                  {expandedSection === "documents" && <p>Bu projeye henüz belge eklenmedi.</p>}
                  <button onClick={() => setExpandedSection(expandedSection === "links" ? null : "links")}><span><Link2 size={14} /> Bağlantılar</span><ChevronRight size={14} /></button>
                  {expandedSection === "links" && <p>Henüz bağlantı eklenmedi.</p>}
                </div>
                <div className="inspector-actions">
                  <button onClick={() => window.alert("Proje çalışma ekranını birlikte tasarlayacağız.")}><ArrowUpRight size={14} /> Projeyi aç</button>
                  <button onClick={() => window.alert("Proje düzenleme akışını sonraki adımda belirleyebiliriz.")}><MoreHorizontal size={14} /> Diğer işlemler</button>
                </div>
              </aside>
            )}
          </div>
        ) : (
          <div className="planned-view">
            <div className="planned-view-icon"><Sparkles size={20} /></div>
            <span>ÇALIŞMA ALANI</span><h1>{activeNav}</h1>
            <p>Bu ekranın içeriğini ve işlevlerini birlikte, adım adım belirleyeceğiz.</p>
            <small>Şimdilik herhangi bir modül veya iş akışı sabitlenmedi.</small>
          </div>
        )}
      </main>

      {showCreate && (
        <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setShowCreate(false); }}>
          <form className="create-modal" onSubmit={createProject}>
            <button type="button" className="modal-close" onClick={() => setShowCreate(false)} aria-label="Kapat"><X size={17} /></button>
            <div className="modal-icon"><Plus size={18} /></div>
            <h2>Yeni proje oluştur</h2>
            <p>Bu önizlemede proje bilgileri yalnızca bellekte tutulur.</p>
            <label htmlFor="project-name">PROJE ADI</label>
            <input id="project-name" autoFocus value={newProjectName} onChange={(event) => setNewProjectName(event.target.value)} placeholder="Örn. Merkez Ofis Mekanik Tesisatı" />
            <div className="modal-actions">
              <button type="button" className="cancel-button" onClick={() => setShowCreate(false)}>Vazgeç</button>
              <button type="submit" className="primary-button" disabled={!newProjectName.trim()}>Projeyi oluştur</button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}
export default App;
