using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms.Controls;

// Minimalist single-line text input that matches the FlatComboBox / FlatNumericUpDown
// styling. Implemented as a Panel composite around a borderless TextBox because the
// native TextBox border can't be recolored — WM_NCPAINT is repainted by the theme
// service immediately after. Wrapping it lets us own the border completely.
//
// The inner TextBox is exposed via Inner / IPBox-style consumers can read it directly,
// while Text / TextChanged / KeyDown are forwarded so most call sites need no change.
public sealed class FlatTextBox : Panel
{
    private readonly TextBox _inner;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Inner => _inner;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.FromArgb(36, 38, 42);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FocusBorderColor { get; set; } = Color.FromArgb(96, 165, 250);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color HoverBorderColor { get; set; } = Color.FromArgb(60, 64, 70);

    public override string Text
    {
        get => _inner.Text;
        set => _inner.Text = value;
    }

    public override Font Font
    {
        get => _inner.Font;
        set { _inner.Font = value; base.Font = value; }
    }

    public override Color ForeColor
    {
        get => _inner.ForeColor;
        set { _inner.ForeColor = value; base.ForeColor = value; }
    }

    private bool _hover;

    public FlatTextBox()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);

        BackColor = Color.FromArgb(30, 32, 36);
        Padding = new Padding(8, 5, 8, 5);

        _inner = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = BackColor,
            ForeColor = Color.FromArgb(232, 234, 238),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
        };
        Controls.Add(_inner);

        _inner.GotFocus += (_, _) => Invalidate();
        _inner.LostFocus += (_, _) => Invalidate();
        _inner.MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        _inner.MouseLeave += (_, _) => { _hover = false; Invalidate(); };
        MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        MouseLeave += (_, _) => { _hover = false; Invalidate(); };
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        var colors = ThemeManager.CurrentColors;
        Color border = _inner.Focused ? colors.Accent : (_hover ? ThemeManager.Shade(colors.Shadow, 0.24f) : colors.Shadow);
        using var pen = new Pen(border, 1);
        g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        if (_inner != null) _inner.BackColor = BackColor;
    }

    // Forward common events for source-compatibility with callers that handled them on the TextBox.
    public new event EventHandler? TextChanged
    {
        add => _inner.TextChanged += value;
        remove => _inner.TextChanged -= value;
    }

    public new event KeyEventHandler? KeyDown
    {
        add => _inner.KeyDown += value;
        remove => _inner.KeyDown -= value;
    }

    // Click on the border passes focus to the inner editor so users can click anywhere.
    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        _inner.Focus();
    }
}
