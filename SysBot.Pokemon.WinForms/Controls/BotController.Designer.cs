#nullable enable
using System.Windows.Forms;
using System;
using System.Drawing;
using SysBot.Pokemon.WinForms.Controls;


namespace SysBot.Pokemon.WinForms;


partial class BotController
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
            _glowTimer?.Stop();
            _glowTimer?.Dispose();
            _progressAnimationTimer?.Stop();
            _progressAnimationTimer?.Dispose();
            _sparkleTimer?.Stop();
            _sparkleTimer?.Dispose();
            _holdTimer?.Stop();
            _holdTimer?.Dispose();
            _statusGlowTimer?.Stop();
            _statusGlowTimer?.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Component Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        pnlStatus = new Panel();
        lblStatus = new Label();
        lblConnectionName = new Label();
        lblConnectionInfo = new Label();
        btnActions = new Button();
        RCMenu = new ContextMenuStrip(components);
        SuspendLayout();
        //
        // pnlStatus — small status dot at the left edge of the row.
        //
        pnlStatus.BackColor = Color.FromArgb(248, 113, 113);
        pnlStatus.Location = new Point(14, 14);
        pnlStatus.Name = "pnlStatus";
        pnlStatus.Size = new Size(8, 8);
        pnlStatus.TabIndex = 0;
        _statusGlowTimer = new Timer
        {
            Interval = 30 // Lower is smoother
        };
        _statusGlowTimer.Tick += (_, _) => AnimateStatusGlow();
        _statusGlowTimer.Start();
        //
        // lblStatus
        //
        lblStatus.AutoSize = true;
        lblStatus.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold);
        lblStatus.ForeColor = Color.FromArgb(180, 184, 192);
        lblStatus.Location = new Point(32, 9);
        lblStatus.Name = "lblStatus";
        lblStatus.Size = new Size(122, 18);
        lblStatus.TabIndex = 2;
        lblStatus.Text = "DISCONNECTED";
        //
        // lblConnectionName — primary identity (IP / label).
        //
        lblConnectionName.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular);
        lblConnectionName.ForeColor = Color.FromArgb(232, 234, 238);
        lblConnectionName.Location = new Point(32, 30);
        lblConnectionName.Name = "lblConnectionName";
        lblConnectionName.AutoSize = false;
        lblConnectionName.Size = new Size(520, 22);
        lblConnectionName.TabIndex = 3;
        lblConnectionName.Text = "???";
        //
        // lblBotMeta — "Routine · time", a plain Label so it lines up exactly with
        // lblConnectionName (IP) and lblConnectionInfo (last-logged). RichTextBox added
        // an extra 3-4 px of left padding that broke vertical alignment.
        //
        lblBotMeta = new Label
        {
            Name = "lblBotMeta",
            AutoSize = false,
            Location = new Point(32, 54),
            Size = new Size(540, 20),
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular),
            ForeColor = Color.FromArgb(160, 164, 172),
            TextAlign = ContentAlignment.MiddleLeft,
            Text = string.Empty,
            UseCompatibleTextRendering = false,
        };
        Controls.Add(lblBotMeta);
        // Keep the rtbBotMeta field alive for source compatibility but invisible.
        rtbBotMeta = new RichTextBox { Visible = false, Location = new Point(-100, -100), Size = new Size(0, 0) };
        Controls.Add(rtbBotMeta);
        //
        // lblConnectionInfo — last-logged hint, smallest weight.
        //
        lblConnectionInfo.Font = new Font("Segoe UI", 8.5F);
        lblConnectionInfo.ForeColor = Color.FromArgb(120, 124, 132);
        lblConnectionInfo.Location = new Point(32, 78);
        lblConnectionInfo.Name = "lblConnectionInfo";
        lblConnectionInfo.AutoSize = false;
        lblConnectionInfo.Size = new Size(540, 20);
        lblConnectionInfo.TabIndex = 4;
        BringToFront();
        //
        // lblRoutine — kept as a hidden placeholder so existing references stay valid.
        //
        lblRoutine = new Label
        {
            AutoSize = true,
            Font = new Font("Segoe UI", 8.5F, FontStyle.Regular),
            ForeColor = Color.FromArgb(140, 144, 152),
            Location = new Point(-100, -100),
            Name = "lblRoutine",
            Size = new Size(0, 0),
            Visible = false,
        };
        Controls.Add(lblRoutine);
        //
        // btnActions
        //
        btnActions.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnActions.BackColor = Color.FromArgb(30, 32, 36);
        btnActions.FlatAppearance.BorderColor = Color.FromArgb(36, 38, 42);
        btnActions.FlatAppearance.BorderSize = 1;
        btnActions.FlatAppearance.MouseDownBackColor = Color.FromArgb(36, 38, 42);
        btnActions.FlatAppearance.MouseOverBackColor = Color.FromArgb(36, 38, 42);
        btnActions.FlatStyle = FlatStyle.Flat;
        btnActions.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        btnActions.ForeColor = Color.FromArgb(232, 234, 238);
        btnActions.Cursor = Cursors.Hand;
        btnActions.Location = new Point(576, 14);
        btnActions.Name = "btnActions";
        btnActions.Size = new Size(116, 28);
        btnActions.TabIndex = 4;
        btnActions.Text = "Actions  ▾";
        btnActions.UseVisualStyleBackColor = false;
        btnActions.Click += BtnActions_Click;
        //
        // RCMenu
        //
        RCMenu.ImageScalingSize = new Size(20, 20);
        RCMenu.Name = "RCMenu";
        RCMenu.Size = new Size(61, 4);
        //
        // BotController
        //
        BackColor = Color.FromArgb(22, 23, 26);
        Controls.Add(pnlStatus);
        Controls.Add(lblStatus);
        Controls.Add(lblConnectionName);
        Controls.Add(lblConnectionInfo);
        Controls.Add(btnActions);
        Margin = new Padding(0, 0, 0, 1);
        Name = "BotController";
        // Slightly taller than before to leave a clear band above the 2px progress bar
        // for the running Pikachu mascot.
        Size = new Size(700, 122);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Panel pnlStatus = null!;
    private Label lblStatus = null!;
    private Label lblConnectionInfo = null!;
#pragma warning disable CS0649 // Field is never assigned
    private Label? lblLastLogTime;
#pragma warning restore CS0649
    private Label lblConnectionName = new Label();
    private Label lblRoutine = null!;
    private Label lblBotMeta = null!;
    private RichTextBox rtbBotMeta = null!;
    private Button btnActions = null!;
    private ContextMenuStrip RCMenu = null!;
    private Timer _statusGlowTimer = null!;

}

