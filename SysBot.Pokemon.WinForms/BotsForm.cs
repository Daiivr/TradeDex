using SysBot.Base;
using SysBot.Pokemon;
using SysBot.Pokemon.WinForms.Controls;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SysBot.Base;
using SysBot.Pokemon;
using SysBot.Pokemon.Localization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using SysBot.Pokemon.WinForms.Controls;
using SysBot.Pokemon.WinForms.Helpers;
using System.Diagnostics;


namespace SysBot.Pokemon.WinForms
{
    public partial class BotsForm : Form
    {
        private bool _isInitializing = false;

        public PictureBox ImageOverlay = null!;
        public FlowLayoutPanel BotPanel => _FLP_Bots;

        public Button StartButton => _B_Start;
        public Button StopButton => _B_Stop;
        public Button RebootStopButton => _B_RebootStop;
        public Button UpdateButton => _updater;
        public Button AddBotButton => _B_New;
        public TextBox IPBox => _TB_IP.Inner;
        // FlatNumericUpDown now exposes Value/Minimum/Maximum directly, so callers that
        // used to talk to a NumericUpDown can talk to the wrapper unchanged.
        public FlatNumericUpDown PortBox => _NUD_Port;

        public ComboBox ProtocolBox => _CB_Protocol;
        public ComboBox RoutineBox => _CB_Routine;

        private readonly List<BotController> BotControls = new();

        private FancyButton _B_Start = null!;
        private FancyButton _B_Stop = null!;
        private FancyButton _B_RebootStop = null!;
        private FancyButton _updater = null!;
        private FancyButton _B_New = null!;
        private FancyButton _B_Reload = null!;
        private ToolTip _toolTips = null!;

        private FlatTextBox _TB_IP = null!;
        private FlatNumericUpDown _NUD_Port = null!;

        private ComboBox _CB_Protocol = null!;
        private ComboBox _CB_Routine = null!;
        private ComboBox _CB_GameMode = null!;

        private FlowLayoutPanel _FLP_Bots = null!;
#pragma warning disable CS0169 // Field is never used
        private PictureBox? _pictureBox1;
#pragma warning restore CS0169
        private PictureBox _updateNotificationImage = null!;
        private Label _updateVersionLabel = null!;
        private string _availableUpdateVersion = string.Empty;

        public BotsForm()
        {
            AutoScaleMode = AutoScaleMode.None;
            InitializeControls();
            _isInitializing = true;
            LoadGameModeFromConfig();
            _isInitializing = false;
        }

