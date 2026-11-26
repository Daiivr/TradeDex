using System;
using System.Windows.Forms;
using SysBot.Pokemon.Localization;
using SysBot.Pokemon.WinForms.Helpers;

namespace SysBot.Pokemon.WinForms
{
    public partial class HubForm : Form
    {
        private readonly object _hubConfig;

        public HubForm(object selectedObject)
        {
            InitializeComponent();
            Text = AppLocalization.Get(LocalizationKeys.NavHub);

            _hubConfig = selectedObject;
            PG_Hub.PropertySort = PropertySort.Categorized;
            PG_Hub.SelectedObject = _hubConfig;
            DarkScrollHelper.ApplyScrollBarsRecursive(PG_Hub);
            AppLocalization.LanguageChanged += (_, _) => ApplyLocalization();

            ApplyTheme();

            // Optional: Auto-save on close
            this.FormClosed += (_, _) =>
            {
                PG_Hub.Refresh(); // Apply changes
                Main.Instance?.SaveCurrentConfig();
            };
        }

        public void ApplyLocalization()
        {
            Text = AppLocalization.Get(LocalizationKeys.NavHub);
            LocalizedPropertyGrid.RefreshObject(_hubConfig);
            var selected = PG_Hub.SelectedGridItem;
            PG_Hub.SelectedObject = null;
            PG_Hub.SelectedObject = _hubConfig;
            if (selected != null)
                PG_Hub.Refresh();
        }

        public void ApplyTheme()
        {
            var colors = ThemeManager.CurrentColors;
            BackColor = colors.Background;
            PG_Hub.BackColor = colors.ControlBackground;
            PG_Hub.ViewBackColor = colors.ControlBackground;
            PG_Hub.ViewForeColor = colors.ForeColor;
            PG_Hub.LineColor = colors.Border;
            PG_Hub.CategoryForeColor = colors.ForeColor;
            PG_Hub.CommandsBackColor = colors.ControlBackground;
            PG_Hub.CommandsForeColor = colors.ForeColor;
            PG_Hub.HelpBackColor = colors.ControlBackground;
            PG_Hub.HelpForeColor = colors.Muted;
        }
    }
}
