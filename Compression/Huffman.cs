using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Compression
{
    public class Huffman
    {
        private readonly Helper helper;
        private readonly ManualResetEventSlim pauseEvent;

        public Huffman(ManualResetEventSlim pauseEvent)
        {
            this.helper = new Helper();
            this.pauseEvent = pauseEvent;
        }

        public async Task CompressMultipleFiles(List<(string FullPath, string RelativePath)> files, string outputPath, string password = null, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            using (var fs = new FileStream(outputPath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                // Write password protection flag first
                writer.Write(!string.IsNullOrEmpty(password));

                writer.Write(true); // Is multi-file archive (not necessarily a directory)
                writer.Write(files.Count);

                foreach (var file in files)
                {
                    // Encode the string relative path in bytes
                    byte[] pathBytes = Encoding.UTF8.GetBytes(file.RelativePath);
                    writer.Write((short)pathBytes.Length);
                    writer.Write(pathBytes);

                    // Get the file metadata and store its size
                    writer.Write(new FileInfo(file.FullPath).Length);
                }

                var compressedFiles = new ConcurrentDictionary<string, byte[]>();
                int processedFiles = 0;

                await Parallel.ForEachAsync(files, cancellationToken, async (file, ct) =>
                {
                    pauseEvent.Wait(ct); // Wait if paused

                    try
                    {
                        var (data, ext, size) = await helper.Readfile(file.FullPath);
                        byte[] compressedData = await CompressFile(data, ext, password, progress, ct);
                        compressedFiles.TryAdd(file.RelativePath, compressedData);

                        // Update progressBar
                        Interlocked.Increment(ref processedFiles);
                        progress?.Report((int)((double)processedFiles / files.Count * 100));
                        //await Task.Delay(1, cancellationToken); // Let UI process
                    }
                    catch (OperationCanceledException)
                    {
                        compressedFiles.Clear();
                        throw;
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine($"Error compressing {file.FullPath}: {e.Message}");
                    }
                });

                // Write compressed data
                foreach (var file in files)
                {
                    if (compressedFiles.TryGetValue(file.RelativePath, out var data))
                    {
                        writer.Write(data.Length);
                        writer.Write(data);
                    }
                }
            }

            //encrypt the entire archive if password is provided
            if (!string.IsNullOrEmpty(password))
            {
                byte[] fileBytes = await File.ReadAllBytesAsync(outputPath);
                byte[] encryptedBytes = helper.EncryptData(fileBytes, password);
                await File.WriteAllBytesAsync(outputPath, encryptedBytes, cancellationToken);
            }
        }

        public async Task<List<(string RelativePath, long OriginalSize)>> GetCompressedFileList(byte[] compressedData, string password = null)
        {
            try
            {
                // Try to decrypt if password is provided
                if (!string.IsNullOrEmpty(password))
                {
                    compressedData = helper.DecryptData(compressedData, password);
                }

                using (var ms = new MemoryStream(compressedData))
                using (var reader = new BinaryReader(ms))
                {
                    bool isPasswordProtected = reader.ReadBoolean();
                    if (isPasswordProtected && string.IsNullOrEmpty(password))
                    {
                        throw new Exception("This file is password protected");
                    }

                    bool isMultiFile = reader.ReadBoolean();
                    if (!isMultiFile) return new List<(string, long)>();

                    int fileCount = reader.ReadInt32();
                    var fileList = new List<(string, long)>(fileCount);

                    for (int i = 0; i < fileCount; i++)
                    {
                        // Read the stored bytes length
                        short pathLength = reader.ReadInt16();

                        // Read the bytes and decode it back to string (relative path)
                        string relativePath = Encoding.UTF8.GetString(reader.ReadBytes(pathLength));

                        // Get stored size
                        long originalSize = reader.ReadInt64();

                        // Add the file to list
                        fileList.Add((relativePath, originalSize));
                    }

                    return fileList;
                }
            }
            catch (CryptographicException)
            {
                throw new Exception("Incorrect password or corrupted file");
            }
        }

        public async Task DecompressSelectedFiles(byte[] compressedData, string outputDir,
        List<string> filesToExtract, string password = null, IProgress<int> progress = null,
        CancellationToken cancellationToken = default)
        {
            if (compressedData == null || compressedData.Length == 0)
                return;

            if (filesToExtract == null || filesToExtract.Count == 0)
                return; // Nothing to extract

            try
            {
                // Try to decrypt if password is provided
                if (!string.IsNullOrEmpty(password))
                {
                    compressedData = helper.DecryptData(compressedData, password);
                }

                using (var ms = new MemoryStream(compressedData))
                using (var reader = new BinaryReader(ms))
                {
                    bool isPasswordProtected = reader.ReadBoolean();
                    if (isPasswordProtected && string.IsNullOrEmpty(password))
                    {
                        throw new Exception("This file is password protected");
                    }

                    bool isMultiFile = reader.ReadBoolean();
                    if (!isMultiFile) return;

                    int fileCount = reader.ReadInt32();
                    var fileEntries = new List<(string Path, long Size, int Offset, int Length)>();

                    // Read file index
                    for (int i = 0; i < fileCount; i++)
                    {
                        // Pause or Cancel
                        pauseEvent.Wait(cancellationToken); // Wait if paused
                        cancellationToken.ThrowIfCancellationRequested();
                        //

                        // Read the stored bytes length
                        short pathLength = reader.ReadInt16();

                        // Read the bytes and decode it back to string (relative path)
                        string relativePath = Encoding.UTF8.GetString(reader.ReadBytes(pathLength));

                        // Get stored size
                        long originalSize = reader.ReadInt64();

                        // Add the file to list
                        fileEntries.Add((relativePath, originalSize, 0, 0));
                    }

                    // Read data offsets
                    for (int i = 0; i < fileCount; i++)
                    {
                        // Read the compressed data length
                        int dataLength = reader.ReadInt32();

                        // Update data offset 
                        var entry = fileEntries[i];
                        fileEntries[i] = (entry.Path, entry.Size, (int)ms.Position, dataLength);
                        ms.Seek(dataLength, SeekOrigin.Current);
                    }

                    // Process selected files
                    int processedFiles = 0;
                    foreach (var entry in fileEntries)
                    {
                        // Check if file is to be extracted 
                        if (!filesToExtract.Contains(entry.Path)) continue;

                        // Pause or Cancel
                        pauseEvent.Wait(cancellationToken); // Wait if paused
                        cancellationToken.ThrowIfCancellationRequested();
                        //

                        ms.Seek(entry.Offset, SeekOrigin.Begin);
                        byte[] fileData = reader.ReadBytes(entry.Length);

                        var (data, _) = await DecompressFile(fileData, password, progress, cancellationToken);
                        string outputPath = Path.Combine(outputDir, entry.Path);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                        // Write file
                        await File.WriteAllBytesAsync(outputPath, data, cancellationToken);

                        processedFiles++;
                        progress?.Report((int)((double)processedFiles / filesToExtract.Count * 100));
                    }
                }
            }
            catch (CryptographicException)
            {
                throw new Exception("Incorrect password or corrupted file");
            }
        }

        public async Task DecompressMultipleFiles(byte[] compressedData, string dirPath, string password = null, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (compressedData == null || compressedData.Length == 0)
                return;

            try
            {
                // Try to decrypt if password is provided
                if (!string.IsNullOrEmpty(password))
                {
                    compressedData = helper.DecryptData(compressedData, password);
                }

                using (var ms = new MemoryStream(compressedData))
                using (var reader = new BinaryReader(ms))
                {
                    bool isPasswordProtected = reader.ReadBoolean();
                    if (isPasswordProtected && string.IsNullOrEmpty(password))
                    {
                        throw new Exception("This file is password protected");
                    }

                    bool isMultiFile = reader.ReadBoolean();
                    if (!isMultiFile) return;

                    Directory.CreateDirectory(dirPath);
                    int fileCount = reader.ReadInt32();

                    var fileEntries = new List<(string RelativePath, long OriginalSize)>();
                    for (int i = 0; i < fileCount; i++)
                    {
                        // Pause or Cancel
                        pauseEvent.Wait(cancellationToken); // Wait if paused
                        cancellationToken.ThrowIfCancellationRequested();
                        //

                        // Read the stored bytes length
                        short pathLength = reader.ReadInt16();

                        // Read the bytes and decode it back to string (relative path)
                        string relativePath = Encoding.UTF8.GetString(reader.ReadBytes(pathLength));

                        // Get stored size
                        long originalSize = reader.ReadInt64();

                        // Add the file to list
                        fileEntries.Add((relativePath, originalSize));
                    }

                    int processedFiles = 0;
                    // await Parallel.ForEachAsync(fileEntries, async (entry, cancellationToken) =>
                    // {
                    foreach (var entry in fileEntries)
                    {
                        // Pause or Cancel
                        pauseEvent.Wait(cancellationToken); // Wait if paused
                        cancellationToken.ThrowIfCancellationRequested();
                        //

                        int fileSize = reader.ReadInt32();
                        byte[] fileData = reader.ReadBytes(fileSize);

                        var (data, _) = await DecompressFile(fileData, password, progress, cancellationToken);
                        string outputPath = Path.Combine(dirPath, entry.RelativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                        // Write file
                        await File.WriteAllBytesAsync(outputPath, data, cancellationToken);

                        // Update progressBar
                        Interlocked.Increment(ref processedFiles);
                        progress?.Report((int)((double)processedFiles / fileCount * 100));
                        //await Task.Delay(1, cancellationToken); // Let UI process
                    //});
                    }
                }
            }
            catch (CryptographicException)
            {
                throw new Exception("Incorrect password or corrupted file");
            }
        }

        public async Task<byte[]> CompressFile(byte[] data, string ext, string password = null, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0)
                return new byte[0];

            Dictionary<byte, int> frequencies = await helper.CalculateFrequencies(data);
            var root = BuildHuffmanTree(frequencies);

            var codes = new Dictionary<byte, string>();
            GenerateCodes(root, "", codes);

            // Write header + frequency table
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                var bitWriter = new BitWriter(writer);

                // Write password protection flag first
                writer.Write(!string.IsNullOrEmpty(password));

                // Write header info
                writer.Write(false); // Not a directory
                writer.Write((byte)ext.Length);
                writer.Write(ext.ToCharArray());
                writer.Write(frequencies.Count);
                foreach (var pair in frequencies)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value);
                }

                // First pass: calculate total bits needed
                long totalBits = 0;
                foreach (byte b in data)
                {
                    totalBits += codes[b].Length;
                }
                writer.Write(totalBits);

                // Second pass: write actual bits
                int processedBytes = 0;
                foreach (byte b in data)
                {
                    // Pause or Cancel
                    pauseEvent.Wait(cancellationToken); // Wait if paused
                    cancellationToken.ThrowIfCancellationRequested();
                    //
                    string code = codes[b];
                    foreach (char bit in code)
                    {
                        // Cancel
                        cancellationToken.ThrowIfCancellationRequested();
                        //
                        bitWriter.WriteBit(bit == '1');
                    }

                    processedBytes++;
                    if (processedBytes % 1000 == 0) // Report progress every 1000 bytes
                    {
                        // Update progressBar
                        progress?.Report((int)((double)processedBytes / data.Length * 100));
                        //await Task.Delay(1, cancellationToken); // Let UI process
                    }
                }

                // Flush stream
                bitWriter.Flush();

                byte[] compressedBytes = ms.ToArray();
                // Encrypt if password is provided
                if (!string.IsNullOrEmpty(password))
                {
                    compressedBytes = helper.EncryptData(compressedBytes, password);
                }

                return compressedBytes;
            }
        }

        public async Task<(byte[] data, string ext)> DecompressFile(byte[] compressedData, string password = null, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (compressedData == null || compressedData.Length == 0)
                return (new byte[0], null);
            try
            {
                // Try to decrypt if password is provided
                if (!string.IsNullOrEmpty(password))
                {
                    compressedData = helper.DecryptData(compressedData, password);
                }

                using (var ms = new MemoryStream(compressedData))
                using (var reader = new BinaryReader(ms))
                {
                    bool isPasswordProtected = reader.ReadBoolean();
                    if (isPasswordProtected && string.IsNullOrEmpty(password))
                    {
                        throw new Exception("This file is password protected");
                    }

                    bool isMultiFile = reader.ReadBoolean();
                    if (isMultiFile) return (new byte[0], null);

                    byte extensionLength = reader.ReadByte();
                    string originalExtension = new string(reader.ReadChars(extensionLength));

                    int count = reader.ReadInt32();
                    var frequencies = new Dictionary<byte, int>();
                    for (int i = 0; i < count; i++)
                    {
                        byte symbol = reader.ReadByte();
                        int frequency = reader.ReadInt32();
                        frequencies[symbol] = frequency;
                    }

                    long totalBits = reader.ReadInt64();
                    var root = BuildHuffmanTree(frequencies);

                    var bitReader = new BitReader(reader);
                    var decompressedData = new List<byte>();
                    long bitsRead = 0;

                    while (bitsRead < totalBits)
                    {
                        // Pause or Cancel
                        pauseEvent.Wait(cancellationToken); // Wait if paused
                        cancellationToken.ThrowIfCancellationRequested();
                        //
                        var node = root;
                        while (!node.IsLeaf())
                        {
                            // Cancel
                            cancellationToken.ThrowIfCancellationRequested();
                            //

                            bool? bit = bitReader.ReadBit();
                            if (bit == null) break;

                            bitsRead++;
                            node = bit.Value ? node.Right : node.Left;
                        }

                        if (node.IsLeaf())
                        {
                            decompressedData.Add(node.Symbol);
                        }

                        if (bitsRead % 1000 == 0) // Report progress every 1000 bits
                        {
                            // Update progressBar
                            progress?.Report((int)((double)bitsRead / totalBits * 100));
                            //await Task.Delay(1, cancellationToken); // Let UI process
                        }
                    }

                    return (decompressedData.ToArray(), originalExtension);
                }
            }
            catch (CryptographicException)
            {
                throw new Exception("Incorrect password or corrupted file");
            }
        }

        private Node BuildHuffmanTree(Dictionary<byte, int> frequencies)
        {
            var priorityQueue = new PriorityQueue<Node>();
            foreach (var symbol in frequencies)
            {
                priorityQueue.Enqueue(new Node
                {
                    Symbol = symbol.Key,
                    Frequency = symbol.Value,
                    Code = ""
                });
            }

            while (priorityQueue.Count > 1)
            {
                var left = priorityQueue.Dequeue();
                var right = priorityQueue.Dequeue();
                var parent = new Node
                {
                    Frequency = left.Frequency + right.Frequency,
                    Code = "",
                    Left = left,
                    Right = right
                };
                priorityQueue.Enqueue(parent);
            }

            return priorityQueue.Dequeue();
        }

        private void GenerateCodes(Node node, string code, Dictionary<byte, string> codes)
        {
            if (node.IsLeaf())
            {
                codes[node.Symbol] = code;
                node.Code = code;
                return;
            }

            GenerateCodes(node.Left, code + "0", codes);
            GenerateCodes(node.Right, code + "1", codes);
        }
    }

    public class PriorityQueue<T> where T : IComparable<T>
    {
        private List<T> data = new List<T>();

        public void Enqueue(T item)
        {
            data.Add(item);
            int ci = data.Count - 1;
            while (ci > 0)
            {
                int pi = (ci - 1) / 2;
                if (data[ci].CompareTo(data[pi]) >= 0) break;
                (data[ci], data[pi]) = (data[pi], data[ci]);
                ci = pi;
            }
        }

        public T Dequeue()
        {
            if (data.Count == 0) throw new InvalidOperationException("Queue is empty");

            int li = data.Count - 1;
            T frontItem = data[0];
            data[0] = data[li];
            data.RemoveAt(li);

            --li;
            int pi = 0;
            while (true)
            {
                int ci = pi * 2 + 1;
                if (ci > li) break;
                int rc = ci + 1;
                if (rc <= li && data[rc].CompareTo(data[ci]) < 0) ci = rc;
                if (data[pi].CompareTo(data[ci]) <= 0) break;
                (data[pi], data[ci]) = (data[ci], data[pi]);
                pi = ci;
            }
            return frontItem;
        }

        public int Count => data.Count;
    }
}