        private void InitializeControls()
        {
            _toolTips = new ToolTip
            {
                AutoPopDelay = 1000,
                InitialDelay = 2000,
                ReshowDelay = 1000,
                ShowAlways = true
            };

            // Buttons — GlowColor is rendered as a quiet 3px left-edge stripe by the modernized FancyButton.
            _B_Start = new FancyButton { Text = AppLocalization.Get(LocalizationKeys.BotsStart), Location = new Point(11, 7), Size = new Size(108, 44) };
            _B_Start.GlowColor = Color.FromArgb(74, 222, 128);
            _toolTips.SetToolTip(_B_Start, AppLocalization.Get(LocalizationKeys.BotsStartTooltip));
            _toolTips.AutoPopDelay = 2500;      // How long it stays visible
            _toolTips.InitialDelay = 500;       // Delay before it shows up
            _toolTips.ReshowDelay = 1000;        // Delay between tooltips
            _toolTips.ShowAlways = true;        // Show even if the form isn’t active

            _B_Stop = new FancyButton { Text = AppLocalization.Get(LocalizationKeys.BotsStop), Location = new Point(126, 7), Size = new Size(108, 44) };
            _B_Stop.GlowColor = Color.FromArgb(248, 113, 113);
            _toolTips.SetToolTip(_B_Stop, AppLocalization.Get(LocalizationKeys.BotsStopTooltip));
            _toolTips.AutoPopDelay = 2500;      // How long it stays visible
            _toolTips.InitialDelay = 2000;       // Delay before it shows up
            _toolTips.ReshowDelay = 1000;        // Delay between tooltips
            _toolTips.ShowAlways = true;        // Show even if the form isn’t active

            _B_RebootStop = new FancyButton { Text = AppLocalization.Get(LocalizationKeys.BotsReboot), Location = new Point(241, 7), Size = new Size(108, 44) };
            _B_RebootStop.GlowColor = Color.FromArgb(192, 132, 252);
            _toolTips.SetToolTip(_B_RebootStop, AppLocalization.Get(LocalizationKeys.BotsRebootTooltip));
            _toolTips.AutoPopDelay = 2500;      // How long it stays visible
            _toolTips.InitialDelay = 2000;       // Delay before it shows up
            _toolTips.ReshowDelay = 1000;        // Delay between tooltips
            _toolTips.ShowAlways = true;        // Show even if the form isn’t active

            _updater = new FancyButton { Text = AppLocalization.Get(LocalizationKeys.BotsUpdate), Location = new Point(356, 7), Size = new Size(108, 44) };
            _toolTips.SetToolTip(_updater, AppLocalization.Get(LocalizationKeys.BotsUpdateTooltip));
            _toolTips.AutoPopDelay = 2500;      // How long it stays visible
            _toolTips.InitialDelay = 2000;       // Delay before it shows up
            _toolTips.ReshowDelay = 1000;        // Delay between tooltips
            _toolTips.ShowAlways = true;        // Show even if the form isn’t active

            // Positioned with 12px gaps from both the routine combo and the game-mode combo.
            // Routine combo: x=318..448. + button: x=460..492. GameMode: x=504.
            _B_New = new FancyButton { Text = "+", Location = new Point(460, 56), Size = new Size(32, 30) };
            _B_New.GlowColor = Color.Empty;
            _B_New.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            _B_New.TextOffset = new Point(0, -3);
            _toolTips.SetToolTip(_B_New, AppLocalization.Get(LocalizationKeys.BotsNewTooltip));
            _toolTips.AutoPopDelay = 2500;      // How long it stays visible
            _toolTips.InitialDelay = 2000;       // Delay before it shows up
            _toolTips.ReshowDelay = 1000;        // Delay between tooltips
            _toolTips.ShowAlways = true;        // Show even if the form isn’t active

            _B_Reload = new FancyButton { Text = AppLocalization.Get(LocalizationKeys.BotsReload), Location = new Point(471, 7), Size = new Size(108, 44) };
            _B_Reload.GlowColor = Color.FromArgb(251, 191, 36);
            _toolTips.SetToolTip(_B_Reload, AppLocalization.Get(LocalizationKeys.BotsReloadTooltip));
            _toolTips.AutoPopDelay = 2500;      // How long it stays visible
            _toolTips.InitialDelay = 2000;       // Delay before it shows up
            _toolTips.ReshowDelay = 1000;        // Delay between tooltips
            _toolTips.ShowAlways = true;        // Show even if the form isn't active

            _B_Reload.Click += (_, _) => RestartApplication();

            // Update Notification Image
            _updateNotificationImage = new PictureBox
            {
                SizeMode = PictureBoxSizeMode.AutoSize,
                Size = new Size(132, 23),
                Location = new Point(574, 59),
                BackColor = Color.Transparent,
                Visible = false,
                Cursor = Cursors.Hand
            };

            // Load the update notification image from embedded resources
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "SysBot.Pokemon.WinForms.Resources.new-release-update.png";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        _updateNotificationImage.Image = Image.FromStream(stream);
                        System.Diagnostics.Debug.WriteLine("Update notification image loaded from embedded resources");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Update notification image resource not found");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load update notification image: {ex.Message}");
            }

            _updateNotificationImage.Click += (s, e) => _updater.PerformClick();
            _toolTips.SetToolTip(_updateNotificationImage, AppLocalization.Get(LocalizationKeys.BotsUpdateAvailableTooltip));

            // Update Version Label (displays version number above the image)
            _updateVersionLabel = new Label
            {
                AutoSize = true,
                Location = new Point(576, 44),
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                Visible = false,
                Text = "",
                Cursor = Cursors.Hand
            };
            _updateVersionLabel.Click += (s, e) => _updater.PerformClick();
            _toolTips.SetToolTip(_updateVersionLabel, AppLocalization.Get(LocalizationKeys.BotsUpdateAvailableTooltip));

            // Colors for boxes and controls (pulled from the current theme).
            var theme = ThemeManager.CurrentColors;
            Color inputBg = theme.Hover;
            Color whiteText = theme.ForeColor;

            // Controls — themed FlatTextBox / FlatNumericUpDown match the FlatComboBox styling.
            // Row laid out with uniform 12px gaps and a shared 30px height so everything
            // aligns to the same baseline.
            // IP(12..142) gap NUD(154..224) gap Protocol(236..306) gap Routine(318..448) gap +(460..492) gap GameMode(504..600)
            const int rowHeight = 30;
            _TB_IP = new FlatTextBox { Location = new Point(12, 57), Size = new Size(130, rowHeight), BackColor = inputBg, ForeColor = whiteText, Text = "192.168.0.1" };
            _NUD_Port = new FlatNumericUpDown { Location = new Point(154, 57), Size = new Size(70, rowHeight), Maximum = 65535, Minimum = 0, Value = 6000, BackColor = inputBg, ForeColor = whiteText };

