using System.Drawing;

namespace SysBot.Pokemon.WinForms
{
    partial class HubForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            PG_Hub = new System.Windows.Forms.PropertyGrid();
            SuspendLayout();
            //
            // PG_Hub — refined dark theme. Colors are also reapplied at runtime via ThemeManager.
            //
            PG_Hub.BackColor = Color.FromArgb(22, 23, 26);
            PG_Hub.CategoryForeColor = Color.FromArgb(180, 184, 192);
            PG_Hub.CategorySplitterColor = Color.FromArgb(36, 38, 42);
            PG_Hub.CommandsBackColor = Color.FromArgb(22, 23, 26);
            PG_Hub.CommandsActiveLinkColor = Color.FromArgb(96, 165, 250);
            PG_Hub.CommandsBorderColor = Color.FromArgb(36, 38, 42);
            PG_Hub.CommandsDisabledLinkColor = Color.FromArgb(120, 124, 132);
            PG_Hub.CommandsForeColor = Color.FromArgb(232, 234, 238);
            PG_Hub.CommandsLinkColor = Color.FromArgb(96, 165, 250);
            PG_Hub.DisabledItemForeColor = Color.FromArgb(120, 124, 132);
            PG_Hub.Dock = System.Windows.Forms.DockStyle.Fill;
            PG_Hub.Font = new Font("Segoe UI", 9F);
            PG_Hub.HelpBackColor = Color.FromArgb(22, 23, 26);
            PG_Hub.HelpBorderColor = Color.FromArgb(36, 38, 42);
            PG_Hub.HelpForeColor = Color.FromArgb(200, 204, 210);
            PG_Hub.LineColor = Color.FromArgb(36, 38, 42);
            PG_Hub.Location = new Point(0, 0);
            PG_Hub.Name = "PG_Hub";
            PG_Hub.SelectedItemWithFocusBackColor = Color.FromArgb(36, 38, 42);
            PG_Hub.SelectedItemWithFocusForeColor = Color.FromArgb(232, 234, 238);
            PG_Hub.Size = new Size(739, 305);
            PG_Hub.TabIndex = 0;
            PG_Hub.ViewBackColor = Color.FromArgb(15, 16, 19);
            PG_Hub.ViewBorderColor = Color.FromArgb(36, 38, 42);
            PG_Hub.ViewForeColor = Color.FromArgb(232, 234, 238);
            //
            // HubForm
            //
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            BackColor = Color.FromArgb(15, 16, 19);
            ClientSize = new Size(739, 305);
            Controls.Add(PG_Hub);
            Name = "HubForm";
            Text = "Hub Controls";
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.PropertyGrid PG_Hub;
    }
}
