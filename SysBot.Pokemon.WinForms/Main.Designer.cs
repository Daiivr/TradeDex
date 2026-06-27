using FontAwesome.Sharp;
using PKHeX.Drawing.PokeSprite.Properties;
using SysBot.Pokemon.Helpers;
using SysBot.Pokemon.WinForms.Properties;
using System.Drawing;
using System.Windows.Forms;

namespace SysBot.Pokemon.WinForms
{

    partial class Main
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>


        internal FontAwesome.Sharp.IconButton btnBots;
        internal FontAwesome.Sharp.IconButton btnHub;
        internal FontAwesome.Sharp.IconButton btnLogs;

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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            panelLeftSide = new Panel();
            btnLogs = new IconButton();
            btnHub = new IconButton();
            CB_Themes = new SysBot.Pokemon.WinForms.Controls.FlatComboBox();
            btnBots = new IconButton();
            panelImageLogo = new Panel();
            panel6 = new Panel();
            panel5 = new Panel();
            panel3 = new Panel();
            pictureLogo = new PictureBox();
            lblTitle = new Label();
            panel4 = new Panel();
            panelTitleBar = new Panel();
            btnClose = new IconPictureBox();
            btnMaximize = new IconPictureBox();
            btnMinimize = new IconPictureBox();
            childFormIcon = new IconPictureBox();
            lblTitleChildForm = new Label();
            upperPanelImage = new PictureBox();
            shadowPanelTop = new Panel();
            shadowPanelLeft = new Panel();
            panelMain = new Panel();
            panel2 = new Panel();
            panel1 = new Panel();
            panelLeftSide.SuspendLayout();
            panelImageLogo.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureLogo).BeginInit();
            panelTitleBar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)btnClose).BeginInit();
            ((System.ComponentModel.ISupportInitialize)btnMaximize).BeginInit();
            ((System.ComponentModel.ISupportInitialize)btnMinimize).BeginInit();
            ((System.ComponentModel.ISupportInitialize)childFormIcon).BeginInit();
            ((System.ComponentModel.ISupportInitialize)upperPanelImage).BeginInit();
            panelMain.SuspendLayout();
            SuspendLayout();
            // 
            // panelLeftSide
            // 
            panelLeftSide.BackColor = Color.FromArgb(22, 23, 26);
            panelLeftSide.Controls.Add(btnLogs);
            panelLeftSide.Controls.Add(btnHub);
            panelLeftSide.Controls.Add(CB_Themes);
            panelLeftSide.Controls.Add(btnBots);
            panelLeftSide.Controls.Add(panelImageLogo);
            panelLeftSide.Controls.Add(lblTitle);
            panelLeftSide.Dock = DockStyle.Left;
            panelLeftSide.Location = new Point(0, 0);
            panelLeftSide.Name = "panelLeftSide";
            panelLeftSide.Size = new Size(220, 447);
            panelLeftSide.TabIndex = 0;
            // 
            // btnLogs
            // 
            btnLogs.Dock = DockStyle.Top;
            btnLogs.FlatAppearance.BorderSize = 0;
            btnLogs.FlatStyle = FlatStyle.Flat;
            btnLogs.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnLogs.ForeColor = Color.FromArgb(232, 234, 238);
            btnLogs.IconChar = IconChar.ListUl;
            btnLogs.IconColor = Color.FromArgb(180, 184, 192);
            btnLogs.IconFont = IconFont.Solid;
            btnLogs.IconSize = 18;
            btnLogs.ImageAlign = ContentAlignment.MiddleLeft;
            btnLogs.Location = new Point(0, 210);
            btnLogs.Name = "btnLogs";
            btnLogs.Padding = new Padding(22, 0, 20, 0);
            btnLogs.Size = new Size(220, 44);
            btnLogs.TabIndex = 3;
            btnLogs.Text = "   Logs";
            btnLogs.TextAlign = ContentAlignment.MiddleLeft;
            btnLogs.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnLogs.UseVisualStyleBackColor = false;
            btnLogs.BackColor = Color.FromArgb(22, 23, 26);
            btnLogs.Click += Logs_Click;
            // 
            // btnHub
            // 
            btnHub.Dock = DockStyle.Top;
            btnHub.FlatAppearance.BorderSize = 0;
            btnHub.FlatStyle = FlatStyle.Flat;
            btnHub.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnHub.ForeColor = Color.FromArgb(232, 234, 238);
            btnHub.IconChar = IconChar.SlidersH;
            btnHub.IconColor = Color.FromArgb(180, 184, 192);
            btnHub.IconFont = IconFont.Solid;
            btnHub.IconSize = 18;
            btnHub.ImageAlign = ContentAlignment.MiddleLeft;
            btnHub.Location = new Point(0, 166);
            btnHub.Name = "btnHub";
            btnHub.Padding = new Padding(22, 0, 20, 0);
            btnHub.Size = new Size(220, 44);
            btnHub.TabIndex = 2;
            btnHub.Text = "   Hub";
            btnHub.TextAlign = ContentAlignment.MiddleLeft;
            btnHub.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnHub.UseVisualStyleBackColor = false;
            btnHub.BackColor = Color.FromArgb(22, 23, 26);
            btnHub.Click += Hub_Click;
            // 
            // CB_Themes
            // 
            CB_Themes.BackColor = Color.FromArgb(30, 32, 36);
            CB_Themes.ForeColor = Color.FromArgb(232, 234, 238);
            CB_Themes.Font = new Font("Segoe UI", 9F);
            CB_Themes.FormattingEnabled = true;
            CB_Themes.Location = new Point(28, 274);
            CB_Themes.Name = "CB_Themes";
            CB_Themes.Size = new Size(164, 28);
            CB_Themes.TabIndex = 5;
            // 
            // btnBots
            // 
            btnBots.Dock = DockStyle.Top;
            btnBots.FlatAppearance.BorderSize = 0;
            btnBots.FlatStyle = FlatStyle.Flat;
            btnBots.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point, 0);
            btnBots.ForeColor = Color.FromArgb(232, 234, 238);
            btnBots.IconChar = IconChar.Robot;
            btnBots.IconColor = Color.FromArgb(180, 184, 192);
            btnBots.IconFont = IconFont.Solid;
            btnBots.IconSize = 18;
            btnBots.ImageAlign = ContentAlignment.MiddleLeft;
            btnBots.Location = new Point(0, 122);
            btnBots.Name = "btnBots";
            btnBots.Padding = new Padding(22, 0, 20, 0);
            btnBots.Size = new Size(220, 44);
            btnBots.TabIndex = 1;
            btnBots.Text = "   Bots";
            btnBots.TextAlign = ContentAlignment.MiddleLeft;
            btnBots.TextImageRelation = TextImageRelation.ImageBeforeText;
            btnBots.UseVisualStyleBackColor = false;
            btnBots.BackColor = Color.FromArgb(22, 23, 26);
            btnBots.Click += Bots_Click;
            // 
            // panelImageLogo
            // 
            panelImageLogo.BackColor = Color.Transparent;
            panelImageLogo.Controls.Add(panel3);
            panelImageLogo.Controls.Add(pictureLogo);
            panelImageLogo.Dock = DockStyle.Top;
            panelImageLogo.Location = new Point(0, 0);
            panelImageLogo.Name = "panelImageLogo";
            panelImageLogo.Size = new Size(220, 110);
            panelImageLogo.TabIndex = 0;
            // 
            // panel6
            // 
            panel6.BackColor = Color.FromArgb(22, 23, 26);
            panel6.Dock = DockStyle.Left;
            panel6.Location = new Point(0, 1);
            panel6.Name = "panel6";
            panel6.Size = new Size(0, 123);
            panel6.TabIndex = 5;
            panel6.Visible = false;
            panel6.Paint += panel6_Paint;
            // 
            // panel5
            // 
            panel5.BackColor = Color.FromArgb(22, 23, 26);
            panel5.Dock = DockStyle.Top;
            panel5.Location = new Point(0, 0);
            panel5.Name = "panel5";
            panel5.Size = new Size(220, 0);
            panel5.TabIndex = 4;
            panel5.Visible = false;
            // 
            // panel3
            // 
            // Hairline divider below the sidebar logo, separating brand from navigation.
            panel3.BackColor = Color.FromArgb(36, 38, 42);
            panel3.Dock = DockStyle.Bottom;
            panel3.Location = new Point(0, 124);
            panel3.Name = "panel3";
            panel3.Size = new Size(220, 1);
            panel3.TabIndex = 3;
            panel3.Paint += panel3_Paint;
            // 
            // pictureLogo
            // 
            pictureLogo.BackColor = Color.Transparent;
            pictureLogo.BackgroundImageLayout = ImageLayout.Zoom;
            pictureLogo.Image = (Image)resources.GetObject("pictureLogo.Image");
            pictureLogo.Location = new Point(20, 14);
            pictureLogo.Name = "pictureLogo";
            pictureLogo.Size = new Size(180, 80);
            pictureLogo.SizeMode = PictureBoxSizeMode.Zoom;
            pictureLogo.TabIndex = 0;
            pictureLogo.TabStop = false;
            // 
            // lblTitle
            // 
            lblTitle.BackColor = Color.Transparent;
            lblTitle.Font = new Font("Segoe UI", 7.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTitle.ForeColor = Color.FromArgb(140, 144, 152);
            lblTitle.Location = new Point(-1, 410);
            lblTitle.Margin = new Padding(0);
            lblTitle.Name = "lblTitle";
            lblTitle.Size = new Size(220, 28);
            lblTitle.TabIndex = 4;
            lblTitle.Text = "TradeDex | v0.0.0 | MODE: None";
            lblTitle.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // panel4
            // 
            panel4.BackColor = Color.FromArgb(22, 23, 26);
            panel4.Dock = DockStyle.Left;
            panel4.Location = new Point(0, 0);
            panel4.Name = "panel4";
            panel4.Size = new Size(0, 50);
            panel4.TabIndex = 4;
            panel4.Visible = false;
            // 
            // panelTitleBar
            // 
            panelTitleBar.BackColor = Color.FromArgb(22, 23, 26);
            panelTitleBar.Controls.Add(btnClose);
            panelTitleBar.Controls.Add(btnMaximize);
            panelTitleBar.Controls.Add(panel4);
            panelTitleBar.Controls.Add(btnMinimize);
            panelTitleBar.Controls.Add(childFormIcon);
            panelTitleBar.Controls.Add(lblTitleChildForm);
            panelTitleBar.Controls.Add(upperPanelImage);
            panelTitleBar.Dock = DockStyle.Top;
            panelTitleBar.Location = new Point(220, 0);
            panelTitleBar.Name = "panelTitleBar";
            panelTitleBar.Size = new Size(721, 56);
            panelTitleBar.TabIndex = 1;
            // 
            // btnClose
            // 
            btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClose.BackColor = Color.Transparent;
            btnClose.ForeColor = Color.FromArgb(180, 184, 192);
            btnClose.IconChar = IconChar.Times;
            btnClose.IconColor = Color.FromArgb(180, 184, 192);
            btnClose.IconFont = IconFont.Auto;
            btnClose.IconSize = 14;
            btnClose.Location = new Point(693, 21);
            btnClose.Name = "btnClose";
            btnClose.Size = new Size(16, 16);
            btnClose.TabIndex = 4;
            btnClose.TabStop = false;
            btnClose.Cursor = Cursors.Hand;
            // 
            // btnMaximize
            // 
            btnMaximize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMaximize.BackColor = Color.Transparent;
            btnMaximize.IconChar = IconChar.Square;
            btnMaximize.IconColor = Color.FromArgb(180, 184, 192);
            btnMaximize.IconFont = IconFont.Regular;
            btnMaximize.IconSize = 12;
            btnMaximize.Location = new Point(665, 22);
            btnMaximize.Name = "btnMaximize";
            btnMaximize.Size = new Size(16, 16);
            btnMaximize.TabIndex = 3;
            btnMaximize.TabStop = false;
            btnMaximize.Cursor = Cursors.Hand;
            // 
            // btnMinimize
            // 
            btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnMinimize.BackColor = Color.Transparent;
            btnMinimize.IconChar = IconChar.Minus;
            btnMinimize.IconColor = Color.FromArgb(180, 184, 192);
            btnMinimize.IconFont = IconFont.Auto;
            btnMinimize.IconSize = 14;
            btnMinimize.Location = new Point(637, 21);
            btnMinimize.Name = "btnMinimize";
            btnMinimize.Size = new Size(16, 16);
            btnMinimize.TabIndex = 2;
            btnMinimize.TabStop = false;
            btnMinimize.Cursor = Cursors.Hand;
            // 
            // childFormIcon
            // 
            childFormIcon.BackColor = Color.Transparent;
            childFormIcon.IconChar = IconChar.Home;
            childFormIcon.IconColor = Color.FromArgb(220, 224, 232);
            childFormIcon.IconFont = IconFont.Auto;
            childFormIcon.IconSize = 28;
            childFormIcon.Location = new Point(22, 14);
            childFormIcon.Name = "childFormIcon";
            childFormIcon.Size = new Size(28, 28);
            childFormIcon.TabIndex = 1;
            childFormIcon.TabStop = false;
            // 
            // lblTitleChildForm
            // 
            lblTitleChildForm.AutoSize = true;
            lblTitleChildForm.Font = new Font("Segoe UI Semibold", 17F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblTitleChildForm.ForeColor = Color.FromArgb(238, 240, 244);
            lblTitleChildForm.Location = new Point(58, 9);
            lblTitleChildForm.Name = "lblTitleChildForm";
            lblTitleChildForm.Size = new Size(170, 32);
            lblTitleChildForm.TabIndex = 0;
            lblTitleChildForm.Text = "Loading ...";
            // 
            // upperPanelImage
            // 
            upperPanelImage.Location = new Point(728, 5);
            upperPanelImage.Name = "upperPanelImage";
            upperPanelImage.Size = new Size(100, 50);
            upperPanelImage.TabIndex = 0;
            upperPanelImage.TabStop = false;
            // 
            // shadowPanelTop
            // 
            shadowPanelTop.BackColor = Color.FromArgb(36, 38, 42);
            shadowPanelTop.Dock = DockStyle.Top;
            shadowPanelTop.Location = new Point(220, 56);
            shadowPanelTop.Name = "shadowPanelTop";
            shadowPanelTop.Size = new Size(721, 1);
            shadowPanelTop.TabIndex = 2;
            // 
            // shadowPanelLeft
            // 
            shadowPanelLeft.BackColor = Color.FromArgb(36, 38, 42);
            shadowPanelLeft.Dock = DockStyle.Left;
            shadowPanelLeft.Location = new Point(220, 57);
            shadowPanelLeft.Name = "shadowPanelLeft";
            shadowPanelLeft.Size = new Size(1, 390);
            shadowPanelLeft.TabIndex = 3;
            // 
            // panelMain
            // 
            panelMain.BackColor = Color.FromArgb(15, 16, 19);
            panelMain.Controls.Add(panel2);
            panelMain.Controls.Add(panel1);
            panelMain.Dock = DockStyle.Fill;
            panelMain.Location = new Point(221, 57);
            panelMain.Name = "panelMain";
            panelMain.Size = new Size(720, 390);
            panelMain.TabIndex = 4;
            // 
            // panel2
            // 
            panel2.BackColor = Color.FromArgb(15, 16, 19);
            panel2.Dock = DockStyle.Bottom;
            panel2.Location = new Point(0, 396);
            panel2.Name = "panel2";
            panel2.Size = new Size(720, 0);
            panel2.TabIndex = 3;
            panel2.Visible = false;
            // 
            // panel1
            // 
            panel1.BackColor = Color.FromArgb(15, 16, 19);
            panel1.Dock = DockStyle.Right;
            panel1.Location = new Point(720, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(0, 396);
            panel1.TabIndex = 4;
            panel1.Visible = false;
            // 
            // Main
            // 
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(941, 447);
            Controls.Add(panelMain);
            Controls.Add(shadowPanelLeft);
            Controls.Add(shadowPanelTop);
            Controls.Add(panelTitleBar);
            Controls.Add(panelLeftSide);
            FormBorderStyle = FormBorderStyle.None;
            Icon = Properties.Resources.icon;
            Margin = new Padding(5, 4, 5, 4);
            MinimumSize = new Size(800, 422);
            Name = "Main";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "TradeDex";
            // Neon outline. Padding(1) on the form leaves a 1px gap around all docked
            // children; the form's own BackColor (set in ThemeManager.ApplyTheme to the
            // theme accent) shows through as a glowing edge so the window doesn't
            // dissolve into a same-color desktop.
            Padding = new Padding(1);
            BackColor = Color.FromArgb(96, 165, 250);
            panelLeftSide.ResumeLayout(false);
            panelImageLogo.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureLogo).EndInit();
            panelTitleBar.ResumeLayout(false);
            panelTitleBar.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)btnClose).EndInit();
            ((System.ComponentModel.ISupportInitialize)btnMaximize).EndInit();
            ((System.ComponentModel.ISupportInitialize)btnMinimize).EndInit();
            ((System.ComponentModel.ISupportInitialize)childFormIcon).EndInit();
            ((System.ComponentModel.ISupportInitialize)upperPanelImage).EndInit();
            panelMain.ResumeLayout(false);
            ResumeLayout(false);
        }

        private void panelTitleBar_Paint(object sender, PaintEventArgs e)
{
    // Draw the existing image first
    if (panelTitleBar.BackgroundImage != null)
        e.Graphics.DrawImage(panelTitleBar.BackgroundImage, 0, 0, panelTitleBar.Width, panelTitleBar.Height);

    // Draw sparkles
    foreach (var sp in sparkles)
    {
        int alpha = (int)(255 * (float)sp.Life / sp.MaxLife);
        using (Brush brush = new SolidBrush(Color.FromArgb(alpha, 255, 255, 255)))
        {
            e.Graphics.FillEllipse(brush, sp.Position.X, sp.Position.Y, sp.Size, sp.Size);
        }
    }
}


        #endregion

        internal Panel panelLeftSide;
        internal Panel panelImageLogo;
        internal PictureBox pictureLogo;
        internal Panel panelTitleBar;
        internal Label lblTitleChildForm;
        internal FontAwesome.Sharp.IconPictureBox childFormIcon;
        internal FontAwesome.Sharp.IconPictureBox btnMaximize;
        internal FontAwesome.Sharp.IconPictureBox btnMinimize;
        internal FontAwesome.Sharp.IconPictureBox btnClose;
        internal Panel shadowPanelTop;
        internal Panel shadowPanelLeft;
        internal Panel panelMain;
        internal Label lblTitle;
        internal Panel panel2;
        internal Panel panel1;
        internal Panel panel4;
        internal Panel panel3;
        internal Panel panel6;
        internal Panel panel5;
        private SysBot.Pokemon.WinForms.Controls.FlatComboBox CB_Themes;
    }
}

