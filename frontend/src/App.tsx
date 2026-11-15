import { useEffect, useMemo, useState, type FormEvent } from "react";
import {
  Activity, Archive, ArrowLeft, ArrowUpRight, Bell, Bookmark,
  Calculator, ChevronDown, Clock3, Eye, FileText, FolderKanban,
  LayoutDashboard, Pencil, Plus, Search, Settings, Sparkles, Trash2, X,
} from "lucide-react";

type Project = {
  id: string;
  code: string;
  name: string;
  description: string;
  createdAt: string;
  updatedAt: string;
};
type ModalMode = "create" | "edit" | null;

const STORAGE_KEY = "mechpro.projects.v1";

function normalizeProjectName(value: string): string {
  return value.normalize("NFKC").trim().replace(/\s+/g, " ").toLocaleLowerCase("tr-TR");
}

function readProjects(): Project[] {
  try {
    const saved = window.localStorage.getItem(STORAGE_KEY);
    if (!saved) return [];
    const parsed: unknown = JSON.parse(saved);
    return Array.isArray(parsed) ? parsed as Project[] : [];
  } catch {
    return [];
  }
}

function formatDate(value: string): string {
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? value
    : date.toLocaleString("tr-TR", { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit" });
}

function App() {
  const [projects, setProjects] = useState<Project[]>(readProjects);
  const [activeNav, setActiveNav] = useState("Genel Bakış");
  const [selectedProjectId, setSelectedProjectId] = useState<string | null>(null);
  const [isProjectDetail, setIsProjectDetail] = useState(false);
  const [query, setQuery] = useState("");
  const [modalMode, setModalMode] = useState<ModalMode>(null);
  const [editingProjectId, setEditingProjectId] = useState<string | null>(null);
  const [projectName, setProjectName] = useState("");
  const [projectDescription, setProjectDescription] = useState("");
  const [formError, setFormError] = useState("");

  useEffect(() => {
    try {
      window.localStorage.setItem(STORAGE_KEY, JSON.stringify(projects));
    } catch {
      // The UI remains usable when browser storage is unavailable.
    }
  }, [projects]);

  const selectedProject = projects.find((project) => project.id === selectedProjectId) ?? null;
  const filteredProjects = useMemo(() => {
    const term = query.trim().toLocaleLowerCase("tr-TR");
    if (!term) return projects;
    return projects.filter((project) =>
      [project.code, project.name, project.description]
        .some((value) => value.toLocaleLowerCase("tr-TR").includes(term)),
    );
  }, [projects, query]);

  const goTo = (page: string) => {
    setActiveNav(page);
    setIsProjectDetail(false);
    setQuery("");
  };

  const openCreate = () => {
    setEditingProjectId(null);
    setProjectName("");
    setProjectDescription("");
    setFormError("");
    setModalMode("create");
  };

  const openEdit = (project: Project) => {
    setEditingProjectId(project.id);
    setProjectName(project.name);
    setProjectDescription(project.description);
    setFormError("");
    setModalMode("edit");
  };

  const saveProject = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    const cleanName = projectName.trim().replace(/\s+/g, " ");
    if (!cleanName) {
      setFormError("Proje adı boş bırakılamaz.");
      return;
    }
    const key = normalizeProjectName(cleanName);
    const duplicate = projects.find((project) =>
      normalizeProjectName(project.name) === key && project.id !== editingProjectId,
    );
    if (duplicate) {
      setFormError("Bu isimde bir proje zaten kayıtlı. Farklı bir isim kullan.");
      return;
    }

    const now = new Date().toISOString();
    if (modalMode === "edit" && editingProjectId) {
      setProjects((current) => current.map((project) =>
        project.id === editingProjectId
          ? { ...project, name: cleanName, description: projectDescription.trim(), updatedAt: now }
          : project,
      ));
      setSelectedProjectId(editingProjectId);
      setActiveNav("Projeler");
      setIsProjectDetail(true);
    } else {
      const nextNumber = Math.max(0, ...projects.map((project) => Number(project.code.replace("MP-", "")))) + 1;
      const project: Project = {
        id: crypto.randomUUID(),
        code: "MP-" + String(nextNumber).padStart(4, "0"),
        name: cleanName,
        description: projectDescription.trim(),
        createdAt: now,
        updatedAt: now,
      };
      setProjects((current) => [project, ...current]);
      setSelectedProjectId(project.id);
      setActiveNav("Projeler");
      setIsProjectDetail(false);
    }
    setModalMode(null);
  };

  const viewProject = (project: Project) => {
    setSelectedProjectId(project.id);
    setActiveNav("Projeler");
    setIsProjectDetail(true);
  };

  const deleteProject = (project: Project) => {
    const confirmed = window.confirm(
      "\"" + project.name + "\" projesini silmek istediğine emin misin?\n\nBu işlem geri alınamaz.",
    );
    if (!confirmed) return;
    setProjects((current) => current.filter((item) => item.id !== project.id));
    if (selectedProjectId === project.id) setSelectedProjectId(null);
    setIsProjectDetail(false);
    setActiveNav("Projeler");
  };

  const navGroups = [
    {
      title: "ÇALIŞMA ALANI",
      items: [
        { label: "Genel Bakış", icon: LayoutDashboard },
        { label: "Projeler", icon: FolderKanban },
        { label: "Hesaplamalar", icon: Calculator },
        { label: "Kütüphane", icon: FileText },
        { label: "Raporlar", icon: Activity },
      ],
    },
    {
      title: "GÖRÜNÜMLER",
      items: [
        { label: "Son Çalışmalar", icon: Clock3 },
        { label: "Yıldızlılar", icon: Bookmark },
        { label: "Arşiv", icon: Archive },
      ],
    },
  ];

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="window-controls"><i /><i /><i /></div>
        <div className="workspace-switcher">
          <div className="brand-mark">M</div>
          <div className="brand-copy"><strong>MechPro</strong><span>DESIGN & ENGINEERING</span></div>
          <ChevronDown size={14} className="workspace-chevron" />
        </div>

        {navGroups.map((group) => (
          <div className="nav-group" key={group.title}>
            <div className="sidebar-heading">{group.title}</div>
            <nav className="sidebar-nav">
              {group.items.map(({ label, icon: Icon }) => (
                <button key={label} className={"nav-item " + (activeNav === label ? "active" : "")}
                  onClick={() => goTo(label)}>
                  <Icon size={16} strokeWidth={1.8} /><span>{label}</span>
                </button>
              ))}
            </nav>
          </div>
        ))}

        <div className="sidebar-heading secondary-heading">ÇALIŞMA MODÜLLERİ</div>
        <div className="module-placeholder"><Sparkles size={15} /><span>Bu alan daha sonra modüllerle doldurulacak.</span></div>

        <div className="sidebar-bottom">
          <div className="offline-card">
            <span className="online-dot" />
            <div><strong>Yerel çalışma</strong><small>Veriler bu bilgisayarda saklanır</small></div>
          </div>
          <button className={"nav-item settings-item " + (activeNav === "Ayarlar" ? "active" : "")} onClick={() => goTo("Ayarlar")}>
            <Settings size={16} /><span>Ayarlar</span>
          </button>
          <div className="sidebar-version">MECHPRO · PREVIEW 0.3</div>
        </div>
      </aside>

      <main className="main-area">
        <header className="topbar">
          <div className="breadcrumb">
            <div className="mini-brand">M</div><strong>MechPro Workspace</strong><ChevronDown size={14} />
          </div>
          <div className="topbar-actions">
            <button className="topbar-button" onClick={() => window.alert("Bu önizlemede güncelleme bağlantısı etkin değil.")}>
              <Sparkles size={14} /> Güncellemeler
            </button>
            <button className="icon-button" aria-label="Bildirimler" onClick={() => window.alert("Şimdilik yeni bildirimin yok.")}>
              <Bell size={16} />
            </button>
            <span className="topbar-separator" /><div className="user-avatar">MP</div>
          </div>
        </header>

        <div className="view-toolbar">
          <div className="view-context">
            <span className="context-chip"><span className="context-symbol">⌘</span> Mekanik tasarım ve hesaplama</span>
            {activeNav === "Projeler" && isProjectDetail && (
              <button className="filter-button" onClick={() => setIsProjectDetail(false)}><ArrowLeft size={14} /> Projelere dön</button>
            )}
          </div>
          {activeNav === "Projeler" && !isProjectDetail && (
            <label className="search-box"><Search size={14} /><input value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Projelerde ara..." /></label>
          )}
          {activeNav === "Genel Bakış" && <span className="local-indicator"><i /> YEREL ÇALIŞMA ALANI</span>}
        </div>

        {activeNav === "Genel Bakış" && (
          <Overview projects={projects} onOpenProjects={() => goTo("Projeler")} onViewProject={viewProject} />
        )}
        {activeNav === "Projeler" && !isProjectDetail && (
          <ProjectsView projects={filteredProjects} total={projects.length} onCreate={openCreate}
            onView={viewProject} onEdit={openEdit} onDelete={deleteProject} />
        )}
        {activeNav === "Projeler" && isProjectDetail && selectedProject && (
          <ProjectDetail project={selectedProject} onBack={() => setIsProjectDetail(false)}
            onEdit={() => openEdit(selectedProject)} onDelete={() => deleteProject(selectedProject)} />
        )}
        {activeNav === "Projeler" && isProjectDetail && !selectedProject && (
          <div className="planned-view"><h1>Proje bulunamadı</h1><button className="secondary-button" onClick={() => setIsProjectDetail(false)}>Projelere dön</button></div>
        )}
        {activeNav !== "Genel Bakış" && activeNav !== "Projeler" && <PlaceholderView title={activeNav} />}
      </main>

      {modalMode && (
        <div className="modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setModalMode(null); }}>
          <form className="create-modal" onSubmit={saveProject}>
            <button type="button" className="modal-close" onClick={() => setModalMode(null)} aria-label="Kapat"><X size={17} /></button>
            <div className="modal-icon">{modalMode === "create" ? <Plus size={18} /> : <Pencil size={17} />}</div>
            <h2>{modalMode === "create" ? "Yeni proje oluştur" : "Projeyi düzenle"}</h2>
            <p>Proje adı benzersiz olmalıdır. Proje verileri bu bilgisayarda saklanır.</p>
            <label htmlFor="project-name">PROJE ADI *</label>
            <input id="project-name" autoFocus value={projectName}
              onChange={(event) => { setProjectName(event.target.value); setFormError(""); }}
              placeholder="Örn. Merkez Ofis Mekanik Tesisatı" />
            <label htmlFor="project-description" className="description-label">AÇIKLAMA</label>
            <textarea id="project-description" value={projectDescription}
              onChange={(event) => setProjectDescription(event.target.value)}
              placeholder="Proje hakkında kısa açıklama..." rows={3} />
            {formError && <div className="form-error">{formError}</div>}
            <div className="modal-actions">
              <button type="button" className="cancel-button" onClick={() => setModalMode(null)}>Vazgeç</button>
              <button type="submit" className="primary-button">{modalMode === "create" ? "Projeyi oluştur" : "Değişiklikleri kaydet"}</button>
            </div>
          </form>
        </div>
      )}
    </div>
  );
}

