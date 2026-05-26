using ControllerCommand = SysBot.Pokemon.WinForms.BotController.BotControlCommand;
using FontAwesome.Sharp;
using PKHeX.Core;
using SysBot.Base;
using SysBot.Pokemon.WinForms;
using SysBot.Pokemon.Discord;
using SysBot.Pokemon.Discord.Helpers;
using SysBot.Pokemon.Helpers;
using SysBot.Pokemon.Localization;
using SysBot.Pokemon.WinForms.Controls;
using SysBot.Pokemon.WinForms.Properties;
using SysBot.Pokemon.Z3;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using static SysBot.Pokemon.WinForms.BotController;
using WebApiCommand = SysBot.Pokemon.WinForms.WebApi.BotControlCommand;
using System.Net.Http;

namespace SysBot.Pokemon.WinForms
{
    public sealed partial class Main : Form
    {
        // Currently active child form
        private Form? activeForm = null;

        // Current running environment
        private IPokeBotRunner RunningEnvironment { get; set; } = null!;

        // Program configuration
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // Do not serialize in the designer
        public static ProgramConfig Config { get; set; } = new();

        // Static properties for update state
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)] // Do not serialize in the designer
        public static bool IsUpdating { get; set; } = false;

        // Singleton instance of Main form
        [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public static Main? Instance { get; private set; }

        // Update available flag
        internal bool hasUpdate = false;

        // Periodic background check for new GitHub releases so the
        // "NEW RELEASE" banner appears without requiring a restart.
        private System.Threading.Timer? _updateCheckTimer;
        private string _lastSeenUpdateVersion = string.Empty;
        private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(1);

        // Currently active button in the left panel, for setting Bots as default
        private IconButton currentBtn = null!;
        private Panel btnLanguage = null!;
        private Label lblLangEn = null!;
        private Label lblLangEs = null!;
        private PictureBox childFormTitleImage = null!;
        private ToolTip languageToolTip = null!;

        private Panel leftBorderBtn = null!;
        private Dictionary<IconButton, Timer> hoverTimers = new();
        private readonly Dictionary<string, Image> childFormTitleImages = new();

#pragma warning disable CS0414 // Field is assigned but never used
        private bool _isFormLoading = true;               // Flag to indicate if the form is still loading (reserved for future use)
#pragma warning restore CS0414                            // Flag to indicate if the form is still loading
        private readonly List<PokeBotState> Bots = new(); // List of bots created in the program
        private BotsForm _botsForm = null!;                       // BotsForm instance to manage bot controls
        private LogsForm _logsForm = null!;                       // LogsForm instance to display logs
        private HubForm _hubForm = null!;                       // HubForm instance to manage hub settings

        public Panel PanelLeftSide => panelLeftSide;      // Expose panelLeftSide for other forms

        // UI EFFECTS VARIABLES
        private Dictionary<IconButton, Timer> pulseTimers = new();
        private readonly Dictionary<Control, Timer> shakeTimers = new();
        private readonly Dictionary<Control, int> shakeFrames = new();
        private readonly Dictionary<Control, Point> originalLocations = new();
        private readonly Random rng = new();
        private readonly List<Sparkle> sparkles = new();
        private readonly List<Sparkle> logoSparkles = new();
        private readonly Random glitterRng = new Random();
        private Timer glitterTimer = null!;

        // Per-mode palettes for the title-bar sparkles
        private static readonly Color[] TitleBarPalette_PLZA = new[]
        {
            Color.FromArgb(255, 80, 190, 140),   // darker mint
            Color.FromArgb(255, 120, 215, 170),  // medium mint
            Color.FromArgb(255, 255, 255, 255),  // white
        };
        private static readonly Color[] TitleBarPalette_LGPE = new[]
        {
            Color.FromArgb(255, 180, 220, 255),  // light baby blue
            Color.FromArgb(255, 255, 200, 220),  // light baby pink
        };
        private static readonly Color[] TitleBarPalette_SV = new[]
        {
            Color.FromArgb(255, 190, 60, 255),   // neon purple
            Color.FromArgb(255, 215, 110, 255),  // lighter neon purple
            Color.FromArgb(255, 255, 130, 130),  // light red
        };
        private static readonly Color[] TitleBarPalette_LA = new[]
        {
            Color.FromArgb(255, 255, 255, 255),  // white
            Color.FromArgb(255, 255, 215, 110),  // gold
            Color.FromArgb(255, 255, 235, 170),  // soft gold
        };
        private static readonly Color[] TitleBarPalette_BDSP = new[]
        {
            Color.FromArgb(255, 80, 110, 170),   // matte blue
            Color.FromArgb(255, 140, 70, 110),   // matte maroon-purple
        };
        private static readonly Color[] TitleBarPalette_SWSH = new[]
        {
            Color.FromArgb(255, 40, 100, 200),   // SWSH blue
            Color.FromArgb(255, 220, 50, 60),    // SWSH red
        };

        private Color[]? GetTitleBarPalette() => Config?.Mode switch
        {
            ProgramMode.PLZA => TitleBarPalette_PLZA,
            ProgramMode.LGPE => TitleBarPalette_LGPE,
            ProgramMode.SV   => TitleBarPalette_SV,
            ProgramMode.LA   => TitleBarPalette_LA,
            ProgramMode.BDSP => TitleBarPalette_BDSP,
            ProgramMode.SWSH => TitleBarPalette_SWSH,
            _ => null, // null falls back to Sparkle's default white/yellow
        };

        ////////////////////////////////////////////////////////////
        // Initialize custom fonts for UI controls with fallbacks //
        ////////////////////////////////////////////////////////////
        private void InitializeFonts()
        {
            // Modern UI uses Segoe UI (built into Windows) instead of decorative fonts.
            // Kept as a no-op so external callers still work; designer values are authoritative.
            try
            {
                foreach (var btn in new[] { btnBots, btnHub, btnLogs })
                    btn.Font = new Font("Segoe UI", 10F, FontStyle.Regular);

                lblTitle.Font = new Font("Segoe UI", 7.75F, FontStyle.Regular);
                lblTitleChildForm.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Regular);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Font Initialization] Warning: {ex.Message}");
            }
        }

        ////////////////////////////////////////////////////////////
        /////// MAIN FORM CONSTRUCTOR INITIALIZING FOR /////////////
        /////// UI EFFECTS, EXCEPTION HANDLERS, BOT HANDLING ///////
        /////// FORMS, WEBSERVER, FONTS, THEMES, UPDATES ///////////
        ////////////////////////////////////////////////////////////

        public Main()
        {
            // GLOBAL EXCEPTION HANDLERS — LOG BEFORE BOT DIES
            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                var agg = e.Exception; // always AggregateException
                var ex = agg.InnerException ?? agg; // unwrap if possible

                // Ignore normal cancellations
                if (ex is OperationCanceledException)
                {
                    e.SetObserved();
                    return;
                }

                // Unwrap AGAIN if it's multiple nested levels (task > agg > inner)
                if (ex is AggregateException agg2 && agg2.InnerException != null)
                    ex = agg2.InnerException;

                // Ignore OperationAborted socket errors
                if (ex is SocketException se && se.SocketErrorCode == SocketError.OperationAborted)
                {
                    e.SetObserved();
                    return;
                }

                // LOG every real problem
                LogUtil.LogSafe(ex, "Unobserved Task Exception");
                e.SetObserved();
            };

            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    LogUtil.LogSafe(ex, "Unhandled Exception");
                else
                    LogUtil.LogGeneric($"Non-Exception - Unhandled Exception: {e.ExceptionObject}", "Unhandled Exception");
            };


            Task.Run(BotMonitor);      // Start the bot monitor

            try
            {
                InitializeComponent();     // Initialize all the form components before program
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Font Awesome") || ex.Message.Contains("font"))
            {
                // FontAwesome.Sharp library failed to load its embedded fonts
                // This is a critical error, but we'll log it and let Program.cs handle it
                Console.WriteLine($"[CRITICAL] FontAwesome.Sharp initialization failed: {ex.Message}");
                Console.WriteLine($"[CRITICAL] Stack trace: {ex.StackTrace}");
                throw; // Re-throw to let Program.cs catch and display user-friendly message
            }

            InitializeFonts();         // Apply custom fonts after component initialization
            pictureLogo.Image = Resources.picture_logo; // load logo from PNG resource (kept out of Main.resx to avoid IUIService build notice)
            SetupTitleBarButtonHoverEffects();
            // panelTitleBar_Paint (sparkle draw) and InitGlitter (sparkle spawn) are intentionally
            // disabled for the minimalist redesign. Dragging via panelTitleBar_MouseDown still works.

            Instance = this;
            InitializeLeftSideImage(); // Initialize the left side BG image in panelLeftSide
            InitializeUpperImage();    // Initialize the upper image in panelTitleBar
            InitializeChildFormTitleImage();

            // Force load FontAwesome font
            btnBots.IconChar = IconChar.Robot;
            btnHub.IconChar = IconChar.TableList;
            btnLogs.IconChar = IconChar.ListDots;
            btnMinimize.IconChar = IconChar.WindowMinimize;
            btnClose.IconChar = IconChar.Close;
            btnMaximize.IconChar = IconChar.WindowMaximize;
            InitializeLanguageButton();
            AppLocalization.LanguageChanged += (_, _) => ApplyLocalization();

            // Wait for the form crap to load before initializing
            this.Load += async (s, e) =>
            {
                try
                {
                    await InitializeAsync();
                }
                catch (Exception ex)
                {
                    LogUtil.LogSafe(ex, "Main InitializeAsync");
                    WinFormsUtil.Error($"Failed to initialize FusionBot UI:\n{ex.Message}");
                }
            };

            // Set up left‑panel buttons & effects
            var baseColor = ThemeManager.CurrentColors.PanelBase; // Base color for buttons according to themes
            var hoverColor = ThemeManager.CurrentColors.Hover;    // Hover color for buttons according to themes
            leftBorderBtn = new Panel { Size = new Size(3, 44), BackColor = ThemeManager.CurrentColors.Accent }; // Slim accent strip for active nav button
            panelLeftSide.Controls.Add(leftBorderBtn);            // Add left border to the panel
            panelTitleBar.MouseDown += panelTitleBar_MouseDown;   // Allow dragging the window from the title bar
            HookDrag(panelTitleBar);


            // Title‑bar controls
            this.Text = ""; // Set the form title to empty

            this.ControlBox = false;                                           // Disable the default Minimize/Maximize/Close
            this.FormBorderStyle = FormBorderStyle.None;                       // Remove the default form border
            this.DoubleBuffered = true;                                        // Enable double buffering to reduce flickering
            this.SetStyle(ControlStyles.ResizeRedraw, true);                   // Redraw on resize
            this.MaximizedBounds = Screen.FromHandle(this.Handle).WorkingArea; // Set the maximized bounds to the working area of the screen
            this.AutoScaleMode = AutoScaleMode.None;


            // Handlers for the Close/Maximize/Minimize buttons
            btnClose.Click += BtnClose_Click;       // Close button
            btnMaximize.Click += BtnMaximize_Click; // Maximize button
            btnMinimize.Click += BtnMinimize_Click; // Minimize button
        }

        // Runs once when Main form first loads
        private async Task InitializeAsync()
        {
            if (IsUpdating)
                return;

            PokeTradeBotSWSH.SeedChecker = new Z3SeedSearchHandler<PK8>(); // Initialize the seed checker for SWSH mode

            _botsForm = new BotsForm(); // Initialize the BotsForm instance

            _logsForm = new LogsForm(); // Initialize the LogsForm instance
            LogUtil.Forwarders.Add(new LogTextBoxForwarder(_logsForm.LogsBox)); // Add a log forwarder to the LogsForm's LogsBox
            _logsForm.LogsBox.MaxLength = 32767; // Set the maximum length of the LogsBox to 32767 characters (why this number though?)

            // If it knows a config file exists in root folder then load that shit up
            if (File.Exists(Program.ConfigPath))
            {
                var lines = File.ReadAllText(Program.ConfigPath);
                Config = JsonSerializer.Deserialize<ProgramConfig>(lines) ?? new ProgramConfig();
                AppLocalization.SetLanguage(Config.Language);
                LogConfig.MaxArchiveFiles = Config.Hub.MaxArchiveFiles;
                LogConfig.LoggingEnabled = Config.Hub.LoggingEnabled;

                // Clean up bad bot entries
                Config.Bots = Config.Bots
                    .Where(b => b != null && b.IsValid() && !string.IsNullOrWhiteSpace(b.Connection?.IP))
                    .GroupBy(b => $"{b.Connection.IP}:{b.Connection.Port}")
                    .Select(g => g.First())
                    .ToArray();

                // Set the current game mode for BatchCommandNormalizer
                BatchCommandNormalizer.CurrentGameMode = Config.Mode;

                RunningEnvironment = GetRunner(Config);

                foreach (var bot in Config.Bots)
                {
                    if (!Bots.Any(b => b.Connection.Equals(bot.Connection)))
                    {
                        if (string.IsNullOrWhiteSpace(bot.Connection?.IP) || bot.Connection.Port <= 0) // Check if the bot has a valid IP and port
                        {
                            Console.WriteLine(AppLocalization.LocalizeRuntimeMessage("Skipping invalid bot with empty IP or port."));
                            continue;
                        }
                        bot.Initialize();
                        AddBot(bot);
                    }
                }
            }
            else
            {
                // config.json shits
                Config = new ProgramConfig();
                AppLocalization.SetLanguage(Config.Language);

                // Set the current game mode for BatchCommandNormalizer
                BatchCommandNormalizer.CurrentGameMode = Config.Mode;

                RunningEnvironment = GetRunner(Config); // What mode is this bitch on?
                Config.Hub.Folder.CreateDefaults(Program.WorkingDirectory); // Hubbabubba
            }
            // Load other form shit and/or save valuable shit to config
            LoadControls();
            ApplyLocalization();
            Text = $"{(string.IsNullOrEmpty(Config.Hub.BotName) ? "TradeDex |" : Config.Hub.BotName)} {TradeBot.Version} | Mode: {Config.Mode}";
            UpdateBackgroundImage(Config.Mode);        // Call the method to update image in leftSidePanel
            UpdateUpperImage(Config.Mode);        // Call the method to update image in panelTitleBar
            LoadThemeOptions();

            CB_Themes.SelectedIndexChanged += CB_Themes_SelectedIndexChanged;

            // Download Fonts link removed in the minimalist redesign — fonts are no longer
            // a user-facing concern now that Segoe UI replaced the decorative families.
            // (InitializeFontsLink remains in the file for potential future re-enable.)
            LoadLogoImage(Config.Hub.BotLogoImage); // Load a URL image to replace logo
            InitUtil.InitializeStubs(Config.Mode);     // Stubby McStubbinson will set environment based on config mode
            OpenChildForm(_botsForm);
            SetupThemeAwareButtons();

            // Activate the Bots button after everything is initialized
            ActivateButton(btnBots);

            SaveCurrentConfig();

            _botsForm.StartButton.Click += B_Start_Click;           // Start button... do any of these really need explaining?
            _botsForm.StopButton.Click += B_Stop_Click;             // Stop button
            _botsForm.RebootStopButton.Click += B_RebootStop_Click; // Reboot and Stop button
            _botsForm.UpdateButton.Click += Updater_Click;          // Update button
            _botsForm.AddBotButton.Click += B_New_Click;            // Add button
            lblTitle.Text = Text; // Set the title label text to the form's text

            _ = CheckForUpdatesInBackgroundAsync();
            StartPeriodicUpdateChecks();

            this.ActiveControl = null;
            LogUtil.LogInfo("System", AppLocalization.Get(LocalizationKeys.LogBotInitializationComplete));

            // Start web server async to avoid UI blocking
            _ = Task.Run(() =>
            {
                try
                {
                    this.InitWebServer();
                }
                catch (Exception ex)
                {
                    LogUtil.LogError($"Failed to initialize web server: {ex.Message}", "System");
                }
            });
        }

        private async Task CheckForUpdatesInBackgroundAsync()
        {
            try
            {
                var (updateAvailable, _, newVersion) = await UpdateChecker.CheckForUpdatesAsync(forceShow: false, showDialog: false);
                hasUpdate = updateAvailable;

                // Avoid pushing the same notification twice on subsequent polls.
                var versionKey = newVersion ?? string.Empty;
                if (versionKey == _lastSeenUpdateVersion)
                    return;
                _lastSeenUpdateVersion = versionKey;

                if (!IsDisposed && _botsForm != null)
                {
                    BeginInvoke(() => _botsForm.SetUpdateNotification(updateAvailable, newVersion));
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogError($"Update check failed: {ex.Message}", "System");
            }
        }

        private void StartPeriodicUpdateChecks()
        {
            // Run a background poll every UpdateCheckInterval. We dedupe by
            // version inside CheckForUpdatesInBackgroundAsync so the banner
            // only animates in once per new release detected at runtime.
            _updateCheckTimer?.Dispose();
            _updateCheckTimer = new System.Threading.Timer(_ =>
            {
                if (IsDisposed) return;
                _ = CheckForUpdatesInBackgroundAsync();
            }, null, UpdateCheckInterval, UpdateCheckInterval);

            // Stop polling when the main form goes away to avoid leaking the
            // timer or firing late callbacks against a disposed UI.
            FormClosed += (_, _) =>
            {
                _updateCheckTimer?.Dispose();
                _updateCheckTimer = null;
            };
        }


        ///////////////////////////////////////////////////
        ///////// SET CURRENT RUNNING ENVIRONMENT /////////
        ///////////////////////////////////////////////////

        private static IPokeBotRunner GetRunner(ProgramConfig cfg) => cfg.Mode switch
        {
            ProgramMode.SWSH => new PokeBotRunnerImpl<PK8>(cfg.Hub, new BotFactory8SWSH()),
            ProgramMode.BDSP => new PokeBotRunnerImpl<PB8>(cfg.Hub, new BotFactory8BS()),
            ProgramMode.LA => new PokeBotRunnerImpl<PA8>(cfg.Hub, new BotFactory8LA()),
            ProgramMode.SV => new PokeBotRunnerImpl<PK9>(cfg.Hub, new BotFactory9SV()),
            ProgramMode.PLZA => new PokeBotRunnerImpl<PA9>(cfg.Hub, new BotFactory9PLZA()),
            ProgramMode.LGPE => new PokeBotRunnerImpl<PB7>(cfg.Hub, new BotFactory7LGPE()),
            _ => throw new IndexOutOfRangeException("Unsupported mode."), // A LIE
        };

        /// <summary>
        /// Switch game mode live without requiring a program reload
        /// </summary>
        /// <param name="newMode">The new ProgramMode to switch to</param>
        public void SwitchGameMode(ProgramMode newMode)
        {
            if (Config.Mode == newMode)
            {
                LogUtil.LogInfo($"Already in {newMode} mode - no change needed", "GameMode");
                return;
            }

            try
            {
                LogUtil.LogInfo($"Switching from {Config.Mode} to {newMode} mode...", "GameMode");

                // Check if any bots are currently running
                var runningBots = _botsForm.BotPanel.Controls.OfType<BotController>()
                    .Where(c => c.GetBot()?.IsRunning == true)
                    .ToList();

                if (runningBots.Any())
                {
                    var result = SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(
                        $"There are {runningBots.Count} bot(s) currently running.\n\n" +
                        "Switching game modes will stop all running bots.\n\n" +
                        "Do you want to continue?",
                        "Stop Running Bots?",
                        MessageBoxButtons.YesNo,
                        SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Warning);

                    if (result != DialogResult.Yes)
                    {
                        LogUtil.LogInfo("Game mode switch cancelled by user", "GameMode");
                        return;
                    }

                    // Stop all running bots
                    LogUtil.LogInfo("Stopping all running bots before mode switch...", "GameMode");
                    SendAll(WebApiCommand.Stop);

                    // Wait a moment for bots to stop
                    System.Threading.Thread.Sleep(500);
                }

                // Store old mode for logging
                var oldMode = Config.Mode;

                // Update the config mode
                Config.Mode = newMode;

                // Update BatchCommandNormalizer to use the new mode
                BatchCommandNormalizer.CurrentGameMode = newMode;

                // Recreate the running environment with the new mode
                RunningEnvironment = GetRunner(Config);
                LogUtil.LogInfo($"Running environment recreated for {newMode}", "GameMode");

                // Update UI elements
                if (InvokeRequired)
                {
                    Invoke((Action)(() =>
                    {
                        Text = $"{(string.IsNullOrEmpty(Config.Hub.BotName) ? "TradeDex |" : Config.Hub.BotName)} {TradeBot.Version} | Mode: {newMode}";
                        lblTitle.Text = Text;
                        UpdateBackgroundImage(newMode);
                        UpdateUpperImage(newMode);
                    }));
                }
                else
                {
                    Text = $"{(string.IsNullOrEmpty(Config.Hub.BotName) ? "TradeDex |" : Config.Hub.BotName)} {TradeBot.Version} | Mode: {newMode}";
                    lblTitle.Text = Text;
                    UpdateBackgroundImage(newMode);
                    UpdateUpperImage(newMode);
                }

                // Reinitialize sprite system for the new mode
                InitUtil.InitializeStubs(newMode);
                LogUtil.LogInfo($"Sprite system initialized for {newMode}", "GameMode");

                // Reload routine combobox with mode-specific routines
                LoadControls();
                LogUtil.LogInfo("Routine options updated for new mode", "GameMode");

                // Save the updated config to disk
                SaveCurrentConfig();
                LogUtil.LogInfo($"Config saved with new mode: {newMode}", "GameMode");

                LogUtil.LogInfo($"Successfully switched from {oldMode} to {newMode}", "GameMode");
                SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(
                    $"Game mode successfully changed to {newMode}!\n\n" +
                    "You can now start your bots and they will operate in the new mode.",
                    "Mode Switch Successful",
                    MessageBoxButtons.OK,
                    SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Success);
            }
            catch (Exception ex)
            {
                LogUtil.LogError($"Failed to switch game mode: {ex.Message}", "GameMode");
                SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(
                    $"Failed to switch game mode:\n\n{ex.Message}\n\nPlease try reloading the program.",
                    "Mode Switch Failed",
                    MessageBoxButtons.OK,
                    SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Error);
            }
        }


        //////////////////////////////////////////////////////////////////
        /////// BOT CONTROL AND COMMAND LOGIC FOR UI AND WEBSERVER ///////
        //////////////////////////////////////////////////////////////////

        private async Task BotMonitor()
        {
            while (!Disposing)
            {
                try
                {
                    if (_botsForm?.BotPanel?.Controls == null) // If the BotPanel or its controls are null, skip it for being a bitch
                        continue; // Yes, you may.

                    foreach (var bot in _botsForm.BotPanel.Controls.OfType<BotController>()) // Iterate through each BotController in the BotPanel
                        bot.ReloadStatus(); // Read the state of the bot controller to update its UI, but do it sexy
                }
                catch { /* Fail silently, iterator safety */ }

                await Task.Delay(2_000).ConfigureAwait(false); // Wait for 2 seconds before checking the bot states again, which is longer than I lasted with my wife
            }
        }

        // Load the controls for the BotsForm
        private void LoadControls()
        {
            // Establish global minimum size for the BotsForm
            MinimumSize = Size;

            // Routine Selection
            var routines = ((PokeRoutineType[])Enum.GetValues(typeof(PokeRoutineType))).Where(z => RunningEnvironment.SupportsRoutine(z)) // Get all routine types
                .Select(z => new { Text = z.ToString(), Value = (int)z }).ToList(); // Create a list of routine types with their text and value
            _botsForm.RoutineBox.DisplayMember = "Text";                            // Set the display text for the RoutineBox
            _botsForm.RoutineBox.ValueMember = "Value";                             // Set the value number for the RoutineBox (Flextrade, etc.)
            _botsForm.RoutineBox.DataSource = routines;                             // Bind the RoutineBox to the list of routine types (Dropdown list)
            _botsForm.RoutineBox.SelectedValue = (int)PokeRoutineType.FlexTrade;    // Set the default to FlexTrade in RoutineBox

            // Protocol Selection
            var protocols = ((SwitchProtocol[])Enum.GetValues(typeof(SwitchProtocol))) // Get all switch protocols
                .Select(z => new { Text = z.ToString(), Value = (int)z }).ToList();    // Create a list of protocols with their text and value
            _botsForm.ProtocolBox.DisplayMember = "Text";                              // Set the display text for the ProtocolBox
            _botsForm.ProtocolBox.ValueMember = "Value";                               // Set the value number for the ProtocolBox (WiFi/USB)
            _botsForm.ProtocolBox.DataSource = protocols;                              // Bind the ProtocolBox to the list of protocols (Dropdown list)
            _botsForm.ProtocolBox.SelectedValue = (int)SwitchProtocol.WiFi;            // Set the default to WiFi in ProtocolBox
            SaveCurrentConfig();                                                       // Save the current config for BotsForm data

            try
            {
                string? exePath = Application.ExecutablePath;
                if (!string.IsNullOrEmpty(exePath))
                {
                    string? dirPath = Path.GetDirectoryName(exePath);
                    if (!string.IsNullOrEmpty(dirPath))
                    {
                        string portInfoPath = Path.Combine(dirPath, $"TradeDex_{Environment.ProcessId}.port");
                        if (File.Exists(portInfoPath))
                            File.Delete(portInfoPath);
                    }
                }
            }
            catch { }
        }

        // Start the bot with the current config
        private void B_Start_Click(object? sender, EventArgs e) // Start all bots on Start button click
        {
            SaveCurrentConfig();                               // Save the current config before starting the bot

            LogUtil.LogInfo("Form", AppLocalization.Get(LocalizationKeys.LogStartingAllBots));   // Log the start action
            RunningEnvironment.InitializeStart();              // Initialize the bot runner
            SendAll(WebApi.BotControlCommand.Start);                  // Send the Start command to all bots present in the controller
            _logsForm.LogsBox.Select();                        // Select the logs box in the LogsForm to write to

            if (Bots.Count == 0)
                WinFormsUtil.Alert(AppLocalization.Get(LocalizationKeys.AlertNoBotsStart));
        }

        // Restart the bot and stop all consoles with current config
        private void B_RebootStop_Click(object? sender, EventArgs e) // Restart all bots and reboot the game on console
        {
            B_Stop_Click(sender, e); // Stop all bots first

            // Log the reboot and stop action
            Task.Run(async () =>
            {
                await Task.Delay(3_500).ConfigureAwait(false);             // Add 3.5 second delay before rebooting
                SaveCurrentConfig();                                       // Save the current config before rebooting
                LogUtil.LogInfo("Form", AppLocalization.Get(LocalizationKeys.LogRestartingAllConsoles)); // Log the restart bots action
                RunningEnvironment.InitializeStart();                      // Start up the bot runner again
                SendAll(WebApi.BotControlCommand.RebootAndStop);                  // Send the RebootAndStop command to all bots
                await Task.Delay(5_000).ConfigureAwait(false);             // Add a 5 second delay before restarting the bots
                SendAll(WebApi.BotControlCommand.Start);                          // Start the bot after the delay
                _logsForm.LogsBox.Select();                                // Select the logs box in the LogsForm to write to

                if (Bots.Count == 0)
                    WinFormsUtil.Alert(AppLocalization.Get(LocalizationKeys.AlertNoBotsReboot));
            });
        }

        // Sends command to all bots when them buttons be pushed
        private void SendAll(WebApiCommand cmd)
        {
            RunningEnvironment.InitializeStart();

            foreach (var c in _botsForm.BotPanel.Controls.OfType<BotController>())
            {
                var translated = TranslateCommand(cmd);
                c.SendCommand(translated);
                c.ReloadStatus();

                if (translated == BotController.BotControlCommand.Stop)
                    c.ResetProgress();
            }
        }
        private BotController.BotControlCommand TranslateCommand(WebApiCommand webCmd)
        {
            return webCmd switch
            {
                WebApiCommand.Start => BotController.BotControlCommand.Start,
                WebApiCommand.Stop => BotController.BotControlCommand.Stop,
                WebApiCommand.Idle => BotController.BotControlCommand.Idle,
                WebApiCommand.Resume => BotController.BotControlCommand.Resume,
                WebApiCommand.Restart => BotController.BotControlCommand.Restart,
                WebApiCommand.RebootAndStop => BotController.BotControlCommand.RebootAndStop,
                WebApiCommand.ScreenOnAll => BotController.BotControlCommand.ScreenOn,
                WebApiCommand.ScreenOffAll => BotController.BotControlCommand.ScreenOff,

                _ => BotController.BotControlCommand.None
            };
        }

        // Stop or Idle/Resume all bots
        private void B_Stop_Click(object? sender, EventArgs e)     // Stop all bots on Stop button click
        {
            var env = RunningEnvironment;                         // Get the current running environment
            if (!_botsForm.BotPanel.Controls.OfType<BotController>().Any(c => c.IsRunning()) && (ModifierKeys & Keys.Alt) == 0)
            // If not running and no Alt key pressed
            {
                WinFormsUtil.Alert("Nothing's running, genius."); // Derp
                return;
            }

            var cmd = WebApi.BotControlCommand.Stop; // Default command to stop all bots

            if ((ModifierKeys & Keys.Control) != 0 || (ModifierKeys & Keys.Shift) != 0) // If Control or Shift key is pressed (Honestly didn't know this ever existed. Cool shit)
            {
                if (env.IsRunning)
                {
                    WinFormsUtil.Alert("Commanding all bots to Idle.", "Press Stop (without a modifier key) to hard-stop and unlock control, or press Stop with the modifier key again to resume.");
                    cmd = WebApi.BotControlCommand.Idle;
                }
                else
                {
                    WinFormsUtil.Alert("Commanding all bots to resume their original task.", "Press Stop (without a modifier key) to hard-stop and unlock control.");
                    cmd = WebApi.BotControlCommand.Resume;
                }
            }
            else
            {
                env.StopAll(); // Stop in the name of love. (All bots)
            }
            SendAll(cmd);
        }

        // Add a new bot with the current config
        private void B_New_Click(object? sender, EventArgs e) // Add a new bot on Add button click
        {
            var cfg = CreateNewBotConfig(); // Create a new bot config based on current settings in BotsForm

            // If the config is null or invalid, show an alert and return
            if (cfg == null)
                return;
            if (!AddBot(cfg))
            {
                WinFormsUtil.Alert("Unable to add bot; ensure details are valid and not duplicate with an already existing bot.");
                return;
            }
            System.Media.SystemSounds.Asterisk.Play(); // Play a sound to indicate the bot was added successfully
        }

        // Update handling
        private async void Updater_Click(object? sender, EventArgs e)
        {
            await UpdateChecker.CheckForUpdatesAsync(forceShow: true); // Will auto-handle the UpdateForm without all the other crap
        }

        // Add a new bot to the environment and UI
        private bool AddBot(PokeBotState? cfg)
        {
            if (cfg == null || !cfg.IsValid()) // Ensure cfg is not null before calling IsValid()
                return false;

            if (Bots.Any(z => z.Connection.Equals(cfg.Connection)))
                return false;

            PokeRoutineExecutorBase newBot;
            try
            {
                newBot = RunningEnvironment.CreateBotFromConfig(cfg);
            }
            catch
            {
                return false;
            }

            try
            {
                RunningEnvironment.Add(newBot);
            }
            catch (ArgumentException ex)
            {
                WinFormsUtil.Error(ex.Message);
                return false;
            }

            AddBotControl(cfg);
            Bots.Add(cfg);
            Config.Bots = Bots.ToArray();
            SaveCurrentConfig();
            HookBotProgress(cfg, newBot);

            return true;
        }

        private void AddBotControl(PokeBotState cfg)
        {
            var row = new BotController { Width = _botsForm.BotPanel.Width };
            row.Initialize(RunningEnvironment, cfg);
            _botsForm.BotPanel.Controls.Add(row);
            _botsForm.BotPanel.SetFlowBreak(row, true);
            row.ReloadStatus();
            ProgressHelper.Initialize(row);

            row.Click += (s, e) =>
            {
                var details = cfg.Connection;
                _botsForm.IPBox.Text = details.IP;
                _botsForm.PortBox.Value = details.Port;
                _botsForm.ProtocolBox.SelectedIndex = (int)details.Protocol;
                _botsForm.RoutineBox.SelectedValue = (int)cfg.InitialRoutine;
            };

            row.Remove += (s, e) =>
            {
                Bots.RemoveAll(b => b.Connection.Equals(row.State.Connection));
                RunningEnvironment.Remove(row.State, !RunningEnvironment.Config.SkipConsoleBotCreation);
                _botsForm.BotPanel.Controls.Remove(row);
                Config.Bots = Bots.ToArray();
                SaveCurrentConfig();
            };
        }


        ///////////////////////////////////////////////////
        ////// THEME MANAGEMENT FOR MAIN UI ELEMENTS //////
        ///////////////////////////////////////////////////


        // Update the method signature to explicitly allow nullability for the 'sender' parameter.
        private void CB_Themes_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (sender is not ComboBox comboBox)
                return;

            if (comboBox.SelectedItem is not string selected)
                return;

            Config.Theme = selected;
            SaveCurrentConfig();
            ThemeManager.ApplyTheme(this, selected);
        }

        private void LoadThemeOptions()
        {
            CB_Themes.Items.Clear();
            foreach (var key in ThemeManager.ThemePresets.Keys)
                CB_Themes.Items.Add(key);

            // The minimalist redesign replaced the old 30+ theme presets. Migrate stale
            // config values to the new default so the dropdown isn't blank.
            if (!ThemeManager.ThemePresets.ContainsKey(Config.Theme ?? string.Empty))
                Config.Theme = "Graphite";

            CB_Themes.SelectedItem = Config.Theme;
            ThemeManager.ApplyTheme(this, Config.Theme);
        }


        ///////////////////////////////////////////////////////////////////
        /////// BOT HANDLING FOR INITIATING A NEW BOT IN THE FORMS ////////
        /////// ALSO HOLDS RANDOM CALL TO STOP THE WEBSERVER AFTER ////////
        ///////////////////////////////////////////////////////////////////
        private PokeBotState? CreateNewBotConfig() // Create a new bot configuration based on the current settings in the BotsForm
        {
            var ip = _botsForm.IPBox.Text.Trim();    // Get the IP address from the IPBox and trim any whitespace
            var port = (int)_botsForm.PortBox.Value; // Get the port number from the PortBox
            if (string.IsNullOrWhiteSpace(ip))       // Check if the IP address is empty or whitespace
            {
                WinFormsUtil.Error("IP address cannot be empty.");
                return null;
            }
            if (!System.Net.IPAddress.TryParse(ip, out _))
            {
                WinFormsUtil.Error($"Invalid IP address: {ip}");
                return null;
            }
            if (_botsForm.ProtocolBox.SelectedValue == null)
            {
                WinFormsUtil.Error("Please select a protocol.");
                return null;
            }
            if (_botsForm.RoutineBox.SelectedValue == null)
            {
                WinFormsUtil.Error("Please select a routine.");
                return null;
            }
            this.StopWebServer(); // Stop the web server to free up resources before adding a new bot

            // Create a new SwitchConnectionConfig based on the IP and port
            var cfg = BotConfigUtil.GetConfig<SwitchConnectionConfig>(ip, port); // Get the connection config based on the IP and port
            cfg.Protocol = (SwitchProtocol)_botsForm.ProtocolBox.SelectedValue;  // Set the protocol from the ProtocolBox
            var pk = new PokeBotState { Connection = cfg };                      // Create a new PokeBotState with the connection config
            var type = (PokeRoutineType)_botsForm.RoutineBox.SelectedValue;      // Set the routine type from the RoutineBox
            pk.Initialize(type);                                                 // Initialize the PokeBotState with the selected routine type
            return pk;                                                           // Return the new PokeBotState configuration
        }


        ///////////////////////////////////////////////////
        ////////// IMAGES FOR THE MAIN UI FORMS ///////////
        ///////////////////////////////////////////////////

        // Initialize the method for the left side image in the panelLeftSide
        private PictureBox leftSideImage = null!;
        private PictureBox updateNotificationImage = null!;
        private Label updateVersionLabel = null!;
        private ToolTip updateNotificationToolTip = null!;
        private string availableUpdateVersion = string.Empty;
        private bool updateNotificationVisible;

        // Font download link in title bar
        private LinkLabel downloadFontsLink = null!;

        // Initialize the meat and potatoes for the left side image in the panelLeftSide
        private void InitializeLeftSideImage()
        {
            // Slim, centered mode badge below the theme dropdown — reinstated after the
            // initial minimalist pass; sized smaller so it whispers instead of shouting.
            leftSideImage = new PictureBox
            {
                Size = new Size(160, 60),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None,
                Visible = true,
                Anchor = AnchorStyles.Top,
            };

            updateNotificationToolTip = new ToolTip
            {
                AutoPopDelay = 2500,
                InitialDelay = 500,
                ReshowDelay = 500,
                ShowAlways = true
            };

            updateVersionLabel = new Label
            {
                AutoSize = false,
                Size = new Size(180, 14),
                Font = new Font("Segoe UI", 7F, FontStyle.Bold),
                ForeColor = Color.FromArgb(245, 247, 250),
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Visible = false,
                Cursor = Cursors.Hand
            };

            updateNotificationImage = new PictureBox
            {
                Size = new Size(132, 23),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None,
                Visible = false,
                Cursor = Cursors.Hand
            };
            LoadUpdateNotificationImage();

            updateVersionLabel.Click += (_, _) => Updater_Click(updateVersionLabel, EventArgs.Empty);
            updateNotificationImage.Click += (_, _) => Updater_Click(updateNotificationImage, EventArgs.Empty);
            updateNotificationToolTip.SetToolTip(updateVersionLabel, AppLocalization.Get(LocalizationKeys.BotsUpdateAvailableTooltip));
            updateNotificationToolTip.SetToolTip(updateNotificationImage, AppLocalization.Get(LocalizationKeys.BotsUpdateAvailableTooltip));

            panelLeftSide.Controls.Add(leftSideImage);
            panelLeftSide.Controls.Add(updateVersionLabel);
            panelLeftSide.Controls.Add(updateNotificationImage);
            panelLeftSide.Resize += (s, e) => PositionLeftSideImage();
            PositionLeftSideImage();
        }

        private void LoadUpdateNotificationImage()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                const string resourceName = "SysBot.Pokemon.WinForms.Resources.new-release-update.png";
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                    return;

                using var source = Image.FromStream(stream);
                updateNotificationImage.Image = new Bitmap(source);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load update notification image: {ex.Message}");
            }
        }

        // Position the left side image in the panelLeftSide.
        private void PositionLeftSideImage()
        {
            if (leftSideImage == null || panelLeftSide == null || CB_Themes == null)
                return;

            int usableWidth = panelLeftSide.ClientSize.Width
                              - panelLeftSide.Padding.Left
                              - panelLeftSide.Padding.Right;

            int horizontalCenter = panelLeftSide.Padding.Left
                                   + (usableWidth - leftSideImage.Width) / 2;

            int verticalOffsetBelowTheme = CB_Themes.Bottom + (updateNotificationVisible ? 6 : 24);
            if (updateNotificationVisible && lblTitle != null)
            {
                int updateBlockHeight = updateVersionLabel.Height + updateNotificationImage.Height + 2;
                int latestModeTop = lblTitle.Top - leftSideImage.Height - updateBlockHeight - 2;
                verticalOffsetBelowTheme = Math.Min(verticalOffsetBelowTheme, latestModeTop);
            }

            leftSideImage.Location = new Point(horizontalCenter, verticalOffsetBelowTheme);
            PositionUpdateNotification();
        }

        private void PositionUpdateNotification()
        {
            if (updateVersionLabel == null || updateNotificationImage == null || panelLeftSide == null || leftSideImage == null)
                return;

            int usableWidth = panelLeftSide.ClientSize.Width
                              - panelLeftSide.Padding.Left
                              - panelLeftSide.Padding.Right;

            int labelX = panelLeftSide.Padding.Left + (usableWidth - updateVersionLabel.Width) / 2;
            int imageX = panelLeftSide.Padding.Left + (usableWidth - updateNotificationImage.Width) / 2;
            updateVersionLabel.Location = new Point(labelX, leftSideImage.Bottom + 1);
            updateNotificationImage.Location = new Point(imageX, updateVersionLabel.Bottom - 1);

            updateVersionLabel.BringToFront();
            updateNotificationImage.BringToFront();
        }

        public void SetUpdateNotification(bool isUpdateAvailable, string newVersion = "")
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => SetUpdateNotification(isUpdateAvailable, newVersion)));
                return;
            }

            updateNotificationVisible = isUpdateAvailable && !string.IsNullOrWhiteSpace(newVersion);
            availableUpdateVersion = updateNotificationVisible ? newVersion : string.Empty;

            if (updateNotificationVisible)
                updateVersionLabel.Text = AppLocalization.Format(LocalizationKeys.BotsUpdateNowTo, newVersion);

            updateVersionLabel.Visible = updateNotificationVisible;
            updateNotificationImage.Visible = updateNotificationVisible;
            PositionLeftSideImage();
        }

        // Initialize the method for the upper panel image in the upperPanelImage
        private PictureBox upperPanelImage = null!;

        private void InitializeUpperImage()
        {
            // Mode artwork in the title bar — sized to fill the bar's full height so there are
            // no black gaps above or below the artwork. StretchImage so the source PNG covers
            // the full box; the mode images all share a similar horizontal aspect so light
            // anisotropic scaling is acceptable here and avoids transparent banding.
            upperPanelImage = new PictureBox
            {
                Size = new Size(240, panelTitleBar.Height),
                SizeMode = PictureBoxSizeMode.StretchImage,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Visible = true
            };

            panelTitleBar.Controls.Add(upperPanelImage);
            panelTitleBar.Resize += (s, e) => PositionUpperImage();
            PositionUpperImage();
        }

        private void PositionUpperImage()
        {
            if (upperPanelImage == null || panelTitleBar == null)
                return;

            // Match the title bar height exactly each layout pass so resizing the form keeps
            // the artwork edge-to-edge vertically.
            upperPanelImage.Height = panelTitleBar.Height;

            // Sit just to the left of the language + window buttons (which start around btnMinimize.Left).
            int rightAnchor = btnMinimize != null ? btnMinimize.Left : panelTitleBar.Width;
            int x = rightAnchor - upperPanelImage.Width - 86; // leave room for the language pill
            upperPanelImage.Location = new Point(Math.Max(x, 200), 0);
            upperPanelImage.BringToFront();
        }

        // Legacy helper kept for backward-compat with older references; the new PositionUpperImage
        // (defined alongside InitializeUpperImage) is the active implementation.

        private void LoadLogoImage(string logoPath)
        {
            if (string.IsNullOrWhiteSpace(logoPath))
                return;

            try
            {
                if (Uri.IsWellFormedUriString(logoPath, UriKind.Absolute))
                {
                    using var httpClient = new HttpClient();
                    using var stream = httpClient.GetStreamAsync(logoPath).Result;
                    if (stream != null)
                        pictureLogo.Image = Image.FromStream(stream);
                }
                else
                {
                    string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                    string fullPath = Path.Combine(exeDir, logoPath);
                    if (File.Exists(fullPath))
                        pictureLogo.Image = Image.FromFile(fullPath);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(AppLocalization.LocalizeRuntimeMessage($"[Logo Load] Failed to load logo: {ex.Message}"));
            }
        }

        // Update the background image based on the current game mode
        private void UpdateBackgroundImage(ProgramMode mode)
        {
            if (leftSideImage == null) return;

            switch (mode)
            {
                case ProgramMode.PLZA:
                    leftSideImage.Image = Resources.plza_mode_image; // Set the image for SV mode
                    break;
                case ProgramMode.SV:
                    leftSideImage.Image = Resources.sv_mode_image;   // Set the image for SV mode
                    break;
                case ProgramMode.SWSH:
                    leftSideImage.Image = Resources.swsh_mode_image; // Set the image for SWSH mode
                    break;
                case ProgramMode.BDSP:
                    leftSideImage.Image = Resources.bdsp_mode_image; // Set the image for BDSP mode
                    break;
                case ProgramMode.LA:
                    leftSideImage.Image = Resources.pla_mode_image;  // Set the image for PLA mode
                    break;
                case ProgramMode.LGPE:
                    leftSideImage.Image = Resources.lgpe_mode_image; // Set the image for LGPE mode
                    break;
                default:
                    leftSideImage.Image = null;
                    break;
            }
        }

        // Update the upper image based on the current game mode
        private void UpdateUpperImage(ProgramMode mode)
        {
            if (upperPanelImage == null) return;

            switch (mode)
            {
                case ProgramMode.PLZA:
                    upperPanelImage.Image = Resources.plza_mode_upper;
                    break;

                case ProgramMode.SV:
                    upperPanelImage.Image = Resources.sv_mode_upper;
                    break;

                case ProgramMode.SWSH:
                    upperPanelImage.Image = Resources.swsh_mode_upper;
                    break;

                case ProgramMode.BDSP:
                    upperPanelImage.Image = Resources.bdsp_mode_upper;
                    break;

                case ProgramMode.LA:
                    upperPanelImage.Image = Resources.pla_mode_upper;
                    break;

                case ProgramMode.LGPE:
                    upperPanelImage.Image = Resources.lgpe_mode_upper;
                    break;

                default:
                    upperPanelImage.Image = null;
                    break;
            }
        }


        ///////////////////////////////////////////////////
        //////////// PROGRESS BAR UPDATE LOGIC ////////////
        ///////////////////////////////////////////////////
       
        private void HookBotProgress(PokeBotState cfg, PokeRoutineExecutorBase bot)
        {
            BotController? botControl = _botsForm.BotPanel.Controls
                .OfType<BotController>()
                .FirstOrDefault(c => c.State.Connection.Equals(cfg.Connection));

            if (botControl == null)
                return;

            ProgressHelper.Initialize(botControl); // Only if you're using this style

            void SetProgress(int percent)
            {
                if (_botsForm.InvokeRequired)
                    _botsForm.BeginInvoke((Action)(() => botControl.SetProgressValue(percent)));
                else
                    botControl.SetProgressValue(percent);
            }

            switch (bot)
            {
                case PokeTradeBotPLZA zaBot:
                    zaBot.TradeProgressChanged += SetProgress;
                    break;
                case PokeTradeBotSV svBot:
                    svBot.TradeProgressChanged += SetProgress;
                    break;
                case PokeTradeBotSWSH swshBot:
                    swshBot.TradeProgressChanged += SetProgress;
                    break;
                case PokeTradeBotBS bsBot:
                    bsBot.TradeProgressChanged += SetProgress;
                    break;
                case PokeTradeBotLA laBot:
                    laBot.TradeProgressChanged += SetProgress;
                    break;
                case PokeTradeBotLGPE lgpeBot:
                    lgpeBot.TradeProgressChanged += SetProgress;
                    break;
            }
        }


        ///////////////////////////////////////////////////
        //// FLP BOT AND PROTOCOL ADDITIONAL HANDLING /////
        ///////////////////////////////////////////////////

        // Resize the BotController controls when the panel is resized, focused on width
        private void FLP_Bots_Resize(object sender, EventArgs e)
        {
            // Resize all BotController controls in the BotPanel to match the width of the panel
            foreach (var c in _botsForm.BotPanel.Controls.OfType<BotController>()) // Iterate through each BotController in the BotPanel
                c.Width = _botsForm.BotPanel.Width;                                // Set the width of the BotController to the width of the BotPanel
        }

        // Protocol and IP selection handling
        private void CB_Protocol_SelectedIndexChanged(object sender, EventArgs e)
        {
            _botsForm.IPBox.Visible = _botsForm.ProtocolBox.SelectedIndex == 0; // Show the IPBox only if the selected protocol is WiFi
        }


        ///////////////////////////////////////////////////
        /////// MOVE UI VIA MOUSE ON PANELTITLEBAR ////////
        ///////////////////////////////////////////////////

        private void InitializeTitleBarDrag()
        {
            // Original panel mouse down
            panelTitleBar.MouseDown += panelTitleBar_MouseDown;

            // Forward mouse events from all children to panel
            foreach (Control ctrl in panelTitleBar.Controls)
            {
                ctrl.MouseDown += panelTitleBar_MouseDown;
            }
        }

        private void HookDrag(Control parent)
        {
            parent.MouseDown += panelTitleBar_MouseDown;
            foreach (Control child in parent.Controls)
                HookDrag(child);
        }

        [DllImport("user32.DLL", EntryPoint = "ReleaseCapture")] // Release the mouse capture
        private extern static void ReleaseCapture();             // Release the mouse capture to allow dragging the window
        [DllImport("user32.DLL", EntryPoint = "SendMessage")]    // Send a message to the window
        private extern static void SendMessage(System.IntPtr hWnd, int wMsg, int wParam, int lParam); // Send a message to the window to allow dragging
        // Update the method signature to explicitly allow nullability for the 'sender' parameter.
        private void panelTitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            // Don't drag window when clicking on title bar buttons.
            if (sender == btnClose || sender == btnMaximize || sender == btnMinimize || sender == btnLanguage)
                return;

            if (btnLanguage != null && sender is Control senderControl && senderControl.Parent == btnLanguage)
                return;

            if (btnLanguage != null && btnLanguage.Visible && sender == panelTitleBar)
            {
                var languageBounds = btnLanguage.Bounds;
                if (languageBounds.Contains(e.Location))
                    return;
            }

            ReleaseCapture();                           // Release the mouse capture
            SendMessage(this.Handle, 0x112, 0xf012, 0); // Send a message to the window to allow dragging
        }


        ///////////////////////////////////////////////////
        ////////// BOTS/HUB/LOGS BUTTON HANDLING //////////
        ////////// CLOSE/MAX/MIN BUTTON HANDLING //////////
        ///////////////////////////////////////////////////

        // Pill geometry — restrained title-bar chrome
        private const int LangPillWidth = 62;
        private const int LangPillHeight = 24;
        private const int LangPillSegmentWidth = 31;
        private bool _langHoverEn;
        private bool _langHoverEs;

        // Method to activate Bots button and load BotsForm
        private void InitializeLanguageButton()
        {
            languageToolTip = new ToolTip
            {
                AutoPopDelay = 2500,
                InitialDelay = 500,
                ReshowDelay = 500,
                ShowAlways = true
            };

            btnLanguage = new DoubleBufferedPanel
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.Transparent,
                Location = new Point(btnMinimize.Left - LangPillWidth - 14, 15),
                Name = "btnLanguage",
                Size = new Size(LangPillWidth, LangPillHeight),
                Cursor = Cursors.Hand
            };
            btnLanguage.Paint += LanguagePill_Paint;

            // Same weight on both segments — color carries the active state, not weight
            var pillFont = new Font("Segoe UI Semibold", 8F, FontStyle.Regular);

            lblLangEn = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                Font = pillFont,
                Location = new Point(0, 0),
                Margin = Padding.Empty,
                Padding = new Padding(0, 0, 0, 3),
                Name = "lblLangEn",
                Size = new Size(LangPillSegmentWidth, LangPillHeight),
                Text = "EN",
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblLangEn.Click += (_, _) => SetLanguage(AppLanguage.English);
            lblLangEn.MouseEnter += (_, _) => { _langHoverEn = true; ApplyLocalization(); };
            lblLangEn.MouseLeave += (_, _) => { _langHoverEn = false; ApplyLocalization(); };

            lblLangEs = new Label
            {
                AutoSize = false,
                BackColor = Color.Transparent,
                Font = pillFont,
                Location = new Point(LangPillSegmentWidth, 0),
                Margin = Padding.Empty,
                Padding = new Padding(0, 0, 0, 3),
                Name = "lblLangEs",
                Size = new Size(LangPillSegmentWidth, LangPillHeight),
                Text = "ES",
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            lblLangEs.Click += (_, _) => SetLanguage(AppLanguage.Spanish);
            lblLangEs.MouseEnter += (_, _) => { _langHoverEs = true; ApplyLocalization(); };
            lblLangEs.MouseLeave += (_, _) => { _langHoverEs = false; ApplyLocalization(); };

            btnLanguage.Controls.Add(lblLangEn);
            btnLanguage.Controls.Add(lblLangEs);
            panelTitleBar.Controls.Add(btnLanguage);
            btnLanguage.BringToFront();
            ApplyLocalization();
        }

        private void LanguagePill_Paint(object? sender, PaintEventArgs e)
        {
            if (btnLanguage == null)
                return;

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var theme = ThemeManager.CurrentColors;
            var bounds = new Rectangle(0, 0, btnLanguage.Width - 1, btnLanguage.Height - 1);
            int radius = bounds.Height;

            // Track: inset/recessed surface (Background is darker than the title bar's PanelBase),
            // so the pill reads like an inset field — clearer container than the previous flat-on-flat.
            using (var trackPath = CreatePillPath(bounds, radius))
            using (var trackBrush = new SolidBrush(theme.Background))
            using (var borderPen = new Pen(theme.Shadow, 1f))
            {
                g.FillPath(trackBrush, trackPath);
                g.DrawPath(borderPen, trackPath);
            }

            // Thumb: raised tile — uses Hover, which is brighter than PanelBase, so it reads
            // as lifted above both the track and the title bar. Single semantic moment for color
            // is reserved for the text + the 2px accent underline.
            bool isEnglish = AppLocalization.Language == AppLanguage.English;
            const int inset = 3;
            int halfWidth = (btnLanguage.Width - inset * 2) / 2;
            int thumbX = isEnglish ? inset : inset + halfWidth;
            var thumb = new Rectangle(
                thumbX,
                inset,
                halfWidth,
                btnLanguage.Height - (inset * 2) - 1);

            using (var thumbPath = CreatePillPath(thumb, thumb.Height))
            using (var thumbBrush = new SolidBrush(theme.Hover))
            {
                g.FillPath(thumbBrush, thumbPath);
            }
        }

        private static GraphicsPath CreatePillPath(Rectangle bounds, int radius)
        {
            radius = Math.Max(2, Math.Min(radius, Math.Min(bounds.Width, bounds.Height)));
            var path = new GraphicsPath();
            int d = radius;
            path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
            path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
            path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private sealed class DoubleBufferedPanel : Panel
        {
            public DoubleBufferedPanel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.UserPaint
                    | ControlStyles.SupportsTransparentBackColor, true);
                UpdateStyles();
            }
        }

        private void SetLanguage(AppLanguage next)
        {
            if (AppLocalization.Language == next)
                return;

            if (HasRunningBots())
            {
                WinFormsUtil.Alert(AppLocalization.Get(LocalizationKeys.LanguageChangeBlockedBotRunning));
                return;
            }

            Config.Language = next;
            AppLocalization.SetLanguage(next);
            SaveCurrentConfig();
            LogUtil.LogInfo("System", AppLocalization.Format(LocalizationKeys.LogLanguageChanged, next));
        }

        private bool HasRunningBots()
        {
            if (RunningEnvironment?.IsRunning == true)
                return true;

            return _botsForm?.BotPanel.Controls.OfType<BotController>().Any(c => c.IsRunning()) == true;
        }

        private void InitializeChildFormTitleImage()
        {
            childFormTitleImage = new PictureBox
            {
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None,
                SizeMode = PictureBoxSizeMode.Zoom,
                Location = new Point(22, 8),
                Size = new Size(170, 38),
                Visible = false
            };

            childFormIcon.Visible = false;
            lblTitleChildForm.Visible = false;
            panelTitleBar.Controls.Add(childFormTitleImage);
            childFormTitleImage.BringToFront();
        }

        private Image? GetChildFormTitleImage(IconButton? btn)
        {
            var resourceFile = btn switch
            {
                var b when b == btnBots => "title_bots.png",
                var b when b == btnHub => "title_hub.png",
                var b when b == btnLogs && AppLocalization.Language == AppLanguage.Spanish => "title_registros.png",
                var b when b == btnLogs => "title_logs.png",
                _ => null
            };

            if (resourceFile == null)
                return null;

            if (childFormTitleImages.TryGetValue(resourceFile, out var cached))
                return cached;

            var resourceName = $"SysBot.Pokemon.WinForms.Resources.{resourceFile}";
            using var stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
            if (stream == null)
                return null;

            var image = Image.FromStream(stream);
            childFormTitleImages[resourceFile] = image;
            return image;
        }

        private void UpdateChildFormTitle(IconButton? btn)
        {
            if (lblTitleChildForm == null || childFormIcon == null)
                return;

            if (btn != null)
            {
                lblTitleChildForm.Text = btn.Text.Trim();
                childFormIcon.IconChar = btn.IconChar;
                childFormIcon.IconColor = ThemeManager.CurrentColors.Muted;
            }
            else
            {
                lblTitleChildForm.Text = AppLocalization.Get(LocalizationKeys.Loading);
            }

            if (childFormTitleImage == null)
                return;

            var image = GetChildFormTitleImage(btn);
            if (image == null)
            {
                childFormTitleImage.Visible = false;
                childFormIcon.Visible = btn != null;
                lblTitleChildForm.Visible = true;
                return;
            }

            int titleHeight = btn switch
            {
                var b when b == btnBots => 34,
                var b when b == btnHub => 34,
                _ => 38
            };
            const int maxTitleWidth = 210;
            int width = Math.Min(maxTitleWidth, Math.Max(120, (int)Math.Round(image.Width * (titleHeight / (double)image.Height))));
            childFormTitleImage.Image = image;
            childFormTitleImage.Size = new Size(width, titleHeight);
            childFormTitleImage.Location = new Point(20, Math.Max(0, (panelTitleBar.Height - titleHeight) / 2));
            childFormTitleImage.Visible = true;
            childFormTitleImage.BringToFront();
            childFormIcon.Visible = false;
            lblTitleChildForm.Visible = false;
        }

        private void ApplyLocalization()
        {
            // Three-space prefix keeps the label visually separated from the icon
            // in the minimalist sidebar layout.
            if (btnBots != null)
                btnBots.Text = "   " + AppLocalization.Get(LocalizationKeys.NavBots);
            if (btnHub != null)
                btnHub.Text = "   " + AppLocalization.Get(LocalizationKeys.NavHub);
            if (btnLogs != null)
                btnLogs.Text = "   " + AppLocalization.Get(LocalizationKeys.NavLogs);

            if (btnLanguage != null && lblLangEn != null && lblLangEs != null)
            {
                var theme = ThemeManager.CurrentColors;
                bool isEnglish = AppLocalization.Language == AppLanguage.English;

                // Active glyph rides the raised thumb in ForeColor for maximum legibility;
                // the 2px Accent strip painted below carries the semantic tie to the nav.
                // Inactive glyph sits on the recessed track in Muted, brightening on hover.
                lblLangEn.ForeColor = isEnglish
                    ? theme.ForeColor
                    : (_langHoverEn ? theme.ForeColor : theme.Muted);
                lblLangEs.ForeColor = !isEnglish
                    ? theme.ForeColor
                    : (_langHoverEs ? theme.ForeColor : theme.Muted);

                btnLanguage.Invalidate();

                var tooltip = AppLocalization.Get(LocalizationKeys.LanguageButtonTooltip);
                languageToolTip?.SetToolTip(btnLanguage, tooltip);
                languageToolTip?.SetToolTip(lblLangEn, tooltip);
                languageToolTip?.SetToolTip(lblLangEs, tooltip);
            }

            if (downloadFontsLink != null)
                downloadFontsLink.Text = AppLocalization.Get(LocalizationKeys.DownloadFonts);

            if (updateVersionLabel != null && updateNotificationVisible && !string.IsNullOrWhiteSpace(availableUpdateVersion))
                updateVersionLabel.Text = AppLocalization.Format(LocalizationKeys.BotsUpdateNowTo, availableUpdateVersion);

            if (updateNotificationToolTip != null && updateVersionLabel != null && updateNotificationImage != null)
            {
                updateNotificationToolTip.SetToolTip(updateVersionLabel, AppLocalization.Get(LocalizationKeys.BotsUpdateAvailableTooltip));
                updateNotificationToolTip.SetToolTip(updateNotificationImage, AppLocalization.Get(LocalizationKeys.BotsUpdateAvailableTooltip));
            }

            _botsForm?.ApplyLocalization();
            _logsForm?.ApplyLocalization();
            _hubForm?.ApplyLocalization();

            UpdateChildFormTitle(currentBtn);
        }

        private void Bots_Click(object sender, EventArgs e)
        {
            if (sender is IconButton btn)
            {
                ActivateButton(btn);
                OpenChildForm(_botsForm);
            }
        }

        // Method to activate Hub button and load HubForm
        private void Hub_Click(object sender, EventArgs e)
        {
            if (sender is IconButton btn)
            {
                ActivateButton(btn);
                // Ensure HubForm exists
                if (_hubForm == null || _hubForm.IsDisposed)
                    _hubForm = new HubForm(Config.Hub);

                OpenChildForm(_hubForm);
            }
        }

        // Method to activate Logs button and load LogsForm
        private void Logs_Click(object sender, EventArgs e)
        {
            if (sender is IconButton btn)
            {
                ActivateButton(btn);
                OpenChildForm(_logsForm);
            }
        }

        // Close button
        private void BtnClose_Click(object? sender, EventArgs e)
        {
            Application.Exit(); // Exit program on Close button click
        }

        // Maximize and Restore button
        private void BtnMaximize_Click(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Normal)   // If the window is in normal state, then...
                WindowState = FormWindowState.Maximized; // ...Maximize the window
            else
                WindowState = FormWindowState.Normal; // Restore the window to normal state if Maximized
        }

        // Minimize button
        private void BtnMinimize_Click(object sender, EventArgs e)
        {
            WindowState = FormWindowState.Minimized; // Minimize the window on Minimize button click
        }

        // Method and logic to open the child form(Bots, Hub, or Logs) in panelMain
        private async void OpenChildForm(Form childForm)
        {
            activeForm?.Hide(); // Hide the currently active form, if any
            activeForm = childForm; // Set the new active form

            // If the form is not already in the panel, add it
            if (!panelMain.Controls.Contains(childForm))
            {
                childForm.TopLevel = false; // Set the form to be a non-top-level form
                childForm.FormBorderStyle = FormBorderStyle.None; // Remove the border style
                panelMain.Controls.Add(childForm); // Add the form to the panelMain controls
            }

            childForm.Dock = DockStyle.None; // needed for slide
            childForm.Size = panelMain.ClientSize; // set size to panel size
            childForm.Left = panelMain.ClientSize.Width; // reset slide start
            childForm.Opacity = 0; // reset fade start
            childForm.Show(); // Show the form
            childForm.BringToFront(); // Bring the form to the front of panelMain

            // Slide/fade animation
            while (childForm.Left > 0 || childForm.Opacity < 1)
            {
                await Task.Delay(10);
                childForm.Left = Math.Max(childForm.Left - 40, 0);
                childForm.Opacity = Math.Min(childForm.Opacity + 0.05, 1);
            }

            childForm.Dock = DockStyle.Fill;
        }


        ///////////////////////////////////////////////////
        ///////// GUI THEMING AND BUTTON HANDLING /////////
        ///////////////////////////////////////////////////

        public void SetupThemeAwareButtons()
        {
            // Use the current theme colors
            var colors = ThemeManager.CurrentColors;

            // Re-read tokens for the language pill so it follows theme swaps
            if (btnLanguage != null)
                ApplyLocalization();

            foreach (var btn in new[] { btnBots, btnHub, btnLogs })
            {
                btn.BackColor = colors.PanelBase;
                btn.ForeColor = colors.ForeColor;
                btn.UseVisualStyleBackColor = false;
                btn.FlatStyle = FlatStyle.Flat;
                btn.FlatAppearance.BorderSize = 0;

                // Hover animations
                btn.MouseEnter += (s, e) => StartHoverFade(btn, colors.Shadow, 150);
                btn.MouseLeave += (s, e) => StartHoverFade(btn, colors.PanelBase);

                // Click effects
                btn.Click += (s, e) =>
                {
                    ActivateButton(btn);
                    FlashClick(btn);
                };
            }
        }

        // Method to activate the buttons and set the left border
        private void ActivateButton(IconButton btn)
        {
            if (currentBtn == btn) return; // already active

            // Reset previous
            DisableButton();

            currentBtn = btn;

            var accent = ThemeManager.CurrentColors.Accent;
            var hover = ThemeManager.CurrentColors.Hover;

            // Subtle active state: highlighted background + accent icon. No pulsing outline.
            btn.BackColor = hover;
            btn.IconColor = accent;
            btn.ForeColor = ThemeManager.CurrentColors.ForeColor;
            btn.FlatAppearance.BorderSize = 0;

            // Position the slim left-edge accent strip next to the active button.
            if (leftBorderBtn != null)
            {
                leftBorderBtn.BackColor = accent;
                leftBorderBtn.Size = new Size(3, btn.Height);
                leftBorderBtn.Location = new Point(0, btn.Top);
                leftBorderBtn.BringToFront();
                leftBorderBtn.Visible = true;
            }

            UpdateChildFormTitle(btn);
        }


        // Method to slide and fade in the child forms (Bots, Hub, Logs) when it is opened
        private async void SlideFadeInForm(Form form)
        {
            form.Dock = DockStyle.None;                               // Remove any docking style from the form
            form.Size = panelMain.ClientSize;                         // Set the size of the form to match the panelMain size to make a seamless transition
            form.Location = new Point(panelMain.ClientSize.Width, 0); // Set the initial location of the form to the right edge of the panelMain
            form.Opacity = 0;                                         // Set the initial opacity of the form to 0 (invisible)
            form.Show();                                              // Show the form in all its glory

            // Slide the form to the left and increase its opacity
            while (form.Left > 0 || form.Opacity < 1.0) // While the form is not fully visible
            {
                await Task.Delay(10);                              // Wait for 10 milliseconds for smoother animation
                form.Left = Math.Max(form.Left - 40, 0);           // Move the form left by 40 pixels, but not less than 0
                form.Opacity = Math.Min(form.Opacity + 0.05, 1.0); // Increase the opacity of the form by 0.05, but not more than 1.0 (fully visible)
            }
            form.Dock = DockStyle.Fill; // Set the form to fill the entire panelMain like it should
            form.BringToFront();        // Bring the form to the front of panelMain
        }

        // Smooth hover fade
        private void StartHoverFade(IconButton btn, Color targetColor, int durationMs = 200)
        {
            if (hoverTimers.ContainsKey(btn))
            {
                hoverTimers[btn].Stop();
                hoverTimers[btn].Dispose();
                hoverTimers.Remove(btn);
            }

            Color startColor = btn.BackColor;
            int steps = durationMs / 10; // ~60 FPS
            int currentStep = 0;

            Timer timer = new Timer { Interval = 26 };
            timer.Tick += (s, e) =>
            {
                currentStep++;
                float t = EaseInOut((float)currentStep / steps);
                btn.BackColor = LerpColor(startColor, targetColor, t);

                if (currentStep >= steps)
                {
                    timer.Stop();
                    timer.Dispose();
                    hoverTimers.Remove(btn);
                }
            };

            hoverTimers[btn] = timer;
            timer.Start();
        }

        private void StartOutlinePulse(IconButton btn, Color baseColor)
        {
            // stop any existing pulse first
            if (pulseTimers.TryGetValue(btn, out var oldTimer))
            {
                oldTimer.Stop();
                oldTimer.Dispose();
                pulseTimers.Remove(btn);
            }

            float t = 0f;
            bool forward = true;

            Timer pulseTimer = new Timer { Interval = 16 }; // ~60 FPS
            pulseTimer.Tick += (s, e) =>
            {
                // update t
                t += forward ? 0.03f : -0.03f;
                if (t >= 1f) { t = 1f; forward = false; }
                if (t <= 0f) { t = 0f; forward = true; }

                // calculate new color, clamp to 0-255
                int Clamp(int val) => Math.Max(0, Math.Min(255, val));
                float intensity = 0.6f + 0.4f * t; // base 60% -> 100%
                btn.FlatAppearance.BorderColor = Color.FromArgb(
                    Clamp((int)(baseColor.R * intensity)),
                    Clamp((int)(baseColor.G * intensity)),
                    Clamp((int)(baseColor.B * intensity))
                 );
            };

            pulseTimer.Start();
            pulseTimers[btn] = pulseTimer;
        }

        // Call this when disabling a button
        private void StopOutlinePulse(IconButton btn)
        {
            if (pulseTimers.TryGetValue(btn, out var timer))
            {
                timer.Stop();
                timer.Dispose();
                pulseTimers.Remove(btn);
            }
            // reset outline color
            btn.FlatAppearance.BorderColor = ThemeManager.CurrentColors.PanelBase;
        }

        // Click flash animation
        private async void FlashClick(IconButton btn)
        {
            Color flashColor = ThemeManager.CurrentColors.Shadow;
            Color original = btn.BackColor;

            btn.BackColor = flashColor;
            await Task.Delay(100); // quick flash
            btn.BackColor = original;
        }

        // Smooth color interpolation
        private Color LerpColor(Color start, Color end, float t)
        {
            int r = (int)(start.R + (end.R - start.R) * t);
            int g = (int)(start.G + (end.G - start.G) * t);
            int b = (int)(start.B + (end.B - start.B) * t);
            return Color.FromArgb(r, g, b);
        }

        // Easing function for smooth transitions
        private float EaseInOut(float t) => t < 0.5f ? 4 * t * t * t : 1 - MathF.Pow(-2 * t + 2, 3) / 2;

        private void SetupTitleBarButtonHoverEffects()
        {
            // Quiet, monochrome title-bar controls. Only close goes red on hover.
            Color normalIcon = Color.FromArgb(180, 184, 192);
            Color hoverIcon = Color.FromArgb(232, 234, 238);
            Color hoverClose = Color.FromArgb(232, 70, 80);

            btnClose.IconColor = normalIcon;
            btnClose.MouseEnter += (s, e) => btnClose.IconColor = hoverClose;
            btnClose.MouseLeave += (s, e) => btnClose.IconColor = normalIcon;

            btnMaximize.IconColor = normalIcon;
            btnMaximize.MouseEnter += (s, e) => btnMaximize.IconColor = hoverIcon;
            btnMaximize.MouseLeave += (s, e) => btnMaximize.IconColor = normalIcon;

            btnMinimize.IconColor = normalIcon;
            btnMinimize.MouseEnter += (s, e) => btnMinimize.IconColor = hoverIcon;
            btnMinimize.MouseLeave += (s, e) => btnMinimize.IconColor = normalIcon;
        }

        private void InitGlitter()
        {
            // Sparkle effects intentionally disabled for the minimalist redesign.
            // Method kept (and still called from the constructor) as a safe no-op so callers
            // and any future restoration of the effect don't require additional plumbing.
        }

        // Method to disable the current button and reset its style to default
        private void DisableButton()
        {
            if (currentBtn != null)
            {
                StopOutlinePulse(currentBtn); // STOP pulse animation (no-op if not running)
                currentBtn.BackColor = ThemeManager.CurrentColors.PanelBase; // default bg
                currentBtn.IconColor = Color.FromArgb(180, 184, 192);        // muted icon
                currentBtn.TextAlign = ContentAlignment.MiddleLeft;
                currentBtn.TextImageRelation = TextImageRelation.ImageBeforeText;
                currentBtn.ImageAlign = ContentAlignment.MiddleLeft;
                currentBtn.FlatAppearance.BorderSize = 0; // remove outline
            }
        }


        ///////////////////////////////////////////////////
        ///////////// DOWNLOAD FONTS LINK /////////////////
        ///////////////////////////////////////////////////

        // Initialize the Download Fonts link in the title bar
        private void InitializeFontsLink()
        {
            try
            {
                // Check if user has chosen to hide the fonts link
                if (Config.HideFontsLink)
                {
                    LogUtil.LogInfo("Fonts link is hidden per user preference", "System");
                    return;
                }

                LogUtil.LogInfo("Initializing Download Fonts link", "System");

                downloadFontsLink = new LinkLabel
                {
                    Text = AppLocalization.Get(LocalizationKeys.DownloadFonts),
                    AutoSize = true,
                    LinkColor = Color.FromArgb(51, 255, 255),
                    VisitedLinkColor = Color.FromArgb(51, 255, 255),
                    ActiveLinkColor = Color.FromArgb(51, 200, 200),
                    LinkBehavior = LinkBehavior.HoverUnderline,
                    Font = new Font("Segoe UI", 7.5F),
                    BackColor = Color.Transparent,
                    Cursor = Cursors.Hand
                };

                // Position below the Minimize/Maximize/Close buttons - custom XY positioning
                // Move significantly to the left and slightly down from the close button
                downloadFontsLink.Location = new Point(btnClose.Left - 76, btnClose.Bottom + 14);

                // Add click handlers - both Click and LinkClicked for compatibility
                downloadFontsLink.Click += DownloadFontsLink_Click;
                downloadFontsLink.LinkClicked += DownloadFontsLink_LinkClicked;

                // Add to title bar
                panelTitleBar.Controls.Add(downloadFontsLink);
                downloadFontsLink.BringToFront();

                LogUtil.LogInfo("Download Fonts link initialized successfully", "System");
            }
            catch (Exception ex)
            {
                LogUtil.LogError($"Failed to initialize Download Fonts link: {ex.Message}", "System");
            }
        }

        // Handle the Download Fonts link click (left-click)
        private void DownloadFontsLink_Click(object? sender, EventArgs e)
        {
            try
            {
                LogUtil.LogInfo("Download Fonts link clicked (Click event)", "System");

                // Show custom dialog
                using var dialog = new FontDownloadDialog();
                var result = dialog.ShowDialog(this);

                LogUtil.LogInfo($"Dialog result: {result}", "System");

                if (result == DialogResult.Yes)
                {
                    // User clicked Yes, download the fonts
                    DownloadFonts();
                }

                // Check if user selected to hide the link
                if (dialog.DontShowAgain)
                {
                    Config.HideFontsLink = true;
                    SaveCurrentConfig();

                    // Hide the link
                    if (downloadFontsLink != null)
                    {
                        panelTitleBar.Controls.Remove(downloadFontsLink);
                        downloadFontsLink.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogError($"Error in Download Fonts link click: {ex.Message}", "System");
                WinFormsUtil.Error($"Error showing font download dialog:\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}");
            }
        }

        // Handle the Download Fonts link click (right-click/LinkClicked event)
        private void DownloadFontsLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                LogUtil.LogInfo("Download Fonts link clicked", "System");

                // Show custom dialog
                using var dialog = new FontDownloadDialog();
                var result = dialog.ShowDialog(this);

                LogUtil.LogInfo($"Dialog result: {result}", "System");

                if (result == DialogResult.Yes)
                {
                    // User clicked Yes, download the fonts
                    DownloadFonts();
                }

                // Check if user selected to hide the link
                if (dialog.DontShowAgain)
                {
                    Config.HideFontsLink = true;
                    SaveCurrentConfig();

                    // Hide the link
                    if (downloadFontsLink != null)
                    {
                        panelTitleBar.Controls.Remove(downloadFontsLink);
                        downloadFontsLink.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                LogUtil.LogError($"Error in Download Fonts link click: {ex.Message}", "System");
                WinFormsUtil.Error($"Error showing font download dialog:\n{ex.Message}\n\nStack trace:\n{ex.StackTrace}");
            }
        }

        // Download the fonts file
        private async void DownloadFonts()
        {
            const string downloadUrl = "https://github.com/Secludedly/FusionBot/raw/refs/heads/main/.extra/Fonts.7z";

            try
            {
                // Let user choose download location
                using var folderDialog = new FolderBrowserDialog
                {
                    Description = "Select where to download Fonts.7z",
                    ShowNewFolderButton = true,
                    // Set default to Downloads folder
                    SelectedPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                        "Downloads"
                    )
                };

                var dialogResult = folderDialog.ShowDialog(this);
                if (dialogResult != DialogResult.OK || string.IsNullOrWhiteSpace(folderDialog.SelectedPath))
                {
                    LogUtil.LogInfo("Font download cancelled by user", "System");
                    return;
                }

                string filePath = Path.Combine(folderDialog.SelectedPath, "Fonts.7z");

                // Check if file already exists
                if (File.Exists(filePath))
                {
                    var overwriteResult = SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(
                        $"The file Fonts.7z already exists in this location.\n\nDo you want to overwrite it?",
                        "File Exists",
                        MessageBoxButtons.YesNo,
                        SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Question
                    );

                    if (overwriteResult != DialogResult.Yes)
                    {
                        LogUtil.LogInfo("Font download cancelled - file already exists", "System");
                        return;
                    }
                }

                // Download the file
                LogUtil.LogInfo($"Downloading fonts to: {filePath}", "System");

                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromMinutes(5);

                var response = await client.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();

                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(filePath, fileBytes);

                LogUtil.LogInfo($"Fonts downloaded successfully to: {filePath}", "System");
                WinFormsUtil.Alert($"Fonts downloaded successfully!\n\nLocation: {filePath}\n\nPlease install the fonts and restart the program.");
            }
            catch (Exception ex)
            {
                LogUtil.LogError($"Failed to download fonts: {ex.Message}", "System");
                WinFormsUtil.Error($"Failed to download fonts:\n{ex.Message}");
            }
        }


        ///////////////////////////////////////////////////
        //////////////// SAVING TO CONFIG /////////////////
        ///////////////////////////////////////////////////
        public void SaveCurrentConfig()
        {
            try
            {
                string json = JsonSerializer.Serialize(Config, new JsonSerializerOptions // Serialize the current config to json
                {
                    WriteIndented = true                     // Format the json with indentation for readability
                });
                File.WriteAllText(Program.ConfigPath, json); // Save the serialized json to the config file
            }
            catch (Exception ex)
            {
                WinFormsUtil.Error($"Failed to save configuration:\n{ex.Message}");
            }
        }

        // WINFORMS JUNK THAT'S NEEDED
        private void panel6_Paint(object sender, PaintEventArgs e)
        {

        }
    }

    ///////////////////////////////////////////////////
    ///////// FONT DOWNLOAD DIALOG FORM ///////////////
    ///////////////////////////////////////////////////

    public class FontDownloadDialog : Form
    {
        private CheckBox chkDontShowAgain = null!;
        private Button btnYes = null!;
        private Button btnNo = null!;
        private Label lblMessage = null!;

        public bool DontShowAgain => chkDontShowAgain?.Checked ?? false;

        public FontDownloadDialog()
        {
            try
            {
                InitializeDialog();
            }
            catch (Exception ex)
            {
                LogUtil.LogError($"Error initializing FontDownloadDialog: {ex.Message}", "System");
                throw;
            }
        }

        private void InitializeDialog()
        {
            // Form settings
            this.Text = L("Download Fonts");
            this.Size = new Size(500, 240);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(32, 32, 32);

            // Message label
            lblMessage = new Label
            {
                Text = L("Would you like to download the fonts used in this program in order to install them to display the text correctly?\n\nBe sure after you install the fonts that you reload the program."),
                Location = new Point(20, 20),
                Size = new Size(440, 80),
                Font = new Font("Segoe UI", 9.75F),
                ForeColor = Color.White,
                AutoSize = false
            };

            // Checkbox
            chkDontShowAgain = new CheckBox
            {
                Text = L("Do not display a link to Download Fonts again"),
                Location = new Point(20, 110),
                Size = new Size(350, 24),
                Font = new Font("Segoe UI", 9F),
                ForeColor = Color.White,
                BackColor = Color.Transparent
            };

            // Yes button
            btnYes = new Button
            {
                Text = L("Yes"),
                Location = new Point(280, 145),
                Size = new Size(90, 30),
                Font = new Font("Segoe UI", 9F),
                DialogResult = DialogResult.Yes,
                BackColor = Color.FromArgb(0, 120, 215),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnYes.FlatAppearance.BorderSize = 0;

            // No button
            btnNo = new Button
            {
                Text = L("No"),
                Location = new Point(380, 145),
                Size = new Size(90, 30),
                Font = new Font("Segoe UI", 9F),
                DialogResult = DialogResult.No,
                BackColor = Color.FromArgb(60, 60, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnNo.FlatAppearance.BorderSize = 0;

            // Add controls to form
            this.Controls.Add(lblMessage);
            this.Controls.Add(chkDontShowAgain);
            this.Controls.Add(btnYes);
            this.Controls.Add(btnNo);

            // Set accept and cancel buttons
            this.AcceptButton = btnYes;
            this.CancelButton = btnNo;
        }

        private static string L(string message) => AppLocalization.LocalizeRuntimeMessage(message);
    }
}
