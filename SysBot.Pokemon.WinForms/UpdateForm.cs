using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
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
            //
            // labelUpdateInfo
            //
            labelUpdateInfo.AutoSize = true;
            labelUpdateInfo.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular);
            labelUpdateInfo.ForeColor = theme.ForeColor;
            labelUpdateInfo.Location = new Point(20, 18);
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
            labelChangelogTitle.Location = new Point(20, 52);
            labelChangelogTitle.Name = "labelChangelogTitle";
            labelChangelogTitle.Size = new Size(85, 18);
            labelChangelogTitle.TabIndex = 3;
            labelChangelogTitle.Text = L("Changelog:").ToUpperInvariant();
            //
            // buttonDownload
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
            buttonDownload.Location = new Point(0, 275);
            buttonDownload.Name = "buttonDownload";
            buttonDownload.Size = new Size(708, 46);
            buttonDownload.TabIndex = 1;
            buttonDownload.Text = L("Download Update");
            buttonDownload.UseVisualStyleBackColor = false;
            buttonDownload.Click += ButtonDownload_Click;
            //
            // textBoxChangelog
            //
            textBoxChangelog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            textBoxChangelog.BackColor = theme.Hover;
            textBoxChangelog.BorderStyle = BorderStyle.None;
            textBoxChangelog.Font = new Font("Consolas", 9.5F, FontStyle.Regular);
            textBoxChangelog.ForeColor = Color.FromArgb(200, 204, 210);
            textBoxChangelog.Location = new Point(20, 78);
            textBoxChangelog.Multiline = true;
            textBoxChangelog.Name = "textBoxChangelog";
            textBoxChangelog.ReadOnly = true;
            textBoxChangelog.ScrollBars = ScrollBars.Vertical;
            textBoxChangelog.Size = new Size(668, 180);
            textBoxChangelog.TabIndex = 2;
            //
            // UpdateForm
            //
            BackColor = theme.PanelBase;
            ClientSize = new Size(708, 321);
            Controls.Add(labelUpdateInfo);
            Controls.Add(buttonDownload);
            Controls.Add(textBoxChangelog);
            Controls.Add(labelChangelogTitle);
            ForeColor = theme.ForeColor;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "UpdateForm";
            StartPosition = FormStartPosition.CenterScreen;
            ResumeLayout(false);
            PerformLayout();
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
