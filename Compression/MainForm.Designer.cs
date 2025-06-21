namespace Compression
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.txtPath = new System.Windows.Forms.TextBox();
            this.fileBrowse = new System.Windows.Forms.Button();
            this.folderBrowse = new System.Windows.Forms.Button();
            this.ShannonLabel = new System.Windows.Forms.Label();
            this.ShannonCompress = new System.Windows.Forms.Button();
            this.ShannonDecompress = new System.Windows.Forms.Button();
            this.HuffmanLabel = new System.Windows.Forms.Label();
            this.HuffmanCompress = new System.Windows.Forms.Button();
            this.HuffmanDecompress = new System.Windows.Forms.Button();
            this.btnPause = new System.Windows.Forms.Button();
            this.btnResume = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.lblResults = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtPath
            // 
            this.txtPath.Location = new System.Drawing.Point(16, 58);
            this.txtPath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtPath.Name = "txtPath";
            this.txtPath.Size = new System.Drawing.Size(468, 27);
            this.txtPath.TabIndex = 0;
            // 
            // fileBrowse
            // 
            this.fileBrowse.Location = new System.Drawing.Point(494, 55);
            this.fileBrowse.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.fileBrowse.Name = "fileBrowse";
            this.fileBrowse.Size = new System.Drawing.Size(140, 35);
            this.fileBrowse.TabIndex = 1;
            this.fileBrowse.Text = "Browse file...";
            this.fileBrowse.UseVisualStyleBackColor = true;
            this.fileBrowse.Click += new System.EventHandler(this.fileBrowse_Click);
            // 
            // folderBrowse
            // 
            this.folderBrowse.Location = new System.Drawing.Point(644, 55);
            this.folderBrowse.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.folderBrowse.Name = "folderBrowse";
            this.folderBrowse.Size = new System.Drawing.Size(140, 35);
            this.folderBrowse.TabIndex = 1;
            this.folderBrowse.Text = "Browse folder...";
            this.folderBrowse.UseVisualStyleBackColor = true;
            this.folderBrowse.Click += new System.EventHandler(this.folderBrowse_Click);
            // 
            // ShannonLabel
            // 
            this.ShannonLabel.AutoSize = true;
            this.ShannonLabel.Location = new System.Drawing.Point(181, 115);
            this.ShannonLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ShannonLabel.Name = "ShannonLabel";
            this.ShannonLabel.Size = new System.Drawing.Size(80, 23);
            this.ShannonLabel.TabIndex = 5;
            this.ShannonLabel.Text = "Shannon-Fano";
            // 
            // ShannonCompress
            // 
            this.ShannonCompress.Location = new System.Drawing.Point(67, 150);
            this.ShannonCompress.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ShannonCompress.Name = "ShannonCompress";
            this.ShannonCompress.Size = new System.Drawing.Size(160, 46);
            this.ShannonCompress.TabIndex = 2;
            this.ShannonCompress.Text = "Compress";
            this.ShannonCompress.UseVisualStyleBackColor = true;
            this.ShannonCompress.Click += new System.EventHandler(this.ShannonCompress_Click);
            // 
            // ShannonDecompress
            // 
            this.ShannonDecompress.Location = new System.Drawing.Point(235, 150);
            this.ShannonDecompress.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ShannonDecompress.Name = "ShannonDecompress";
            this.ShannonDecompress.Size = new System.Drawing.Size(160, 46);
            this.ShannonDecompress.TabIndex = 3;
            this.ShannonDecompress.Text = "Decompress";
            this.ShannonDecompress.UseVisualStyleBackColor = true;
            this.ShannonDecompress.Click += new System.EventHandler(this.ShannonDecompress_Click);
            // 
            // HuffmanLabel
            // 
            this.HuffmanLabel.AutoSize = true;
            this.HuffmanLabel.Location = new System.Drawing.Point(535, 115);
            this.HuffmanLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.HuffmanLabel.Name = "HuffmanLabel";
            this.HuffmanLabel.Size = new System.Drawing.Size(80, 23);
            this.HuffmanLabel.TabIndex = 5;
            this.HuffmanLabel.Text = "Huffman";
            // 
            // HuffmanCompress
            // 
            this.HuffmanCompress.Location = new System.Drawing.Point(405, 150);
            this.HuffmanCompress.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.HuffmanCompress.Name = "HuffmanCompress";
            this.HuffmanCompress.Size = new System.Drawing.Size(160, 46);
            this.HuffmanCompress.TabIndex = 2;
            this.HuffmanCompress.Text = "Compress";
            this.HuffmanCompress.UseVisualStyleBackColor = true;
            this.HuffmanCompress.Click += new System.EventHandler(this.HuffmanCompress_Click);
            // 
            // HuffmanDecompress
            // 
            this.HuffmanDecompress.Location = new System.Drawing.Point(573, 150);
            this.HuffmanDecompress.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.HuffmanDecompress.Name = "HuffmanDecompress";
            this.HuffmanDecompress.Size = new System.Drawing.Size(160, 46);
            this.HuffmanDecompress.TabIndex = 3;
            this.HuffmanDecompress.Text = "Decompress";
            this.HuffmanDecompress.UseVisualStyleBackColor = true;
            this.HuffmanDecompress.Click += new System.EventHandler(this.HuffmanDecompress_Click);
            // 
            // btnPause
            // 
            this.btnPause.Enabled = false;
            this.btnPause.Location = new System.Drawing.Point(235, 424);
            this.btnPause.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(160, 46);
            this.btnPause.TabIndex = 8;
            this.btnPause.Text = "Pause";
            this.btnPause.UseVisualStyleBackColor = true;
            this.btnPause.Click += new System.EventHandler(this.btnPause_Click);
            // 
            // btnResume
            // 
            this.btnResume.Enabled = false;
            this.btnResume.Location = new System.Drawing.Point(405, 424);
            this.btnResume.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnResume.Name = "btnResume";
            this.btnResume.Size = new System.Drawing.Size(160, 46);
            this.btnResume.TabIndex = 9;
            this.btnResume.Text = "Resume";
            this.btnResume.UseVisualStyleBackColor = true;
            this.btnResume.Click += new System.EventHandler(this.btnResume_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Enabled = false;
            this.btnCancel.Location = new System.Drawing.Point(573, 424);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(160, 46);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // lblResults
            // 
            this.lblResults.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblResults.Location = new System.Drawing.Point(16, 232);
            this.lblResults.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblResults.Name = "lblResults";
            this.lblResults.Size = new System.Drawing.Size(763, 122);
            this.lblResults.TabIndex = 4;
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(16, 437);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(209, 31);
            this.lblStatus.TabIndex = 8;
            this.lblStatus.Text = "Ready";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 34);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(69, 20);
            this.label1.TabIndex = 5;
            this.label1.Text = "File path:";
            //
            // progress bar
            //
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.progressBar.Location = new System.Drawing.Point(16, 370);
            this.progressBar.Size = new System.Drawing.Size(763, 30);
            this.progressBar.TabIndex = 6;
            this.progressBar.Minimum = 0;
            this.progressBar.Maximum = 100;
            this.progressBar.Value = 0;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblResults);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnResume);
            this.Controls.Add(this.btnPause);
            this.Controls.Add(this.ShannonDecompress);
            this.Controls.Add(this.ShannonCompress);
            this.Controls.Add(this.ShannonLabel);
            this.Controls.Add(this.HuffmanDecompress);
            this.Controls.Add(this.HuffmanCompress);
            this.Controls.Add(this.HuffmanLabel);
            this.Controls.Add(this.folderBrowse);
            this.Controls.Add(this.fileBrowse);
            this.Controls.Add(this.txtPath);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "MainForm";
            this.Text = "File Compressor";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button fileBrowse;
        private System.Windows.Forms.Button folderBrowse;
        private System.Windows.Forms.Label ShannonLabel;
        private System.Windows.Forms.Button ShannonCompress;
        private System.Windows.Forms.Button ShannonDecompress;
        private System.Windows.Forms.Label HuffmanLabel;
        private System.Windows.Forms.Button HuffmanCompress;
        private System.Windows.Forms.Button HuffmanDecompress;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnResume;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblResults;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ProgressBar progressBar;
    }
}