            _CB_Protocol = new FlatComboBox { Location = new Point(236, 57), Size = new Size(70, rowHeight), BackColor = inputBg, ForeColor = whiteText };
            var protocols = ((SwitchProtocol[])Enum.GetValues(typeof(SwitchProtocol)))
                .Select(z => new { Text = z.ToString(), Value = (int)z }).ToArray();
            _CB_Protocol.DisplayMember = "Text";
            _CB_Protocol.ValueMember = "Value";
            _CB_Protocol.DataSource = protocols;
            _CB_Protocol.SelectedValue = (int)SwitchProtocol.WiFi;

            _CB_Routine = new FlatComboBox { Location = new Point(318, 57), Size = new Size(130, rowHeight), BackColor = inputBg, ForeColor = whiteText };
            var routines = ((PokeRoutineType[])Enum.GetValues(typeof(PokeRoutineType)))
                .Select(z => new { Text = z.ToString(), Value = (int)z }).ToArray();
            _CB_Routine.DisplayMember = "Text";
            _CB_Routine.ValueMember = "Value";
            _CB_Routine.DataSource = routines;
            _CB_Routine.SelectedValue = (int)PokeRoutineType.FlexTrade;

            _CB_GameMode = new FlatComboBox { Location = new Point(504, 57), Size = new Size(96, rowHeight), BackColor = inputBg, ForeColor = whiteText };
            _CB_GameMode.Items.AddRange(new object[]
            {
                new GameModeItem("SWSH", "SWSH"),
                new GameModeItem("BDSP", "BDSP"),
                new GameModeItem("PLA", "PLA"),
                new GameModeItem("SV", "SV"),
                new GameModeItem("LGPE", "LGPE"),
                new GameModeItem("PLZA", "PLZA"),
            });
            _CB_GameMode.SelectedIndex = -1;
            _CB_GameMode.SelectedIndexChanged += CB_GameMode_SelectedIndexChanged;

            _FLP_Bots = new FlowLayoutPanel
            {
                Location = new Point(10, 89),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom,
                Size = new Size(ClientSize.Width - 18, ClientSize.Height - 100),
                AutoScroll = true,
                BorderStyle = BorderStyle.None,
                WrapContents = false,
                FlowDirection = FlowDirection.TopDown,
                BackColor = theme.Background,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            DarkScrollHelper.Apply(_FLP_Bots);

            this.BackColor = theme.Background;

            Controls.AddRange(new Control[] {
                _B_Start, _B_Stop, _B_RebootStop, _updater, _B_New,
                _B_Reload, _TB_IP, _NUD_Port, _CB_Protocol, _CB_Routine, _CB_GameMode,
                _FLP_Bots, _updateNotificationImage, _updateVersionLabel
            });

            ApplyLocalization();
            Size = new Size(722, 53);

            ApplyTheme();
        }

        private sealed record GameModeItem(string Display, string Code)
        {
            public override string ToString() => Display;
        }

        /// <summary>
        /// Recolors this form and all of its controls to the currently selected theme.
        /// Called on construction and whenever the theme changes (via <see cref="Main.RefreshChildThemes"/>).
        /// </summary>
        public void ApplyTheme()
        {
            var colors = ThemeManager.CurrentColors;

            BackColor = colors.PanelBase;
            _FLP_Bots.BackColor = colors.ListBackground;

            // Input controls
            foreach (var box in new Control[] { _TB_IP, _NUD_Port, _CB_Protocol, _CB_Routine, _CB_GameMode })
            {
                box.BackColor = colors.ControlBackground;
                box.ForeColor = colors.ForeColor;
            }

            _updateVersionLabel.ForeColor = colors.ForeColor;

            // Command buttons (Start/Stop/Reboot/Update/Reload + the "+" add button)
            // draw their own text, so recolor them explicitly per theme.
            foreach (var fb in new[] { _B_Start, _B_Stop, _B_RebootStop, _updater, _B_Reload, _B_New })
                fb.ForeColor = colors.CommandButtonForeColor;
            ApplyCommandButtonPalette();

            // Cascade to the bot controllers hosted in the list
            foreach (Control c in _FLP_Bots.Controls)
            {
                if (c is BotController controller)
                    controller.ApplyTheme();
            }
        }

