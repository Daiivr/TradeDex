using SysBot.Pokemon.WinForms.Helpers;
using PKHeX.Core;
using PKHeX.Core.AutoMod;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms;

internal static class ThemedCollectionEditors
{
    private static bool _registered;

    public static void Register()
    {
        if (_registered)
            return;

        _registered = true;
        RegisterEditor<List<Badge>, ThemedBadgeCollectionEditor>();
        RegisterEditor<List<LeagueEmoji>, ThemedLeagueEmojiCollectionEditor>();
        RegisterEditor<List<TradeSettings.MoveTypeEmojiInfo>, ThemedMoveTypeEmojiCollectionEditor>();
        RegisterEditor<List<TradeSettings.TeraTypeEmojiInfo>, ThemedTeraTypeEmojiCollectionEditor>();
        RegisterEditor<List<EncounterTypeGroup>, ThemedEncounterPriorityCollectionEditor>();
        RegisterEditor<List<GameVersion>, ThemedGameVersionPriorityCollectionEditor>();
    }

    private static void RegisterEditor<TCollection, TEditor>()
        where TEditor : UITypeEditor
    {
        TypeDescriptor.AddAttributes(typeof(TCollection), new EditorAttribute(typeof(TEditor), typeof(UITypeEditor)));
    }
}

internal sealed class ThemedBadgeCollectionEditor : ThemedCollectionEditor
{
    public ThemedBadgeCollectionEditor() : base(typeof(List<Badge>), "Editor de insignias")
    {
    }
}

internal sealed class ThemedLeagueEmojiCollectionEditor : ThemedCollectionEditor
{
    public ThemedLeagueEmojiCollectionEditor() : base(typeof(List<LeagueEmoji>), "Editor de ligas")
    {
    }
}

internal sealed class ThemedMoveTypeEmojiCollectionEditor : ThemedCollectionEditor
{
    public ThemedMoveTypeEmojiCollectionEditor() : base(typeof(List<TradeSettings.MoveTypeEmojiInfo>), "Editor de emojis de tipos")
    {
    }
}

internal sealed class ThemedTeraTypeEmojiCollectionEditor : ThemedCollectionEditor
{
    public ThemedTeraTypeEmojiCollectionEditor() : base(typeof(List<TradeSettings.TeraTypeEmojiInfo>), "Editor de emojis tera")
    {
    }
}

internal sealed class ThemedEncounterPriorityCollectionEditor : ThemedCollectionEditor
{
    public ThemedEncounterPriorityCollectionEditor() : base(typeof(List<EncounterTypeGroup>), "Editor de prioridad de encuentros")
    {
    }
}

internal sealed class ThemedGameVersionPriorityCollectionEditor : ThemedCollectionEditor
{
    public ThemedGameVersionPriorityCollectionEditor() : base(typeof(List<GameVersion>), "Editor de prioridad de versiones")
    {
    }
}

internal abstract class ThemedCollectionEditor : CollectionEditor
{
    private const int TitleBarHeight = 40;
    private static readonly ConditionalWeakTable<Form, object?> BorderlessForms = new();
    private static readonly ConditionalWeakTable<ListBox, FlatHorizontalScrollBar> ListScrollBars = new();
    private readonly string title;

    protected ThemedCollectionEditor(Type type, string title) : base(type)
    {
        this.title = title;
    }

    protected override CollectionForm CreateCollectionForm()
    {
        var form = base.CreateCollectionForm();
        form.Text = title;
        form.HandleCreated += (_, _) => ApplyTheme(form);
        form.Shown += (_, _) => ApplyTheme(form);
        return form;
    }

