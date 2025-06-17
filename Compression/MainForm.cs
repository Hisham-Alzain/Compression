using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Compression
{
    public partial class MainForm : Form
    {
        private Helper helper;
        public MainForm()
        {
            InitializeComponent();
            helper = new Helper();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

        }
        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtFilePath.Text = openFileDialog.FileName;
                }
            }
        }

        private void ShannonCompress_Click(object sender, EventArgs e) { }

        private void ShannonDecompress_Click(object sender, EventArgs e) { }

        private void HuffmanCompress_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFilePath.Text) || !File.Exists(txtFilePath.Text))
            {
                MessageBox.Show("Please select a valid file first.");
                return;
            }

            try
            {
                // Get data
                var (data, ext, size) = helper.Readfile(txtFilePath.Text);

                // Compress the data
                Huffman compressor = new Huffman();
                byte[] compressedData = compressor.Compress(data, ext);
                long compressedSize = compressedData.Length;

                // Save compressed file
                string fileName = Path.GetFileName(txtFilePath.Text);
                string filePath = txtFilePath.Text.Replace(fileName, "");
                fileName = fileName.Replace("." + ext, "");
                string compressedFilePath = filePath + fileName + "-compressed.huff";
                helper.Writefile(compressedFilePath, compressedData);

                // Calculate and display compression ratio
                double ratio = (double)compressedSize / size * 100;
                lblResults.Text = $"Original: {size} bytes\n" +
                                $"Compressed: {compressedSize} bytes\n" +
                                $"Compression ratio: {ratio:F2}%";

                MessageBox.Show($"File compressed successfully!\nSaved as: {compressedFilePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during compression: {ex.Message}");
            }
        }

        private void HuffmanDecompress_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtFilePath.Text) || !File.Exists(txtFilePath.Text))
            {
                MessageBox.Show("Please select a valid file first.");
                return;
            }

            if (!txtFilePath.Text.EndsWith(".huff"))
            {
                MessageBox.Show("Please select a .huff compressed file.");
                return;
            }

            try
            {
                // Get data
                var (data, _, size) = helper.Readfile(txtFilePath.Text);

                // Decompress the data
                Huffman decompressor = new Huffman();
                (byte[] decompressedData, string ext) = decompressor.Decompress(data);
                long decompressedSize = decompressedData.Length;

                // Save decompressed file
                string decompressedFilePath = txtFilePath.Text.Replace("-compressed.huff", "-decompressed." + ext);
                helper.Writefile(decompressedFilePath, decompressedData);

                // Display results
                double ratio = (double)size / decompressedSize * 100;
                lblResults.Text = $"Compressed: {size} bytes\n" +
                                $"Decompressed: {decompressedSize} bytes\n" +
                                $"Compression ratio: {ratio:F2}%";

                MessageBox.Show($"File decompressed successfully!\nSaved as: {decompressedFilePath}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during decompression: {ex.Message}");
            }
        }

        private void MainForm_Load_1(object sender, EventArgs e)
        {

        }
    }
}
