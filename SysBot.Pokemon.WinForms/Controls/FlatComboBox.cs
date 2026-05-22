using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms.Controls;

// Minimalist ComboBox that hides the legacy Windows dropdown button and renders
// its own chevron, theme-aware background, hairline border, and themed list items.
// DropDownStyle is forced to DropDownList (no inline edit), matching every usage
// in this codebase. The actual selected-text rendering is left to the base control
// so accessibility, localization and IME handling continue to work — we just paint
// the chrome on top of it.
public sealed class FlatComboBox : ComboBox
{
    private const int WM_PAINT = 0x000F;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.FromArgb(36, 38, 42);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ArrowColor { get; set; } = Color.FromArgb(180, 184, 192);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FocusBorderColor { get; set; } = Color.FromArgb(96, 165, 250);

    private bool _hover;
    private bool _focused;

    public FlatComboBox()
    {
        DropDownStyle = ComboBoxStyle.DropDownList;
        FlatStyle = FlatStyle.Flat;
        DrawMode = DrawMode.OwnerDrawFixed;
        ItemHeight = 22;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular);
        BackColor = Color.FromArgb(30, 32, 36);
        ForeColor = Color.FromArgb(232, 234, 238);
        DrawItem += OnDrawItem;
        MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        MouseLeave += (_, _) => { _hover = false; Invalidate(); };
        GotFocus += (_, _) => { _focused = true; Invalidate(); };
        LostFocus += (_, _) => { _focused = false; Invalidate(); };
    }

    private void OnDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (e.Index < 0) return;

        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        Color bg = selected ? Color.FromArgb(36, 38, 42) : Color.FromArgb(22, 23, 26);
        Color fg = Color.FromArgb(232, 234, 238);

        using (var b = new SolidBrush(bg))
            e.Graphics.FillRectangle(b, e.Bounds);

        // Subtle accent strip on the selected row.
        if (selected)
        {
            using var accent = new SolidBrush(Color.FromArgb(96, 165, 250));
            e.Graphics.FillRectangle(accent, e.Bounds.X, e.Bounds.Y, 2, e.Bounds.Height);
        }

        string text = GetItemText(Items[e.Index]) ?? string.Empty;
        var textRect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 14, e.Bounds.Height);
        TextRenderer.DrawText(e.Graphics, text, e.Font ?? Font, textRect, fg,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);
        if (m.Msg == WM_PAINT)
        {
            using var g = Graphics.FromHwnd(Handle);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            PaintChrome(g);
        }
    }

    private void PaintChrome(Graphics g)
    {
        int arrowAreaWidth = 22;
        var arrowRect = new Rectangle(Width - arrowAreaWidth, 0, arrowAreaWidth, Height);

        // Cover the legacy dropdown button with the theme background so it never peeks through.
        using (var bg = new SolidBrush(BackColor))
            g.FillRectangle(bg, arrowRect);

        // Draw a chevron centered in the arrow zone.
        int cx = arrowRect.X + arrowRect.Width / 2;
        int cy = arrowRect.Y + arrowRect.Height / 2;
        using (var pen = new Pen(ArrowColor, 1.5f) { StartCap = LineCap.Round, EndCap = LineCap.Round })
        {
            g.DrawLine(pen, cx - 4, cy - 2, cx, cy + 2);
            g.DrawLine(pen, cx, cy + 2, cx + 4, cy - 2);
        }

        // Outer hairline border. Slightly brighter on focus/hover.
        Color border = _focused ? FocusBorderColor : (_hover ? Color.FromArgb(60, 64, 70) : BorderColor);
        using (var pen = new Pen(border, 1))
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }
}