    private static void ApplyTheme(Form form)
    {
        var colors = ThemeManager.GetCurrentColors() ?? ThemeManager.ThemePresets["Graphite"];
        var surface = colors.PanelBase;
        var field = colors.ControlBackground;
        var border = colors.Border;

        MakeBorderless(form, colors);
        form.BackColor = surface;
        form.ForeColor = colors.ForeColor;
        form.Font = SystemFonts.MessageBoxFont;

        foreach (Control control in GetControlTree(form))
            ApplyControlTheme(control, colors, surface, field, border);

        DarkScrollHelper.ApplyScrollBarsRecursive(form);
        form.ControlAdded -= FormControlAdded;
        form.ControlAdded += FormControlAdded;
    }

    private static void ApplyControlTheme(Control control, ThemeColors colors, Color surface, Color field, Color border)
    {
        control.ForeColor = colors.ForeColor;

        switch (control)
        {
            case PropertyGrid grid:
                ApplyPropertyGridTheme(grid, colors, surface, field, border);
                break;
            case Button button:
                ApplyButtonTheme(button, colors, field, border);
                break;
            case TextBoxBase text:
                text.BackColor = field;
                text.ForeColor = colors.ForeColor;
                text.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ListBox list:
                list.BackColor = field;
                list.ForeColor = colors.ForeColor;
                list.BorderStyle = BorderStyle.None;
                ApplyListBoxTheme(list, colors);
                break;
            case ListView listView:
                listView.BackColor = field;
                listView.ForeColor = colors.ForeColor;
                listView.BorderStyle = BorderStyle.FixedSingle;
                break;
            case TreeView tree:
                tree.BackColor = field;
                tree.ForeColor = colors.ForeColor;
                tree.BorderStyle = BorderStyle.FixedSingle;
                break;
            case ComboBox combo:
                combo.BackColor = field;
                combo.ForeColor = colors.ForeColor;
                combo.FlatStyle = FlatStyle.Flat;
                break;
            case ToolStrip strip:
                strip.BackColor = surface;
                strip.ForeColor = colors.ForeColor;
                strip.Renderer = new ThemedToolStripRenderer(colors);
                break;
            case Panel or TableLayoutPanel or FlowLayoutPanel or GroupBox or SplitContainer:
                control.BackColor = surface;
                break;
            default:
                if (control.GetType().Name.Contains("GridView", StringComparison.OrdinalIgnoreCase))
                    control.BackColor = field;
                else if (control is not Label)
                    control.BackColor = surface;
                break;
        }

        control.ControlAdded -= ControlAdded;
        control.ControlAdded += ControlAdded;
    }

    private static void MakeBorderless(Form form, ThemeColors colors)
    {
        if (BorderlessForms.TryGetValue(form, out _))
            return;

        BorderlessForms.Add(form, null);
        var existingControls = form.Controls.Cast<Control>().ToArray();
        form.SuspendLayout();
        form.FormBorderStyle = FormBorderStyle.None;
        form.Padding = new Padding(1);
        form.ClientSize = new Size(form.ClientSize.Width, form.ClientSize.Height + TitleBarHeight + 1);
        form.Paint += (_, e) =>
        {
            using var pen = new Pen(colors.Accent, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, form.Width - 1, form.Height - 1);
        };

        foreach (var control in existingControls)
            control.Top += TitleBarHeight + 1;

        var titleBar = new Panel
        {
            Name = "TradeDexCollectionEditorTitleBar",
            Dock = DockStyle.Top,
            Height = TitleBarHeight,
            BackColor = colors.PanelBase,
        };

        var titleLabel = new Label
        {
            AutoSize = false,
            Text = form.Text,
            TextAlign = ContentAlignment.MiddleLeft,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular),
            ForeColor = colors.ForeColor,
            Location = new Point(18, 0),
            Size = new Size(Math.Max(120, form.ClientSize.Width - 72), TitleBarHeight),
        };
        titleBar.Controls.Add(titleLabel);