function Overview({ projects, onOpenProjects, onViewProject }: {
  projects: Project[];
  onOpenProjects: () => void;
  onViewProject: (project: Project) => void;
}) {
  const recent = [...projects].sort((a, b) => b.updatedAt.localeCompare(a.updatedAt)).slice(0, 4);
  return (
    <div className="page-content overview-page">
      <div className="eyebrow">MEKANİK TASARIM VE HESAPLAMA</div>
      <h1>Çalışma alanına hoş geldin.</h1>
      <p className="page-subtitle">Mekanik tesisat projelerini ve mühendislik çalışmalarını tek yerde düzenle.</p>
      <div className="overview-cards">
        <article className="overview-card"><div className="card-icon"><FolderKanban size={17} /></div><span>KAYITLI PROJELER</span><strong>{projects.length}</strong><small>Yerel proje arşivindeki toplam kayıt</small></article>
        <article className="overview-card"><div className="card-icon purple"><Calculator size={17} /></div><span>ÇALIŞMA ODAĞI</span><strong>Tasarım</strong><small>Mühendislik hesapları ve teknik çalışmalar</small></article>
        <article className="overview-card"><div className="card-icon green"><Archive size={17} /></div><span>VERİ DEPOLAMA</span><strong>Yerel</strong><small>Proje verileri bu bilgisayarda saklanır</small></article>
      </div>
      <div className="section-heading">
        <div><h2>Son düzenlenen projeler</h2><p>En son düzenlediğin projelere hızlı erişim.</p></div>
        <button className="text-button" onClick={onOpenProjects}>Tüm projeler <ArrowUpRight size={14} /></button>
      </div>
      <div className="recent-projects">
        {recent.length === 0 ? (
          <div className="empty-state"><div className="empty-icon"><FolderKanban size={22} /></div><strong>Henüz kayıtlı proje yok</strong><p>İlk projen oluşturulduğunda burada görünecek. Proje eklemek için Projeler sekmesine geç.</p></div>
        ) : recent.map((project) => (
          <button className="recent-project-row" key={project.id} onClick={() => onViewProject(project)}>
            <div className="project-type-icon">MP</div>
            <div className="recent-project-name"><strong>{project.name}</strong><small>{project.code}</small></div>
            <span className="recent-updated">Düzenlendi {formatDate(project.updatedAt)}</span>
            <ArrowUpRight size={15} className="recent-arrow" />
          </button>
        ))}
      </div>
      <div className="design-focus-banner"><div className="focus-icon"><Sparkles size={18} /></div><div><strong>MechPro tasarım odaklı geliştiriliyor</strong><p>İmalat ve ilerleme takibi yerine mühendislik tasarımı ve hesaplamalarına odaklanacağız.</p></div></div>
    </div>
  );
}

