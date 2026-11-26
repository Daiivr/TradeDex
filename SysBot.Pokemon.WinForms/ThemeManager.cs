using SysBot.Pokemon.WinForms;
using System.Collections.Generic;
using System.Drawing;

public static class ThemeManager
{
    // Modern minimalist palette. Four restrained dark presets — single accent, near-black surfaces,
    // hairline borders. PanelBase = sidebar/title bar. Shadow = hairline / divider. Hover = subtle lift.
    public static Dictionary<string, ThemeColors> ThemePresets { get; } = new()
    {
        ["Graphite"] = new ThemeColors
        {
            PanelBase = Color.FromArgb(22, 23, 26),
            Shadow    = Color.FromArgb(36, 38, 42),
            Hover     = Color.FromArgb(30, 32, 36),
            Accent    = Color.FromArgb(96, 165, 250),
            ForeColor = Color.FromArgb(232, 234, 238),
            Muted     = Color.FromArgb(140, 144, 152),
            Background = Color.FromArgb(15, 16, 19),
        },

        ["Onyx"] = new ThemeColors
        {
            PanelBase = Color.FromArgb(14, 15, 18),
            Shadow    = Color.FromArgb(30, 32, 36),
            Hover     = Color.FromArgb(22, 23, 26),
            Accent    = Color.FromArgb(120, 180, 255),
            ForeColor = Color.FromArgb(232, 234, 238),
            Muted     = Color.FromArgb(128, 132, 140),
            Background = Color.FromArgb(8, 9, 11),
        },

        ["Slate"] = new ThemeColors
        {
            PanelBase = Color.FromArgb(26, 29, 35),
            Shadow    = Color.FromArgb(42, 46, 54),
            Hover     = Color.FromArgb(34, 38, 46),
            Accent    = Color.FromArgb(125, 211, 252),
            ForeColor = Color.FromArgb(230, 234, 241),
            Muted     = Color.FromArgb(148, 156, 170),
            Background = Color.FromArgb(18, 20, 25),
        },

        ["Mono"] = new ThemeColors
        {
            PanelBase = Color.FromArgb(18, 18, 18),
            Shadow    = Color.FromArgb(44, 44, 44),
            Hover     = Color.FromArgb(28, 28, 28),
            Accent    = Color.FromArgb(245, 245, 245),
            ForeColor = Color.FromArgb(245, 245, 245),
            Muted     = Color.FromArgb(140, 140, 140),
            Background = Color.FromArgb(10, 10, 10),
        },

        ["Cute"] = new ThemeColors
        {
            PanelBase = Color.FromArgb(24, 24, 24),
            Shadow    = Color.FromArgb(84, 48, 60),
            Hover     = Color.FromArgb(48, 36, 48),
            Accent    = Color.FromArgb(228, 84, 132),
            ForeColor = Color.FromArgb(252, 252, 252),
            Muted     = Color.FromArgb(240, 120, 156),
            Background = Color.FromArgb(12, 12, 12),
        },
    };

    public static string CurrentThemeName { get; private set; } = "Graphite";

    public static ThemeColors CurrentColors => ThemePresets[CurrentThemeName];

    public static void ApplyTheme(Main form, string themeName)
    {
        if (!ThemePresets.TryGetValue(themeName, out var colors))
        {
            // Fall back to the default if a legacy theme name is still saved in config.
            themeName = "Graphite";
            colors = ThemePresets[themeName];
        }

        CurrentThemeName = themeName;

        // Neon outline — the form's BackColor shows through the 1px Padding ring as a
        // glowing border so the window stays distinct on dark desktops.
        form.BackColor = colors.Accent;

        // Workspace background — slightly deeper than the chrome for subtle depth.
        form.panelMain.BackColor = colors.Background;

        // Sidebar + title bar.
        form.panelLeftSide.BackColor = colors.PanelBase;
        form.panelTitleBar.BackColor = colors.PanelBase;

        // Hairlines/dividers — the old "shadow rim" panels are repurposed as 1px hairlines.
        form.shadowPanelTop.BackColor = colors.Shadow;
        form.shadowPanelLeft.BackColor = colors.Shadow;
        form.panel1.BackColor = colors.PanelBase;
        form.panel2.BackColor = colors.PanelBase;
        form.panel3.BackColor = colors.PanelBase;
        form.panel4.BackColor = colors.PanelBase;
        form.panel5.BackColor = colors.PanelBase;
        form.panel6.BackColor = colors.PanelBase;

        // Sidebar nav buttons — flat, no border.
        form.btnBots.BackColor = colors.PanelBase;
        form.btnHub.BackColor = colors.PanelBase;
        form.btnLogs.BackColor = colors.PanelBase;

        form.btnBots.ForeColor = colors.ForeColor;
        form.btnHub.ForeColor = colors.ForeColor;
        form.btnLogs.ForeColor = colors.ForeColor;
        form.lblTitle.ForeColor = colors.Muted;

        // Reapply hover handlers so they pick up the new palette.
        form.SetupThemeAwareButtons();
        form.ApplyThemeArtwork();

        // Cascade the theme into the child forms (Bots/Hub/Logs) and their controls
        form.RefreshChildThemes();
    }

    public static ThemeColors? GetCurrentColors()
        => ThemePresets.TryGetValue(CurrentThemeName, out var colors) ? colors : null;

    // ──────────────────────────────────────────────────────────────────────
    //  Color helpers — used to derive the secondary surface colors so every
    //  preset themes the child forms without having to declare extra colors.
    // ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Lightens (factor &gt; 0, toward white) or darkens (factor &lt; 0, toward
    /// black) a color. <paramref name="factor"/> is a 0–1 ratio.
    /// </summary>
    public static Color Shade(Color c, float factor)
    {
        if (factor < 0)
        {
            float f = 1f + factor; // e.g. -0.30 -> scale channels by 0.70
            return Color.FromArgb(c.A, Clamp(c.R * f), Clamp(c.G * f), Clamp(c.B * f));
        }

        return Color.FromArgb(c.A,
            Clamp(c.R + (255 - c.R) * factor),
            Clamp(c.G + (255 - c.G) * factor),
            Clamp(c.B + (255 - c.B) * factor));
    }

    /// <summary>Linearly blends <paramref name="a"/> toward <paramref name="b"/> by <paramref name="t"/> (0–1).</summary>
    public static Color Blend(Color a, Color b, float t)
        => Color.FromArgb(
            Clamp(a.R + (b.R - a.R) * t),
            Clamp(a.G + (b.G - a.G) * t),
            Clamp(a.B + (b.B - a.B) * t));

    private static int Clamp(float v) => (int)(v < 0 ? 0 : v > 255 ? 255 : v);
}

public class ThemeColors
{
    public Color PanelBase { get; set; }
    public Color Shadow { get; set; }
    public Color Hover { get; set; }
    public Color ForeColor { get; set; }

    // New refined-palette additions. Existing call sites still work via PanelBase/Shadow/Hover/ForeColor.
    public Color Accent { get; set; } = Color.FromArgb(96, 165, 250);
    public Color Muted { get; set; } = Color.FromArgb(140, 144, 152);
    public Color Background { get; set; } = Color.FromArgb(15, 16, 19);
    public Color ControlBackground => Hover;
    public Color ControllerForeColor => ForeColor;
    public Color Border => Shadow;
    public Color Highlight => Accent;
    public Color ListBackground => Background;
    public Color CommandButtonForeColor => ForeColor;
}
