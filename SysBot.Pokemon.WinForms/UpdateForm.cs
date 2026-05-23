using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
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
        private TextBox textBoxChangelog = null!;
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
            DarkScrollHelper.Apply(textBoxChangelog);
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
            textBoxChangelog = new TextBox();
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
            // textBoxChangelog — anchored to all four sides, original size preserved.
            //
            textBoxChangelog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxChangelog.BackColor = theme.Hover;
            textBoxChangelog.BorderStyle = BorderStyle.None;
            textBoxChangelog.Font = new Font("Consolas", 9.5F, FontStyle.Regular);
            textBoxChangelog.ForeColor = Color.FromArgb(200, 204, 210);
            textBoxChangelog.Location = new Point(20, 41 + 78);
            textBoxChangelog.Multiline = true;
            textBoxChangelog.Name = "textBoxChangelog";
            textBoxChangelog.ReadOnly = true;
            textBoxChangelog.ScrollBars = ScrollBars.Vertical;
            textBoxChangelog.Size = new Size(666, 180);
            textBoxChangelog.TabIndex = 2;
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
            Controls.Add(textBoxChangelog);
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
            textBoxChangelog.Text = changelog;
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
