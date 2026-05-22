using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using FontAwesome.Sharp;

namespace SysBot.Pokemon.WinForms.Controls;

public enum ThemedMessageIcon { None, Info, Warning, Error, Question, Success }

// Custom modal dialog used in place of the native MessageBox so error/alert/prompt
// surfaces match the rest of the redesigned WinForms UI.
public sealed class ThemedMessageBox : Form
{
    public static DialogResult Show(string message, string title = "TradeDex",
        MessageBoxButtons buttons = MessageBoxButtons.OK,
        ThemedMessageIcon icon = ThemedMessageIcon.Info,
        IWin32Window? owner = null)
    {
        using var dlg = new ThemedMessageBox(message, title, buttons, icon);
        return owner != null ? dlg.ShowDialog(owner) : dlg.ShowDialog();
    }

    private ThemedMessageBox(string message, string title, MessageBoxButtons buttons, ThemedMessageIcon icon)
    {
        var theme = ThemeManager.GetCurrentColors() ?? ThemeManager.ThemePresets["Graphite"];

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterParent;
        ShowInTaskbar = false;
        BackColor = theme.PanelBase;
        ForeColor = theme.ForeColor;
        Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        MinimumSize = new Size(360, 160);
        AutoScaleMode = AutoScaleMode.None;
        KeyPreview = true;
        Padding = new Padding(1);

        // ── Title bar ────────────────────────────────────────────────
        var titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = theme.PanelBase,
        };

