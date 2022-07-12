namespace Vehicle_Detection
{
    partial class VehicleDetection
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
            this.SuspendLayout();
            // 
            // VehicleDetection
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(989, 548);
            this.Name = "VehicleDetection";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.VehicleDetection_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private Accord.Controls.VideoSourcePlayer videoPlayer;
        private System.Windows.Forms.PictureBox thresholdedBox;
        private System.Windows.Forms.PictureBox maskedBox;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label carLabel;
        private System.Windows.Forms.Timer timer;
    }
}

