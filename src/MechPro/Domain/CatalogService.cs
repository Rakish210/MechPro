using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

namespace MechPro.Domain
{
    [XmlRoot("MechProCatalog")]
    public class MechProCatalog
    {
        [XmlAttribute("schemaVersion")]
        public int SchemaVersion { get; set; } = 1;

        [XmlArray("Materials")]
        [XmlArrayItem("Material")]
        public List<CatalogEntry> Materials { get; set; } = new List<CatalogEntry>();

        [XmlArray("DuctSystems")]
        [XmlArrayItem("System")]
        public List<CatalogEntry> Systems { get; set; } = new List<CatalogEntry>();

        [XmlArray("Fittings")]
        [XmlArrayItem("Fitting")]
        public List<CatalogEntry> Fittings { get; set; } = new List<CatalogEntry>();

        [XmlArray("Equipment")]
        [XmlArrayItem("Item")]
        public List<CatalogEntry> Equipment { get; set; } = new List<CatalogEntry>();
    }

    public class CatalogEntry
    {
        [XmlAttribute("id")]
        public string Id { get; set; }

        [XmlAttribute("name")]
        public string Name { get; set; }

        [XmlAttribute("kind")]
        public string Kind { get; set; }

        [XmlAttribute("layer")]
        public string Layer { get; set; }

        [XmlAttribute("color")]
        public string Color { get; set; }

        [XmlAttribute("angle")]
        public double Angle { get; set; }

        [XmlAttribute("lengthFactor")]
        public double LengthFactor { get; set; }

        public override string ToString()
        {
            return Name ?? Id ?? string.Empty;
        }
    }

    public static class CatalogService
    {
        private const string ResourceName = "MechPro.Catalog.default-catalog.xml";
        private static readonly object SyncRoot = new object();
        private static MechProCatalog _catalog;

        public static string CatalogPath
        {
            get
            {
                string folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MechPro");
                return Path.Combine(folder, "catalog.xml");
            }
        }

        public static MechProCatalog Current
        {
            get
            {
                lock (SyncRoot)
                {
                    if (_catalog == null)
                        _catalog = LoadFromDisk();
                    return _catalog;
                }
            }
        }

        public static void Reload()
        {
            lock (SyncRoot)
                _catalog = LoadFromDisk();
        }

        public static CatalogEntry FindById(IEnumerable<CatalogEntry> entries, string id)
        {
            if (entries == null || string.IsNullOrWhiteSpace(id))
                return null;

            foreach (CatalogEntry entry in entries)
            {
                if (string.Equals(entry.Id, id, StringComparison.OrdinalIgnoreCase))
                    return entry;
            }
            return null;
        }

        public static void Save(MechProCatalog catalog)
        {
            if (catalog == null)
                throw new ArgumentNullException("catalog");

            ValidateCatalog(catalog);
            string folder = Path.GetDirectoryName(CatalogPath);
            Directory.CreateDirectory(folder);

            string temporaryPath = CatalogPath + ".tmp";
            string backupPath = CatalogPath + ".bak";
            XmlSerializer serializer = new XmlSerializer(typeof(MechProCatalog));

            using (FileStream stream = new FileStream(
                temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                serializer.Serialize(stream, catalog);
                stream.Flush(true);
            }

            if (File.Exists(CatalogPath))
                File.Replace(temporaryPath, CatalogPath, backupPath, true);
            else
                File.Move(temporaryPath, CatalogPath);

            lock (SyncRoot)
                _catalog = catalog;
        }

        private static MechProCatalog LoadFromDisk()
        {
            EnsureCatalogFile();

            XmlSerializer serializer = new XmlSerializer(typeof(MechProCatalog));
            using (FileStream stream = File.OpenRead(CatalogPath))
            {
                MechProCatalog catalog = serializer.Deserialize(stream) as MechProCatalog;
                if (catalog == null)
                    throw new InvalidOperationException("MechPro katalog dosyası okunamadı.");

                if (catalog.Materials == null) catalog.Materials = new List<CatalogEntry>();
                if (catalog.Systems == null) catalog.Systems = new List<CatalogEntry>();
                if (catalog.Fittings == null) catalog.Fittings = new List<CatalogEntry>();
                if (catalog.Equipment == null) catalog.Equipment = new List<CatalogEntry>();

                ValidateCatalog(catalog);
                return catalog;
            }
        }

        private static void EnsureCatalogFile()
        {
            if (File.Exists(CatalogPath))
                return;

            string folder = Path.GetDirectoryName(CatalogPath);
            Directory.CreateDirectory(folder);

            using (Stream resource = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream(ResourceName))
            {
                if (resource == null)
                    throw new FileNotFoundException(
                        "Gömülü varsayılan MechPro katalog dosyası bulunamadı: " + ResourceName);

                using (FileStream target = File.Create(CatalogPath))
                    resource.CopyTo(target);
            }
        }

        private static void ValidateCatalog(MechProCatalog catalog)
        {
            if (catalog.Materials.Count == 0)
                throw new InvalidDataException("Katalogda en az bir malzeme tanımı bulunmalıdır.");
            if (catalog.Systems.Count == 0)
                throw new InvalidDataException("Katalogda en az bir kanal sistemi bulunmalıdır.");

            ValidateEntries(catalog.Materials, "malzeme");
            ValidateEntries(catalog.Systems, "kanal sistemi");
            ValidateEntries(catalog.Fittings, "bağlantı");
            ValidateEntries(catalog.Equipment, "ekipman");
        }

        private static void ValidateEntries(List<CatalogEntry> entries, string category)
        {
            HashSet<string> ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (CatalogEntry entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id) ||
                    string.IsNullOrWhiteSpace(entry.Name))
                    throw new InvalidDataException("Katalogda geçersiz " + category + " kaydı var.");

                if (!ids.Add(entry.Id))
                    throw new InvalidDataException("Katalogda yinelenen kimlik: " + entry.Id);
            }
        }
    }
}