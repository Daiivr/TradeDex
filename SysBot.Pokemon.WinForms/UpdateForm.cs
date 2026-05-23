using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using FontAwesome.Sharp;
using SysBot.Pokemon.Localization;
using SysBot.Pokemon.WinForms.Helpers;

namespace SysBot.Pokemon.WinForms
{
    public class UpdateForm : Form
    {
        private Button buttonDownload = null!;
        private Label labelUpdateInfo = null!;
        private Label labelChangelogTitle = null!;
        private WebBrowser changelogBrowser = null!;
        private readonly bool isUpdateRequired;
        private readonly bool isUpdateAvailable;
        private readonly string newVersion;

        public UpdateForm(bool updateRequired, string newVersion, bool updateAvailable)
        {
            isUpdateRequired = updateRequired;
            this.newVersion = newVersion;
            isUpdateAvailable = updateAvailable;

            InitializeComponent();
            ConfigureDynamicUpdateInfo();
            labelChangelogTitle.Text = $"{L("Changelog")} ({newVersion}):";
            DarkScrollHelper.Apply(changelogBrowser);
            DarkScrollHelper.ApplyNativeTree(changelogBrowser);
            Load += async (sender, e) => await FetchAndDisplayChangelog();
            UpdateFormText();
        }

        public void PerformUpdate()
        {
            Application.Restart();
        }

        private void ConfigureDynamicUpdateInfo()
        {
            if (isUpdateRequired)
            {
                labelUpdateInfo.Text = L("A required update is available. You must update to continue using this application.");
                ControlBox = false;
            }
            else if (isUpdateAvailable)
            {
                labelUpdateInfo.Text = L("A new version is available. Please download the latest version.");
            }
            else
            {
                labelUpdateInfo.Text = L("You are on the latest version. You can re-download if needed.");
                buttonDownload.Text = L("Re-Download Latest Version");
            }

            if (string.IsNullOrWhiteSpace(buttonDownload.Text))
                buttonDownload.Text = L("Download Update");
        }

        private void InitializeComponent()
        {
            var theme = ThemeManager.CurrentColors;

            labelUpdateInfo = new Label();
            buttonDownload = new Button();
            changelogBrowser = new WebBrowser();
            labelChangelogTitle = new Label();
            SuspendLayout();

            // ── Form chrome ───────────────────────────────────────────────
            // FormBorderStyle.None drops the Windows native title bar. The neon outline is
            // drawn in OnPaint (below). Padding(1) reserves a 1px ring around the form
            // that docked children (titleBar Top, buttonDownload Bottom) DON'T cover — so
            // the Paint stroke stays visible on all four sides. BackColor stays PanelBase
            // (dark) so any gap between absolute-positioned controls shows as dark rather
            // than accent blue.
            FormBorderStyle = FormBorderStyle.None;
            BackColor = theme.PanelBase;
            Padding = new Padding(1);
            ClientSize = new Size(708, 321 + 41);
            ForeColor = theme.ForeColor;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UpdateForm";
            StartPosition = FormStartPosition.CenterScreen;
            KeyPreview = true;
            KeyDown += (_, ke) => { if (ke.KeyCode == Keys.Escape) Close(); };
            Paint += (_, e) =>
            {
                using var pen = new Pen(theme.Accent, 1);
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            };

            // ── Title bar (docked Top) ───────────────────────────────────
            var titleBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = theme.PanelBase,
            };
            var titleIcon = new IconPictureBox
            {
                BackColor = Color.Transparent,
                IconChar = IconChar.Download,
                IconColor = theme.Accent,
                IconFont = IconFont.Auto,
                IconSize = 17,
                Location = new Point(18, 12),
                Size = new Size(18, 18),
                TabStop = false,
            };
            var titleLabel = new Label
            {
                AutoSize = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10F, FontStyle.Regular),
                ForeColor = theme.ForeColor,
                Location = new Point(46, 0),
                Size = new Size(532, 40),
                Text = L("Update"),
            };
            TextChanged += (_, _) => titleLabel.Text = Text;
            titleBar.Controls.Add(titleIcon);
            titleBar.Controls.Add(titleLabel);

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
            btnClose.Click += (_, _) => Close();
            titleBar.Controls.Add(btnClose);
            titleBar.Resize += (_, _) => btnClose.Location = new Point(titleBar.Width - 40, 0);
            btnClose.Location = new Point(titleBar.Width - 40, 0);

