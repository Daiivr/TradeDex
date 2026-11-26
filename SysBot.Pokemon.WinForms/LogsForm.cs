using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Collections.Generic;
using SysBot.Pokemon.Localization;
using SysBot.Pokemon.WinForms.Helpers;

namespace SysBot.Pokemon.WinForms
{
    public partial class LogsForm : Form
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public RichTextBox LogsBox { get; private set; }

        private Panel searchBoxShell = null!;
        private TextBox searchBox = null!;
        private Button nextButton = null!;
        private Button prevButton = null!;
        private Button clearButton = null!;
        private Label resultLabel = null!;
        private Label placeholderLabel = null!;
        private Panel _logsPanel = null!;
        private Panel _topPanel = null!;

        private List<int> matchIndices = new();
        private int currentMatchIndex = -1;
        private bool searchPlaceholderActive;
        private bool suppressSearchTextChanged;
        private string? resultLocalizationKey;
        private object[] resultLocalizationArgs = Array.Empty<object>();

        public LogsForm()
        {
            InitializeComponent();

            // Use FontManager for custom fonts with fallback
            Font logsFont;
            try
            {
                logsFont = FontManager.Get("Ubuntu Mono", 8);
            }
            catch
            {
                logsFont = new Font("Ubuntu Mono", 8);
            }

            var theme = ThemeManager.CurrentColors;
            LogsBox = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                Font = new Font("Consolas", 9F, FontStyle.Regular),
                BackColor = theme.Background,
                ForeColor = Color.FromArgb(200, 204, 210),
                BorderStyle = BorderStyle.None,
                WordWrap = true,
                ScrollBars = RichTextBoxScrollBars.Both,
                ContextMenuStrip = CreateContextMenu()
            };

            // Use the same font for placeholder with fallback
            Font placeholderFont;
            try
            {
                placeholderFont = FontManager.Get("Ubuntu Mono", 10, FontStyle.Italic);
            }
            catch
            {
                placeholderFont = new Font("Ubuntu Mono", 10, FontStyle.Italic);
            }