        var lblTitle = new Label
        {
            Text = title,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 10F, FontStyle.Regular),
            ForeColor = theme.ForeColor,
            Location = new Point(18, 0),
            Size = new Size(420, 40),
        };
        titleBar.Controls.Add(lblTitle);

        var btnClose = new IconButton
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            IconChar = IconChar.Times,
            IconColor = Color.FromArgb(180, 184, 192),
            IconSize = 14,
            Size = new Size(40, 40),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel,
        };
        btnClose.FlatAppearance.BorderSize = 0;
        btnClose.FlatAppearance.MouseOverBackColor = Color.Transparent;
        btnClose.MouseEnter += (_, _) => btnClose.IconColor = Color.FromArgb(232, 70, 80);
        btnClose.MouseLeave += (_, _) => btnClose.IconColor = Color.FromArgb(180, 184, 192);
        titleBar.Controls.Add(btnClose);
        titleBar.Resize += (_, _) => btnClose.Location = new Point(titleBar.Width - 40, 0);
        btnClose.Location = new Point(titleBar.Width - 40, 0);

        // Hairline divider under the title.
        var titleHair = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = theme.Shadow };

        // ── Footer with buttons ──────────────────────────────────────
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 64,
            BackColor = theme.PanelBase,
            Padding = new Padding(20, 14, 20, 16),
        };
        var footerHair = new Panel { Dock = DockStyle.Bottom, Height = 1, BackColor = theme.Shadow };

        var buttonRow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            BackColor = theme.PanelBase,
            WrapContents = false,
        };
        footer.Controls.Add(buttonRow);

        // ── Body ─────────────────────────────────────────────────────
        var body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = theme.PanelBase,
            Padding = new Padding(22, 22, 22, 14),
        };

        var iconBox = new IconPictureBox
        {
            IconColor = GetIconColor(icon, theme),
            IconChar = GetIconChar(icon),
            IconSize = 28,
            Size = new Size(32, 32),
            BackColor = Color.Transparent,
            Location = new Point(22, 24),
            Visible = icon != ThemedMessageIcon.None,
        };
        body.Controls.Add(iconBox);

        var lblMessage = new Label
        {
            Text = message,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft,
            ForeColor = theme.ForeColor,
            Font = new Font("Segoe UI", 9.75F, FontStyle.Regular),
            Location = new Point(icon == ThemedMessageIcon.None ? 22 : 64, 18),
            Size = new Size(400, 80),
        };
        body.Controls.Add(lblMessage);

        // ── Buttons ──────────────────────────────────────────────────
        switch (buttons)
        {
            case MessageBoxButtons.OK:
                buttonRow.Controls.Add(MakeButton("OK", DialogResult.OK, primary: true));
                break;
            case MessageBoxButtons.OKCancel:
                buttonRow.Controls.Add(MakeButton("OK", DialogResult.OK, primary: true));
                buttonRow.Controls.Add(MakeButton("Cancel", DialogResult.Cancel));
                break;
            case MessageBoxButtons.YesNo:
                buttonRow.Controls.Add(MakeButton("Yes", DialogResult.Yes, primary: true));
                buttonRow.Controls.Add(MakeButton("No", DialogResult.No));
                break;
            case MessageBoxButtons.YesNoCancel:
                buttonRow.Controls.Add(MakeButton("Yes", DialogResult.Yes, primary: true));
                buttonRow.Controls.Add(MakeButton("No", DialogResult.No));
                buttonRow.Controls.Add(MakeButton("Cancel", DialogResult.Cancel));
                break;
            default:
                buttonRow.Controls.Add(MakeButton("OK", DialogResult.OK, primary: true));
                break;
        }

        Controls.Add(body);
        Controls.Add(footerHair);
        Controls.Add(footer);
        Controls.Add(titleHair);
        Controls.Add(titleBar);

        // Size to fit content.
        using (var g = CreateGraphics())
        {
            var measured = TextRenderer.MeasureText(g, message, lblMessage.Font, new Size(560, 0), TextFormatFlags.WordBreak);
            int width = Math.Max(400, Math.Min(620, measured.Width + (icon == ThemedMessageIcon.None ? 44 : 90) + 40));
            int height = Math.Max(160, measured.Height + 40 + 64 + 56);
            ClientSize = new Size(width, height);
            lblMessage.Size = new Size(width - (icon == ThemedMessageIcon.None ? 44 : 90), height - 40 - 64 - 30);
        }

        // Outer 1px border so the borderless form has a defined edge.
        Paint += (_, e) =>
        {
            using var pen = new Pen(theme.Shadow, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
        };

        // Drag-to-move on the title bar.
        Drag.Attach(this, titleBar);
        Drag.Attach(this, lblTitle);

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape && buttons != MessageBoxButtons.OK)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                DialogResult = buttons == MessageBoxButtons.YesNo || buttons == MessageBoxButtons.YesNoCancel
                    ? DialogResult.Yes
                    : DialogResult.OK;
                Close();
            }
        };

        AcceptButton = (Button?)buttonRow.Controls[0];
    }

    private static Button MakeButton(string text, DialogResult result, bool primary = false)
    {
        var theme = ThemeManager.GetCurrentColors() ?? ThemeManager.ThemePresets["Graphite"];
        var btn = new Button
        {
            Text = text,
            DialogResult = result,
            Size = new Size(96, 32),
            Margin = new Padding(8, 0, 0, 0),
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            Cursor = Cursors.Hand,
            UseVisualStyleBackColor = false,
        };

        if (primary)
        {
            btn.BackColor = theme.Accent;
            btn.ForeColor = Color.FromArgb(15, 16, 19);
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = ControlPaint.Light(theme.Accent, 0.1f);
            btn.FlatAppearance.MouseDownBackColor = ControlPaint.Dark(theme.Accent, 0.05f);
        }
        else
        {
            btn.BackColor = theme.Hover;
            btn.ForeColor = theme.ForeColor;
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = theme.Shadow;
            btn.FlatAppearance.MouseOverBackColor = theme.Shadow;
            btn.FlatAppearance.MouseDownBackColor = theme.Shadow;
        }
        return btn;
    }

    private static IconChar GetIconChar(ThemedMessageIcon icon) => icon switch
    {
        ThemedMessageIcon.Info => IconChar.InfoCircle,
        ThemedMessageIcon.Warning => IconChar.ExclamationTriangle,
        ThemedMessageIcon.Error => IconChar.TimesCircle,
        ThemedMessageIcon.Question => IconChar.QuestionCircle,
        ThemedMessageIcon.Success => IconChar.CheckCircle,
        _ => IconChar.None,
    };

    private static Color GetIconColor(ThemedMessageIcon icon, ThemeColors theme) => icon switch
    {
        ThemedMessageIcon.Info => theme.Accent,
        ThemedMessageIcon.Warning => Color.FromArgb(251, 191, 36),
        ThemedMessageIcon.Error => Color.FromArgb(248, 113, 113),
        ThemedMessageIcon.Question => theme.Accent,
        ThemedMessageIcon.Success => Color.FromArgb(74, 222, 128),
        _ => theme.Muted,
    };

    // Small drag helper so the borderless ThemedMessageBox can be moved by its title bar.
    private static class Drag
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public static void Attach(Form form, Control handle)
        {
            handle.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                ReleaseCapture();
                SendMessage(form.Handle, 0xA1, 0x2, 0);
            };
        }
    }
}
