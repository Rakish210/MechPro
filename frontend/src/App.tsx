import {
  Activity, Bot, Boxes, Calculator, ClipboardList, CloudOff, Droplets,
  Flame, FolderKanban, Gauge, House, Plus, Settings2, Wind, Wrench,
} from "lucide-react";

const systems = [
  { name: "Sıhhi Tesisat", detail: "Temiz su, pis su ve yağmur suyu", icon: Droplets, color: "blue" },
  { name: "Yangın Tesisatı", detail: "Sprinkler, hidrant ve yangın dolabı", icon: Flame, color: "red" },
  { name: "Havalandırma", detail: "Hava debisi, kanal ve fan", icon: Wind, color: "cyan" },
  { name: "Isıtma ve Soğutma", detail: "Isı yükü, borulama ve ekipman", icon: Activity, color: "orange" },
  { name: "Doğalgaz", detail: "Gaz yükü, boru ve basınç kaybı", icon: Gauge, color: "amber" },
  { name: "Diğer Mekanik Sistemler", detail: "Endüstriyel ve özel tesisatlar", icon: Boxes, color: "violet" },
];

function App() {
  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand"><div className="brand-mark"><Wrench size={20} /></div><div><b>MechPro</b><small>ENGINEERING WORKSPACE</small></div></div>
        <div className="nav-label">ÇALIŞMA ALANI</div>
        <nav>
          <a className="nav-item active" href="#overview"><House size={17} /> Genel Bakış</a>
          <a className="nav-item" href="#projects"><FolderKanban size={17} /> Projeler</a>
          <a className="nav-item" href="#calculations"><Calculator size={17} /> Hesap Merkezi</a>
          <a className="nav-item" href="#quantities"><ClipboardList size={17} /> Metraj ve Keşif</a>
        </nav>
        <div className="nav-label systems-label">MEKANİK SİSTEMLER</div>
        <nav>{systems.map((item) => { const Icon = item.icon; return <a className="nav-item" href="#systems" key={item.name}><Icon size={17} />{item.name}</a>; })}</nav>
        <div className="sidebar-bottom"><div className="offline-card"><CloudOff size={16}/><div><b>Yerel çalışma</b><small>Çevrimdışı kullanıma hazır</small></div></div><a className="nav-item" href="#settings"><Settings2 size={17}/> Ayarlar</a><small className="version">MECHPRO · ÖN İZLEME 0.1.0</small></div>
      </aside>
      <main className="main">
        <header className="topbar"><span>Çalışma Alanı <i>/</i> <b>Genel Bakış</b></span><span className="user-chip"><span className="avatar">MP</span> Mühendis</span></header>
        <div className="content" id="overview">
          <section className="welcome"><div><label>MEKANİK TESİSAT MÜHENDİSLİĞİ</label><h1>Çalışma alanına hoş geldin.</h1><p>Projelerini yönet, mühendislik hesaplarını tek merkezden yürüt.</p></div><button className="primary"><Plus size={17}/> Yeni proje</button></section>
          <div className="status"><span className="dot"/> Yerel uygulama arayüzü hazır <span className="separator"/> <CloudOff size={15}/> İnternet bağlantısı gerektirmez</div>
          <section className="section" id="projects"><div className="section-head"><div><h2>Proje yönetimi</h2><p>Projelerin ve mühendislik çalışmaların için başlangıç noktası.</p></div><span className="link">Tüm projeler ↗</span></div><div className="empty"><FolderKanban size={25}/><b>Henüz proje yok</b><p>İlk mekanik tesisat projen oluşturulduğunda burada görünecek.</p><button className="secondary"><Plus size={15}/> İlk projeni oluştur</button></div></section>
          <section className="section" id="systems"><div className="section-head"><div><h2>Mekanik tesisat modülleri</h2><p>MechPro'nun kapsayacağı temel mühendislik disiplinleri.</p></div><span className="count">6 MODÜL ALANI</span></div><div className="system-grid">{systems.map((item) => { const Icon = item.icon; return <article className="system-card" key={item.name}><div className={"system-icon " + item.color}><Icon size={20}/></div><div className="system-copy"><b>{item.name}</b><p>{item.detail}</p></div><small>PLANLANDI</small></article>; })}</div></section>
          <section className="assistant"><div className="bot-icon"><Bot size={22}/></div><div><b>AI Mühendislik Asistanı</b><p>İsteğe bağlı çevrimiçi yardımcı. Temel hesaplama işlevlerinden bağımsız tasarlanacaktır.</p></div><small>İLERİ AŞAMA</small></section>
          <footer><span>MechPro · Mekanik Tesisat Mühendislik Platformu</span><span><i className="dot"/> Yerel çalışma alanı</span></footer>
        </div>
      </main>
    </div>
  );
}
export default App;