        private void ApplyCommandButtonPalette()
        {
            bool cute = string.Equals(ThemeManager.CurrentThemeName, "Cute", StringComparison.OrdinalIgnoreCase);

            _B_Start.GlowColor = cute ? Color.FromArgb(252, 204, 96) : Color.FromArgb(74, 222, 128);
            _B_Stop.GlowColor = cute ? Color.FromArgb(228, 84, 132) : Color.FromArgb(248, 113, 113);
            _B_RebootStop.GlowColor = cute ? Color.FromArgb(240, 120, 156) : Color.FromArgb(192, 132, 252);
            _updater.GlowColor = cute ? Color.FromArgb(240, 132, 168) : Color.Empty;
            _B_Reload.GlowColor = cute ? Color.FromArgb(252, 204, 96) : Color.FromArgb(251, 191, 36);
            _B_New.GlowColor = cute ? Color.FromArgb(228, 84, 132) : Color.Empty;

            foreach (var fb in new[] { _B_Start, _B_Stop, _B_RebootStop, _updater, _B_Reload, _B_New })
                fb.Invalidate();
        }

        public void ApplyLocalization()
        {
            Text = AppLocalization.Get(LocalizationKeys.NavBots);
            _B_Start.Text = AppLocalization.Get(LocalizationKeys.BotsStart);
            _B_Stop.Text = AppLocalization.Get(LocalizationKeys.BotsStop);
            _B_RebootStop.Text = AppLocalization.Get(LocalizationKeys.BotsReboot);
            _updater.Text = AppLocalization.Get(LocalizationKeys.BotsUpdate);
            _B_Reload.Text = AppLocalization.Get(LocalizationKeys.BotsReload);
            if (_updateVersionLabel.Visible && !string.IsNullOrWhiteSpace(_availableUpdateVersion))
                _updateVersionLabel.Text = AppLocalization.Format(LocalizationKeys.BotsUpdateNowTo, _availableUpdateVersion);
            _toolTips.SetToolTip(_B_Start, AppLocalization.Get(LocalizationKeys.BotsStartTooltip));
            _toolTips.SetToolTip(_B_Stop, AppLocalization.Get(LocalizationKeys.BotsStopTooltip));
            _toolTips.SetToolTip(_B_RebootStop, AppLocalization.Get(LocalizationKeys.BotsRebootTooltip));
            _toolTips.SetToolTip(_updater, AppLocalization.Get(LocalizationKeys.BotsUpdateTooltip));
            _toolTips.SetToolTip(_B_New, AppLocalization.Get(LocalizationKeys.BotsNewTooltip));
            _toolTips.SetToolTip(_B_Reload, AppLocalization.Get(LocalizationKeys.BotsReloadTooltip));
            _toolTips.SetToolTip(_updateNotificationImage, AppLocalization.Get(LocalizationKeys.BotsUpdateAvailableTooltip));
            _toolTips.SetToolTip(_updateVersionLabel, AppLocalization.Get(LocalizationKeys.BotsUpdateAvailableTooltip));
        }

