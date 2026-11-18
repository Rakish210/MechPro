using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Windows;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace MechPro.UI
{
    public static class MechProPalette
    {
        private static PaletteSet _palette;
        private static NumericUpDown _widthInput;
        private static NumericUpDown _heightInput;

        public static double DuctWidth { get; private set; } = 400.0;
        public static double DuctHeight { get; private set; } = 200.0;

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
                _palette.MinimumSize = new Size(285, 360);
                _palette.Size = new Size(330, 470);
                _palette.Add("Dikdörtgen Kanal", BuildPanel());
            }

            _palette.Visible = true;
        }

        private static Control BuildPanel()
        {
            Panel root = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(36, 40, 47),
                Padding = new Padding(16),
                AutoScroll = true
            };

            Label brand = new Label
            {
                Text = "MECHPRO",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 17, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(16, 18)
            };
            root.Controls.Add(brand);

            Label subtitle = new Label
            {
                Text = "MECHANICAL DESIGN SYSTEM",
                ForeColor = Color.FromArgb(160, 174, 192),
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                AutoSize = true,
                Location = new Point(18, 49)
            };
            root.Controls.Add(subtitle);

            Label section = new Label
            {
                Text = "KANAL PARAMETRELERİ",
                ForeColor = Color.FromArgb(91, 173, 230),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(18, 91)
            };
            root.Controls.Add(section);

            AddField(root, "Genişlik (mm)", 126, out _widthInput, 400);
            AddField(root, "Yükseklik (mm)", 207, out _heightInput, 200);

            Label note = new Label
            {
                Text = "Başlangıç ve bitiş noktalarını çizim alanından seçin.",
                ForeColor = Color.FromArgb(190, 198, 208),
                Font = new Font("Segoe UI", 8.5f),
                AutoSize = false,
                Size = new Size(260, 42),
                Location = new Point(18, 292)
            };
            root.Controls.Add(note);

            Button drawButton = new Button
            {
                Text = "KANAL OLUŞTUR",
                Location = new Point(18, 345),
                Size = new Size(260, 42),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(36, 128, 190),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            drawButton.FlatAppearance.BorderSize = 0;
            drawButton.Click += (sender, args) =>
            {
                DuctWidth = (double)_widthInput.Value;
                DuctHeight = (double)_heightInput.Value;

                Document document = Application.DocumentManager.MdiActiveDocument;
                if (document != null)
                    document.SendStringToExecute("MECHPRODRAW\n", true, false, false);
            };
            root.Controls.Add(drawButton);

            Label footer = new Label
            {
                Text = "MECHPRO  |  RECTANGULAR DUCT",
                ForeColor = Color.FromArgb(125, 137, 153),
                Font = new Font("Segoe UI", 7),
                AutoSize = true,
                Location = new Point(18, 405)
            };
            root.Controls.Add(footer);

            return root;
        }

        private static void AddField(Control parent, string caption, int top,
            out NumericUpDown input, decimal defaultValue)
        {
            Label label = new Label
            {
                Text = caption,
                ForeColor = Color.FromArgb(225, 230, 237),
                Font = new Font("Segoe UI", 9),
                AutoSize = true,
                Location = new Point(18, top)
            };
            parent.Controls.Add(label);

            input = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 100000,
                DecimalPlaces = 0,
                Increment = 10,
                Value = defaultValue,
                Location = new Point(18, top + 25),
                Size = new Size(260, 28),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(53, 59, 69),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            parent.Controls.Add(input);
        }
    }
}