using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms.Controls;

// Minimalist numeric input. Implemented as a Panel + plain TextBox instead of wrapping
// .NET's NumericUpDown, because the latter's UpDownBase parent reserves an internal
// "spinner area" that paints a dark rectangle next to the digits — even when the native
// spinner is hidden — and overriding the relevant clamps requires non-virtual members.
// We re-implement Value / Minimum / Maximum / increment / decrement manually here so the
// public surface that callers expect (Value as decimal, clamped to Min/Max) is preserved.
public sealed class FlatNumericUpDown : Panel
{
    private const int ArrowZoneWidth = 18;

    private readonly TextBox _editor;
    private decimal _value;
    private decimal _minimum;
    private decimal _maximum = 100m;
    private decimal _increment = 1m;
    private bool _syncing;

    public event EventHandler? ValueChanged;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public TextBox Inner => _editor;

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color BorderColor { get; set; } = Color.FromArgb(36, 38, 42);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color FocusBorderColor { get; set; } = Color.FromArgb(96, 165, 250);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ChevronColor { get; set; } = Color.FromArgb(180, 184, 192);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Color ChevronHoverColor { get; set; } = Color.FromArgb(232, 234, 238);

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Value
    {
        get => _value;
        set
        {
            var clamped = Clamp(value);
            if (clamped == _value) { SyncTextFromValue(); return; }
            _value = clamped;
            SyncTextFromValue();
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Minimum
    {
        get => _minimum;
        set
        {
            _minimum = value;
            if (_minimum > _maximum) _maximum = _minimum;
            Value = Clamp(_value);
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Maximum
    {
        get => _maximum;
        set
        {
            _maximum = value;
            if (_maximum < _minimum) _minimum = _maximum;
            Value = Clamp(_value);
        }
    }

    [Browsable(false)]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public decimal Increment
    {
        get => _increment;
        set => _increment = value <= 0 ? 1m : value;
    }

    private bool _hover;
    private bool _hoverUp;
    private bool _hoverDown;

    public FlatNumericUpDown()
    {
        DoubleBuffered = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.ResizeRedraw, true);

        BackColor = Color.FromArgb(30, 32, 36);
        Padding = new Padding(0);

        _editor = new TextBox
        {
            BorderStyle = BorderStyle.None,
            BackColor = BackColor,
            ForeColor = Color.FromArgb(232, 234, 238),
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            TextAlign = HorizontalAlignment.Left,
            Margin = new Padding(0),
        };
        Controls.Add(_editor);

        _editor.GotFocus += (_, _) => Invalidate();
        _editor.LostFocus += (_, _) => { CommitTextToValue(); Invalidate(); };
        _editor.KeyDown += OnEditorKeyDown;
        _editor.KeyPress += OnEditorKeyPress;
        _editor.TextChanged += OnEditorTextChanged;
        _editor.MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        _editor.MouseLeave += (_, _) => { _hover = false; Invalidate(); };
        MouseEnter += (_, _) => { _hover = true; Invalidate(); };
        MouseLeave += (_, _) =>
        {
            _hover = false; _hoverUp = false; _hoverDown = false;
            Invalidate();
        };

        SyncTextFromValue();
    }

    protected override void OnResize(EventArgs eventargs)
    {
        base.OnResize(eventargs);
        LayoutEditor();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        LayoutEditor();
    }

    private void LayoutEditor()
    {
        if (_editor == null) return;
        int textHeight = TextRenderer.MeasureText("0", _editor.Font).Height;
        int y = Math.Max(0, (Height - textHeight) / 2);
        _editor.Location = new Point(8, y);
        _editor.Size = new Size(Math.Max(0, Width - ArrowZoneWidth - 12), textHeight);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        Color border = _editor.Focused ? FocusBorderColor : (_hover ? Color.FromArgb(60, 64, 70) : BorderColor);
        using (var pen = new Pen(border, 1))
            g.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);

        var arrowZone = new Rectangle(Width - ArrowZoneWidth - 1, 1, ArrowZoneWidth, Height - 2);
        using (var bg = new SolidBrush(BackColor))
            g.FillRectangle(bg, arrowZone);

        int cx = arrowZone.X + arrowZone.Width / 2;
        int upCy = arrowZone.Y + arrowZone.Height / 4;
        int dnCy = arrowZone.Y + arrowZone.Height * 3 / 4;

        DrawChevron(g, cx, upCy, isUp: true, _hoverUp ? ChevronHoverColor : ChevronColor);
        DrawChevron(g, cx, dnCy, isUp: false, _hoverDown ? ChevronHoverColor : ChevronColor);
    }

    private static void DrawChevron(Graphics g, int cx, int cy, bool isUp, Color color)
    {
        using var pen = new Pen(color, 1.4f) { StartCap = LineCap.Round, EndCap = LineCap.Round };
        if (isUp)
        {
            g.DrawLine(pen, cx - 3, cy + 1, cx, cy - 2);
            g.DrawLine(pen, cx, cy - 2, cx + 3, cy + 1);
        }
        else
        {
            g.DrawLine(pen, cx - 3, cy - 1, cx, cy + 2);
            g.DrawLine(pen, cx, cy + 2, cx + 3, cy - 1);
        }
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var rect = ArrowRect();
        if (rect.Contains(e.Location))
        {
            int splitY = rect.Y + rect.Height / 2;
            bool up = e.Y < splitY;
            if (up != _hoverUp || (!up) != _hoverDown)
            {
                _hoverUp = up;
                _hoverDown = !up;
                Invalidate();
            }
        }
        else if (_hoverUp || _hoverDown)
        {
            _hoverUp = _hoverDown = false;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        var rect = ArrowRect();
        if (e.Button == MouseButtons.Left && rect.Contains(e.Location))
        {
            int splitY = rect.Y + rect.Height / 2;
            if (e.Y < splitY) Increment_Value(); else Decrement_Value();
            _editor.Focus();
            return;
        }
        base.OnMouseDown(e);
        _editor.Focus();
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Up) { Increment_Value(); e.Handled = true; e.SuppressKeyPress = true; }
        else if (e.KeyCode == Keys.Down) { Decrement_Value(); e.Handled = true; e.SuppressKeyPress = true; }
        else if (e.KeyCode == Keys.Enter) { CommitTextToValue(); e.Handled = true; e.SuppressKeyPress = true; }
    }

    private void OnEditorKeyPress(object? sender, KeyPressEventArgs e)
    {
        // Allow control characters (backspace, etc.), digits, and a leading minus when
        // negative values are valid.
        if (char.IsControl(e.KeyChar)) return;
        if (char.IsDigit(e.KeyChar)) return;
        if (e.KeyChar == '-' && Minimum < 0 && _editor.SelectionStart == 0 && !_editor.Text.Contains('-')) return;
        e.Handled = true;
    }

    private void OnEditorTextChanged(object? sender, EventArgs e)
    {
        if (_syncing) return;
        // Don't fully commit here (lets the user type freely), but parse-best-effort so
        // bound consumers see the in-progress value if they peek before blur.
        if (decimal.TryParse(_editor.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
        {
            var clamped = Clamp(v);
            if (clamped != _value)
            {
                _value = clamped;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void CommitTextToValue()
    {
        if (decimal.TryParse(_editor.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
            Value = v;
        else
            SyncTextFromValue();
    }

    private void Increment_Value()
    {
        Value = _value + _increment;
        _editor.SelectionStart = _editor.Text.Length;
    }

    private void Decrement_Value()
    {
        Value = _value - _increment;
        _editor.SelectionStart = _editor.Text.Length;
    }

    private void SyncTextFromValue()
    {
        if (_editor == null) return;
        _syncing = true;
        _editor.Text = _value.ToString(CultureInfo.InvariantCulture);
        _syncing = false;
    }

    private decimal Clamp(decimal v)
    {
        if (v < _minimum) return _minimum;
        if (v > _maximum) return _maximum;
        return v;
    }

    protected override void OnBackColorChanged(EventArgs e)
    {
        base.OnBackColorChanged(e);
        if (_editor != null) _editor.BackColor = BackColor;
    }

    private Rectangle ArrowRect()
        => new Rectangle(Width - ArrowZoneWidth - 1, 1, ArrowZoneWidth, Height - 2);
}