            placeholderLabel = new Label
            {
                Text = AppLocalization.Get(LocalizationKeys.LogsNothingLogged),
                ForeColor = theme.Muted,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular)
            };

            _logsPanel = new Panel { Dock = DockStyle.Fill };
            _logsPanel.Controls.Add(placeholderLabel);
            _logsPanel.Controls.Add(LogsBox);

            _topPanel = CreateSearchPanel();

            // Add the panels in correct order
            Controls.Add(_logsPanel);   // ✅ instead of LogsBox directly
            Controls.Add(_topPanel);

            LogsBox.TextChanged += LogsBox_TextChanged;
            DarkScrollHelper.Apply(LogsBox);
            ApplyLocalization();
            ApplyTheme();
        }

        public void ApplyTheme()
        {
            var colors = ThemeManager.CurrentColors;
            BackColor = colors.Background;

            if (_topPanel != null)
                _topPanel.BackColor = colors.PanelBase;
            if (_logsPanel != null)
                _logsPanel.BackColor = colors.Background;
            if (LogsBox != null)
            {
                LogsBox.BackColor = colors.Background;
                LogsBox.ForeColor = colors.ForeColor;
            }
            if (placeholderLabel != null)
                placeholderLabel.ForeColor = colors.Muted;
            if (searchBoxShell != null)
            {
                searchBoxShell.BackColor = colors.ControlBackground;
                searchBoxShell.Invalidate();
            }
            if (searchBox != null)
            {
                searchBox.BackColor = colors.ControlBackground;
                searchBox.ForeColor = colors.ForeColor;
            }
            if (resultLabel != null)
                resultLabel.ForeColor = colors.Muted;
            foreach (var button in new[] { nextButton, prevButton, clearButton })
            {
                if (button == null)
                    continue;
                button.BackColor = colors.PanelBase;
                button.ForeColor = colors.CommandButtonForeColor;
                button.FlatAppearance.BorderColor = colors.Border;
                button.FlatAppearance.MouseOverBackColor = colors.Highlight;
            }
        }

        public void ApplyLocalization()
        {
            Text = AppLocalization.Get(LocalizationKeys.NavLogs);
            placeholderLabel.Text = AppLocalization.Get(LocalizationKeys.LogsNothingLogged);
            if (nextButton != null)
                nextButton.Text = AppLocalization.Get(LocalizationKeys.LogsNext);
            if (prevButton != null)
                prevButton.Text = AppLocalization.Get(LocalizationKeys.LogsPrevious);
            if (clearButton != null)
                clearButton.Text = AppLocalization.Get(LocalizationKeys.LogsClear);
            if (searchBox != null && searchPlaceholderActive)
                ShowSearchPlaceholder();
            RefreshResultLabel();
            if (LogsBox != null)
                LogsBox.ContextMenuStrip = CreateContextMenu();
        }

        private void LogsBox_TextChanged(object? sender, EventArgs e)
        {
            placeholderLabel.Visible = string.IsNullOrEmpty(LogsBox.Text);
        }

        private Panel CreateSearchPanel()
        {
            var theme = ThemeManager.CurrentColors;
            var panel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 36,
                BackColor = theme.PanelBase
            };

            searchBoxShell = new Panel
            {
                Location = new Point(6, 5),
                Size = new Size(204, 26),
                BackColor = Color.FromArgb(34, 36, 41)
            };
            searchBoxShell.Paint += SearchBoxShell_Paint;

            searchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                BackColor = searchBoxShell.BackColor,
                ForeColor = theme.Muted,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                Location = new Point(10, 5),
                Size = new Size(184, 18),
            };
            ShowSearchPlaceholder();

            searchBox.Enter += (s, e) =>
            {
                if (searchPlaceholderActive)
                    HideSearchPlaceholder();

                searchBoxShell.Invalidate();
            };

            searchBox.Leave += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(searchBox.Text))
                {
                    ShowSearchPlaceholder();
                }

                searchBoxShell.Invalidate();
            };

            searchBox.TextChanged += SearchBox_TextChanged;

            // Button font with fallback
            Font buttonFont;
            try
            {
                buttonFont = FontManager.Get("Montserrat", 8);
            }
            catch
            {
                buttonFont = new Font("Montserrat", 8);
            }

            nextButton = new FancyButton
            {
                Text = AppLocalization.Get(LocalizationKeys.LogsNext),
                Location = new Point(216, 5),
                Size = new Size(76, 26),
                Font = buttonFont
            };
            nextButton.Click += (s, e) => PerformSearch(SearchDirection.Forward);

            prevButton = new FancyButton
            {
                Text = AppLocalization.Get(LocalizationKeys.LogsPrevious),
                Location = new Point(298, 5),
                Size = new Size(76, 26),
                Font = buttonFont
            };
            prevButton.Click += (s, e) => PerformSearch(SearchDirection.Backward);

            clearButton = new FancyButton
            {
                Text = AppLocalization.Get(LocalizationKeys.LogsClear),
                Location = new Point(380, 5),
                Size = new Size(84, 26),
                Font = buttonFont
            };
            clearButton.Click += (s, e) =>
            {
                ClearHighlights();
                matchIndices.Clear();
                currentMatchIndex = -1;
                SetResultLabel(null);
                ShowSearchPlaceholder();
            };

            resultLabel = new Label
            {
                AutoSize = true,
                Location = new Point(474, 10),
                ForeColor = theme.ForeColor,
                Font = buttonFont
            };

            searchBoxShell.Controls.Add(searchBox);
            panel.Controls.Add(searchBoxShell);
            panel.Controls.Add(nextButton);
            panel.Controls.Add(prevButton);
            panel.Controls.Add(resultLabel);
            panel.Controls.Add(clearButton);

            return panel;
        }

        private void SearchBox_TextChanged(object? sender, EventArgs e)
        {
            if (suppressSearchTextChanged || searchPlaceholderActive)
                return;

            DoSearch(searchBox.Text);
        }

        private void DoSearch(string term)
        {
            ClearHighlights();
            matchIndices.Clear();
            currentMatchIndex = -1;

            if (string.IsNullOrWhiteSpace(term))
            {
                SetResultLabel(LocalizationKeys.LogsNoSearchTerm);
                return;
            }

            var text = LogsBox.Text;
            int start = 0;
            while ((start = text.IndexOf(term, start, StringComparison.OrdinalIgnoreCase)) >= 0)
            {
                matchIndices.Add(start);
                start += term.Length;
            }

            if (matchIndices.Count == 0)
            {
                SetResultLabel(LocalizationKeys.LogsNoMatches);
                return;
            }

            foreach (int index in matchIndices)
            {
                LogsBox.Select(index, term.Length);
                LogsBox.SelectionBackColor = Color.DarkOrange;
            }

            MoveToMatch(0);
        }

        private void MoveToMatch(int direction)
        {
            if (matchIndices.Count == 0)
                return;

            if (direction == 0)
                currentMatchIndex = 0;
            else
                currentMatchIndex = (currentMatchIndex + direction + matchIndices.Count) % matchIndices.Count;

            int matchPos = matchIndices[currentMatchIndex];
            LogsBox.Select(matchPos, searchBox.Text.Length);
            LogsBox.ScrollToCaret();

            if (!searchBox.Focused)
                LogsBox.Focus();


            SetResultLabel(LocalizationKeys.LogsMatchCount, currentMatchIndex + 1, matchIndices.Count);
        }

        private void ClearHighlights()
        {
            var originalColor = ThemeManager.CurrentColors.Background;

            LogsBox.SelectAll();
            LogsBox.SelectionBackColor = originalColor;
            LogsBox.DeselectAll();
        }

        private void SearchBox_Enter(object sender, EventArgs e)
        {
            if (searchPlaceholderActive)
                HideSearchPlaceholder();
        }

        private void SearchBox_Leave(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(searchBox.Text))
                ShowSearchPlaceholder();
        }
        private enum SearchDirection
        {
            Current = 0,
            Forward = 1,
            Backward = -1
        }
        private void PerformSearch(SearchDirection direction)
        {
            string term = searchBox.Text;

            if (string.IsNullOrWhiteSpace(term) || term == AppLocalization.Get(LocalizationKeys.LogsSearchPlaceholder))
            {
                SetResultLabel(LocalizationKeys.LogsEnterSearchTerm);
                return;
            }

            // If it's the initial "Current" search, perform the scan
            if (direction == SearchDirection.Current)
            {
                DoSearch(term);
                return;
            }

            if (matchIndices.Count == 0)
            {
                SetResultLabel(LocalizationKeys.LogsNoMatches);
                return;
            }

            // Forward or Backward movement
            int move = (int)direction;
            MoveToMatch(move);
        }

        private ContextMenuStrip CreateContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add(new ToolStripMenuItem(AppLocalization.Get(LocalizationKeys.ContextCopy), null, (sender, e) => LogsBox.Copy()));
            contextMenu.Items.Add(new ToolStripMenuItem(AppLocalization.Get(LocalizationKeys.ContextClear), null, (sender, e) => LogsBox.Clear()));
            contextMenu.Items.Add(new ToolStripMenuItem(AppLocalization.Get(LocalizationKeys.ContextSelectAll), null, (sender, e) =>
            {
                LogsBox.SelectAll();
            }));

            return contextMenu;
        }

        private void SearchBoxShell_Paint(object? sender, PaintEventArgs e)
        {
            var theme = ThemeManager.CurrentColors;
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var rect = new Rectangle(0, 0, searchBoxShell.Width - 1, searchBoxShell.Height - 1);
            using var path = RoundedRect(rect, 4);
            using var fill = new SolidBrush(searchBoxShell.BackColor);
            using var border = new Pen(searchBox.Focused ? theme.Accent : Color.FromArgb(58, 62, 70), searchBox.Focused ? 1.5f : 1f);

            e.Graphics.FillPath(fill, path);
            e.Graphics.DrawPath(border, path);

            if (searchBox.Focused)
            {
                using var accent = new SolidBrush(theme.Accent);
                e.Graphics.FillRectangle(accent, 1, 1, 3, searchBoxShell.Height - 2);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;

            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();

            return path;
        }

        private void ShowSearchPlaceholder()
        {
            if (searchBox == null)
                return;

            suppressSearchTextChanged = true;
            searchPlaceholderActive = true;
            searchBox.Text = AppLocalization.Get(LocalizationKeys.LogsSearchPlaceholder);
            searchBox.ForeColor = Color.FromArgb(172, 178, 188);
            searchBox.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            suppressSearchTextChanged = false;
        }

        private void HideSearchPlaceholder()
        {
            suppressSearchTextChanged = true;
            searchPlaceholderActive = false;
            searchBox.Text = string.Empty;
            searchBox.ForeColor = ThemeManager.CurrentColors.ForeColor;
            searchBox.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            suppressSearchTextChanged = false;
        }

        private void SetResultLabel(string? localizationKey, params object[] args)
        {
            resultLocalizationKey = localizationKey;
            resultLocalizationArgs = args;
            RefreshResultLabel();
        }

        private void RefreshResultLabel()
        {
            if (resultLabel == null)
                return;

            if (string.IsNullOrEmpty(resultLocalizationKey))
            {
                resultLabel.Text = string.Empty;
                return;
            }

            resultLabel.Text = resultLocalizationArgs.Length == 0
                ? AppLocalization.Get(resultLocalizationKey)
                : AppLocalization.Format(resultLocalizationKey, resultLocalizationArgs);
        }
    }
}