function ProjectsView({ projects, total, onCreate, onView, onEdit, onDelete }: {
  projects: Project[]; total: number; onCreate: () => void;
  onView: (project: Project) => void; onEdit: (project: Project) => void;
  onDelete: (project: Project) => void;
}) {
  return (
    <div className="page-content projects-page">
      <div className="page-heading">
        <div><h1>Projeler</h1><p>Mekanik tasarım ve hesaplama çalışmalarını yönet.</p></div>
        <button className="primary-button" onClick={onCreate}><Plus size={15} /> Yeni proje</button>
      </div>
      <div className="project-table">
        <div className="project-table-head"><span>KOD</span><span>PROJE ADI</span><span>AÇIKLAMA</span><span>SON DÜZENLEME</span><span>İŞLEMLER</span></div>
        {projects.length === 0 ? (
          <div className="empty-state table-empty">
            <div className="empty-icon"><FolderKanban size={22} /></div>
            <strong>{total === 0 ? "Henüz proje bulunmuyor" : "Aramana uygun proje bulunamadı"}</strong>
            <p>{total === 0 ? "Yeni proje oluşturarak tasarım çalışmalarını burada düzenleyebilirsin." : "Arama ifadesini değiştirip tekrar deneyebilirsin."}</p>
          </div>
        ) : projects.map((project) => (
          <div className="project-table-row" key={project.id}>
            <span className="project-code">{project.code}</span>
            <button className="project-name-button" onClick={() => onView(project)}><strong>{project.name}</strong><small>Proje çalışma alanını görüntüle</small></button>
            <span className="project-description">{project.description || "—"}</span>
            <span className="project-date">{formatDate(project.updatedAt)}</span>
            <div className="project-actions">
              <button title="Görüntüle" aria-label="Görüntüle" onClick={() => onView(project)}><Eye size={14} /></button>
              <button title="Düzenle" aria-label="Düzenle" onClick={() => onEdit(project)}><Pencil size={14} /></button>
              <button title="Sil" aria-label="Sil" className="delete-action" onClick={() => onDelete(project)}><Trash2 size={14} /></button>
            </div>
          </div>
        ))}
      </div>
      <div className="table-footer"><span>{projects.length} / {total} proje gösteriliyor</span><span>Proje verileri yerel olarak saklanır</span></div>
    </div>
  );
}