        var closeButton = new Button
        {
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Text = "x",
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.Transparent,
            ForeColor = colors.ForeColor,
            Size = new Size(40, TitleBarHeight),
            Cursor = Cursors.Hand,
            TabStop = false,
        };
        closeButton.FlatAppearance.BorderSize = 0;
        closeButton.FlatAppearance.MouseOverBackColor = colors.Hover;
        closeButton.FlatAppearance.MouseDownBackColor = colors.Shadow;
        closeButton.Click += (_, _) =>
        {
            form.DialogResult = DialogResult.Cancel;
            form.Close();
        };
        titleBar.Controls.Add(closeButton);

        var titleHairline = new Panel
        {
            Dock = DockStyle.Top,
            Height = 1,
            BackColor = colors.Border,
        };

        void ResizeTitleControls()
        {
            closeButton.Location = new Point(titleBar.Width - closeButton.Width, 0);
            titleLabel.Size = new Size(Math.Max(120, closeButton.Left - 18), TitleBarHeight);
        }

        titleBar.Resize += (_, _) => ResizeTitleControls();
        ResizeTitleControls();

        DragHelper.Attach(form, titleBar);
        DragHelper.Attach(form, titleLabel);
        form.Controls.Add(titleHairline);
        form.Controls.Add(titleBar);
        titleBar.BringToFront();
        titleHairline.BringToFront();
        form.ResumeLayout(false);
    }

    private static void ApplyListBoxTheme(ListBox list, ThemeColors colors)
    {
        if (!ListScrollBars.TryGetValue(list, out var scrollBar))
        {
            EnableDoubleBuffering(list);
            scrollBar = new FlatHorizontalScrollBar();
            scrollBar.ValueChanged += (_, _) => ScrollListHorizontally(list, scrollBar.Value);
            list.ParentChanged += (_, _) => UpdateListScrollBar(list, scrollBar, colors);
            list.LocationChanged += (_, _) => UpdateListScrollBar(list, scrollBar, colors);
            list.SizeChanged += (_, _) => UpdateListScrollBar(list, scrollBar, colors);
            list.VisibleChanged += (_, _) => UpdateListScrollBar(list, scrollBar, colors);
            list.HandleCreated += (_, _) => UpdateListScrollBar(list, scrollBar, colors);
            list.Disposed += (_, _) => scrollBar.Dispose();
            ListScrollBars.Add(list, scrollBar);
        }

        list.BeginUpdate();
        list.DrawMode = DrawMode.Normal;
        list.HorizontalScrollbar = true;
        UpdateListHorizontalExtent(list);
        list.EndUpdate();
        UpdateListScrollBar(list, scrollBar, colors);
        DarkScrollHelper.Apply(list);
    }

    private static void AttachListScrollBar(ListBox list, FlatHorizontalScrollBar scrollBar)
    {
        if (list.FindForm() is not { } form || scrollBar.IsDisposed)
            return;

        if (scrollBar.Parent != form)
        {
            scrollBar.Parent?.Controls.Remove(scrollBar);
            form.Controls.Add(scrollBar);
        }

        if (!ReferenceEquals(scrollBar.Tag, form))
        {
            scrollBar.Tag = form;
            form.Resize += (_, _) => UpdateListScrollBar(list, scrollBar, ThemeManager.GetCurrentColors() ?? ThemeManager.ThemePresets["Graphite"]);
            form.Layout += (_, _) => UpdateListScrollBar(list, scrollBar, ThemeManager.GetCurrentColors() ?? ThemeManager.ThemePresets["Graphite"]);
        }

        scrollBar.BringToFront();
    }

    private static void UpdateListScrollBar(ListBox list, FlatHorizontalScrollBar scrollBar, ThemeColors colors)
    {
        if (list.IsDisposed || scrollBar.IsDisposed || list.Parent is null || !list.IsHandleCreated || list.FindForm() is not { } form)
            return;

        AttachListScrollBar(list, scrollBar);
        var height = Math.Max(14, SystemInformation.HorizontalScrollBarHeight);
        var listBottomLeft = form.PointToClient(list.PointToScreen(new Point(0, list.Height - height)));
        scrollBar.Bounds = new Rectangle(listBottomLeft.X, listBottomLeft.Y, list.Width, height);
        scrollBar.Configure(
            ThemeManager.Shade(colors.ControlBackground, 0.03f),
            ThemeManager.Shade(colors.ControlBackground, 0.22f),
            ThemeManager.Shade(colors.ControlBackground, 0.34f));

        var visibleWidth = Math.Max(1, list.ClientSize.Width);
        var maximum = Math.Max(0, list.HorizontalExtent - visibleWidth);
        scrollBar.SetScrollRange(maximum, visibleWidth);
        scrollBar.Visible = list.Visible && maximum > 0;
        scrollBar.BringToFront();
    }

    private static void ScrollListHorizontally(ListBox list, int value)
    {
        if (list.IsDisposed || !list.IsHandleCreated)
            return;

        SetScrollPos(list.Handle, NativeHorizontalScrollBar, value, true);
        SendMessage(list.Handle, WindowMessageHorizontalScroll, MakeWParam(ScrollBarThumbPosition, value), IntPtr.Zero);
        list.Invalidate();
    }

    private static void UpdateListHorizontalExtent(ListBox list)
    {
        var maxWidth = 0;
        using var graphics = list.CreateGraphics();
        foreach (var item in list.Items)
        {
            var text = list.GetItemText(item);
            var measured = TextRenderer.MeasureText(graphics, text, list.Font, new Size(int.MaxValue, list.ItemHeight), TextFormatFlags.SingleLine);
            maxWidth = Math.Max(maxWidth, measured.Width + 16);
        }

        list.HorizontalExtent = Math.Max(list.ClientSize.Width, maxWidth);
    }

    private static void EnableDoubleBuffering(Control control)
    {
        try
        {
            typeof(Control)
                .GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(control, true);
        }
        catch
        {
            // Best effort. Native controls can still work without this.
        }
    }

    private static void FormControlAdded(object? sender, ControlEventArgs e) => ControlAdded(sender, e);

    private static void ControlAdded(object? sender, ControlEventArgs e)
    {
        if (e.Control is not { } control)
            return;

        var colors = ThemeManager.GetCurrentColors() ?? ThemeManager.ThemePresets["Graphite"];
        ApplyControlTheme(control, colors, colors.PanelBase, colors.ControlBackground, colors.Border);
        foreach (Control child in control.Controls)
            ApplyControlTheme(child, colors, colors.PanelBase, colors.ControlBackground, colors.Border);
    }

    private static void ApplyPropertyGridTheme(PropertyGrid grid, ThemeColors colors, Color surface, Color field, Color border)
    {
        grid.BackColor = field;
        grid.ForeColor = colors.ForeColor;
        grid.ViewBackColor = field;
        grid.ViewForeColor = colors.ForeColor;
        grid.CategoryForeColor = colors.Accent;
        grid.CommandsBackColor = surface;
        grid.CommandsForeColor = colors.ForeColor;
        grid.HelpBackColor = surface;
        grid.HelpForeColor = colors.Muted;
        grid.LineColor = border;
        grid.DisabledItemForeColor = colors.Muted;
        grid.SelectedItemWithFocusBackColor = colors.Accent;
        grid.SelectedItemWithFocusForeColor = Color.White;
        grid.ToolbarVisible = true;
        grid.HelpVisible = false;
        grid.PropertySort = PropertySort.Categorized;
        TrySetColorProperty(grid, "ViewBorderColor", field);
        FlattenPropertyGridBorders(grid);
        ThemePropertyGridToolStrips(grid, colors);
        DarkScrollHelper.ApplyScrollBarsRecursive(grid);
    }

    private static void FlattenPropertyGridBorders(Control root)
    {
        foreach (Control child in root.Controls)
        {
            TrySetBorderStyle(child, BorderStyle.None);
            FlattenPropertyGridBorders(child);
        }
    }

    private static void TrySetBorderStyle(Control control, BorderStyle borderStyle)
    {
        try
        {
            var property = control.GetType().GetProperty("BorderStyle", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.CanWrite == true && property.PropertyType == typeof(BorderStyle))
                property.SetValue(control, borderStyle);
        }
        catch
        {
            // Internal PropertyGrid controls vary by runtime; keep this cosmetic.
        }
    }

    private static void TrySetColorProperty(object target, string propertyName, Color value)
    {
        try
        {
            var property = target.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.CanWrite == true && property.PropertyType == typeof(Color))
                property.SetValue(target, value);
        }
        catch
        {
            // Cosmetic only.
        }
    }

    private static void ThemePropertyGridToolStrips(Control root, ThemeColors colors)
    {
        foreach (Control child in root.Controls)
        {
            if (child is ToolStrip strip)
            {
                strip.Visible = true;
                strip.BackColor = colors.PanelBase;
                strip.ForeColor = colors.ForeColor;
                strip.GripStyle = ToolStripGripStyle.Hidden;
                strip.Margin = Padding.Empty;
                strip.Padding = Padding.Empty;
                strip.Renderer = new ThemedToolStripRenderer(colors);
            }

            ThemePropertyGridToolStrips(child, colors);
        }
    }

    private static void ApplyButtonTheme(Button button, ThemeColors colors, Color field, Color border)
    {
        button.UseVisualStyleBackColor = false;
        button.BackColor = string.Equals(button.Text, "OK", StringComparison.OrdinalIgnoreCase) ? colors.Accent : field;
        button.ForeColor = Color.White;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = border;
        button.FlatAppearance.MouseOverBackColor = ThemeManager.Shade(button.BackColor, 0.08f);
        button.FlatAppearance.MouseDownBackColor = ThemeManager.Shade(button.BackColor, -0.12f);
    }

    private static IEnumerable<Control> GetControlTree(Control root)
    {
        yield return root;

        foreach (Control child in root.Controls)
        {
            foreach (var nested in GetControlTree(child))
                yield return nested;
        }
    }

    private static void EnableDarkTitleBar(Form form)
    {
        if (!form.IsHandleCreated)
            return;

        try
        {
            var enabled = 1;
            if (DwmSetWindowAttribute(form.Handle, DwmWindowAttributeUseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
                DwmSetWindowAttribute(form.Handle, DwmWindowAttributeUseImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));
        }
        catch
        {
            // Cosmetic only. Older Windows builds can ignore this safely.
        }
    }

    private const int DwmWindowAttributeUseImmersiveDarkModeBefore20H1 = 19;
    private const int DwmWindowAttributeUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

    private const int WindowMessageHorizontalScroll = 0x0114;
    private const int NativeHorizontalScrollBar = 0;
    private const int ScrollBarThumbPosition = 4;

    [DllImport("user32.dll")]
    private static extern int SetScrollPos(IntPtr hWnd, int nBar, int nPos, bool bRedraw);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private static IntPtr MakeWParam(int lowWord, int highWord) => (IntPtr)((highWord << 16) | (lowWord & 0xFFFF));

    private static class DragHelper
    {
        [DllImport("user32.dll")]
        private static extern bool ReleaseCapture();

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

        public static void Attach(Form form, Control handle)
        {
            handle.MouseDown += (_, e) =>
            {
                if (e.Button != MouseButtons.Left)
                    return;

                ReleaseCapture();
                SendMessage(form.Handle, 0xA1, 0x2, 0);
            };
        }
    }

    private sealed class FlatHorizontalScrollBar : Control
    {
        private const int MinThumbWidth = 42;
        private Color trackColor = Color.FromArgb(26, 27, 32);
        private Color thumbColor = Color.FromArgb(86, 88, 96);
        private Color hoverThumbColor = Color.FromArgb(106, 108, 118);
        private bool dragging;
        private int dragOffset;
        private int maximum;
        private int viewport = 1;
        private int value;

        public event EventHandler? ValueChanged;

        public FlatHorizontalScrollBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
            TabStop = false;
            Cursor = Cursors.Hand;
        }

        public int Value
        {
            get => value;
            private set
            {
                var next = Math.Max(0, Math.Min(maximum, value));
                if (this.value == next)
                    return;

                this.value = next;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void Configure(Color track, Color thumb, Color hoverThumb)
        {
            trackColor = track;
            thumbColor = thumb;
            hoverThumbColor = hoverThumb;
            BackColor = track;
            Invalidate();
        }

        public void SetScrollRange(int max, int visibleWidth)
        {
            maximum = Math.Max(0, max);
            viewport = Math.Max(1, visibleWidth);
            Value = Math.Min(Value, maximum);
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using var trackBrush = new SolidBrush(trackColor);
            e.Graphics.FillRectangle(trackBrush, ClientRectangle);

            if (maximum <= 0 || Width <= 0)
                return;

            var thumb = GetThumbBounds();
            using var thumbBrush = new SolidBrush(ClientRectangle.Contains(PointToClient(MousePosition)) || dragging ? hoverThumbColor : thumbColor);
            e.Graphics.FillRectangle(thumbBrush, thumb);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button != MouseButtons.Left)
                return;

            var thumb = GetThumbBounds();
            if (thumb.Contains(e.Location))
            {
                dragging = true;
                dragOffset = e.X - thumb.Left;
            }
            else
            {
                Value = ValueFromX(e.X - thumb.Width / 2);
                dragging = true;
                dragOffset = thumb.Width / 2;
            }

            Capture = true;
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (dragging)
                Value = ValueFromX(e.X - dragOffset);
            else
                Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            if (!dragging)
                Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragging = false;
            Capture = false;
            Invalidate();
        }

        private Rectangle GetThumbBounds()
        {
            var thumbWidth = GetThumbWidth();
            var travel = Math.Max(1, Width - thumbWidth);
            var left = maximum <= 0 ? 0 : (int)Math.Round(value / (double)maximum * travel);
            return new Rectangle(left, 3, thumbWidth, Math.Max(6, Height - 6));
        }

        private int GetThumbWidth()
        {
            if (maximum <= 0)
                return Width;

            var total = maximum + viewport;
            var proportional = (int)Math.Round(Width * (viewport / (double)total));
            return Math.Max(MinThumbWidth, Math.Min(Width, proportional));
        }

        private int ValueFromX(int x)
        {
            var thumbWidth = GetThumbWidth();
            var travel = Math.Max(1, Width - thumbWidth);
            var clamped = Math.Max(0, Math.Min(travel, x));
            return (int)Math.Round(clamped / (double)travel * maximum);
        }
    }
}

