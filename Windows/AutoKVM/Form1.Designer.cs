namespace AutoKVM
{
    partial class Form1
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
            if(disposing && (components != null)) {
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
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.switchAllToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.switchDisplaysToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.switchKeyboardMouseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "AutoKVM";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.switchAllToolStripMenuItem,
            this.switchDisplaysToolStripMenuItem,
            this.switchKeyboardMouseToolStripMenuItem});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(204, 92);
            // 
            // switchAllToolStripMenuItem
            // 
            this.switchAllToolStripMenuItem.Name = "switchAllToolStripMenuItem";
            this.switchAllToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this.switchAllToolStripMenuItem.Text = "Switch All";
            this.switchAllToolStripMenuItem.Click += new System.EventHandler(this.switchAllToolStripMenuItem_Click);
            // 
            // switchDisplaysToolStripMenuItem
            // 
            this.switchDisplaysToolStripMenuItem.Name = "switchDisplaysToolStripMenuItem";
            this.switchDisplaysToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this.switchDisplaysToolStripMenuItem.Text = "Switch Displays";
            this.switchDisplaysToolStripMenuItem.Click += new System.EventHandler(this.switchDisplaysToolStripMenuItem_Click);
            // 
            // switchKeyboardMouseToolStripMenuItem
            // 
            this.switchKeyboardMouseToolStripMenuItem.Name = "switchKeyboardMouseToolStripMenuItem";
            this.switchKeyboardMouseToolStripMenuItem.Size = new System.Drawing.Size(203, 22);
            this.switchKeyboardMouseToolStripMenuItem.Text = "Switch Keyboard/Mouse";
            this.switchKeyboardMouseToolStripMenuItem.Click += new System.EventHandler(this.switchKeyboardMouseToolStripMenuItem_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 261);
            this.Name = "Form1";
            this.Text = "Form1";
            this.contextMenuStrip1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem switchAllToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem switchDisplaysToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem switchKeyboardMouseToolStripMenuItem;

    }
}