            var titleHair = new Panel { Dock = DockStyle.Top, Height = 1, BackColor = theme.Shadow };

            // Form background behind the docked content (visible only as the 1px neon ring,
            // because all the docked children fully cover the interior).
            // Note: the body region is the form itself — Padding(1) keeps the 1px neon ring
            // visible, and the original control positions below now live directly on the form,
            // shifted to sit under the title bar.

            //
            // labelUpdateInfo — same coordinates as the original layout, kept absolute.
            //
            labelUpdateInfo.AutoSize = true;
            labelUpdateInfo.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            labelUpdateInfo.ForeColor = theme.ForeColor;
            labelUpdateInfo.Location = new Point(20, 41 + 18);
            labelUpdateInfo.Name = "labelUpdateInfo";
            labelUpdateInfo.Size = new Size(158, 20);
            labelUpdateInfo.TabIndex = 0;
            labelUpdateInfo.Text = L("Checking for updates...");
            //
            // labelChangelogTitle
            //
            labelChangelogTitle.AutoSize = true;
            labelChangelogTitle.ForeColor = theme.Muted;
            labelChangelogTitle.Font = new Font("Segoe UI", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
            labelChangelogTitle.Location = new Point(20, 41 + 52);
            labelChangelogTitle.Name = "labelChangelogTitle";
            labelChangelogTitle.Size = new Size(85, 18);
            labelChangelogTitle.TabIndex = 3;
            labelChangelogTitle.Text = L("Changelog:").ToUpperInvariant();
            //
            // changelogBrowser — GitHub-style rendered markdown, anchored to all four sides.
            //
            changelogBrowser.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            changelogBrowser.AllowWebBrowserDrop = false;
            changelogBrowser.BackColor = theme.Hover;
            changelogBrowser.IsWebBrowserContextMenuEnabled = false;
            changelogBrowser.Location = new Point(20, 41 + 78);
            changelogBrowser.MinimumSize = new Size(20, 20);
            changelogBrowser.Name = "changelogBrowser";
            changelogBrowser.ScriptErrorsSuppressed = true;
            changelogBrowser.ScrollBarsEnabled = true;
            changelogBrowser.Size = new Size(666, 180);
            changelogBrowser.TabIndex = 2;
            changelogBrowser.WebBrowserShortcutsEnabled = true;
            changelogBrowser.DocumentText = BuildChangelogHtml(L("Loading changelog..."));
            changelogBrowser.DocumentCompleted += (_, _) => DarkScrollHelper.ApplyNativeTree(changelogBrowser);
            changelogBrowser.Navigating += ChangelogBrowser_Navigating;
            //
            // buttonDownload — same Dock=Bottom as the original layout.
            //
            buttonDownload.BackColor = theme.Hover;
            buttonDownload.Dock = DockStyle.Bottom;
            buttonDownload.FlatStyle = FlatStyle.Flat;
            buttonDownload.FlatAppearance.BorderSize = 0;
            buttonDownload.FlatAppearance.MouseOverBackColor = theme.Shadow;
            buttonDownload.FlatAppearance.MouseDownBackColor = theme.Shadow;
            buttonDownload.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            buttonDownload.ForeColor = theme.ForeColor;
            buttonDownload.Cursor = Cursors.Hand;
            buttonDownload.Name = "buttonDownload";
            buttonDownload.Size = new Size(706, 46);
            buttonDownload.TabIndex = 1;
            buttonDownload.Text = L("Download Update");
            buttonDownload.UseVisualStyleBackColor = false;
            buttonDownload.Click += ButtonDownload_Click;

            // Order matters: docked-Bottom button is added BEFORE the anchored controls so
            // WinForms reserves its space first. The two title-bar children are added LAST
            // so they dock above everything else.
            Controls.Add(buttonDownload);
            Controls.Add(labelUpdateInfo);
            Controls.Add(labelChangelogTitle);
            Controls.Add(changelogBrowser);
            Controls.Add(titleHair);
            Controls.Add(titleBar);

            DragHelper.Attach(this, titleBar);
            DragHelper.Attach(this, titleIcon);
            DragHelper.Attach(this, titleLabel);

            // Form background fills any uncovered area — match the body color so the only
            // accent that shows is the 1px Padding ring around the edge.
            // (Form.BackColor stays = theme.Accent for the neon border; the area underneath
            // every control is the controls' own BackColor = theme.PanelBase / theme.Hover.)

            ResumeLayout(false);
            PerformLayout();
        }