internal sealed class ThemedToolStripRenderer(ThemeColors colors) : ToolStripProfessionalRenderer(new ThemedToolStripColorTable(colors))
{
    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        // Keep PropertyGrid toolbars visually flush with the dark editor surface.
    }
}

internal sealed class ThemedToolStripColorTable(ThemeColors colors) : ProfessionalColorTable
{
    public override Color ToolStripDropDownBackground => colors.PanelBase;
    public override Color ImageMarginGradientBegin => colors.PanelBase;
    public override Color ImageMarginGradientMiddle => colors.PanelBase;
    public override Color ImageMarginGradientEnd => colors.PanelBase;
    public override Color MenuBorder => colors.Border;
    public override Color MenuItemBorder => colors.Accent;
    public override Color MenuItemSelected => colors.Hover;
    public override Color ButtonSelectedBorder => colors.Accent;
    public override Color ButtonSelectedGradientBegin => colors.Hover;
    public override Color ButtonSelectedGradientMiddle => colors.Hover;
    public override Color ButtonSelectedGradientEnd => colors.Hover;
    public override Color ButtonPressedBorder => colors.Accent;
    public override Color ButtonPressedGradientBegin => colors.ControlBackground;
    public override Color ButtonPressedGradientMiddle => colors.ControlBackground;
    public override Color ButtonPressedGradientEnd => colors.ControlBackground;
}
