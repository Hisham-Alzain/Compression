using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Compression
{
    public partial class MainForm : Form
    {
        private readonly Helper helper;

        // Cancel
        private CancellationTokenSource cts;
        // Pause & Resume
        private ManualResetEventSlim pauseEvent;
        private bool isPaused = false;

        public MainForm()
        {
            InitializeComponent();
            helper = new Helper();
            pauseEvent = new ManualResetEventSlim(true);

            // lock buttons
            btnCancel.Enabled = false;
            btnPause.Enabled = false;
            btnResume.Enabled = false;
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
                LockUnlockBtn(false);
                progressBar.Value = 0;
                lblStatus.Text = "Starting compression...";
                var progress = new Progress<int>(percent =>
                {
                    progressBar.Value = percent;
                    lblStatus.Text = $"Compressing... {percent}%";
                });

                // Initialize new cancellation and pauseEvent
                cts = new CancellationTokenSource();
                pauseEvent = new ManualResetEventSlim(true);
                isPaused = false;

                // Unlock pause & cancel
                btnPause.Enabled = true;
                btnCancel.Enabled = true;
                btnResume.Enabled = false;

                Huffman compressor = new Huffman(pauseEvent);

                if (File.Exists(txtPath.Text))
                {
                    // Get data
                    var (data, ext, size) = await helper.Readfile(txtPath.Text);

                    // Compress the data
                    byte[] compressedData = await compressor.CompressFile(data, ext, progress, cts.Token);
                    long compressedSize = compressedData.Length;

                    // Save compressed file
                    string fileName = Path.GetFileName(txtPath.Text);
                    string filePath = txtPath.Text.Replace(fileName, "");
                    fileName = fileName.Replace("." + ext, "");
                    string compressedFilePath = filePath + fileName + "-compressed.huff";
                    await File.WriteAllBytesAsync(compressedFilePath, compressedData);

                    // Calculate and display compression ratio
                    double ratio = helper.CalculateRatio(compressedSize, size);

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
                    await compressor.CompressDirectory(files, distPath, dirName, progress, cts.Token);
                    MessageBox.Show($"Folder compressed successfully!\nSaved as: {distPath}{dirName}-compressed.huff");
                }
                else
                {
                    MessageBox.Show($"Error during compression: {null}");
                }
                
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Compression canceled";
                MessageBox.Show("Compression was canceled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during compression: {ex.Message}");
            }
            finally
            {
                // Wait a moment for operations to complete
                await Task.Delay(100);

                // Reset UI
                LockUnlockBtn(true);

                // lock buttons
                btnCancel.Enabled = false;
                btnPause.Enabled = false;
                btnResume.Enabled = false;

                progressBar.Value = 0;
                lblStatus.Text = "Ready";
                lblResults.Text = string.Empty;

                //// Clean up resources
                //pauseEvent?.Set();
                //cts?.Dispose();
                //pauseEvent?.Dispose();

                // reset event
                pauseEvent?.Dispose();
                pauseEvent = new ManualResetEventSlim(true);
                // reset cts
                cts?.Dispose();
                cts = new CancellationTokenSource();
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
                LockUnlockBtn(false);
                progressBar.Value = 0;
                lblStatus.Text = "Starting decompression...";
                var progress = new Progress<int>(percent =>
                {
                    progressBar.Value = percent;
                    lblStatus.Text = $"Decompressing... {percent}%";
                });

                // Initialize new cancellation and pauseEvent
                cts = new CancellationTokenSource();
                pauseEvent = new ManualResetEventSlim(true);
                isPaused = false;

                // Unlock pause & cancel
                btnPause.Enabled = true;
                btnCancel.Enabled = true;
                btnResume.Enabled = false;

                Huffman decompressor = new Huffman(pauseEvent);

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
                    string dirPath = txtPath.Text.Replace("-compressed", "-decompressed");
                    dirPath = txtPath.Text.Replace(".huff", "");
                    await decompressor.DecompressDirectory(data, dirPath, progress, cts.Token);
                    MessageBox.Show($"Folder decompressed successfully!\nSaved as: {dirPath}");
                }
                else
                {
                    // Decompress the data
                    (byte[] decompressedData, string ext) = decompressor.DecompressFile(data, progress, cts.Token);
                    long decompressedSize = decompressedData.Length;

                    // Save decompressed file
                    string decompressedFilePath = txtPath.Text.Replace("-compressed", "-decompressed");
                    decompressedFilePath = txtPath.Text.Replace(".huff", "." + ext);
                    await File.WriteAllBytesAsync(decompressedFilePath, decompressedData);

                    // Display results
                    double ratio = helper.CalculateRatio(size, decompressedSize);

                    lblResults.Text = $"Compressed: {size} bytes\n" +
                                    $"Decompressed: {decompressedSize} bytes\n" +
                                    $"Compression ratio: {ratio:F2}%";

                    MessageBox.Show($"File decompressed successfully!\nSaved as: {decompressedFilePath}");
                }
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Decompression canceled";
                MessageBox.Show("Decompression was canceled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during decompression: {ex.Message}");
            }
            finally
            {
                // Wait a moment for operations to complete
                await Task.Delay(100);

                // Reset UI
                LockUnlockBtn(true);

                // lock buttons
                btnCancel.Enabled = false;
                btnPause.Enabled = false;
                btnResume.Enabled = false;

                progressBar.Value = 0;
                lblStatus.Text = "Ready";
                lblResults.Text = string.Empty;

                //// Clean up resources
                //pauseEvent?.Set();
                //cts?.Dispose();
                //pauseEvent?.Dispose();

                // reset event
                pauseEvent?.Dispose();
                pauseEvent = new ManualResetEventSlim(true);
                // reset cts
                cts?.Dispose();
                cts = new CancellationTokenSource();
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            MessageBox.Show($"Cancel {lblStatus.Text}");
            lblResults.Text = cts.ToString();
            if (cts != null && !cts.IsCancellationRequested)
            {
                cts.Cancel();
            }
            else {
                lblResults.Text = "GG CTS";
            }
            btnCancel.Enabled = false;
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            pauseEvent.Reset();
            isPaused = true;

            btnPause.Enabled = false;
            btnResume.Enabled = true;
            lblStatus.Text = "Paused...";
        }

        private void btnResume_Click(object sender, EventArgs e)
        {
            pauseEvent.Set();
            isPaused = false;

            btnResume.Enabled = false;
            btnPause.Enabled = true;
            lblStatus.Text = "Resumed...";
        }

        private void MainForm_Load_1(object sender, EventArgs e)
        {

        }

        private void LockUnlockBtn(bool unlock = false)
        {
            fileBrowse.Enabled = unlock;
            folderBrowse.Enabled = unlock;
            ShannonCompress.Enabled = unlock;
            ShannonDecompress.Enabled = unlock;
            HuffmanCompress.Enabled = unlock;
            HuffmanDecompress.Enabled = unlock;
        }
    }
}