        // Small helper that lets a borderless Form be dragged by clicking on a child control.
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
                    if (e.Button != MouseButtons.Left) return;
                    ReleaseCapture();
                    SendMessage(form.Handle, 0xA1, 0x2, 0);
                };
            }
        }

        private void UpdateFormText()
        {
            Text = isUpdateAvailable ? $"{L("Update Available")} ({newVersion})" : L("Re-Download Latest Version");
        }

        private async Task FetchAndDisplayChangelog()
        {
            _ = new UpdateChecker();
            string changelog = await UpdateChecker.FetchChangelogAsync();
            changelogBrowser.DocumentText = BuildChangelogHtml(changelog);
        }

        private void ChangelogBrowser_Navigating(object? sender, WebBrowserNavigatingEventArgs e)
        {
            if (e.Url == null || e.Url.AbsoluteUri == "about:blank")
                return;

            e.Cancel = true;

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Url.AbsoluteUri,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Unable to open changelog link: {ex.Message}");
            }
        }

        private static string BuildChangelogHtml(string markdown)
        {
            var theme = ThemeManager.CurrentColors;
            string body = MarkdownToHtml(markdown);
            string background = ToHex(theme.Hover);
            string surface = ToHex(theme.PanelBase);
            string border = ToHex(theme.Shadow);
            string text = ToHex(theme.ForeColor);
            string muted = ToHex(theme.Muted);
            string accent = ToHex(theme.Accent);
            string scrollTrack = ToHex(Mix(theme.Hover, theme.PanelBase, 0.45));
            string scrollButton = ToHex(Mix(theme.PanelBase, theme.Shadow, 0.30));
            string scrollThumb = ToHex(Mix(theme.Shadow, theme.Accent, 0.38));
            string scrollThumbHover = ToHex(Mix(theme.Accent, theme.ForeColor, 0.16));
            string scrollArrow = ToHex(Mix(theme.Muted, theme.Accent, 0.22));

            return $$"""
                <!doctype html>
                <html>
                <head>
                    <meta http-equiv="X-UA-Compatible" content="IE=edge" />
                    <meta charset="utf-8" />
                    <style>
                        html, body {
                            margin: 0;
                            padding: 0;
                            background: {{background}};
                            color: {{text}};
                            font-family: "Segoe UI", Arial, sans-serif;
                            font-size: 13px;
                            line-height: 1.48;
                            overflow-x: hidden;
                            scrollbar-base-color: {{scrollButton}};
                            scrollbar-face-color: {{scrollThumb}};
                            scrollbar-track-color: {{scrollTrack}};
                            scrollbar-arrow-color: {{scrollArrow}};
                            scrollbar-highlight-color: {{scrollThumbHover}};
                            scrollbar-shadow-color: {{surface}};
                            scrollbar-3dlight-color: {{scrollButton}};
                            scrollbar-darkshadow-color: {{background}};
                        }

                        * {
                            scrollbar-base-color: {{scrollButton}};
                            scrollbar-face-color: {{scrollThumb}};
                            scrollbar-track-color: {{scrollTrack}};
                            scrollbar-arrow-color: {{scrollArrow}};
                            scrollbar-highlight-color: {{scrollThumbHover}};
                            scrollbar-shadow-color: {{surface}};
                            scrollbar-3dlight-color: {{scrollButton}};
                            scrollbar-darkshadow-color: {{background}};
                        }

                        ::-webkit-scrollbar {
                            width: 10px;
                            height: 10px;
                            background: {{scrollTrack}};
                        }

                        ::-webkit-scrollbar-track {
                            background: {{scrollTrack}};
                            border-left: 1px solid {{surface}};
                        }

                        ::-webkit-scrollbar-thumb {
                            min-height: 32px;
                            background: linear-gradient(180deg, {{scrollThumbHover}}, {{scrollThumb}});
                            border: 2px solid {{scrollTrack}};
                            border-radius: 999px;
                        }

                        ::-webkit-scrollbar-thumb:hover {
                            background: {{scrollThumbHover}};
                        }

                        ::-webkit-scrollbar-button {
                            background: {{scrollButton}};
                            height: 10px;
                        }

                        body {
                            padding: 14px 16px;
                            box-sizing: border-box;
                        }

                        h1, h2, h3, h4, h5, h6 {
                            color: {{text}};
                            font-weight: 650;
                            line-height: 1.25;
                            margin: 18px 0 8px;
                        }

                        h1:first-child, h2:first-child, h3:first-child {
                            margin-top: 0;
                        }

                        h1 { font-size: 22px; padding-bottom: 9px; border-bottom: 1px solid {{border}}; }
                        h2 { font-size: 18px; padding-bottom: 7px; border-bottom: 1px solid {{border}}; }
                        h3 { font-size: 15px; }
                        h4, h5, h6 { font-size: 13px; color: {{muted}}; }
                        p { margin: 8px 0; }
                        ul, ol { margin: 8px 0 12px; padding-left: 24px; }
                        li { margin: 3px 0; }
                        a { color: {{accent}}; text-decoration: none; }
                        a:hover { text-decoration: underline; }
                        strong { font-weight: 700; }
                        em { color: {{muted}}; }
                        code {
                            font-family: Consolas, "Courier New", monospace;
                            font-size: 12px;
                            color: {{text}};
                            background: {{surface}};
                            border: 1px solid {{border}};
                            border-radius: 3px;
                            padding: 1px 4px;
                        }

                        pre {
                            margin: 10px 0;
                            padding: 10px 12px;
                            overflow-x: auto;
                            background: {{surface}};
                            border: 1px solid {{border}};
                            border-radius: 4px;
                        }

                        pre code {
                            padding: 0;
                            border: 0;
                            background: transparent;
                            white-space: pre;
                        }

                        blockquote {
                            margin: 10px 0;
                            padding: 0 0 0 12px;
                            color: {{muted}};
                            border-left: 3px solid {{border}};
                        }

                        hr {
                            height: 3px;
                            margin: 20px 0 24px;
                            background: {{border}};
                            border: 0;
                            border-radius: 999px;
                        }
                    </style>
                </head>
                <body>{{body}}</body>
                </html>
                """;
        }

        private static string MarkdownToHtml(string markdown)
        {
            if (string.IsNullOrWhiteSpace(markdown))
                return "<p>No changelog available.</p>";

            var html = new StringBuilder();
            var paragraph = new List<string>();
            bool inCodeBlock = false;
            bool inUnorderedList = false;
            bool inOrderedList = false;
            string codeLanguage = string.Empty;

            foreach (var rawLine in markdown.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n'))
            {
                string line = rawLine.TrimEnd();
                string trimmed = line.Trim();

                if (trimmed.StartsWith("```", StringComparison.Ordinal))
                {
                    FlushParagraph();
                    CloseLists();

                    if (!inCodeBlock)
                    {
                        inCodeBlock = true;
                        codeLanguage = WebUtility.HtmlEncode(trimmed.Length > 3 ? trimmed[3..].Trim() : string.Empty);
                        html.Append("<pre><code");
                        if (!string.IsNullOrEmpty(codeLanguage))
                            html.Append($" class=\"language-{codeLanguage}\"");
                        html.Append('>');
                    }
                    else
                    {
                        html.Append("</code></pre>");
                        inCodeBlock = false;
                        codeLanguage = string.Empty;
                    }

                    continue;
                }

                if (inCodeBlock)
                {
                    html.Append(WebUtility.HtmlEncode(line));
                    html.Append('\n');
                    continue;
                }

                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    FlushParagraph();
                    CloseLists();
                    continue;
                }

                if (Regex.IsMatch(trimmed, @"^([-*_])(?:\s*\1){2,}$"))
                {
                    FlushParagraph();
                    CloseLists();
                    html.Append("<hr>");
                    continue;
                }

                if (trimmed.StartsWith("> ", StringComparison.Ordinal))
                {
                    FlushParagraph();
                    CloseLists();
                    html.Append("<blockquote>");
                    html.Append(RenderInline(trimmed[2..].Trim()));
                    html.Append("</blockquote>");
                    continue;
                }

                var heading = Regex.Match(trimmed, @"^(#{1,6})\s+(.+)$");
                if (heading.Success)
                {
                    FlushParagraph();
                    CloseLists();

                    int level = heading.Groups[1].Value.Length;
                    html.Append($"<h{level}>");
                    html.Append(RenderInline(heading.Groups[2].Value.Trim()));
                    html.Append($"</h{level}>");
                    continue;
                }

                var unordered = Regex.Match(trimmed, @"^[-*+]\s+(.+)$");
                if (unordered.Success)
                {
                    FlushParagraph();
                    if (inOrderedList)
                    {
                        html.Append("</ol>");
                        inOrderedList = false;
                    }
                    if (!inUnorderedList)
                    {
                        html.Append("<ul>");
                        inUnorderedList = true;
                    }

                    html.Append("<li>");
                    html.Append(RenderInline(unordered.Groups[1].Value));
                    html.Append("</li>");
                    continue;
                }

                var ordered = Regex.Match(trimmed, @"^\d+\.\s+(.+)$");
                if (ordered.Success)
                {
                    FlushParagraph();
                    if (inUnorderedList)
                    {
                        html.Append("</ul>");
                        inUnorderedList = false;
                    }
                    if (!inOrderedList)
                    {
                        html.Append("<ol>");
                        inOrderedList = true;
                    }

                    html.Append("<li>");
                    html.Append(RenderInline(ordered.Groups[1].Value));
                    html.Append("</li>");
                    continue;
                }

                CloseLists();
                paragraph.Add(trimmed);
            }

            FlushParagraph();
            CloseLists();

            if (inCodeBlock)
                html.Append("</code></pre>");

            return html.ToString();

            void FlushParagraph()
            {
                if (paragraph.Count == 0)
                    return;

                html.Append("<p>");
                html.Append(RenderInline(string.Join(" ", paragraph)));
                html.Append("</p>");
                paragraph.Clear();
            }

            void CloseLists()
            {
                if (inUnorderedList)
                {
                    html.Append("</ul>");
                    inUnorderedList = false;
                }

                if (inOrderedList)
                {
                    html.Append("</ol>");
                    inOrderedList = false;
                }
            }
        }

        private static string RenderInline(string value)
        {
            string encoded = WebUtility.HtmlEncode(value);

            encoded = Regex.Replace(encoded, @"`([^`]+)`", "<code>$1</code>");
            encoded = Regex.Replace(encoded, @"\*\*(.+?)\*\*", "<strong>$1</strong>");
            encoded = Regex.Replace(encoded, @"__(.+?)__", "<strong>$1</strong>");
            encoded = Regex.Replace(encoded, @"(?<!\*)\*(?!\*)(.+?)(?<!\*)\*(?!\*)", "<em>$1</em>");
            encoded = Regex.Replace(encoded, @"(?<!_)_(?!_)(.+?)(?<!_)_(?!_)", "<em>$1</em>");
            encoded = Regex.Replace(encoded, @"\[(.+?)\]\((https?:\/\/[^\s)]+)\)", "<a href=\"$2\">$1</a>");
            encoded = Regex.Replace(encoded, @"(?<![""=])(https?:\/\/[^\s<]+)", "<a href=\"$1\">$1</a>");

            return encoded;
        }

        private static string ToHex(Color color)
            => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

        private static Color Mix(Color from, Color to, double amount)
        {
            amount = Math.Clamp(amount, 0, 1);
            return Color.FromArgb(
                (int)Math.Round(from.R + ((to.R - from.R) * amount)),
                (int)Math.Round(from.G + ((to.G - from.G) * amount)),
                (int)Math.Round(from.B + ((to.B - from.B) * amount)));
        }

        private async void ButtonDownload_Click(object? sender, EventArgs e)
        {
            await PerformUpdateAsync();
        }
        public async Task PerformUpdateAsync()
        {
            buttonDownload.Enabled = false;
            buttonDownload.Text = L("Downloading...");
            try
            {
                string? downloadUrl = await UpdateChecker.FetchDownloadUrlAsync();
                if (!string.IsNullOrWhiteSpace(downloadUrl))
                {
                    string downloadedFilePath = await StartDownloadProcessAsync(downloadUrl);
                    if (!string.IsNullOrEmpty(downloadedFilePath))
                        InstallUpdate(downloadedFilePath);
                }
                else
                {
                    SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(L("Failed to fetch the download URL. Please check your internet connection and try again."),
                        L("Download Error"), MessageBoxButtons.OK, SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Error);
                }
            }
            catch (Exception ex)
            {
                SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(L($"Update failed: {ex.Message}"), L("Update Error"), MessageBoxButtons.OK, SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Error);
            }
            finally
            {
                buttonDownload.Enabled = true;
                buttonDownload.Text = isUpdateAvailable ? L("Download Update") : L("Re-Download Latest Version");
            }
        }


        private static async Task<string> StartDownloadProcessAsync(string downloadUrl)
        {
            Main.IsUpdating = true;
            string tempPath = Path.Combine(Path.GetTempPath(), $"SysBot.Pokemon.WinForms_{Guid.NewGuid()}.exe");
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Add("User-Agent", "TradeDex");
                var response = await client.GetAsync(downloadUrl);
                response.EnsureSuccessStatusCode();
                var fileBytes = await response.Content.ReadAsByteArrayAsync();
                await File.WriteAllBytesAsync(tempPath, fileBytes);
            }
            return tempPath;
        }
        private void InstallUpdate(string downloadedFilePath)
        {
            try
            {
                string currentExePath = Application.ExecutablePath;
                string applicationDirectory = Path.GetDirectoryName(currentExePath) ?? "";
                string executableName = Path.GetFileName(currentExePath);
                string backupPath = Path.Combine(applicationDirectory, $"{executableName}.backup");
                // Create batch file for update process
                string batchPath = Path.Combine(Path.GetTempPath(), "UpdateSysBot.bat");
                string batchContent = @$"
                                            @echo off
                                            timeout /t 2 /nobreak >nul
                                            echo Updating SysBot...
                                            rem Backup current version
                                            if exist ""{currentExePath}"" (
                                                if exist ""{backupPath}"" (
                                                    del ""{backupPath}""
                                                )
                                                move ""{currentExePath}"" ""{backupPath}""
                                            )
                                            rem Install new version
                                            move ""{downloadedFilePath}"" ""{currentExePath}""
                                            rem Start new version
                                            start """" ""{currentExePath}""
                                            rem Clean up
                                            del ""%~f0""
                                            ";
                File.WriteAllText(batchPath, batchContent);
                // Start the update batch file
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = batchPath,
                    CreateNoWindow = true,
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                Process.Start(startInfo);
                // Exit the current instance
                Application.Exit();
            }
            catch (Exception ex)
            {
                SysBot.Pokemon.WinForms.Controls.ThemedMessageBox.Show(L($"Failed to install update: {ex.Message}"), L("Update Error"), MessageBoxButtons.OK, SysBot.Pokemon.WinForms.Controls.ThemedMessageIcon.Error);
            }
        }

        private static string L(string message) => AppLocalization.LocalizeRuntimeMessage(message);
    }
}
