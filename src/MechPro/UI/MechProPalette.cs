using Autodesk.AutoCAD.ApplicationServices;
using AcApplication = Autodesk.AutoCAD.ApplicationServices.Application;
using Autodesk.AutoCAD.Windows;
using MechPro.Domain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace MechPro.UI
{
    public static class MechProPalette
    {
        private static PaletteSet _palette;

        private static NumericUpDown _widthInput;
        private static NumericUpDown _heightInput;
        private static NumericUpDown _thicknessInput;
        private static NumericUpDown _reducerWidthInput;
        private static NumericUpDown _connectionToleranceInput;
        private static ComboBox _materialInput;
        private static ComboBox _systemInput;
        private static ComboBox _fittingInput;
        private static ComboBox _equipmentInput;

        public static double DuctWidth { get; private set; } = 400.0;
        public static double DuctHeight { get; private set; } = 200.0;
        public static double Thickness { get; private set; } = 0.6;
        public static double ReducerOutletWidth { get; private set; } = 250.0;
        public static double ConnectionTolerance { get; private set; } = 25.0;
        public static string MaterialId { get; private set; } = "galvanized";
        public static string SystemId { get; private set; } = "supply";
        public static string FittingId { get; private set; } = "elbow90";
        public static string EquipmentId { get; private set; } = "damper";
        public static string EditingHandle { get; private set; }

        public static void Show()
        {
            if (_palette == null)
            {
                _palette = new PaletteSet("MECHPRO | Mechanical Design",
                    new Guid("E9D68D72-9D7C-4CF1-8A7A-7CDA3C67A121"));
                _palette.Style =
                    PaletteSetStyles.ShowAutoHideButton |
                    PaletteSetStyles.ShowCloseButton |
                    PaletteSetStyles.ShowPropertiesMenu;
                _palette.DockEnabled = DockSides.Left | DockSides.Right;
                _palette.MinimumSize = new Size(320, 520);
                _palette.Size = new Size(370, 650);
                _palette.Add("Kanal", BuildDuctPanel());
                _palette.Add("Bağlantılar", BuildFittingPanel());
                _palette.Add("Ekipman", BuildEquipmentPanel());
                _palette.Add("Ağ", BuildNetworkPanel());
                RefreshCatalogLists();
            }

            _palette.Visible = true;
        }

        public static void RefreshCatalogLists()
        {
            if (_palette == null)
                return;

            MechProCatalog catalog = CatalogService.Current;
            BindCatalog(_materialInput, catalog.Materials, MaterialId);
            BindCatalog(_systemInput, catalog.Systems, SystemId);
            BindCatalog(_fittingInput, catalog.Fittings, FittingId);
            BindCatalog(_equipmentInput, catalog.Equipment, EquipmentId);
        }

        public static void LoadDuctForEdit(MechProObjectData data, string handle)
        {
            Show();
            EditingHandle = handle;

            SetNumeric(_widthInput, (decimal)data.Width);
            SetNumeric(_heightInput, (decimal)data.Height);
            SetNumeric(_thicknessInput, (decimal)data.Thickness);
            SelectCatalogEntry(_materialInput, data.MaterialId);
            SelectCatalogEntry(_systemInput, data.SystemId);
        }

        public static void ClearEditMode()
        {
            EditingHandle = null;
        }

        private static Control BuildDuctPanel()
        {
            Panel root = CreateRoot();
            AddBrand(root, "KANAL TASARIMI");
            int y = 92;

            AddSection(root, "KANAL ÖLÇÜLERİ", ref y);
            AddNumberField(root, "Genişlik (mm)", ref y, out _widthInput, 400, 1, 100000, 0, 10);
            AddNumberField(root, "Yükseklik (mm)", ref y, out _heightInput, 200, 1, 100000, 0, 10);

            AddSection(root, "SAC VE MALZEME", ref y);
            AddNumberField(root, "Sac kalınlığı (mm)", ref y, out _thicknessInput,
                0.6m, 0.1m, 20m, 1, 0.1m);
            AddComboField(root, "Malzeme", ref y, out _materialInput);
            AddComboField(root, "Kanal türü / hava sistemi", ref y, out _systemInput);

            Label hint = AddHint(root,
                "Örnek: 400x200 / 0.6. Ölçü, sac kalınlığı, malzeme ve sistem bilgisi kanala kaydedilir. Her sistem kendi layer ve rengini kullanır.",
                ref y, 64);

            Button draw = AddButton(root, "GÜZERGÂH ÇİZ", ref y, true);
            draw.Click += (s, e) =>
            {
                CaptureDuctSettings();
                EditingHandle = null;
                SendCommand("MECHPRODRAW");
            };

            Button edit = AddButton(root, "ÇİZİLİ KANALIN ÖZELLİKLERİNİ YÜKLE", ref y, false);
            edit.Click += (s, e) => SendCommand("MECHPROEDIT");

            Button apply = AddButton(root, "SEÇİLİ KANALI GÜNCELLE", ref y, true);
            apply.Click += (s, e) =>
            {
                CaptureDuctSettings();
                if (string.IsNullOrWhiteSpace(EditingHandle))
                    SendCommand("MECHPROEDIT");
                else
                    SendCommand("MECHPROAPPLY");
            };

            Button catalog = AddButton(root, "KATALOĞU AÇ / DÜZENLE", ref y, false);
            catalog.Click += (s, e) => OpenCatalog();

            Label footer = new Label
            {
                Text = "MECHPRO  |  PARAMETRİK 2D HVAC",
                ForeColor = Color.FromArgb(91, 105, 123),
                Font = new Font("Segoe UI", 7.5f),
                AutoSize = true,
                Location = new Point(18, y + 5)
            };
            root.Controls.Add(footer);
            root.Height = y + 35;
            return root;
        }

        private static Control BuildFittingPanel()
        {
            Panel root = CreateRoot();
            AddBrand(root, "BAĞLANTI ELEMANLARI");
            int y = 94;

            AddSection(root, "KATALOGDAN ELEMAN SEÇ", ref y);
            AddComboField(root, "Bağlantı türü", ref y, out _fittingInput);
            AddNumberField(root, "Redüksiyon çıkış genişliği (mm)", ref y,
                out _reducerWidthInput, 250, 1, 100000, 0, 10);

            AddHint(root,
                "90° dirsek, pantolon paçası, redüksiyon ve T bağlantı bu katalogdan seçilir. Yeni bağlantı tanımı eklemek için katalog XML dosyasını düzenleyin.",
                ref y, 72);

            Button place = AddButton(root, "BAĞLANTI YERLEŞTİR", ref y, true);
            place.Click += (s, e) =>
            {
                CaptureDuctSettings();
                FittingId = SelectedId(_fittingInput, FittingId);
                ReducerOutletWidth = (double)_reducerWidthInput.Value;
                SendCommand("MECHPROFITTING");
            };

            Button open = AddButton(root, "BAĞLANTI KATALOĞUNU DÜZENLE", ref y, false);
            open.Click += (s, e) => OpenCatalog();
            root.Height = y + 20;
            return root;
        }

        private static Control BuildEquipmentPanel()
        {
            Panel root = CreateRoot();
            AddBrand(root, "EKİPMAN VE MENFEZLER");
            int y = 94;

            AddSection(root, "EKİPMAN KATALOĞU", ref y);
            AddComboField(root, "Ekipman türü", ref y, out _equipmentInput);
            AddHint(root,
                "Damper, yangın damperi, menfez, AHU, fan, VAV, susturucu, filtre ve serpantin sembolleri. Ekipman katalogu XML üzerinden genişletilebilir.",
                ref y, 78);

            Button place = AddButton(root, "EKİPMAN YERLEŞTİR", ref y, true);
            place.Click += (s, e) =>
            {
                CaptureDuctSettings();
                EquipmentId = SelectedId(_equipmentInput, EquipmentId);
                SendCommand("MECHPROEQUIPMENT");
            };

            Button open = AddButton(root, "EKİPMAN KATALOĞUNU DÜZENLE", ref y, false);
            open.Click += (s, e) => OpenCatalog();
            root.Height = y + 20;
            return root;
        }

        private static Control BuildNetworkPanel()
        {
            Panel root = CreateRoot();
            AddBrand(root, "KANAL AĞI VE BAĞLANTILAR");
            int y = 94;

            AddSection(root, "BAĞLANTI DENETİMİ", ref y);
            AddNumberField(root, "Bağlantı toleransı (mm)", ref y,
                out _connectionToleranceInput, 25, 1, 1000, 0, 5);
            AddHint(root,
                "Kayıtlı kanal, bağlantı parçası ve ekipman portlarını tarar. Yakın portları, açık uçları ve kanal güzergâhına oturan ekipmanları raporlar.",
                ref y, 86);

            Button audit = AddButton(root, "KANAL AĞINI DENETLE", ref y, true);
            audit.Click += (s, e) =>
            {
                ConnectionTolerance = (double)_connectionToleranceInput.Value;
                SendCommand("MECHPRONETWORK");
            };

            Button connect = AddButton(root, "İKİ ELEMANI BİRBİRİNE BAĞLA", ref y, false);
            connect.Click += (s, e) =>
            {
                ConnectionTolerance = (double)_connectionToleranceInput.Value;
                SendCommand("MECHPROCONNECT");
            };

            Button reload = AddButton(root, "KATALOGLARI YENİDEN YÜKLE", ref y, false);
            reload.Click += (s, e) => SendCommand("MECHPRORELOADCATALOG");

            root.Height = y + 20;
            return root;
        }

        private static Panel CreateRoot()
        {
            return new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(16),
                AutoScroll = true
            };
        }

        private static void AddBrand(Control root, string subtitle)
        {
            Label brand = new Label
            {
                Text = "MECHPRO",
                ForeColor = Color.FromArgb(25, 42, 64),
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(16, 16)
            };
            root.Controls.Add(brand);

            Label sub = new Label
            {
                Text = subtitle,
                ForeColor = Color.FromArgb(28, 112, 178),
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(18, 54)
            };
            root.Controls.Add(sub);
        }

        private static void AddSection(Control root, string title, ref int y)
        {
            Label section = new Label
            {
                Text = title,
                ForeColor = Color.FromArgb(32, 91, 145),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(18, y)
            };
            root.Controls.Add(section);
            y += 27;
        }

        private static void AddNumberField(
            Control root, string caption, ref int y, out NumericUpDown input,
            decimal value, decimal minimum, decimal maximum, int decimalPlaces, decimal increment)
        {
            Label label = new Label
            {
                Text = caption,
                ForeColor = Color.FromArgb(42, 55, 73),
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(18, y)
            };
            root.Controls.Add(label);
            y += 22;

            input = new NumericUpDown
            {
                Minimum = minimum,
                Maximum = maximum,
                DecimalPlaces = decimalPlaces,
                Increment = increment,
                Value = value,
                Location = new Point(18, y),
                Size = new Size(290, 27),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(25, 39, 58),
                BorderStyle = BorderStyle.FixedSingle,
                ThousandsSeparator = true,
                AutoSize = false
            };
            root.Controls.Add(input);
            y += 42;
        }

        private static void AddComboField(
            Control root, string caption, ref int y, out ComboBox input)
        {
            Label label = new Label
            {
                Text = caption,
                ForeColor = Color.FromArgb(225, 230, 237),
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(18, y)
            };
            root.Controls.Add(label);
            y += 22;

            input = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(18, y),
                Size = new Size(290, 32),
                Font = new Font("Segoe UI", 9.5f),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(25, 39, 58),
                IntegralHeight = false,
                DropDownHeight = 260,
                MaxDropDownItems = 12,
                ItemHeight = 25,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            input.DrawItem += DrawCatalogItem;
            root.Controls.Add(input);
            y += 43;
        }

        private static Label AddHint(Control root, string text, ref int y, int height)
        {
            Label hint = new Label
            {
                Text = text,
                ForeColor = Color.FromArgb(83, 98, 118),
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = false,
                Size = new Size(292, height),
                Location = new Point(18, y)
            };
            root.Controls.Add(hint);
            y += height + 12;
            return hint;
        }

        private static Button AddButton(Control root, string caption, ref int y, bool primary)
        {
            Button button = new Button
            {
                Text = caption,
                Location = new Point(18, y),
                Size = new Size(290, 39),
                FlatStyle = FlatStyle.Flat,
                BackColor = primary
                    ? Color.FromArgb(36, 128, 190)
                    : Color.FromArgb(60, 68, 80),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;
            root.Controls.Add(button);
            y += 49;
            return button;
        }

        private static void BindCatalog(
            ComboBox combo, List<CatalogEntry> entries, string preferredId)
        {
            if (combo == null)
                return;

            combo.BeginUpdate();
            combo.DataSource = null;
            combo.Items.Clear();
            combo.DisplayMember = "Name";
            combo.ValueMember = "Id";
            combo.DataSource = new List<CatalogEntry>(entries ?? new List<CatalogEntry>());
            SelectCatalogEntry(combo, preferredId);
            combo.EndUpdate();
        }

        private static void SelectCatalogEntry(ComboBox combo, string id)
        {
            if (combo == null || combo.Items.Count == 0)
                return;

            for (int i = 0; i < combo.Items.Count; i++)
            {
                CatalogEntry entry = combo.Items[i] as CatalogEntry;
                if (entry != null && string.Equals(entry.Id, id,
                    StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            combo.SelectedIndex = 0;
        }

        private static string SelectedId(ComboBox combo, string fallback)
        {
            CatalogEntry entry = combo == null ? null : combo.SelectedItem as CatalogEntry;
            return entry == null ? fallback : entry.Id;
        }

        private static void CaptureDuctSettings()
        {
            if (_widthInput != null) DuctWidth = (double)_widthInput.Value;
            if (_heightInput != null) DuctHeight = (double)_heightInput.Value;
            if (_thicknessInput != null) Thickness = (double)_thicknessInput.Value;
            MaterialId = SelectedId(_materialInput, MaterialId);
            SystemId = SelectedId(_systemInput, SystemId);
        }

        private static void SetNumeric(NumericUpDown input, decimal value)
        {
            if (input == null)
                return;
            if (value < input.Minimum) value = input.Minimum;
            if (value > input.Maximum) value = input.Maximum;
            input.Value = value;
        }

        private static void OpenCatalog()
        {
            try
            {
                CatalogEditorForm.Open(null);
                CatalogService.Reload();
                RefreshCatalogLists();
            }
            catch (Exception ex)
            {
                MessageBox.Show("MechPro katalog yöneticisi açılamadı.\n" + ex.Message,
                    "MechPro", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private static void DrawCatalogItem(object sender, DrawItemEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo == null || e.Index < 0 || e.Index >= combo.Items.Count)
                return;

            CatalogEntry entry = combo.Items[e.Index] as CatalogEntry;
            string text = entry == null
                ? Convert.ToString(combo.Items[e.Index])
                : entry.Name;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color background = selected
                ? Color.FromArgb(218, 235, 250)
                : Color.White;

            using (SolidBrush brush = new SolidBrush(background))
                e.Graphics.FillRectangle(brush, e.Bounds);

            TextRenderer.DrawText(
                e.Graphics,
                text ?? string.Empty,
                combo.Font,
                Rectangle.Inflate(e.Bounds, -7, 0),
                Color.FromArgb(27, 45, 66),
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter |
                TextFormatFlags.EndEllipsis);

            e.DrawFocusRectangle();
        }

        private static void SendCommand(string command)
        {
            Document document = AcApplication.DocumentManager.MdiActiveDocument;
            if (document != null)
                document.SendStringToExecute(command + "\n", true, false, false);
        }
    }
}