        private void CB_GameMode_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_isInitializing)
                return; // Don't do anything if we're still initializing

            if (_CB_GameMode.SelectedIndex == -1)
                return;

            var selectedMode = (_CB_GameMode.SelectedItem as GameModeItem)?.Code;
            ProgramMode newMode = selectedMode switch
            {
                "SWSH" => ProgramMode.SWSH,
                "BDSP" => ProgramMode.BDSP,
                "PLA" => ProgramMode.LA,
                "SV" => ProgramMode.SV,
                "LGPE" => ProgramMode.LGPE,
                "PLZA" => ProgramMode.PLZA,
                _ => ProgramMode.SWSH
            };

            try
            {
                // Use Main instance to switch mode live
                if (Main.Instance != null)
                {
                    Main.Instance.SwitchGameMode(newMode);
                }
                else
                {
                    SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(
                        AppLocalization.Get(LocalizationKeys.BotsMainFormUnavailable),
                        AppLocalization.Get(LocalizationKeys.BotsModeSwitchErrorTitle),
                        MessageBoxButtons.OK,
                        SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Error);
                }
            }
            catch (Exception ex)
            {
                SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(AppLocalization.Format(LocalizationKeys.BotsFailedSwitchMode, ex.Message), AppLocalization.Get(LocalizationKeys.DialogErrorTitle), MessageBoxButtons.OK, SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Error);
            }
        }

        private void LoadGameModeFromConfig()
        {
            try
            {
                string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName!;
                string exeDir = Path.GetDirectoryName(exePath)!;
                string configPath = Path.Combine(exeDir, "config.json");

                if (!File.Exists(configPath))
                {
                    SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(AppLocalization.Format(LocalizationKeys.BotsConfigFileNotFound, configPath));
                    return;
                }

                string json = File.ReadAllText(configPath);
                using var doc = JsonDocument.Parse(json);
                int mode = 1; // default to SWSH

                if (doc.RootElement.TryGetProperty("Mode", out var modeElement))
                    mode = modeElement.GetInt32();

                string modeText = mode switch
                {
                    1 => "SWSH",
                    2 => "BDSP",
                    3 => "PLA",
                    4 => "SV",
                    5 => "LGPE",
                    6 => "PLZA",
                    _ => "SWSH"
                };

                int index = -1;
                for (int i = 0; i < _CB_GameMode.Items.Count; i++)
                {
                    if (_CB_GameMode.Items[i] is GameModeItem gm && gm.Code == modeText)
                    {
                        index = i;
                        break;
                    }
                }
                if (index >= 0)
                    _CB_GameMode.SelectedIndex = index;
            }
            catch (Exception ex)
            {
                SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(AppLocalization.Format(LocalizationKeys.BotsFailedLoadConfig, ex.Message), icon: SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Error);
            }
        }

        private void RestartApplication()
        {
            string exePath = Application.ExecutablePath;

            try
            {
                // Start a new instance
                System.Diagnostics.Process.Start(exePath);
            }
            catch (Exception ex)
            {
                SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(AppLocalization.Format(LocalizationKeys.BotsFailedRestart, ex.Message), icon: SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Error);
                return;
            }

            // Kill current one
            Application.Exit();
        }

        public void AddNewBot(IPokeBotRunner runner, PokeBotState cfg)
        {
            if (cfg == null)
                return;

            // Create a new BotController
            var controller = new BotController
            {
                Margin = new Padding(0),
                Padding = new Padding(0),
            };

            // 👇 Grab size from the first existing controller
            if (_FLP_Bots.Controls.Count > 0 && _FLP_Bots.Controls[0] is BotController existing)
            {
                controller.Size = existing.Size;
            }
            else
            {
                // Default size if no others exist
                controller.Size = new Size(722, 53);
            }

            controller.Initialize(runner, cfg);
            controller.Remove += (_, _) => RemoveBot(controller);
            controller.Click += (_, _) => LoadBotSettingsToUI(cfg);

            // Add and finalize
            _FLP_Bots.Controls.Add(controller);
            _FLP_Bots.SetFlowBreak(controller, true);
            BotControls.Add(controller);

            _FLP_Bots.PerformLayout();
            _FLP_Bots.Update();

            var source = runner.GetBot(cfg);
            if (source?.Bot?.Connection != null)
            {
                BotControllerManager.RegisterController(source.Bot.Connection.Label, controller);
            }
            else
            {
                Debug.WriteLine("Warning: could not register controller – missing bot or connection info.");
            }
        }

        private void RemoveBot(BotController controller)
        {
            _FLP_Bots.Controls.Remove(controller);
            BotControls.Remove(controller);
        }

        public void ReadAllBotStates()
        {
            foreach (var bot in BotControls)
                bot.ReloadStatus();
        }

        private void LoadBotSettingsToUI(PokeBotState cfg)
        {
            var details = cfg.Connection;
            _TB_IP.Text = details.IP;
            _NUD_Port.Value = details.Port;
            _CB_Protocol.SelectedValue = (int)details.Protocol;
            _CB_Routine.SelectedValue = (int)cfg.InitialRoutine;
        }

        // StyleComboBox removed — FlatComboBox now handles its own painting.

        /// <summary>
        /// Shows or hides the update notification image with the specified version.
        /// </summary>
        /// <param name="isUpdateAvailable">Whether an update is available</param>
        /// <param name="newVersion">The new version string (e.g., "v7.3.9")</param>
        public void SetUpdateNotification(bool isUpdateAvailable, string newVersion = "")
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetUpdateNotification(isUpdateAvailable, newVersion)));
                return;
            }

            _availableUpdateVersion = string.Empty;
            _updateVersionLabel.Visible = false;
            _updateNotificationImage.Visible = false;
            Main.Instance?.SetUpdateNotification(isUpdateAvailable, newVersion);
        }
    }
}
