using MechPro.Domain;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace MechPro.UI
{
    public sealed class CatalogEditorForm : Form
    {
        private readonly DataGridView _materials = CreateGrid();
        private readonly DataGridView _systems = CreateGrid();
        private readonly DataGridView _fittings = CreateGrid();
        private readonly DataGridView _equipment = CreateGrid();

        private CatalogEditorForm()
        {
            Text = "MechPro | Katalog yöneticisi";
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(760, 540);
            Size = new Size(900, 650);
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Segoe UI", 9f);
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimizeBox = false;
            MaximizeBox = true;

            BuildLayout();
            LoadCatalog(CatalogService.Current);
        }

        public static void Open(IWin32Window owner)
        {
            using (CatalogEditorForm form = new CatalogEditorForm())
            {
                if (owner == null)
                    form.ShowDialog();
                else
                    form.ShowDialog(owner);
            }
        }

        private void BuildLayout()
        {
            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 76,
                BackColor = Color.White,
                Padding = new Padding(18, 12, 18, 8)
            };

            Label title = new Label
            {
                Text = "KATALOG YÖNETİMİ",
                AutoSize = true,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                ForeColor = Color.FromArgb(28, 43, 64),
                Location = new Point(18, 10)
            };
            header.Controls.Add(title);

            Label subtitle = new Label
            {
                Text = "Malzeme, kanal sistemi, bağlantı ve ekipman tanımlarını kod değiştirmeden düzenleyin.",
                AutoSize = true,
                ForeColor = Color.FromArgb(92, 106, 126),
                Font = new Font("Segoe UI", 8.5f),
                Location = new Point(19, 42)
            };
            header.Controls.Add(subtitle);
            Controls.Add(header);

            TabControl tabs = new TabControl
            {
                Dock = DockStyle.Fill,
                Padding = new Point(14, 5),
                Font = new Font("Segoe UI", 9f)
            };
            tabs.TabPages.Add(CreatePage("Malzemeler", _materials));
            tabs.TabPages.Add(CreatePage("Kanal sistemleri", _systems));
            tabs.TabPages.Add(CreatePage("Bağlantılar", _fittings));
            tabs.TabPages.Add(CreatePage("Ekipmanlar", _equipment));
            Controls.Add(tabs);

            Panel footer = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 68,
                BackColor = Color.White,
                Padding = new Padding(16)
            };

            Label path = new Label
            {
                Text = "Dosya: " + CatalogService.CatalogPath,
                AutoEllipsis = true,
                ForeColor = Color.FromArgb(105, 117, 135),
                Location = new Point(18, 8),
                Size = new Size(570, 20),
                Font = new Font("Segoe UI", 8f)
            };
            footer.Controls.Add(path);

            Button cancel = new Button
            {
                Text = "KAPAT",
                DialogResult = DialogResult.Cancel,
                Size = new Size(108, 34),
                Location = new Point(footer.Width - 246, 27),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(235, 239, 245),
                ForeColor = Color.FromArgb(41, 55, 74)
            };
            cancel.FlatAppearance.BorderSize = 0;
            footer.Controls.Add(cancel);

            Button save = new Button
            {
                Text = "KAYDET",
                Size = new Size(118, 34),
                Location = new Point(footer.Width - 126, 27),
                Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(32, 112, 184),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            save.FlatAppearance.BorderSize = 0;
            save.Click += SaveCatalog;
            footer.Controls.Add(save);

            Controls.Add(footer);
            AcceptButton = save;
            CancelButton = cancel;
        }

        private static TabPage CreatePage(string title, DataGridView grid)
        {
            TabPage page = new TabPage(title)
            {
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(10)
            };

            Label note = new Label
            {
                Text = "Yeni kayıt eklemek için tablonun son satırını kullanın. Kimlik (ID) benzersiz olmalıdır.",
                Dock = DockStyle.Top,
                Height = 34,
                ForeColor = Color.FromArgb(75, 91, 112),
                TextAlign = ContentAlignment.MiddleLeft
            };
            grid.Dock = DockStyle.Fill;
            page.Controls.Add(grid);
            page.Controls.Add(note);
            return page;
        }

        private static DataGridView CreateGrid()
        {
            DataGridView grid = new DataGridView
            {
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                GridColor = Color.FromArgb(222, 228, 236),
                EnableHeadersVisualStyles = false,
                ColumnHeadersHeight = 36,
                RowTemplate = { Height = 30 },
                AllowUserToAddRows = true,
                AllowUserToDeleteRows = true,
                AllowUserToResizeRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.CellSelect,
                MultiSelect = false,
                EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2,
                RowHeadersVisible = false
            };

            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(230, 236, 244);
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(36, 52, 73);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 8.5f, FontStyle.Bold);
            grid.DefaultCellStyle.BackColor = Color.White;
            grid.DefaultCellStyle.ForeColor = Color.FromArgb(36, 48, 66);
            grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(211, 231, 249);
            grid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(25, 49, 76);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 252);

            AddColumn(grid, "ID", "id", 1.0f);
            AddColumn(grid, "Ad", "name", 1.6f);
            AddColumn(grid, "Tür / İşleyici", "kind", 1.2f);
            AddColumn(grid, "Layer", "layer", 1.15f);
            AddColumn(grid, "Renk", "color", 0.85f);
            AddColumn(grid, "Açı", "angle", 0.55f);
            AddColumn(grid, "Uzunluk katsayısı", "lengthFactor", 0.9f);
            return grid;
        }

        private static void AddColumn(
            DataGridView grid, string header, string name, float fillWeight)
        {
            grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                HeaderText = header,
                DataPropertyName = name,
                Name = name,
                FillWeight = fillWeight,
                SortMode = DataGridViewColumnSortMode.NotSortable
            });
        }

        private void LoadCatalog(MechProCatalog catalog)
        {
            LoadEntries(_materials, catalog.Materials);
            LoadEntries(_systems, catalog.Systems);
            LoadEntries(_fittings, catalog.Fittings);
            LoadEntries(_equipment, catalog.Equipment);
        }

        private static void LoadEntries(
            DataGridView grid, IList<CatalogEntry> entries)
        {
            grid.Rows.Clear();
            foreach (CatalogEntry entry in entries)
            {
                grid.Rows.Add(
                    entry.Id,
                    entry.Name,
                    entry.Kind,
                    entry.Layer,
                    entry.Color,
                    entry.Angle.ToString(CultureInfo.CurrentCulture),
                    entry.LengthFactor.ToString(CultureInfo.CurrentCulture));
            }
        }

        private void SaveCatalog(object sender, EventArgs e)
        {
            try
            {
                ValidateAndEndEdit();

                MechProCatalog catalog = new MechProCatalog
                {
                    Materials = ReadEntries(_materials),
                    Systems = ReadEntries(_systems),
                    Fittings = ReadEntries(_fittings),
                    Equipment = ReadEntries(_equipment)
                };

                CatalogService.Save(catalog);
                MessageBox.Show(
                    "Katalog başarıyla kaydedildi.\n\nYeni kayıtlar MechPro paletine yüklendi.",
                    "MechPro",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                MechProPalette.RefreshCatalogLists();
                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Katalog kaydedilemedi.\n\n" + ex.Message,
                    "MechPro katalog hatası",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ValidateAndEndEdit()
        {
            _materials.EndEdit();
            _systems.EndEdit();
            _fittings.EndEdit();
            _equipment.EndEdit();
            ValidateGrid(_materials, "Malzeme");
            ValidateGrid(_systems, "Kanal sistemi");
            ValidateGrid(_fittings, "Bağlantı");
            ValidateGrid(_equipment, "Ekipman");
        }

        private static void ValidateGrid(DataGridView grid, string category)
        {
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string id = CellText(row, 0);
                string name = CellText(row, 1);
                if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(name))
                    continue;
                if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                    throw new InvalidOperationException(
                        category + " satırında hem ID hem Ad girilmelidir.");

                ParseNumber(row, 5, category + " açı");
                ParseNumber(row, 6, category + " uzunluk katsayısı");
            }
        }

        private static List<CatalogEntry> ReadEntries(DataGridView grid)
        {
            List<CatalogEntry> entries = new List<CatalogEntry>();
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string id = CellText(row, 0);
                string name = CellText(row, 1);
                if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(name))
                    continue;

                entries.Add(new CatalogEntry
                {
                    Id = id,
                    Name = name,
                    Kind = CellText(row, 2),
                    Layer = CellText(row, 3),
                    Color = CellText(row, 4),
                    Angle = ParseNumber(row, 5, "Açı"),
                    LengthFactor = ParseNumber(row, 6, "Uzunluk katsayısı")
                });
            }
            return entries;
        }

        private static string CellText(DataGridViewRow row, int column)
        {
            object value = row.Cells[column].Value;
            return value == null ? string.Empty : Convert.ToString(value).Trim();
        }

        private static double ParseNumber(
            DataGridViewRow row, int column, string fieldName)
        {
            string raw = CellText(row, column);
            if (string.IsNullOrWhiteSpace(raw))
                return 0.0;

            double result;
            if (double.TryParse(raw, NumberStyles.Float,
                CultureInfo.CurrentCulture, out result))
                return result;
            if (double.TryParse(raw, NumberStyles.Float,
                CultureInfo.InvariantCulture, out result))
                return result;

            throw new FormatException(fieldName + " için geçersiz sayı: " + raw);
        }
    }
}