function ProjectDetail({ project, onBack, onEdit, onDelete }: {
  project: Project; onBack: () => void; onEdit: () => void; onDelete: () => void;
}) {
  return (
    <div className="page-content detail-page">
      <button className="back-button" onClick={onBack}><ArrowLeft size={14} /> Projelere dön</button>
      <div className="detail-heading">
        <div className="project-type-icon large">MP</div>
        <div className="detail-title">
          <div className="eyebrow">PROJE ÇALIŞMA ALANI · {project.code}</div>
          <h1>{project.name}</h1>
          <p>Oluşturuldu {formatDate(project.createdAt)} <span>·</span> Son düzenleme {formatDate(project.updatedAt)}</p>
        </div>
        <div className="detail-actions">
          <button className="secondary-button" onClick={onEdit}><Pencil size={14} /> Düzenle</button>
          <button className="danger-button" onClick={onDelete}><Trash2 size={14} /> Sil</button>
        </div>
      </div>
      <section className="detail-description"><span>PROJE AÇIKLAMASI</span><p>{project.description || "Bu proje için henüz açıklama eklenmemiş."}</p></section>
      <section className="project-workspace-placeholder">
        <div className="placeholder-icon"><Sparkles size={20} /></div>
        <h2>Tasarım ve hesaplama çalışma alanı</h2>
        <p>Bu proje alanını ihtiyaç duyduğumuz mühendislik tasarımı ve hesaplama modülleriyle adım adım oluşturacağız.</p>
      </section>
    </div>
  );
}

function PlaceholderView({ title }: { title: string }) {
  return (
    <div className="planned-view">
      <div className="planned-view-icon"><Sparkles size={20} /></div>
      <span>ÇALIŞMA ALANI</span><h1>{title}</h1>
      <p>Bu menünün içeriğini ve kullanılacak modülleri ihtiyaçlara göre daha sonra belirleyeceğiz.</p>
    </div>
  );
}

export default App;
