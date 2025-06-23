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
    public class ShannonFano
    {
        private readonly Helper helper;
        private readonly ManualResetEventSlim pauseEvent;

        public ShannonFano(ManualResetEventSlim pauseEvent)
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
                        byte[] compressedData = await Compress(data, ext, password, progress, ct);
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
            // Encrypt the entire archive if password is provided
            if (!string.IsNullOrEmpty(password))
            {
                byte[] fileBytes = await File.ReadAllBytesAsync(outputPath);
                byte[] encryptedBytes = helper.EncryptData(fileBytes, password);
                await File.WriteAllBytesAsync(outputPath, encryptedBytes, cancellationToken);
            }
        }



        // Single file compression (updated to use in-memory)

        public async Task<byte[]> Compress(byte[] data, string ext, string password = null, IProgress<int> progress = null, CancellationToken cancellationToken = default)
        {
            if (data == null || data.Length == 0)
                return new byte[0];

            Dictionary<byte, int> frequencies = await helper.CalculateFrequencies(data);
            var root = BuildShannonFanoTree(frequencies);

            var codes = new Dictionary<byte, string>();
            GenerateCodes(root, "", codes);

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

                // Calculate and write total bits needed
                long totalBits = data.Sum(b => codes[b].Length);
                writer.Write(totalBits);

                // Write compressed data
                int processedBytes = 0;
                foreach (byte b in data)
                {
                    pauseEvent.Wait(cancellationToken);
                    cancellationToken.ThrowIfCancellationRequested();

                    foreach (char bit in codes[b])
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        bitWriter.WriteBit(bit == '1');
                    }

                    processedBytes++;
                    if (processedBytes % 1000 == 0)
                    {
                        progress?.Report((int)((double)processedBytes / data.Length * 100));
                    }
                }

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

                        var (data, _) = await Decompress(fileData, password, progress, cancellationToken);
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

                        var (data, _) = await Decompress(fileData, password, progress, cancellationToken);
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

        public async Task<(byte[] data, string ext)> Decompress(byte[] compressedData, string password = null, IProgress<int> progress = null, CancellationToken cancellationToken = default)
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

                    bool is_dir = reader.ReadBoolean();
                    if (is_dir) return (new byte[0], null);

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
                    var root = BuildShannonFanoTree(frequencies);

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

        // Main compression logic (returns compressed bytes)
        private async Task<byte[]> CompressBytesAsync(byte[] data, string originalExtension, IProgress<int> progress = null)
        {
            if (data == null || data.Length == 0)
                return Array.Empty<byte>();

            Dictionary<byte, int> frequencies = await helper.CalculateFrequencies(data);
            List<Node> symbols = CreateSymbol(frequencies);
            BuildShannonFanoTree(symbols, 0, symbols.Count - 1);

            Dictionary<byte, string> codeTable = symbols.ToDictionary(n => n.Symbol, n => n.Code);

            // Generate encoded bits
            StringBuilder encodedBits = new StringBuilder();
            foreach (byte b in data)
            {
                encodedBits.Append(codeTable[b]);
            }

            // Pad bits
            int padding = (8 - (encodedBits.Length % 8)) % 8;
            encodedBits.Append('0', padding);

            // Convert to bytes
            int byteCount = encodedBits.Length / 8;
            byte[] compressedData = new byte[byteCount];
            Parallel.For(0, byteCount, i =>
            {
                compressedData[i] = Convert.ToByte(encodedBits.ToString().Substring(i * 8, 8), 2);
            });

            // Write to memory stream
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                // Write header
                string ext = originalExtension.TrimStart('.');
                if (string.IsNullOrEmpty(ext)) ext = "bin";

                writer.Write((byte)ext.Length);
                writer.Write(ext.ToCharArray());
                writer.Write((ushort)symbols.Count);

                // Write symbol table in parallel
                var sync = new object();
                Parallel.ForEach(symbols, symbol =>
                {
                    lock (sync)
                    {
                        writer.Write(symbol.Symbol);
                        writer.Write(symbol.Frequency);
                    }
                });

                // Write compressed data
                writer.Write(compressedData);
                return ms.ToArray();
            }
        }

        // Main decompression logic (returns decompressed data)
        private (byte[] data, string extension) DecompressBytes(byte[] compressedData, IProgress<int> progress = null)
        {
            using (var ms = new MemoryStream(compressedData))
            using (var reader = new BinaryReader(ms))
            {
                // Read header
                byte extLen = reader.ReadByte();
                string extension = new string(reader.ReadChars(extLen));
                ushort symbolCount = reader.ReadUInt16();

                Dictionary<byte, int> frequencies = new Dictionary<byte, int>();
                for (int i = 0; i < symbolCount; i++)
                {
                    byte symbol = reader.ReadByte();
                    int freq = reader.ReadInt32();
                    frequencies[symbol] = freq;
                }

                // Read compressed data
                byte[] compressedContent = reader.ReadBytes((int)(ms.Length - ms.Position));
                string encodedBits = string.Join("", compressedContent.Select(b => Convert.ToString(b, 2).PadLeft(8, '0')));

                // Build tree
                List<Node> symbols = CreateSymbol(frequencies);
                BuildShannonFanoTree(symbols, 0, symbols.Count - 1);
                Dictionary<string, byte> reverseCodeTable = symbols.ToDictionary(n => n.Code, n => n.Symbol);

                // Decode in parallel
                var result = new ConcurrentBag<byte>();
                var codes = new ConcurrentQueue<string>(symbols.Select(n => n.Code).OrderByDescending(c => c.Length));
                string current = "";

                Parallel.ForEach(encodedBits, (bit, state, index) =>
                {
                    current += bit == '1' ? "1" : "0";
                    foreach (var code in codes)
                    {
                        if (current == code)
                        {
                            //result.Enqueue(reverseCodeTable[code]);
                            current = "";
                            break;
                        }
                    }
                });

                return (result.ToArray(), extension);
            }
        }

        // Helper methods remain unchanged
        private static List<Node> CreateSymbol(Dictionary<byte, int> frequencies)
        {
            return frequencies.Select(kvp => new Node
            {
                Symbol = kvp.Key,
                Frequency = kvp.Value,
                Code = ""
            })
            .OrderByDescending(n => n.Frequency)
            .ToList();
        }

        private Node BuildShannonFanoTree(Dictionary<byte, int> frequencies)
        {
            // Convert frequency dictionary to nodes and sort by frequency (descending)
            var nodes = frequencies.Select(p => new Node
            {
                Symbol = p.Key,
                Frequency = p.Value
            }).OrderByDescending(n => n.Frequency).ToList();

            return BuildShannonFanoTree(nodes, 0, nodes.Count - 1);
        }

        private Node BuildShannonFanoTree(List<Node> nodes, int start, int end)
        {
            if (start == end)
            {
                return nodes[start];
            }

            int total = nodes.Skip(start).Take(end - start + 1).Sum(n => n.Frequency);
            int sum1 = 0, sum2 = 0;
            int left = start, right = end;

            while (left <= right)
            {
                if (sum1 <= sum2)
                {
                    sum1 += nodes[left++].Frequency;
                }
                else
                {
                    sum2 += nodes[right--].Frequency;
                }
            }

            int split = left - 1;

            // Recursively build subtrees
            Node leftChild = BuildShannonFanoTree(nodes, start, split);
            Node rightChild = BuildShannonFanoTree(nodes, split + 1, end);

            return new Node
            {
                Frequency = leftChild.Frequency + rightChild.Frequency,
                Left = leftChild,
                Right = rightChild
            };
        }

        private void GenerateCodes(Node node, string code, Dictionary<byte, string> codes)
        {
            if (node == null) return;

            if (node.IsLeaf())
            {
                node.Code = code;
                codes[node.Symbol] = code;
                return;
            }

            GenerateCodes(node.Left, code + "0", codes);
            GenerateCodes(node.Right, code + "1", codes);
        }

        public void Pause()
        {
            pauseEvent.Reset();
        }

        public void Resume()
        {
            pauseEvent.Set();
        }
    }
}