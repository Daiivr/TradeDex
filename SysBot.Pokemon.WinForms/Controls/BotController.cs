using SysBot.Base;
using SysBot.Pokemon.Localization;
using SysBot.Pokemon.WinForms.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms;

public partial class BotController : UserControl
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PokeBotState State { get; private set; } = new();
    private IPokeBotRunner? Runner;
    public EventHandler? Remove;
    public List<BotController> BotControls { get; } = new();
    private string _status = "DISCONNECTED";
    private Timer _glowTimer;
    private float _glowPhase = 60f;
    private bool _glowIncreasing = true;
    private Color _glowBaseColor = Color.Red;
    private Panel _progressBarContainer = null!;
    private Panel _progressFill = null!;
    private Timer _progressAnimationTimer = null!;
#pragma warning disable CS0169 // Field is never used
    private Timer? _shimmerTimer;
#pragma warning restore CS0169
    private Timer _sparkleTimer = null!;
    private int _targetProgress = 0;
    private int _currentProgress = 0;
    private Color _glowColor = Color.FromArgb(96, 165, 250);
#pragma warning disable CS0649 // Field is never assigned
    private int _shimmerX;
#pragma warning restore CS0649
    private int _sparkleX = -50;
    private int _sparkleWidth = 50;
    // Calm accent-only progress gradient (Graphite theme accent blue → lighter tint).
    private Color _startColor = Color.FromArgb(59, 130, 246);
    private Color _endColor = Color.FromArgb(125, 211, 252);

    // Running-Pikachu mascot that surfs the leading edge of the progress fill.
    private PictureBox? _pikachu;
    private float _pikachuX = 0f;
    private Point _lastPikachuLocation;
    private bool _hasPikachuLocation;
    private const float PikachuFollowEasing = 0.22f;
    private const float PikachuMaxStepPixels = 4.5f;
    private const float PikachuSnapDistancePixels = 0.35f;
    private bool _holdAt100 = false;
    private Timer _holdTimer = null!;


    public BotController()
    {
        InitializeComponent();
        InitializeContextMenu();
        AppLocalization.LanguageChanged += (_, _) => ApplyLocalization();

        this.Margin = new Padding(0);
        this.Padding = new Padding(0);
        this.SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        this.UpdateStyles();

        _glowTimer = new Timer { Interval = 30 };
        _glowTimer.Tick += (s, e) => AnimateStatusGlow();
        _glowTimer.Start();

        // Disable mouse highlight effects
        foreach (Control control in Controls)
        {
            control.MouseEnter += (_, _) => BackColor = BackColor;
            control.MouseLeave += (_, _) => BackColor = BackColor;
        }

        _progressBarContainer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 2,
            BackColor = Color.FromArgb(15, 16, 19),
            Margin = Padding.Empty,
            Padding = Padding.Empty,
        };
        _progressBarContainer.BorderStyle = BorderStyle.None;

        _progressFill = new Panel
        {
            Height = _progressBarContainer.Height,
            Width = 0,
            Location = new Point(0, 0),
            BackColor = _glowColor,
            Anchor = AnchorStyles.Left | AnchorStyles.Top
        };
        _progressFill.BorderStyle = BorderStyle.None;

        _progressBarContainer.Controls.Add(_progressFill);
        Controls.Add(_progressBarContainer);

        InitializePikachu();

        _progressAnimationTimer = new Timer { Interval = 15 };
        _progressAnimationTimer.Tick += (_, _) => AnimateProgress();
        _progressAnimationTimer.Start();

        // Sparkle animation timer
        _sparkleTimer = new Timer { Interval = 20 }; // ~50 FPS
        _sparkleTimer.Tick += (s, e) =>
        {
            _sparkleX += 8; // move sparkle speed
            if (_sparkleX > _progressFill.Width + _sparkleWidth)
                _sparkleX = -_sparkleWidth;
            _progressFill.Invalidate();
        };
        _sparkleTimer.Start();
    }

    private void _progressFill_Paint(object? sender, PaintEventArgs e)
    {
        if (_progressFill.Width <= 0)
            return;

        Rectangle rect = new Rectangle(_sparkleX, 0, _sparkleWidth, _progressFill.Height);

        using var shimmerBrush = new LinearGradientBrush(
            rect,
            Color.FromArgb(180, Color.White),
            Color.FromArgb(0, Color.White),
            LinearGradientMode.Horizontal
        );

        int filledWidth = _progressFill.Width;
        Rectangle clipRect = new Rectangle(0, 0, filledWidth, _progressFill.Height);

        var oldClip = e.Graphics.Clip;
        e.Graphics.SetClip(clipRect);
        e.Graphics.FillRectangle(shimmerBrush, rect);
        e.Graphics.SetClip(oldClip, System.Drawing.Drawing2D.CombineMode.Replace);
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        _progressFill.Paint += _progressFill_Paint;
    }

    public void SetTradeProgress(int percent)
    {
        if (percent < 0 || percent > 100)
            return;

        _targetProgress = percent;
    }

    public void SetProgressValue(int percent)
    {
        _targetProgress = Math.Clamp(percent, 0, 100);
    }

    public void ResetProgress()
    {
        _targetProgress = 0;
    }

    private void AnimateProgress()
    {
        if (_holdAt100)
        {
            UpdatePikachuPosition();
            return; // Don't animate while in the 6-second hold
        }

        if (_currentProgress == _targetProgress)
        {
            UpdatePikachuPosition();
            return;
        }

        int speed = 2;

        if (_currentProgress < _targetProgress)
            _currentProgress = Math.Min(_currentProgress + speed, _targetProgress);
        else
            _currentProgress = Math.Max(_currentProgress - speed, _targetProgress);

        int totalWidth = _progressBarContainer.Width;
        _progressFill.Width = (totalWidth * _currentProgress) / 100;
        UpdatePikachuPosition();

        // If we hit 100%, trigger the 6-second hold
        if (_currentProgress == 100)
        {
            _holdAt100 = true;
            _progressFill.BackColor = Color.FromArgb(74, 222, 128); // 100% — quiet green

            _holdTimer = new Timer { Interval = 6000 }; // 6 seconds
            _holdTimer.Tick += (s, e) =>
            {
                _holdTimer.Stop();
                _holdAt100 = false;
                _targetProgress = 0; // Reset to 0 and restart animation
            };
            _holdTimer.Start();
            return;
        }

        // Otherwise: gradient & glow during normal progress
        float percentProgress = _currentProgress / 100f;
        Color interpolated = InterpolateColor(_startColor, _endColor, percentProgress);

        int brightnessPulse = (int)(10 + (Math.Sin(DateTime.Now.Millisecond / 200.0) * 20));
        _progressFill.BackColor = ControlPaint.Light(interpolated, brightnessPulse / 100f);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Bottom hairline divider between rows.
        using (var divider = new Pen(Color.FromArgb(36, 38, 42), 1))
        {
            e.Graphics.DrawLine(divider, 0, Height - 1, Width, Height - 1);
        }
    }

    // Loads the embedded Pikachu running GIF (if present) and parents it to the card so
    // it animates over the progress bar. Failing silently if the resource isn't shipped
    // keeps the redesign backwards-compatible with builds where the user hasn't dropped
    // the GIF into Resources\ yet.
    //
    // Implementation detail: GDI+ requires the source stream to stay alive for the entire
    // lifetime of the Image. We copy the embedded resource into a MemoryStream that we
    // keep referenced as a class field — disposing it would invalidate the Image and the
    // PictureBox would render the standard "broken image" red X.
    private System.IO.MemoryStream? _pikachuStream;

    private void InitializePikachu()
    {
        try
        {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            const string resourceName = "SysBot.Pokemon.WinForms.Resources.pikachu_running.gif";
            using var source = assembly.GetManifestResourceStream(resourceName);
            if (source == null)
            {
                Debug.WriteLine($"[Pikachu] Embedded resource '{resourceName}' not found. Available: " +
                    string.Join(", ", assembly.GetManifestResourceNames()));
                return;
            }

            // Copy into a long-lived MemoryStream so the underlying buffer stays valid
            // for ImageAnimator.
            _pikachuStream = new System.IO.MemoryStream();
            source.CopyTo(_pikachuStream);
            _pikachuStream.Position = 0;

            var img = Image.FromStream(_pikachuStream);

            _pikachu = new PictureBox
            {
                Image = img,
                Size = new Size(22, 22),
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.Transparent,
                Visible = false,
                TabStop = false,
            };
            Controls.Add(_pikachu);
            _pikachu.BringToFront();
            // Let mouse events fall through to the underlying card so clicking on Pikachu
            // still selects the row.
            _pikachu.MouseDown += (_, e) => OnMouseDown(e);

            // PictureBox handles animated GIF frame updates itself. Avoid manually
            // driving ImageAnimator here because repeated invalidation can make the
            // sprite look like it is restarting while parked at the progress edge.
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Pikachu] Failed to initialize: {ex.Message}");
            _pikachu = null;
        }
    }

    private void UpdatePikachuPosition()
    {
        if (_pikachu == null || _progressBarContainer == null) return;

        int totalWidth = _progressBarContainer.Width;
        if (totalWidth <= 0)
        {
            SetPikachuVisible(false);
            return;
        }

        int fillWidth = (totalWidth * _currentProgress) / 100;
        float targetX = Math.Clamp(fillWidth - (_pikachu.Width / 2f), 0, Math.Max(0, Width - _pikachu.Width));

        if (_currentProgress <= 0 || !_pikachu.Visible)
        {
            _pikachuX = targetX;
        }
        else
        {
            float delta = targetX - _pikachuX;
            float distance = Math.Abs(delta);

            if (distance <= PikachuSnapDistancePixels)
            {
                _pikachuX = targetX;
            }
            else
            {
                float easedStep = Math.Min(distance * PikachuFollowEasing, PikachuMaxStepPixels);
                _pikachuX += Math.Sign(delta) * easedStep;
            }
        }

        // Sit just above the progress bar (which is 2px tall and bottom-docked).
        int y = Height - _progressBarContainer.Height - _pikachu.Height;
        SetPikachuVisible(_currentProgress > 0);

        Point nextLocation = new((int)Math.Round(_pikachuX), y);
        if (!_hasPikachuLocation || _lastPikachuLocation != nextLocation)
        {
            _pikachu.Location = nextLocation;
            _lastPikachuLocation = nextLocation;
            _hasPikachuLocation = true;
        }
    }

    private void SetPikachuVisible(bool visible)
    {
        if (_pikachu != null && _pikachu.Visible != visible)
            _pikachu.Visible = visible;
    }

    private Color InterpolateColor(Color start, Color end, float progress)
    {
        int r = (int)(start.R + (end.R - start.R) * progress);
        int g = (int)(start.G + (end.G - start.G) * progress);
        int b = (int)(start.B + (end.B - start.B) * progress);
        return Color.FromArgb(r, g, b);
    }

    private void InitializeContextMenu()
    {
        RCMenu.Opening -= RcMenuOnOpening;
        RCMenu.Items.Clear();

        // Your color map for the menu text
        var colorMap = new Dictionary<string, Color>
    {
        { MenuText("▶️", LocalizationKeys.BotMenuStart), Color.LimeGreen },
        { MenuText("⏹️", LocalizationKeys.BotMenuStop), Color.IndianRed },
        { MenuText("⏸️", LocalizationKeys.BotMenuIdle), Color.White },
        { MenuText("🔼", LocalizationKeys.BotMenuResume), Color.White },
        { MenuText("🔁", LocalizationKeys.BotMenuRestart), Color.White },
        { MenuText("🔄", LocalizationKeys.BotMenuRebootStop), Color.White },
        { MenuText("💡", LocalizationKeys.BotMenuScreenOn), Color.White },
        { MenuText("🌑", LocalizationKeys.BotMenuScreenOff), Color.White },
        { MenuText("⛔", LocalizationKeys.BotMenuRemove), Color.IndianRed }
    };

        AddMenuItem(MenuText("▶️", LocalizationKeys.BotMenuStart), BotControlCommand.Start);
        AddMenuItem(MenuText("⏹️", LocalizationKeys.BotMenuStop), BotControlCommand.Stop);
        AddMenuItem(MenuText("⏸️", LocalizationKeys.BotMenuIdle), BotControlCommand.Idle);
        AddMenuItem(MenuText("🔼", LocalizationKeys.BotMenuResume), BotControlCommand.Resume);

        RCMenu.Items.Add(new ToolStripSeparator());

        AddMenuItem(MenuText("🔁", LocalizationKeys.BotMenuRestart), BotControlCommand.Restart);
        AddMenuItem(MenuText("🔄", LocalizationKeys.BotMenuRebootStop), BotControlCommand.RebootAndStop);

        RCMenu.Items.Add(new ToolStripSeparator());

        AddMenuItem(MenuText("💡", LocalizationKeys.BotMenuScreenOn), BotControlCommand.ScreenOn);
        AddMenuItem(MenuText("🌑", LocalizationKeys.BotMenuScreenOff), BotControlCommand.ScreenOff);

        RCMenu.Items.Add(new ToolStripSeparator());

        var remove = new ToolStripMenuItem(MenuText("⛔", LocalizationKeys.BotMenuRemove));
        remove.Click += (_, __) => TryRemove();
        RCMenu.Items.Add(remove);

        RCMenu.Opening += RcMenuOnOpening;

        // Set the custom renderer here
        RCMenu.Renderer = new ColoredMenuRenderer(colorMap);
    }

    private static string MenuText(string icon, string key) => $"{icon} {AppLocalization.Get(key)}";

    private void ApplyLocalization()
    {
        InitializeContextMenu();
        UpdateStatusUI(_status);
        if (lblConnectionName.Text == AppLocalization.Get(LocalizationKeys.BotUnknownConnection) ||
            lblConnectionName.Text == "Unknown Connection")
            lblConnectionName.Text = AppLocalization.Get(LocalizationKeys.BotUnknownConnection);
    }

    private void ColoredMenuItem_DrawItem(object sender, DrawItemEventArgs e)
    {
        if (sender is not ToolStripMenuItem item)
            return;

        Color textColor = item.Tag is Color c ? c : SystemColors.MenuText;

        // Draw background (normal or selected)
        if ((e.State & DrawItemState.Selected) == DrawItemState.Selected)
        {
            e.Graphics.FillRectangle(SystemBrushes.Highlight, e.Bounds);
            textColor = SystemColors.HighlightText; // invert text color on hover
        }
        else
        {
            e.Graphics.FillRectangle(SystemBrushes.Menu, e.Bounds);
        }

        // Draw text left aligned vertically centered
        TextRenderer.DrawText(
            e.Graphics,
            item.Text,
            e.Font,
            e.Bounds,
            textColor,
            TextFormatFlags.VerticalCenter | TextFormatFlags.Left);

        // Draw focus rectangle if needed
        if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
        {
            e.DrawFocusRectangle();
        }
    }

    private void ColoredMenuItem_MeasureItem(object sender, MeasureItemEventArgs e)
    {
        if (sender is ToolStripMenuItem item)
        {
            Size textSize = TextRenderer.MeasureText(item.Text, item.Font);
            e.ItemWidth = textSize.Width;
            e.ItemHeight = textSize.Height;
        }
    }

    private class ColoredMenuRenderer : ToolStripProfessionalRenderer
    {
        private readonly Dictionary<string, Color> _colorMap;
        private readonly Color _backgroundColor = Color.FromArgb(22, 23, 26);
        private readonly int _leftPadding = 22; // padding from left edge

        public ColoredMenuRenderer(Dictionary<string, Color> colorMap)
        {
            _colorMap = colorMap;
        }

        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(_backgroundColor);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            Color bg = e.Item.Selected ? Color.Cyan : _backgroundColor;
            using var brush = new SolidBrush(bg);
            e.Graphics.FillRectangle(brush, e.Item.ContentRectangle);
        }

        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            if (e.Item.Text != null && _colorMap.TryGetValue(e.Item.Text, out Color color))
                e.TextColor = e.Item.Selected ? SystemColors.HighlightText : color;
            else
                e.TextColor = e.Item.Selected ? SystemColors.HighlightText : SystemColors.ControlText;

            // Adjust text rectangle to remove checkmark/image margin
            e.TextRectangle = new Rectangle(
                e.Item.ContentRectangle.Left + _leftPadding,
                e.Item.ContentRectangle.Top,
                e.Item.ContentRectangle.Width - _leftPadding,
                e.Item.ContentRectangle.Height
            );

            base.OnRenderItemText(e);
        }

        protected override void OnRenderItemCheck(ToolStripItemImageRenderEventArgs e)
        {
            // Skip drawing the checkmark box entirely
        }

        protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
        {
            // Skip the image margin area entirely
        }
    }

    private void AddMenuItem(string label, BotControlCommand cmd)
    {
        var bot = GetBotSafely();
        var item = new ToolStripMenuItem(label)
        {
            Tag = cmd,
            Enabled = cmd.IsUsable(bot?.IsRunning == true, bot?.IsPaused == true)
        };
        item.Click += (_, __) => SendCommand(cmd);
        RCMenu.Items.Add(item);
    }

    public void Initialize(IPokeBotRunner runner, PokeBotState cfg)
    {
        Runner = runner;
        State = cfg;
        ReloadStatus();
    }

    public bool IsRunning()
    {
        return _status.Equals("RUNNING", StringComparison.OrdinalIgnoreCase);
    }

    public void UpdateLastLogTime(DateTime time)
    {
        // Example output: "LAST LOG: 6:30:00 PM"
        string formatted = time.ToString("h:mm:ss tt"); // 12-hour, no leading zero on hour, AM/PM
        if (lblLastLogTime != null)
            lblLastLogTime.Text = $"{formatted}";
    }

    public void ReloadStatus(BotSource<PokeBotState>? botSource = null)
    {
        try
        {
            botSource ??= GetBotSafely();
            if (botSource == null) // Ensure botSource is not null
                return;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[ERROR GETTING BOT]: {ex}");
            return;
        }

        var bot = botSource.Bot;
        if (bot == null) return;

        string status = bot.Connection == null ? "DISCONNECTED"
                      : botSource.IsPaused ? "PAUSED"
                      : botSource.IsRunning ? "RUNNING"
                      : "STOPPED";

        _status = status;
        UpdateStatusUI(status);

        lblConnectionName.Text = bot.Connection?.Label ?? AppLocalization.Get(LocalizationKeys.BotUnknownConnection);
        lblConnectionInfo.Text = bot.LastLogged ?? string.Empty;
        SetBotMetaDisplay(State.InitialRoutine.ToString(), bot.LastTime);
    }
    private void SetBotMetaDisplay(string routine, DateTime lastTime)
    {
        // Single-color label so the row aligns pixel-perfect with the IP above and the
        // "↪ No iniciado" below. The middle-dot separator gives a soft visual pause
        // without needing a second color span.
        string timeString = lastTime.ToString("h:mm tt");
        lblBotMeta.Text = $"{routine}  ·  {timeString}";
    }

    private void UpdateStatusUI(string status)
    {
        Color statusColor = status.ToUpperInvariant() switch
        {
            "RUNNING" => Color.LimeGreen,
            "PAUSED" => Color.Goldenrod,
            "STOPPED" => Color.OrangeRed,
            "DISCONNECTED" => Color.Red,
            _ => Color.DimGray
        };

        lblStatus.Text = status.ToUpperInvariant() switch
        {
            "RUNNING" => AppLocalization.Get(LocalizationKeys.BotStatusRunning),
            "PAUSED" => AppLocalization.Get(LocalizationKeys.BotStatusPaused),
            "STOPPED" => AppLocalization.Get(LocalizationKeys.BotStatusStopped),
            "DISCONNECTED" => AppLocalization.Get(LocalizationKeys.BotStatusDisconnected),
            "STOPPING" => AppLocalization.Get(LocalizationKeys.BotStatusStopping),
            "IDLING" => AppLocalization.Get(LocalizationKeys.BotStatusIdling),
            "IDLE" => AppLocalization.Get(LocalizationKeys.BotStatusIdle),
            "REBOOTING" => AppLocalization.Get(LocalizationKeys.BotStatusRebooting),
            "ERROR" => AppLocalization.Get(LocalizationKeys.BotStatusError),
            _ => AppLocalization.Get(LocalizationKeys.BotStatusUnknown),
        };
        lblStatus.ForeColor = statusColor;
        _glowBaseColor = statusColor;
    }

    private void AnimateStatusGlow()
    {
        float min = 60f;
        float max = 255f;
        float speed = 5f;

        _glowPhase += (_glowIncreasing ? speed : -speed);

        if (_glowPhase >= max)
        {
            _glowPhase = max;
            _glowIncreasing = false;
        }
        else if (_glowPhase <= min)
        {
            _glowPhase = min;
            _glowIncreasing = true;
        }

        // Fade between BACKGROUND COLOR and _glowBaseColor
        float t = (_glowPhase - min) / (max - min);

        Color background = Color.FromArgb(22, 23, 26);
        int r = (int)(background.R + (_glowBaseColor.R - background.R) * t);
        int g = (int)(background.G + (_glowBaseColor.G - background.G) * t);
        int b = (int)(background.B + (_glowBaseColor.B - background.B) * t);

        pnlStatus.BackColor = Color.FromArgb(r, g, b);
    }

    public void TryRemove()
    {
        GetBot().Stop();
        Remove?.Invoke(this, EventArgs.Empty);
    }

    public void SendCommand(BotControlCommand cmd)
    {
        if (Runner?.Config.SkipConsoleBotCreation != false)
        {
            LogUtil.LogError("No bots were created because SkipConsoleBotCreation is on!", "Hub");
            return;
        }

        var bot = GetBot();
        switch (cmd)
        {
            case BotControlCommand.Idle: bot.Pause(); break;
            case BotControlCommand.Start: Runner.InitializeStart(); bot.Start(); break;
            case BotControlCommand.Stop: bot.Stop(); break;
            case BotControlCommand.Resume: bot.Resume(); break;
            case BotControlCommand.Restart:
                if (WinFormsUtil.Prompt(MessageBoxButtons.YesNo, AppLocalization.Get(LocalizationKeys.BotRestartConnectionPrompt)) != DialogResult.Yes)
                    return;
                Runner.InitializeStart(); bot.Restart(); break;
            case BotControlCommand.RebootAndStop: bot.RebootAndStop(); break;

            case BotControlCommand.ScreenOn:
                _ = Task.Run(() => BotControlCommandExtensions.SendScreenState(State.Connection.IP, true));
                break;
            case BotControlCommand.ScreenOff:
                _ = Task.Run(() => BotControlCommandExtensions.SendScreenState(State.Connection.IP, false));
                break;

            default:
                WinFormsUtil.Alert(AppLocalization.Get(LocalizationKeys.BotUnsupportedCommand));
                break;
        }
    }

    private void BtnActions_Click(object? sender, EventArgs e)
    {
        if (RCMenu.Items.Count > 0)
            RCMenu.Show(btnActions, new Point(0, btnActions.Height));
    }

    private void RcMenuOnOpening(object? sender, CancelEventArgs e)
    {
        var bot = GetBotSafely();

        foreach (ToolStripItem item in RCMenu.Items)
        {
            if (item is ToolStripMenuItem mi && mi.Tag is BotControlCommand cmd)
            {
                mi.Enabled = cmd.IsUsable(bot?.IsRunning == true, bot?.IsPaused == true);
            }
        }
    }

    public BotSource<PokeBotState> GetBot()
    {
        if (Runner == null) throw new ArgumentNullException(nameof(Runner));
        var bot = Runner.GetBot(State) ?? throw new ArgumentNullException("bot");
        return bot;
    }

    private void ShowRecoveryStatus(object? sender, EventArgs e)
    {
        var bot = GetBot();
        if (bot is null)
        {
            SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(AppLocalization.Get(LocalizationKeys.BotNotFound), AppLocalization.Get(LocalizationKeys.BotRecoveryStatusTitle), MessageBoxButtons.OK, SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Info);
            return;
        }

        var recoveryState = bot.GetRecoveryState();
        if (recoveryState is null)
        {
            SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(AppLocalization.Get(LocalizationKeys.BotRecoveryNotEnabled), AppLocalization.Get(LocalizationKeys.BotRecoveryStatusTitle), MessageBoxButtons.OK, SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Info);
            return;
        }

        var status = AppLocalization.Format(LocalizationKeys.BotRecoveryStatusBody,
            bot.Bot.Connection.Name,
            bot.IsRunning ? AppLocalization.Get(LocalizationKeys.BotStatusRunning) : AppLocalization.Get(LocalizationKeys.BotStatusStopped),
            recoveryState.ConsecutiveFailures,
            recoveryState.CrashHistory.Count,
            recoveryState.IsRecovering ? AppLocalization.Get(LocalizationKeys.CommonYes) : AppLocalization.Get(LocalizationKeys.CommonNo));

        if (recoveryState.LastRecoveryAttempt is not null)
        {
            status += $"Last Recovery: {recoveryState.LastRecoveryAttempt.Value:yyyy-MM-dd HH:mm:ss}\n";
        }

        if (recoveryState.CrashHistory.Count > 0)
        {
            var lastCrash = recoveryState.CrashHistory.OrderByDescending(c => c).FirstOrDefault();
            if (lastCrash != default)
            {
                status += $"Last Crash: {lastCrash:yyyy-MM-dd HH:mm:ss}\n";
            }
        }

        SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(status, AppLocalization.Get(LocalizationKeys.BotRecoveryStatusTitle), MessageBoxButtons.OK, SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Info);
    }

    public string ReadBotState()
    {
        try
        {
            var botSource = GetBot();
            if (botSource is null)
                return "ERROR";

            var bot = botSource.Bot;
            if (bot is null)
                return "ERROR";

            if (!botSource.IsRunning)
                return "STOPPED";

            if (botSource.IsStopping)
                return "STOPPING";

            if (botSource.IsPaused)
            {
                if (bot.Config?.CurrentRoutineType != PokeRoutineType.Idle)
                    return "IDLING";
                else
                    return "IDLE";
            }

            if (botSource.IsRunning && !bot.Connection.Connected)
                return "REBOOTING";

            var cfg = bot.Config;
            if (cfg == null)
                return "UNKNOWN";

            if (cfg.CurrentRoutineType == PokeRoutineType.Idle)
                return "IDLE";

            if (botSource.IsRunning && bot.Connection.Connected)
                return cfg.CurrentRoutineType.ToString();

            return "UNKNOWN";
        }
        catch (Exception ex)
        {
            LogUtil.LogError($"Error reading bot state: {ex.Message}", "BotController");
            return "ERROR";
        }
    }

    public void ReadAllBotStates()
    {
        foreach (var bot in BotControls)
            bot.ReloadStatus();
    }

    private BotSource<PokeBotState>? GetBotSafely()
    {
        try
        {
            return Runner != null ? Runner.GetBot(State) : null;
        }
        catch
        {
            return null;
        }
    }

    public enum BotControlCommand
    {
        None,
        Start,
        Stop,
        Idle,
        Resume,
        Restart,
        RebootAndStop,
        ScreenOn,
        ScreenOff
    }
}
