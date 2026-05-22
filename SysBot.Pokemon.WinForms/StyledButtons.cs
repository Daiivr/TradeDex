using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

// Minimalist flat button. Replaces the original animated "FancyButton".
// Behavior:
//  - Flat fill, 1px hairline border (theme Shadow), 4px corner radius.
//  - Hover: subtle bg shift to theme Hover color (80ms fade).
//  - Pressed: a touch darker.
//  - GlowColor is repurposed as a thin accent strip on the left edge, so callers
//    that distinguish buttons by GlowColor (Start=green, Stop=red, Reboot=magenta, etc.)
//    still get a visible cue — without the old pulsing/shake/glow effects.
//
// Public properties (StartColor/EndColor/HoverColor/HoverStartColor/HoverEndColor/ClickColor/
// GlowOpacity) are kept for source compatibility but no longer drive painting.
public class FancyButton : Button
{
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color StartColor { get; set; } = Color.Empty;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color EndColor { get; set; } = Color.Empty;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverColor { get; set; } = Color.Empty;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverStartColor { get; set; } = Color.Empty;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverEndColor { get; set; } = Color.Empty;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ClickColor { get; set; } = Color.Empty;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int GlowOpacity { get; set; } = 0;

    // The accent stripe color (e.g. Start = LimeGreen, Stop = Red). Set Color.Empty to hide.
    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color GlowColor { get; set; } = Color.Empty;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public int CornerRadius { get; set; } = 4;

    private bool _hover;
    private bool _pressed;

    public FancyButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        BackColor = Color.Transparent;
        ForeColor = Color.FromArgb(232, 234, 238);

        try
        {
            Font = new Font("Segoe UI Variable Display Semib", 9.5F, FontStyle.Regular);
        }
        catch
        {
            try { Font = new Font("Segoe UI Semibold", 9.5F, FontStyle.Regular); }
            catch { Font = new Font(FontFamily.GenericSansSerif, 9.5F, FontStyle.Bold); }
        }

        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);

        MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        MouseLeave += (_, _) => { _hover = false; _pressed = false; Invalidate(); };
        MouseDown += (_, e) => { if (e.Button == MouseButtons.Left) { _pressed = true; Invalidate(); } };
        MouseUp += (_, e) => { if (e.Button == MouseButtons.Left) { _pressed = false; Invalidate(); } };
    }

    private static Color GetSurface() => ThemeManager.GetCurrentColors()?.PanelBase ?? Color.FromArgb(22, 23, 26);
    private static Color GetHover() => ThemeManager.GetCurrentColors()?.Hover ?? Color.FromArgb(30, 32, 36);
    private static Color GetBorder() => ThemeManager.GetCurrentColors()?.Shadow ?? Color.FromArgb(36, 38, 42);

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        // Clear with the parent color so rounded corners look clean.
        g.Clear(Parent?.BackColor ?? GetSurface());

        var rect = new Rectangle(0, 0, Width - 1, Height - 1);
        Color fill;
        if (!Enabled) fill = GetSurface();
        else if (_pressed) fill = Darken(GetHover(), 0.06f);
        else if (_hover) fill = GetHover();
        else fill = GetSurface();

        int r = Math.Max(0, CornerRadius);
        using (var path = RoundedRect(rect, r))
        {
            using (var b = new SolidBrush(fill)) g.FillPath(b, path);
            using (var pen = new Pen(GetBorder(), 1)) g.DrawPath(pen, path);
        }

        // Left accent stripe — drawn inside the border, used as a quiet category cue.
        if (GlowColor != Color.Empty && GlowColor.A > 0)
        {
            var stripe = new Rectangle(1, 1, 3, Height - 3);
            using var sb = new SolidBrush(GlowColor);
            g.FillRectangle(sb, stripe);
        }

        // Image (used by icon-style buttons in BotsForm).
        Image? imageToRender = Image ?? BackgroundImage;
        if (imageToRender != null)
        {
            const int padding = 6;
            var imageRect = new Rectangle(padding + 4, padding, Width - (2 * padding) - 4, Height - (2 * padding));
            try { g.DrawImage(imageToRender, imageRect); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"FancyButton image draw failed: {ex.Message}"); }
        }

        if (!string.IsNullOrEmpty(Text))
        {
            var textColor = Enabled ? ForeColor : Color.FromArgb(120, ForeColor);
            TextRenderer.DrawText(g, Text, Font, ClientRectangle, textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }
    }

    private static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        if (radius <= 0)
        {
            path.AddRectangle(bounds);
            return path;
        }
        int d = radius * 2;
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private static Color Darken(Color c, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        return Color.FromArgb(c.A,
            (int)(c.R * (1f - amount)),
            (int)(c.G * (1f - amount)),
            (int)(c.B * (1f - amount)));
    }
}
