namespace TurnSim {
    partial class Form1 {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.RenderBox = new System.Windows.Forms.PictureBox();
            this.button1 = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.ToggleVis = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.RenderBox)).BeginInit();
            this.SuspendLayout();
            // 
            // RenderBox
            // 
            this.RenderBox.BackColor = System.Drawing.Color.White;
            this.RenderBox.Location = new System.Drawing.Point(670, 30);
            this.RenderBox.Margin = new System.Windows.Forms.Padding(0);
            this.RenderBox.Name = "RenderBox";
            this.RenderBox.Size = new System.Drawing.Size(1122, 1096);
            this.RenderBox.TabIndex = 0;
            this.RenderBox.TabStop = false;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(23, 30);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 1;
            this.button1.Text = "Start";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // txtLog
            // 
            this.txtLog.BackColor = System.Drawing.SystemColors.ButtonHighlight;
            this.txtLog.Location = new System.Drawing.Point(23, 79);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(614, 1047);
            this.txtLog.TabIndex = 2;
            // 
            // ToggleVis
            // 
            this.ToggleVis.Location = new System.Drawing.Point(121, 30);
            this.ToggleVis.Name = "ToggleVis";
            this.ToggleVis.Size = new System.Drawing.Size(75, 23);
            this.ToggleVis.TabIndex = 3;
            this.ToggleVis.Text = "Toggle Vis";
            this.ToggleVis.UseVisualStyleBackColor = true;
            this.ToggleVis.Click += new System.EventHandler(this.ToggleVis_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1823, 1169);
            this.Controls.Add(this.ToggleVis);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.RenderBox);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.RenderBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.PictureBox RenderBox;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.Button ToggleVis;
    }
}

