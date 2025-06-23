using System;
using System.Security.Cryptography;

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
        private String path;

        public MainForm(string path = "")
        {
            registerContextMenuToolStripMenuItem_Click();
            InitializeComponent();
            helper = new Helper();

            if (!string.IsNullOrEmpty(path))
            {
                this.path = path;
                txtPath.Text = path;
            }

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
                openFileDialog.Multiselect = true;
                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = string.Join("|", openFileDialog.FileNames);
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

        private async void ShannonCompress_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPath.Text))
            {
                MessageBox.Show("Please select valid file(s) or folder.");
                return;
            }

            string password = chkUsePassword.Checked ? txtPassword.Text : null;

            string compressedFilePath = "";

            try
            {
                LockUnlockBtn(false);
                progressBar.Value = 0;
                lblStatus.Text = "Starting compression...";
                var progress = new Progress<int>(percent =>
                {
                    progressBar.Value = percent;
                    lblStatus.Text = isPaused ? $"Paused... {percent}%" : $"Compressing... {percent}%";
                });
                UnLockPauseCancel();

                ResetPauseEvent();
                ResetCancellationToken();
                // Initialize shanon and run on a new task
                ShannonFano compressor = new ShannonFano(pauseEvent);
                await Task.Run(async () =>
                {
                    if (File.Exists(txtPath.Text))
                    {
                        // Get data
                        var (data, ext, size) = await helper.Readfile(txtPath.Text);

                        // Compress the data
                        byte[] compressedData = await compressor.Compress(data, ext, password, progress, cts.Token);
                        long compressedSize = compressedData.Length;

                        // Prepare compressed path
                        string fileName = Path.GetFileName(txtPath.Text);
                        string filePath = txtPath.Text.Replace(fileName, "");
                        string compressedName = fileName.Replace("." + ext, "");
                        compressedFilePath = filePath + compressedName + "-compressed.sf";

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
                        compressedFilePath = Path.Combine(distPath, dirName + "-compressed.sf");

                        // Compute original total size
                        long originalSize = 0;
                        foreach (var (fullPath, _) in files)
                            originalSize += new FileInfo(fullPath).Length;

                        // Compress and save the data
                        await compressor.CompressMultipleFiles(files, compressedFilePath, password, progress, cts.Token);

                        // Get compressed size
                        long compressedSize = new FileInfo(compressedFilePath).Length;

                        // Calculate compression ratio
                        double ratio = helper.CalculateRatio(compressedSize, originalSize);

                        this.Invoke(new Action(() =>
                        {
                            lblResults.Text = $"Original: {originalSize} bytes\n" +
                            $"Compressed: {compressedSize} bytes\n" +
                            $"Compression ratio: {ratio:F2}%";

                            MessageBox.Show($"Folder compressed successfully!\nSaved as: {compressedFilePath}");
                        }));
                    }
                    else
                    {
                        // Handle multiple file selection
                        var filePaths = txtPath.Text.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        if (filePaths.Length > 1)
                        {
                            var files = new List<(string FullPath, string RelativePath)>();
                            string commonPath = helper.GetCommonPath(filePaths);

                            foreach (var filePath in filePaths)
                            {
                                string relativePath = filePath.Substring(commonPath.Length).TrimStart(Path.DirectorySeparatorChar);
                                files.Add((filePath, relativePath));
                            }

                            // Prepare compressed path
                            string compressedName = $"{filePaths[0]}-multi-compressed.sf";
                            compressedFilePath = Path.Combine(Path.GetDirectoryName(commonPath), compressedName);

                            // Compress and save the files
                            await compressor.CompressMultipleFiles(files, compressedFilePath, password, progress, cts.Token);

                            // Calculate original size
                            long originalSize = 0;
                            foreach (var (fullPath, _) in files)
                                originalSize += new FileInfo(fullPath).Length;

                            // After compression
                            long compressedSize = new FileInfo(compressedFilePath).Length;
                            double ratio = helper.CalculateRatio(compressedSize, originalSize);

                            this.Invoke(new Action(() =>
                            {
                                lblResults.Text = $"Original: {originalSize} bytes\n" +
                                    $"Compressed: {compressedSize} bytes\n" +
                                    $"Compression ratio: {ratio:F2}%";

                                MessageBox.Show($"{files.Count} files compressed successfully!\nSaved as: {compressedFilePath}");
                            }));
                        }
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

        private async void ShannonDecompress_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPath.Text) || !File.Exists(txtPath.Text))
            {
                MessageBox.Show("Please select a valid file first.");
                return;
            }

            if (!txtPath.Text.EndsWith(".sf"))
            {
                MessageBox.Show("Please select a .sf compressed file.");
                return;
            }

            string password = null;
            if (helper.IsPasswordProtected(txtPath.Text)) // Add this check (same as Huffman)
            {
                using (var passwordForm = new PasswordForm())
                {
                    if (passwordForm.ShowDialog() != DialogResult.OK)
                    {
                        return; // User canceled
                    }
                    password = passwordForm.Password;
                }
            }

            string decompressedPath = "";
            try
            {
                bool isMultiFile;
                LockUnlockBtn(false);
                progressBar.Value = 0;
                lblStatus.Text = "Starting decompression...";
                var progress = new Progress<int>(percent =>
                {
                    progressBar.Value = percent;
                    lblStatus.Text = isPaused ? $"Paused... {percent}%" : $"Decompressing... {percent}%";
                });
                UnLockPauseCancel();

                ResetPauseEvent();
                ResetCancellationToken();
                // Initialize huffman and run on a new task
                ShannonFano decompressor = new ShannonFano(pauseEvent);
                await Task.Run(async () =>
                {
                    // Get data
                    var (data, _, size) = await helper.Readfile(txtPath.Text);

                    // Add password decryption check
                    if (!string.IsNullOrEmpty(password))
                    {
                        var decryptedData = helper.DecryptData(data, password);
                        using (var ms = new MemoryStream(decryptedData))
                        using (var reader = new BinaryReader(ms))
                        {
                            bool isPasswordProtected = reader.ReadBoolean();
                            if (isPasswordProtected && string.IsNullOrEmpty(password))
                            {
                                throw new Exception("This file is password protected");
                            }
                            isMultiFile = reader.ReadBoolean();
                        }
                    }
                    else 
                    { 
                        using (var ms = new MemoryStream(data))
                        using (var reader = new BinaryReader(ms))
                        {
                            bool isPasswordProtected = reader.ReadBoolean();
                            if (isPasswordProtected && string.IsNullOrEmpty(password))
                            {
                                throw new Exception("This file is password protected");
                            }

                            // Read header
                            isMultiFile = reader.ReadBoolean();
                        }
                    }

                    if (isMultiFile)
                    {
                        // Prepare decompressed path
                        decompressedPath = txtPath.Text.Replace("-compressed", "-decompressed").Replace(".sf", "");

                        // Decompress and save the data
                        await decompressor.DecompressMultipleFiles(data, decompressedPath, password, progress, cts.Token);
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show($"Folder decompressed successfully!\nSaved as: {decompressedPath}");
                        }));
                    }
                    else
                    {
                        // Decompress the data
                        (byte[] decompressedData, string ext) = await decompressor.Decompress(data, password, progress, cts.Token);

                        // Prepare decompressed path
                        decompressedPath = txtPath.Text.Replace("-compressed", "-decompressed").Replace(".sf", "." + ext);

                        // Save decompressed file
                        await File.WriteAllBytesAsync(decompressedPath, decompressedData, cts.Token);

                        this.Invoke(new Action(() =>
                        {
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
            catch (CryptographicException) // Add this catch (same as Huffman)
            {
                MessageBox.Show("Incorrect password or corrupted file!");
            }
            catch (Exception ex) when (ex.Message.Contains("password")) // Add this catch (same as Huffman)
            {
                MessageBox.Show("Incorrect password!");
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

        private async void ShannonSelectiveDecompress_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPath.Text) || !File.Exists(txtPath.Text))
            {
                MessageBox.Show("Please select a valid file first.");
                return;
            }

            if (!txtPath.Text.EndsWith(".sf"))
            {
                MessageBox.Show("Please select a .sf compressed file.");
                return;
            }

            string password = null;
            if (helper.IsPasswordProtected(txtPath.Text)) // Add this check (same as Huffman)
            {
                using (var passwordForm = new PasswordForm())
                {
                    if (passwordForm.ShowDialog() != DialogResult.OK)
                    {
                        return; // User canceled
                    }
                    password = passwordForm.Password;
                }
            }

            string decompressedPath = "";
            try
            {
                // Read the compressed file to get file list
                var (data, _, _) = await helper.Readfile(txtPath.Text);

                //
                ResetPauseEvent();
                ResetCancellationToken();
                ShannonFano decompressor = new ShannonFano(pauseEvent);
                var fileList = await decompressor.GetCompressedFileList(data, password);
                //

                // Show file selection dialog
                var selectionForm = new FileSelectionForm(fileList.Select(f => f.RelativePath).ToList());
                if (selectionForm.ShowDialog() != DialogResult.OK ||
                    !selectionForm.SelectedFiles.Any())
                {
                    return;
                }

                // decompress
                LockUnlockBtn(false);
                progressBar.Value = 0;
                lblStatus.Text = "Starting selective decompression...";
                var progress = new Progress<int>(percent =>
                {
                    progressBar.Value = percent;
                    lblStatus.Text = isPaused ? $"Paused... {percent}%" : $"Decompressing... {percent}%";
                });
                UnLockPauseCancel();

                ResetPauseEvent();
                ResetCancellationToken();
                decompressor = new ShannonFano(pauseEvent);

                //string decompressedPath = Path.Combine(
                //    Path.GetDirectoryName(txtPath.Text),
                //    Path.GetFileNameWithoutExtension(txtPath.Text) + "-decompressed");

                await Task.Run(async () =>
                {
                    // Prepare decompressed path
                    decompressedPath = txtPath.Text.Replace("-compressed", "-decompressed").Replace(".sf", "");

                    // Decompress and save the data
                    await decompressor.DecompressSelectedFiles(data, decompressedPath, selectionForm.SelectedFiles, password, progress, cts.Token);

                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show($"{selectionForm.SelectedFiles.Count} files decompressed successfully!\nSaved to: {decompressedPath}");
                    }));
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
            catch (CryptographicException) // Add this catch (same as Huffman)
            {
                MessageBox.Show("Incorrect password or corrupted file!");
            }
            catch (Exception ex) when (ex.Message.Contains("password")) // Add this catch (same as Huffman)
            {
                MessageBox.Show("Incorrect password!");
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

        private async void HuffmanCompress_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtPath.Text))
            {
                MessageBox.Show("Please select valid file(s) or folder.");
                return;
            }

            string password = chkUsePassword.Checked ? txtPassword.Text : null;
            string compressedFilePath = "";
            try
            {
                LockUnlockBtn(false);
                progressBar.Value = 0;
                lblStatus.Text = "Starting compression...";
                var progress = new Progress<int>(percent =>
                {
                    progressBar.Value = percent;
                    lblStatus.Text = isPaused ? $"Paused... {percent}%" : $"Compressing... {percent}%";
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
                        byte[] compressedData = await compressor.CompressFile(data, ext, password, progress, cts.Token);
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
                        // Compute original total size
                        long originalSize = 0;
                        foreach (var (fullPath, _) in files)
                            originalSize += new FileInfo(fullPath).Length;

                        // Compress and save the data
                        await compressor.CompressMultipleFiles(files, compressedFilePath, password, progress, cts.Token);

                        // Get compressed size
                        long compressedSize = new FileInfo(compressedFilePath).Length;

                        // Calculate compression ratio
                        double ratio = helper.CalculateRatio(compressedSize, originalSize);
                        this.Invoke(new Action(() =>
                        {
                            lblResults.Text = $"Original: {originalSize} bytes\n" +
                                              $"Compressed: {compressedSize} bytes\n" +
                                              $"Compression ratio: {ratio:F2}%";

                            MessageBox.Show($"Folder compressed successfully!\nSaved as: {compressedFilePath}");
                        }));
                    }
                    else
                    {
                        // Handle multiple file selection
                        var filePaths = txtPath.Text.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                        if (filePaths.Length > 1)
                        {
                            var files = new List<(string FullPath, string RelativePath)>();
                            string commonPath = helper.GetCommonPath(filePaths);

                            long originalSize = 0;
                            foreach (var (fullPath, _) in files)
                                originalSize += new FileInfo(fullPath).Length;

                            foreach (var filePath in filePaths)
                            {
                                string relativePath = filePath.Substring(commonPath.Length).TrimStart(Path.DirectorySeparatorChar);
                                files.Add((filePath, relativePath));
                            }

                            // Prepare compressed path
                            string compressedName = $"{filePaths[0]}-multi-compressed.huff";
                            compressedFilePath = Path.Combine(Path.GetDirectoryName(commonPath), compressedName);

                            // Compress and save the files
                            await compressor.CompressMultipleFiles(files, compressedFilePath, password, progress, cts.Token);

                            // After compression
                            long compressedSize = new FileInfo(compressedFilePath).Length;
                            double ratio = helper.CalculateRatio(compressedSize, originalSize);

                            this.Invoke(new Action(() =>
                            {
                                lblResults.Text = $"Original: {originalSize} bytes\n" +
                                                  $"Compressed: {compressedSize} bytes\n" +
                                                  $"Compression ratio: {ratio:F2}%";

                                MessageBox.Show($"{files.Count} files compressed successfully!\nSaved as: {compressedFilePath}");
                            }));
                        }
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

            string password = null;
            if (helper.IsPasswordProtected(txtPath.Text))
            {
                using (var passwordForm = new PasswordForm())
                {
                    if (passwordForm.ShowDialog() != DialogResult.OK)
                    {
                        return; // User canceled
                    }
                    password = passwordForm.Password;
                }
            }
            string decompressedPath = "";
            try
            {
                bool isMultiFile;
                LockUnlockBtn(false);
                progressBar.Value = 0;
                lblStatus.Text = "Starting decompression...";
                var progress = new Progress<int>(percent =>
                {
                    progressBar.Value = percent;
                    lblStatus.Text = isPaused ? $"Paused... {percent}%" : $"Decompressing... {percent}%";
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

                    // Try to decrypt if password is provided
                    if (!string.IsNullOrEmpty(password))
                    {
                        var decryptedData = helper.DecryptData(data, password);

                        using (var ms = new MemoryStream(decryptedData))
                        using (var reader = new BinaryReader(ms))
                        {
                            // Read header
                            // Check Password 
                            bool isPasswordProtected = reader.ReadBoolean();
                            if (isPasswordProtected && string.IsNullOrEmpty(password))
                            {
                                throw new Exception("This file is password protected");
                            }

                            // Check file or multiFile
                            isMultiFile = reader.ReadBoolean();
                        }
                    }
                    else
                    {
                        using (var ms = new MemoryStream(data))
                        using (var reader = new BinaryReader(ms))
                        {
                            // Read header
                            // Check Password 
                            bool isPasswordProtected = reader.ReadBoolean();
                            if (isPasswordProtected && string.IsNullOrEmpty(password))
                            {
                                throw new Exception("This file is password protected");
                            }

                            // Check file or multiFile
                            isMultiFile = reader.ReadBoolean();
                        }
                    }

                    if (isMultiFile)
                    {
                        // Prepare decompressed path
                        decompressedPath = txtPath.Text.Replace("-compressed", "-decompressed").Replace(".huff", "");

                        // Decompress and save the data
                        await decompressor.DecompressMultipleFiles(data, decompressedPath, password, progress, cts.Token);
                        this.Invoke(new Action(() =>
                        {
                            MessageBox.Show($"Folder decompressed successfully!\nSaved as: {decompressedPath}");
                        }));
                    }
                    else
                    {
                        // Decompress the data
                        (byte[] decompressedData, string ext) = await decompressor.DecompressFile(data, password, progress, cts.Token);

                        // Prepare decompressed path
                        decompressedPath = txtPath.Text.Replace("-compressed", "-decompressed").Replace(".huff", "." + ext);

                        // Save decompressed file
                        await File.WriteAllBytesAsync(decompressedPath, decompressedData, cts.Token);

                        this.Invoke(new Action(() =>
                        {
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
            catch (CryptographicException)
            {
                MessageBox.Show("Incorrect password or corrupted file!");
            }
            catch (Exception ex) when (ex.Message.Contains("password"))
            {
                MessageBox.Show("Incorrect password!");
                return;
            }
            finally
            {
                await Task.Delay(100);
                ResetUI();
            }
        }

        private async void HuffmanSelectiveDecompress_Click(object sender, EventArgs e)
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

            string password = null;
            if (helper.IsPasswordProtected(txtPath.Text)) // You'll need to implement this check
            {
                using (var passwordForm = new PasswordForm())
                {
                    if (passwordForm.ShowDialog() != DialogResult.OK)
                    {
                        return; // User canceled
                    }
                    password = passwordForm.Password;
                }
            }
            string decompressedPath = "";
            try
            {
                // Read the compressed file to get file list
                var (data, _, _) = await helper.Readfile(txtPath.Text);

                //
                ResetPauseEvent();
                ResetCancellationToken();
                Huffman decompressor = new Huffman(pauseEvent);
                var fileList = await decompressor.GetCompressedFileList(data, password);
                //

                // Show file selection dialog
                var selectionForm = new FileSelectionForm(fileList.Select(f => f.RelativePath).ToList());
                if (selectionForm.ShowDialog() != DialogResult.OK ||
                    !selectionForm.SelectedFiles.Any())
                {
                    return;
                }

                // decompress
                LockUnlockBtn(false);
                progressBar.Value = 0;
                lblStatus.Text = "Starting selective decompression...";
                var progress = new Progress<int>(percent =>
                {
                    progressBar.Value = percent;
                    lblStatus.Text = isPaused ? $"Paused... {percent}%" : $"Decompressing... {percent}%";
                });
                UnLockPauseCancel();

                ResetPauseEvent();
                ResetCancellationToken();
                decompressor = new Huffman(pauseEvent);

                //string decompressedPath = Path.Combine(
                //    Path.GetDirectoryName(txtPath.Text),
                //    Path.GetFileNameWithoutExtension(txtPath.Text) + "-decompressed");

                await Task.Run(async () =>
                {
                    // Prepare decompressed path
                    decompressedPath = txtPath.Text.Replace("-compressed", "-decompressed").Replace(".huff", "");

                    // Decompress and save the data
                    await decompressor.DecompressSelectedFiles(data, decompressedPath, selectionForm.SelectedFiles, password, progress, cts.Token);

                    this.Invoke(new Action(() =>
                    {
                        MessageBox.Show($"{selectionForm.SelectedFiles.Count} files decompressed successfully!\nSaved to: {decompressedPath}");
                    }));
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
            catch (CryptographicException)
            {
                MessageBox.Show("Incorrect password or corrupted file!");
            }
            catch (Exception ex) when (ex.Message.Contains("password"))
            {
                MessageBox.Show("Incorrect password!");
                return;
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

        private void checkBoxPassword(object sender, EventArgs e)
        {
            txtPassword.Visible = chkUsePassword.Checked;
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


        private void registerContextMenuToolStripMenuItem_Click()
        {
            try
            {
                RegistryHelper.RegisterContextMenu();
                MessageBox.Show("Added to Windows context menu!");
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Admin rights required. Run as administrator.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }

        //private void unregisterContextMenuToolStripMenuItem_Click(EventArgs e)
        //{
        //    try
        //    {
        //        RegistryHelper.UnregisterContextMenu();
        //        MessageBox.Show("Removed from Windows context menu!");
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        MessageBox.Show("Admin rights required. Run as administrator.");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Error: {ex.Message}");
        //    }
        //}
    }
}