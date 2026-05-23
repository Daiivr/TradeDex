using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms.Helpers;

// Forces Explorer's dark visual style on a control's scrollbars (and the control's chrome
// where applicable). Works on Windows 10 1809+ and Windows 11. SetColorMode in Program.cs
// covers most cases globally; this helper is a per-control safety net for stubborn natives
// (RichTextBox, FlowLayoutPanel) where the global setting sometimes doesn't propagate.
internal static class DarkScrollHelper
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
    private static extern int SetWindowTheme(IntPtr hWnd, string? pszSubAppName, string? pszSubIdList);

    [DllImport("user32.dll")]
    private static extern bool EnumChildWindows(IntPtr hWndParent, EnumWindowsProc lpEnumFunc, IntPtr lParam);

    public static void Apply(Control? control)
    {
        if (control == null) return;
        if (!control.IsHandleCreated)
        {
            control.HandleCreated += (_, _) => ApplyCore(control);
            return;
        }
        ApplyCore(control);
    }

    public static void ApplyNativeTree(Control? control)
    {
        if (control == null) return;
        if (!control.IsHandleCreated)
        {
            control.HandleCreated += (_, _) => ApplyNativeTreeCore(control.Handle);
            return;
        }
        ApplyNativeTreeCore(control.Handle);
    }

    // Walks every descendant and applies DarkMode_Explorer. Useful for composite controls
    // (PropertyGrid, etc.) whose internal scrollbars live on nested child controls.
    public static void ApplyRecursive(Control? root)
    {
        if (root == null) return;
        Apply(root);
        foreach (Control child in root.Controls)
            ApplyRecursive(child);
        // PropertyGrid (and similar) sometimes spawn child controls lazily.
        root.ControlAdded += (_, e) => ApplyRecursive(e.Control);
    }

    public static void ApplyScrollBarsRecursive(Control? root)
    {
        if (root == null) return;

        if (root is ScrollBar)
            Apply(root);

        ApplyPropertyGridScrollBar(root);

        foreach (Control child in root.Controls)
            ApplyScrollBarsRecursive(child);

        root.HandleCreated += (_, _) => ApplyPropertyGridScrollBar(root);
        root.ControlAdded += (_, e) => ApplyScrollBarsRecursive(e.Control);
    }

    private static void ApplyCore(Control control)
    {
        ApplyHandle(control.Handle);
    }

    private static void ApplyNativeTreeCore(IntPtr handle)
    {
        ApplyHandle(handle);
        EnumChildWindows(handle, (child, _) =>
        {
            ApplyHandle(child);
            return true;
        }, IntPtr.Zero);
    }

    private static void ApplyHandle(IntPtr handle)
    {
        try
        {
            // "DarkMode_Explorer" is undocumented but standard since Win10 1809. Quietly
            // ignored on older runtimes — no need to gate on the OS version.
            SetWindowTheme(handle, "DarkMode_Explorer", null);
        }
        catch
        {
            // Best effort — never throw from a cosmetic helper.
        }
    }

    private static void ApplyPropertyGridScrollBar(Control control)
    {
        if (control is not PropertyGrid grid || !grid.IsHandleCreated)
            return;

        try
        {
            var gridViewField = typeof(PropertyGrid).GetField("_gridView", BindingFlags.Instance | BindingFlags.NonPublic);
            if (gridViewField?.GetValue(grid) is not Control gridView)
                return;

            var scrollBarProperty = gridView.GetType().GetProperty("ScrollBar", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (scrollBarProperty?.GetValue(gridView) is Control scrollBar)
                Apply(scrollBar);
        }
        catch
        {
            // Best effort — keep the grid's built-in expand indicators untouched.
        }
    }
}
