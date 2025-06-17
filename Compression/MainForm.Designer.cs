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
            this.txtFilePath = new System.Windows.Forms.TextBox();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.ShannonLabel = new System.Windows.Forms.Label();
            this.ShannonCompress = new System.Windows.Forms.Button();
            this.ShannonDecompress = new System.Windows.Forms.Button();
            this.HuffmanLabel = new System.Windows.Forms.Label();
            this.HuffmanCompress = new System.Windows.Forms.Button();
            this.HuffmanDecompress = new System.Windows.Forms.Button();
            this.lblResults = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtFilePath
            // 
            this.txtFilePath.Location = new System.Drawing.Point(16, 58);
            this.txtFilePath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtFilePath.Name = "txtFilePath";
            this.txtFilePath.Size = new System.Drawing.Size(513, 27);
            this.txtFilePath.TabIndex = 0;
            // 
            // btnBrowse
            // 
            this.btnBrowse.Location = new System.Drawing.Point(540, 55);
            this.btnBrowse.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(140, 35);
            this.btnBrowse.TabIndex = 1;
            this.btnBrowse.Text = "Browse...";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // ShannonLabel
            // 
            this.ShannonLabel.AutoSize = true;
            this.ShannonLabel.Location = new System.Drawing.Point(130, 115);
            this.ShannonLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.ShannonLabel.Name = "ShannonLabel";
            this.ShannonLabel.Size = new System.Drawing.Size(80, 23);
            this.ShannonLabel.TabIndex = 5;
            this.ShannonLabel.Text = "Shannon-Fano";
            // 
            // ShannonCompress
            // 
            this.ShannonCompress.Location = new System.Drawing.Point(16, 150);
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
            this.ShannonDecompress.Location = new System.Drawing.Point(184, 150);
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
            this.HuffmanLabel.Location = new System.Drawing.Point(482, 115);
            this.HuffmanLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.HuffmanLabel.Name = "HuffmanLabel";
            this.HuffmanLabel.Size = new System.Drawing.Size(80, 23);
            this.HuffmanLabel.TabIndex = 5;
            this.HuffmanLabel.Text = "Huffman";
            // 
            // HuffmanCompress
            // 
            this.HuffmanCompress.Location = new System.Drawing.Point(352, 150);
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
            this.HuffmanDecompress.Location = new System.Drawing.Point(520, 150);
            this.HuffmanDecompress.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.HuffmanDecompress.Name = "HuffmanDecompress";
            this.HuffmanDecompress.Size = new System.Drawing.Size(160, 46);
            this.HuffmanDecompress.TabIndex = 3;
            this.HuffmanDecompress.Text = "Decompress";
            this.HuffmanDecompress.UseVisualStyleBackColor = true;
            this.HuffmanDecompress.Click += new System.EventHandler(this.HuffmanDecompress_Click);
            // 
            // lblResults
            // 
            this.lblResults.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.lblResults.Location = new System.Drawing.Point(16, 232);
            this.lblResults.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lblResults.Name = "lblResults";
            this.lblResults.Size = new System.Drawing.Size(663, 122);
            this.lblResults.TabIndex = 4;
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
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.lblResults);
            this.Controls.Add(this.ShannonDecompress);
            this.Controls.Add(this.ShannonCompress);
            this.Controls.Add(this.ShannonLabel);
            this.Controls.Add(this.HuffmanDecompress);
            this.Controls.Add(this.HuffmanCompress);
            this.Controls.Add(this.HuffmanLabel);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.txtFilePath);
            this.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.Name = "MainForm";
            this.Text = "File Compressor";
            this.Load += new System.EventHandler(this.MainForm_Load_1);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.TextBox txtFilePath;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Label ShannonLabel;
        private System.Windows.Forms.Button ShannonCompress;
        private System.Windows.Forms.Button ShannonDecompress;
        private System.Windows.Forms.Label HuffmanLabel;
        private System.Windows.Forms.Button HuffmanCompress;
        private System.Windows.Forms.Button HuffmanDecompress;
        private System.Windows.Forms.Label lblResults;
        private System.Windows.Forms.Label label1;
    }
}