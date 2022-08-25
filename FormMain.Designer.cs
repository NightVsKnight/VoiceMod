namespace VoiceMod
{
    partial class FormMain
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
            this.voiceModUserControl1 = new VoiceMod.VoiceModUserControl();
            this.SuspendLayout();
            // 
            // voiceModUserControl1
            // 
            this.voiceModUserControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.voiceModUserControl1.Location = new System.Drawing.Point(0, 0);
            this.voiceModUserControl1.MinimumSize = new System.Drawing.Size(360, 300);
            this.voiceModUserControl1.Name = "voiceModUserControl1";
            this.voiceModUserControl1.Size = new System.Drawing.Size(464, 364);
            this.voiceModUserControl1.TabIndex = 0;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(464, 364);
            this.Controls.Add(this.voiceModUserControl1);
            this.MinimumSize = new System.Drawing.Size(480, 340);
            this.Name = "FormMain";
            this.Text = "FormMain";
            this.ResumeLayout(false);

        }

        #endregion

        private VoiceModUserControl voiceModUserControl1;
    }
}

