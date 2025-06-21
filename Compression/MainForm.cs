using System;

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

            LockPauseResumeCancel();
            cts = new CancellationTokenSource();
            pauseEvent = new ManualResetEventSlim(true);
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
            if (string.IsNullOrEmpty(txtPath.Text))
            {
                MessageBox.Show("Please select a valid file or folder.");
                return;
            }

            string compressedFilePath = "";

            try
            {
                LockUnlockBtn(false);
                progressBar.Value = 0;
                lblStatus.Text = "Starting compression...";
                var progress = new Progress<int>(percent =>
                {
                    progressBar.Value = percent;
                    lblStatus.Text = isPaused? $"Paused... {percent}%" : $"Compressing... {percent}%";
                });
                UnLockPauseCancel();

                ResetPauseEvent();
                ResetCancellationToken();
                // Initialize huffman and run on a new task
                Huffman compressor = new Huffman(pauseEvent);
                await Task.Run(async () =>
                {
                    if (File.Exists(txtPath.Text))
                    {
                        // Get data
                        var (data, ext, size) = await helper.Readfile(txtPath.Text);

                        // Compress the data
                        byte[] compressedData = await compressor.CompressFile(data, ext, progress, cts.Token);
                        long compressedSize = compressedData.Length;

                        // Prepare compressed path
                        string fileName = Path.GetFileName(txtPath.Text);
                        string filePath = txtPath.Text.Replace(fileName, "");
                        string compressedName = fileName.Replace("." + ext, "");
                        compressedFilePath = filePath + compressedName + "-compressed.huff";

                        // Save compressed file
                        await File.WriteAllBytesAsync(compressedFilePath, compressedData, cts.Token);

                        // Calculate and display compression ratio
                        double ratio = helper.CalculateRatio(compressedSize, size);
                        this.Invoke(new Action(() =>
                        {
                            lblResults.Text = $"Original: {size} bytes\n" +
                                    $"Compressed: {compressedSize} bytes\n" +
                                    $"Compression ratio: {ratio:F2}%";

                            MessageBox.Show($"File compressed successfully!\nSaved as: {compressedFilePath}");
                        }));
                    }
                    else if (Directory.Exists(txtPath.Text))
                    {
                        // Get data
                        var (files, distPath, dirName) = helper.ReadDirectory(txtPath.Text);

                        // Prepare compressed path
                        compressedFilePath = Path.Combine(distPath, dirName + "-compressed.huff");

                        // Compress and save the data
                        await compressor.CompressDirectory(files, compressedFilePath, progress, cts.Token);

                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show($"Folder compressed successfully!\nSaved as: {compressedFilePath}");
                        }));
                    }
                    else
                    {
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show($"Error during compression: {null}");
                        }));
                    }
                });
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Compression canceled";
                // Clean up partial output file if it exists
                if (File.Exists(compressedFilePath))
                {
                    try { File.Delete(compressedFilePath); }
                    catch { /* Ignore deletion errors */ }
                }
                MessageBox.Show("Compression was canceled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during compression: {ex.Message}");
            }
            finally
            {
                await Task.Delay(100);
                ResetUI();
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

            string decompressedPath = "";
            try
            {
                bool is_dir;
                LockUnlockBtn(false);
                progressBar.Value = 0;
                lblStatus.Text = "Starting decompression...";
                var progress = new Progress<int>(percent =>
                {
                    progressBar.Value = percent;
                    lblStatus.Text = isPaused? $"Paused... {percent}%" : $"Decompressing... {percent}%";
                });
                UnLockPauseCancel();

                ResetPauseEvent();
                ResetCancellationToken();
                // Initialize huffman and run on a new task
                Huffman decompressor = new Huffman(pauseEvent);
                await Task.Run(async () =>
                {
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
                        // Prepare decompressed path
                        decompressedPath = txtPath.Text.Replace("-compressed", "-decompressed").Replace(".huff", "");

                        // Decompress and save the data
                        await decompressor.DecompressDirectory(data, decompressedPath, progress, cts.Token);
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show($"Folder decompressed successfully!\nSaved as: {decompressedPath}");
                        }));
                    }
                    else
                    {
                        // Decompress the data
                        (byte[] decompressedData, string ext) = await decompressor.DecompressFile(data, progress, cts.Token);
                        long decompressedSize = decompressedData.Length;

                        // Prepare decompressed path
                        decompressedPath = txtPath.Text.Replace("-compressed", "-decompressed").Replace(".huff", "." + ext);

                        // Save decompressed file
                        await File.WriteAllBytesAsync(decompressedPath, decompressedData, cts.Token);

                        // Calculate and display compression ratio
                        double ratio = helper.CalculateRatio(size, decompressedSize);
                        this.Invoke(new Action(() =>
                        {
                            lblResults.Text = $"Compressed: {size} bytes\n" +
                                        $"Decompressed: {decompressedSize} bytes\n" +
                                        $"Compression ratio: {ratio:F2}%";

                            MessageBox.Show($"File decompressed successfully!\nSaved as: {decompressedPath}");
                        }));
                    }
                });
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "Decompression canceled";
                // Clean up partial output file if it exists
                if (File.Exists(decompressedPath))
                {
                    try { File.Delete(decompressedPath); }
                    catch { /* Ignore deletion errors */ }
                }
                else if (Directory.Exists(decompressedPath))
                {
                    try { Directory.Delete(decompressedPath, true); }
                    catch { /* Ignore deletion errors */ }
                }
                else { }
                MessageBox.Show("Decompression was canceled.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during decompression: {ex.Message}");
            }
            finally
            {
                await Task.Delay(100);
                ResetUI();
            }
        }

        private void btnPause_Click(object sender, EventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                if (isPaused) return;

                pauseEvent.Reset();
                isPaused = true;

                btnPause.Enabled = false;
                btnResume.Enabled = true;
            }));
        }

        private void btnResume_Click(object sender, EventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                if (!isPaused) return;

                pauseEvent.Set();
                isPaused = false;

                btnPause.Enabled = true;
                btnResume.Enabled = false;
            }));
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.BeginInvoke(new Action(() =>
            {
                cts?.Cancel();
                btnCancel.Enabled = false;
                lblStatus.Text = "Cancelling...";
            }));
        }

        private void ResetPauseEvent()
        {
            isPaused = false;
            pauseEvent?.Dispose();
            pauseEvent = new ManualResetEventSlim(true);
        }

        private void ResetCancellationToken()
        {
            cts?.Dispose();
            cts = new CancellationTokenSource();
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

        private void LockPauseResumeCancel()
        {
            btnCancel.Enabled = false;
            btnPause.Enabled = false;
            btnResume.Enabled = false;
        }

        private void UnLockPauseCancel()
        {
            btnPause.Enabled = true;
            btnCancel.Enabled = true;
            btnResume.Enabled = false;
        }

        private void ResetUI()
        {
            ResetPauseEvent();
            ResetCancellationToken();

            LockUnlockBtn(true);
            LockPauseResumeCancel();

            progressBar.Value = 0;
            lblStatus.Text = "Ready";
            lblResults.Text = string.Empty;
        }
    }
}
