using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Compression
{
    public class Huffman
    {
        private Helper helper;
        private int bufferSize = 1024 * 1024;

        public Huffman()
        {
            helper = new Helper();
        }

        public async Task CompressDirectory(List<(string FullPath, string RelativePath)> files, string distPath, string dirName, IProgress<int> progress = null)
        {
            string outputPath = Path.Combine(distPath, dirName + "-compressed.huff");
            using (var fs = new FileStream(outputPath, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                writer.Write(true);
                writer.Write(files.Count);

                foreach (var file in files)
                {
                    byte[] pathBytes = Encoding.UTF8.GetBytes(file.RelativePath);
                    writer.Write((short)pathBytes.Length);
                    writer.Write(pathBytes);
                    writer.Write(new FileInfo(file.FullPath).Length);
                }

                var compressedFiles = new ConcurrentDictionary<string, byte[]>();
                int processedFiles = 0;

                await Parallel.ForEachAsync(files, async (file, cancellationToken) =>
                {
                    try
                    {
                        var (data, ext, size) = await helper.Readfile(file.FullPath);
                        byte[] compressedData = await CompressFile(data, ext, progress);
                        compressedFiles.TryAdd(file.RelativePath, compressedData);

                        Interlocked.Increment(ref processedFiles);
                        progress?.Report((int)((double)processedFiles / files.Count * 100));
                    }
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine($"Error compressing {file.FullPath}: {ex.Message}");
                    }
                });

                foreach (var file in files)
                {
                    if (compressedFiles.TryGetValue(file.RelativePath, out var data))
                    {
                        writer.Write(data.Length);
                        writer.Write(data);
                    }
                }
            }
        }

        public async Task DecompressDirectory(byte[] compressedData, string dirPath, IProgress<int> progress = null)
        {
            if (compressedData == null || compressedData.Length == 0)
                return;

            using (var ms = new MemoryStream(compressedData))
            using (var reader = new BinaryReader(ms))
            {
                bool is_dir = reader.ReadBoolean();
                if (!is_dir) return;

                Directory.CreateDirectory(dirPath);
                int fileCount = reader.ReadInt32();

                var fileEntries = new List<(string RelativePath, long OriginalSize)>();
                for (int i = 0; i < fileCount; i++)
                {
                    short pathLength = reader.ReadInt16();
                    string relativePath = Encoding.UTF8.GetString(reader.ReadBytes(pathLength));
                    long originalSize = reader.ReadInt64();
                    fileEntries.Add((relativePath, originalSize));
                }

                for (int i = 0; i < fileEntries.Count; i++)
                {
                    int fileSize = reader.ReadInt32();
                    if (fileSize == 0)
                    {
                        string emptyFilePath = Path.Combine(dirPath, fileEntries[i].RelativePath);
                        Directory.CreateDirectory(Path.GetDirectoryName(emptyFilePath));
                        File.Create(emptyFilePath).Dispose();
                        continue;
                    }

                    byte[] fileData = reader.ReadBytes(fileSize);
                    var (data, _) = DecompressFile(fileData, progress);
                    string outputPath = Path.Combine(dirPath, fileEntries[i].RelativePath);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
                    await File.WriteAllBytesAsync(outputPath, data);
                    progress?.Report((int)((double)(i + 1) / fileCount * 100));
                }
            }
        }

        public async Task<byte[]> CompressFile(byte[] data, string ext, IProgress<int> progress = null)
        {
            if (data == null || data.Length == 0)
                return new byte[0];

            Dictionary<byte, int> frequencies = await helper.CalculateFrequencies(data);
            var root = BuildHuffmanTree(frequencies);

            var codes = new Dictionary<byte, string>();
            GenerateCodes(root, "", codes);

            var bitString = new StringBuilder();
            int processedBytes = 0;
            foreach (byte b in data)
            {
                bitString.Append(codes[b]);
                processedBytes++;

                if (processedBytes % 1000 == 0) // Report progress every 1000 bytes
                {
                    progress?.Report((int)((double)processedBytes / data.Length * 100));
                }
            }

                int totalBits = bitString.Length;
            int totalBytes = (totalBits + 7) / 8;
            byte[] compressedBytes = new byte[totalBytes];

            int byteIndex = 0, bitIndex = 0;
            for (int i = 0; i < totalBits; i++)
            {
                if (bitString[i] == '1')
                    compressedBytes[byteIndex] |= (byte)(1 << (7 - bitIndex));

                bitIndex++;
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
            }

            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                writer.Write(false);
                writer.Write((byte)ext.Length);
                writer.Write(ext.ToCharArray());
                writer.Write(frequencies.Count);
                foreach (var pair in frequencies)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value);
                }
                writer.Write(totalBits);
                writer.Write(compressedBytes);
                return ms.ToArray();
            }
        }

        public (byte[] data, string ext) DecompressFile(byte[] compressedData, IProgress<int> progress = null)
        {
            if (compressedData == null || compressedData.Length == 0)
                return (new byte[0], null);

            using (var ms = new MemoryStream(compressedData))
            using (var reader = new BinaryReader(ms))
            {
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

                int totalBits = reader.ReadInt32();
                var root = BuildHuffmanTree(frequencies);

                byte[] compressedBytes = reader.ReadBytes((int)(ms.Length - ms.Position));

                var bitString = new StringBuilder();
                for (int i = 0; i < compressedBytes.Length && bitString.Length < totalBits; i++)
                {
                    for (int j = 7; j >= 0 && bitString.Length < totalBits; j--)
                    { 
                        bitString.Append((compressedBytes[i] & (1 << j)) != 0 ? '1' : '0');
                    }
                    if (i % 1000 == 0) // Report progress every 1000 bytes
                    {
                        progress?.Report((int)((double)i / compressedBytes.Length * 100));
                    }
                }

                var decompressedData = new List<byte>();
                var current = root;
                for (int i = 0; i < totalBits; i++)
                {
                    current = bitString[i] == '0' ? current.Left : current.Right;
                    if (current.IsLeaf())
                    {
                        decompressedData.Add(current.Symbol);
                        current = root;
                    }
                }

                return (decompressedData.ToArray(), originalExtension);
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
