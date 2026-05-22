using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SysBot.Base;
using SysBot.Pokemon;
using SysBot.Pokemon.Localization;
using System.IO;
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
        public TextBox IPBox => _TB_IP;
        public NumericUpDown PortBox => _NUD_Port;

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

        private TextBox _TB_IP = null!;
        private NumericUpDown _NUD_Port = null!;

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
            _B_Start = new FancyButton { Text = AppLocalization.Get(LocalizationKeys.BotsStart), Location = new Point(11, 7), Size = new Size(100, 40) };
            _B_Start.GlowColor = Color.FromArgb(74, 222, 128);
            _toolTips.SetToolTip(_B_Start, AppLocalization.Get(LocalizationKeys.BotsStartTooltip));
            _toolTips.AutoPopDelay = 2500;      // How long it stays visible
            _toolTips.InitialDelay = 500;       // Delay before it shows up
            _toolTips.ReshowDelay = 1000;        // Delay between tooltips
            _toolTips.ShowAlways = true;        // Show even if the form isn’t active

            _B_Stop = new FancyButton { Text = AppLocalization.Get(LocalizationKeys.BotsStop), Location = new Point(126, 7), Size = new Size(100, 40) };
            _B_Stop.GlowColor = Color.FromArgb(248, 113, 113);
            _toolTips.SetToolTip(_B_Stop, AppLocalization.Get(LocalizationKeys.BotsStopTooltip));
            _toolTips.AutoPopDelay = 2500;      // How long it stays visible
            _toolTips.InitialDelay = 2000;       // Delay before it shows up
            _toolTips.ReshowDelay = 1000;        // Delay between tooltips
            _toolTips.ShowAlways = true;        // Show even if the form isn’t active

            _B_RebootStop = new FancyButton { Text = AppLocalization.Get(LocalizationKeys.BotsReboot), Location = new Point(241, 7), Size = new Size(100, 40) };
            _B_RebootStop.GlowColor = Color.FromArgb(192, 132, 252);
            _toolTips.SetToolTip(_B_RebootStop, AppLocalization.Get(LocalizationKeys.BotsRebootTooltip));
            _toolTips.AutoPopDelay = 2500;      // How long it stays visible
            _toolTips.InitialDelay = 2000;       // Delay before it shows up
            _toolTips.ReshowDelay = 1000;        // Delay between tooltips
            _toolTips.ShowAlways = true;        // Show even if the form isn’t active

            _updater = new FancyButton { Text = AppLocalization.Get(LocalizationKeys.BotsUpdate), Location = new Point(356, 7), Size = new Size(100, 40) };
            _toolTips.SetToolTip(_updater, AppLocalization.Get(LocalizationKeys.BotsUpdateTooltip));
            _toolTips.AutoPopDelay = 2500;      // How long it stays visible
            _toolTips.InitialDelay = 2000;       // Delay before it shows up
            _toolTips.ReshowDelay = 1000;        // Delay between tooltips
            _toolTips.ShowAlways = true;        // Show even if the form isn’t active

            // Positioned with deliberate spacing so it doesn't overlap the routine combo or the game-mode combo.
            // Routine combo: x=301, w=130 → ends at 431. Game-mode combo: x=487.
            _B_New = new FancyButton { Text = "+", Location = new Point(446, 56), Size = new Size(32, 30) };
            _B_New.GlowColor = Color.Empty;
            _B_New.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            _toolTips.SetToolTip(_B_New, AppLocalization.Get(LocalizationKeys.BotsNewTooltip));
            _toolTips.AutoPopDelay = 2500;      // How long it stays visible
            _toolTips.InitialDelay = 2000;       // Delay before it shows up
            _toolTips.ReshowDelay = 1000;        // Delay between tooltips
            _toolTips.ShowAlways = true;        // Show even if the form isn’t active

            _B_Reload = new FancyButton { Text = AppLocalization.Get(LocalizationKeys.BotsReload), Location = new Point(471, 7), Size = new Size(100, 40) };
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

            // Controls
            _TB_IP = new TextBox { Location = new Point(12, 57), Width = 120, BackColor = inputBg, ForeColor = whiteText, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9F) };
            _NUD_Port = new NumericUpDown { Location = new Point(144, 57), Width = 65, Maximum = 65535, Minimum = 0, Value = 6000, BackColor = inputBg, ForeColor = whiteText, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 9F) };

            _CB_Protocol = new FlatComboBox { Location = new Point(221, 57), Width = 70, BackColor = inputBg, ForeColor = whiteText };
            var protocols = ((SwitchProtocol[])Enum.GetValues(typeof(SwitchProtocol)))
                .Select(z => new { Text = z.ToString(), Value = (int)z }).ToArray();
            _CB_Protocol.DisplayMember = "Text";
            _CB_Protocol.ValueMember = "Value";
            _CB_Protocol.DataSource = protocols;
            _CB_Protocol.SelectedValue = (int)SwitchProtocol.WiFi;

            _CB_Routine = new FlatComboBox { Location = new Point(301, 57), Width = 130, BackColor = inputBg, ForeColor = whiteText };
            var routines = ((PokeRoutineType[])Enum.GetValues(typeof(PokeRoutineType)))
                .Select(z => new { Text = z.ToString(), Value = (int)z }).ToArray();
            _CB_Routine.DisplayMember = "Text";
            _CB_Routine.ValueMember = "Value";
            _CB_Routine.DataSource = routines;
            _CB_Routine.SelectedValue = (int)PokeRoutineType.FlexTrade;

            _CB_GameMode = new FlatComboBox { Location = new Point(490, 57), Size = new Size(96, 28), BackColor = inputBg, ForeColor = whiteText };
            _CB_GameMode.Items.AddRange(new object[] { "SWSH", "BDSP", "PLA", "SV", "LGPE", "PLZA" });
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

            var selectedMode = _CB_GameMode.SelectedItem?.ToString();
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

                int index = _CB_GameMode.Items.IndexOf(modeText);
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

            if (isUpdateAvailable && !string.IsNullOrWhiteSpace(newVersion))
            {
                _availableUpdateVersion = newVersion;
                _updateVersionLabel.Text = AppLocalization.Format(LocalizationKeys.BotsUpdateNowTo, newVersion);
                _updateVersionLabel.Visible = true;
                _updateVersionLabel.BringToFront();
                _updateNotificationImage.Visible = true;
                _updateNotificationImage.BringToFront();
            }
            else
            {
                _availableUpdateVersion = string.Empty;
                _updateVersionLabel.Visible = false;
                _updateNotificationImage.Visible = false;
            }
        }
    }
}
