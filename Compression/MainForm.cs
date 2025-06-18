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

        private void fileBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = openFileDialog.FileName;
                }
            }
        }

        private void folderBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private void ShannonCompress_Click(object sender, EventArgs e) { }

        private void ShannonDecompress_Click(object sender, EventArgs e) { }

        private async void HuffmanCompress_Click(object sender, EventArgs e)
        {
            // if (!Directory.Exists(folderPath))
            if (string.IsNullOrEmpty(txtPath.Text))
            {
                MessageBox.Show("Please select a valid file or folder.");
                return;
            }

            try
            {
                if (File.Exists(txtPath.Text))
                {
                    // Get data
                    var (data, ext, size) = await helper.Readfile(txtPath.Text);

                    // Compress the data
                    Huffman compressor = new Huffman();
                    byte[] compressedData = await compressor.CompressFile(data, ext);
                    long compressedSize = compressedData.Length;

                    // Save compressed file
                    string fileName = Path.GetFileName(txtPath.Text);
                    string filePath = txtPath.Text.Replace(fileName, "");
                    fileName = fileName.Replace("." + ext, "");
                    string compressedFilePath = filePath + fileName + "-compressed.huff";
                    await File.WriteAllBytesAsync(compressedFilePath, compressedData);

                    // Calculate and display compression ratio
                    double ratio = (double)compressedSize / size * 100;
                    lblResults.Text = $"Original: {size} bytes\n" +
                                    $"Compressed: {compressedSize} bytes\n" +
                                    $"Compression ratio: {ratio:F2}%";

                    MessageBox.Show($"File compressed successfully!\nSaved as: {compressedFilePath}");
                }
                else if (Directory.Exists(txtPath.Text))
                {
                    // Get data
                    var (files, distPath, dirName) = helper.ReadDirectory(txtPath.Text);

                    // Compress the data
                    Huffman compressor = new Huffman();
                    await compressor.CompressDirectory(files, distPath, dirName);
                    MessageBox.Show($"Folder compressed successfully!\nSaved as: {distPath}{dirName}.huff");
                }
                else
                {
                    MessageBox.Show($"Error during compression: {null}");
                }
                
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during compression: {ex.Message}");
            }
        }

        private async void HuffmanDecompress_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPath.Text) || !File.Exists(txtPath.Text))
            {
                MessageBox.Show("Please select a valid file first.");
                return;
            }

            if (!txtPath.Text.EndsWith(".huff"))
            {
                MessageBox.Show("Please select a .huff compressed file.");
                return;
            }

            try
            {
                bool is_dir;
                // Get data
                var (data, _, size) = await helper.Readfile(txtPath.Text);

                using (var ms = new MemoryStream(data))
                using (var reader = new BinaryReader(ms))
                {
                    // Read header
                    is_dir = reader.ReadBoolean();
                }
                if (is_dir) 
                {
                    // Decompress the data
                    Huffman decompressor = new Huffman();
                    string dirPath = txtPath.Text.Replace("-compressed", "-decompressed");
                    dirPath = txtPath.Text.Replace(".huff", "");
                    await decompressor.DecompressDirectory(data, dirPath);
                    MessageBox.Show($"Folder decompressed successfully!\nSaved as: {dirPath}");
                }
                else
                {
                    // Decompress the data
                    Huffman decompressor = new Huffman();
                    (byte[] decompressedData, string ext) = decompressor.DecompressFile(data);
                    long decompressedSize = decompressedData.Length;

                    // Save decompressed file
                    string decompressedFilePath = txtPath.Text.Replace("-compressed", "-decompressed");
                    decompressedFilePath = txtPath.Text.Replace(".huff", "." + ext);
                    await File.WriteAllBytesAsync(decompressedFilePath, decompressedData);

                    // Display results
                    double ratio = (double)size / decompressedSize * 100;
                    lblResults.Text = $"Compressed: {size} bytes\n" +
                                    $"Decompressed: {decompressedSize} bytes\n" +
                                    $"Compression ratio: {ratio:F2}%";

                    MessageBox.Show($"File decompressed successfully!\nSaved as: {decompressedFilePath}");
                }
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
