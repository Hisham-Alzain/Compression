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
            txtPath = new TextBox();
            fileBrowse = new Button();
            folderBrowse = new Button();
            ShannonLabel = new Label();
            ShannonCompress = new Button();
            ShannonDecompress = new Button();
            ShannonSelectiveDecompress = new Button();
            HuffmanLabel = new Label();
            HuffmanCompress = new Button();
            HuffmanDecompress = new Button();
            HuffmanSelectiveDecompress = new Button();
            btnPause = new Button();
            btnResume = new Button();
            btnCancel = new Button();
            lblResults = new Label();
            lblStatus = new Label();
            label1 = new Label();
            progressBar = new ProgressBar();
            SuspendLayout();
            // 
            // txtPath
            // 
            txtPath.Location = new Point(26, 93);
            txtPath.Margin = new Padding(6, 8, 6, 8);
            txtPath.Name = "txtPath";
            txtPath.Size = new Size(758, 39);
            txtPath.TabIndex = 0;
            // 
            // fileBrowse
            // 
            fileBrowse.Location = new Point(803, 88);
            fileBrowse.Margin = new Padding(6, 8, 6, 8);
            fileBrowse.Name = "fileBrowse";
            fileBrowse.Size = new Size(228, 56);
            fileBrowse.TabIndex = 1;
            fileBrowse.Text = "Browse file...";
            fileBrowse.UseVisualStyleBackColor = true;
            fileBrowse.Click += fileBrowse_Click;
            // 
            // folderBrowse
            // 
            folderBrowse.Location = new Point(1046, 88);
            folderBrowse.Margin = new Padding(6, 8, 6, 8);
            folderBrowse.Name = "folderBrowse";
            folderBrowse.Size = new Size(228, 56);
            folderBrowse.TabIndex = 1;
            folderBrowse.Text = "Browse folder...";
            folderBrowse.UseVisualStyleBackColor = true;
            folderBrowse.Click += folderBrowse_Click;
            // 
            // ShannonLabel
            // 
            ShannonLabel.AutoSize = true;
            ShannonLabel.Location = new Point(294, 184);
            ShannonLabel.Margin = new Padding(6, 0, 6, 0);
            ShannonLabel.Name = "ShannonLabel";
            ShannonLabel.Size = new Size(170, 32);
            ShannonLabel.TabIndex = 5;
            ShannonLabel.Text = "Shannon-Fano";
            // 
            // ShannonCompress
            // 
            ShannonCompress.Location = new Point(109, 240);
            ShannonCompress.Margin = new Padding(6, 8, 6, 8);
            ShannonCompress.Name = "ShannonCompress";
            ShannonCompress.Size = new Size(260, 74);
            ShannonCompress.TabIndex = 2;
            ShannonCompress.Text = "Compress";
            ShannonCompress.UseVisualStyleBackColor = true;
            ShannonCompress.Click += ShannonCompress_Click;
            // 
            // ShannonDecompress
            // 
            ShannonDecompress.Location = new Point(382, 240);
            ShannonDecompress.Margin = new Padding(6, 8, 6, 8);
            ShannonDecompress.Name = "ShannonDecompress";
            ShannonDecompress.Size = new Size(260, 74);
            ShannonDecompress.TabIndex = 3;
            ShannonDecompress.Text = "Decompress";
            ShannonDecompress.UseVisualStyleBackColor = true;
            ShannonDecompress.Click += ShannonDecompress_Click;
            // 
            // ShannonSelectiveDecompress
            // 
            ShannonSelectiveDecompress.Location = new Point(209, 336);
            ShannonSelectiveDecompress.Margin = new Padding(6, 8, 6, 8);
            ShannonSelectiveDecompress.Name = "ShannonSelectiveDecompress";
            ShannonSelectiveDecompress.Size = new Size(358, 74);
            ShannonSelectiveDecompress.TabIndex = 3;
            ShannonSelectiveDecompress.Text = "Selective Decompress";
            ShannonSelectiveDecompress.UseVisualStyleBackColor = true;
            ShannonSelectiveDecompress.Click += ShannonSelectiveDecompress_Click;
            // 
            // HuffmanLabel
            // 
            HuffmanLabel.AutoSize = true;
            HuffmanLabel.Location = new Point(869, 184);
            HuffmanLabel.Margin = new Padding(6, 0, 6, 0);
            HuffmanLabel.Name = "HuffmanLabel";
            HuffmanLabel.Size = new Size(108, 32);
            HuffmanLabel.TabIndex = 5;
            HuffmanLabel.Text = "Huffman";
            // 
            // HuffmanCompress
            // 
            HuffmanCompress.Location = new Point(658, 240);
            HuffmanCompress.Margin = new Padding(6, 8, 6, 8);
            HuffmanCompress.Name = "HuffmanCompress";
            HuffmanCompress.Size = new Size(260, 74);
            HuffmanCompress.TabIndex = 2;
            HuffmanCompress.Text = "Compress";
            HuffmanCompress.UseVisualStyleBackColor = true;
            HuffmanCompress.Click += HuffmanCompress_Click;
            // 
            // HuffmanDecompress
            // 
            HuffmanDecompress.Location = new Point(931, 240);
            HuffmanDecompress.Margin = new Padding(6, 8, 6, 8);
            HuffmanDecompress.Name = "HuffmanDecompress";
            HuffmanDecompress.Size = new Size(260, 74);
            HuffmanDecompress.TabIndex = 3;
            HuffmanDecompress.Text = "Decompress";
            HuffmanDecompress.UseVisualStyleBackColor = true;
            HuffmanDecompress.Click += HuffmanDecompress_Click;
            // 
            // HuffmanSelectiveDecompress
            // 
            HuffmanSelectiveDecompress.Location = new Point(746, 336);
            HuffmanSelectiveDecompress.Margin = new Padding(6, 8, 6, 8);
            HuffmanSelectiveDecompress.Name = "HuffmanSelectiveDecompress";
            HuffmanSelectiveDecompress.Size = new Size(358, 74);
            HuffmanSelectiveDecompress.TabIndex = 3;
            HuffmanSelectiveDecompress.Text = "Selective Decompress";
            HuffmanSelectiveDecompress.UseVisualStyleBackColor = true;
            HuffmanSelectiveDecompress.Click += HuffmanSelectiveDecompress_Click;
            // 
            // btnPause
            // 
            btnPause.Enabled = false;
            btnPause.Location = new Point(382, 758);
            btnPause.Margin = new Padding(6, 8, 6, 8);
            btnPause.Name = "btnPause";
            btnPause.Size = new Size(260, 74);
            btnPause.TabIndex = 8;
            btnPause.Text = "Pause";
            btnPause.UseVisualStyleBackColor = true;
            btnPause.Click += btnPause_Click;
            // 
            // btnResume
            // 
            btnResume.Enabled = false;
            btnResume.Location = new Point(658, 758);
            btnResume.Margin = new Padding(6, 8, 6, 8);
            btnResume.Name = "btnResume";
            btnResume.Size = new Size(260, 74);
            btnResume.TabIndex = 9;
            btnResume.Text = "Resume";
            btnResume.UseVisualStyleBackColor = true;
            btnResume.Click += btnResume_Click;
            // 
            // btnCancel
            // 
            btnCancel.Enabled = false;
            btnCancel.Location = new Point(931, 758);
            btnCancel.Margin = new Padding(6, 8, 6, 8);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(260, 74);
            btnCancel.TabIndex = 7;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // lblResults
            // 
            lblResults.BorderStyle = BorderStyle.FixedSingle;
            lblResults.Location = new Point(26, 451);
            lblResults.Margin = new Padding(6, 0, 6, 0);
            lblResults.Name = "lblResults";
            lblResults.Size = new Size(1239, 194);
            lblResults.TabIndex = 4;
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Location = new Point(26, 779);
            lblStatus.Margin = new Padding(6, 0, 6, 0);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(78, 32);
            lblStatus.TabIndex = 8;
            lblStatus.Text = "Ready";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(26, 54);
            label1.Margin = new Padding(6, 0, 6, 0);
            label1.Name = "label1";
            label1.Size = new Size(111, 32);
            label1.TabIndex = 5;
            label1.Text = "File path:";
            // 
            // progressBar
            // 
            progressBar.Location = new Point(26, 672);
            progressBar.Margin = new Padding(5, 5, 5, 5);
            progressBar.Name = "progressBar";
            progressBar.Size = new Size(1240, 48);
            progressBar.TabIndex = 6;
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(13F, 32F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1300, 960);
            Controls.Add(progressBar);
            Controls.Add(label1);
            Controls.Add(lblStatus);
            Controls.Add(lblResults);
            Controls.Add(btnCancel);
            Controls.Add(btnResume);
            Controls.Add(btnPause);
            Controls.Add(ShannonSelectiveDecompress);
            Controls.Add(ShannonDecompress);
            Controls.Add(ShannonCompress);
            Controls.Add(ShannonLabel);
            Controls.Add(HuffmanSelectiveDecompress);
            Controls.Add(HuffmanDecompress);
            Controls.Add(HuffmanCompress);
            Controls.Add(HuffmanLabel);
            Controls.Add(folderBrowse);
            Controls.Add(fileBrowse);
            Controls.Add(txtPath);
            Margin = new Padding(6, 8, 6, 8);
            Name = "MainForm";
            Text = "File Compressor";
            Load += MainForm_Load;
            ResumeLayout(false);
            PerformLayout();

        }

        private System.Windows.Forms.TextBox txtPath;
        private System.Windows.Forms.Button fileBrowse;
        private System.Windows.Forms.Button folderBrowse;
        private System.Windows.Forms.Label ShannonLabel;
        private System.Windows.Forms.Button ShannonCompress;
        private System.Windows.Forms.Button ShannonDecompress;
        private System.Windows.Forms.Button ShannonSelectiveDecompress;
        private System.Windows.Forms.Label HuffmanLabel;
        private System.Windows.Forms.Button HuffmanCompress;
        private System.Windows.Forms.Button HuffmanDecompress;
        private System.Windows.Forms.Button HuffmanSelectiveDecompress;
        private System.Windows.Forms.Button btnPause;
        private System.Windows.Forms.Button btnResume;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Label lblResults;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ProgressBar progressBar;
    }
}