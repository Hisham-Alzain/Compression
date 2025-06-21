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
    public class ShannonFano
    {
        private Helper helper;
        private const int BUFFER_SIZE = 4096;
        private const int DIRECTORY_HEADER_MARKER = 0x44495243; // "DIRC"

        public ShannonFano()
        {
            helper = new Helper();
        }

        // Single file compression (updated to use in-memory)
        public async Task Compress(string inputFile, string outputFile, IProgress<int> progress = null)
        {
            byte[] data = File.ReadAllBytes(inputFile);
            byte[] compressedData = await CompressBytesAsync(data, Path.GetExtension(inputFile), progress);
            File.WriteAllBytes(outputFile, compressedData);
        }

        // Single file decompression (updated to use in-memory)
        public void Decompress(string inputFile, string outputFile, IProgress<int> progress = null)
        {
            byte[] compressedData = File.ReadAllBytes(inputFile);
            var (data, extension) = DecompressBytes(compressedData, progress);

            string finalOutput = string.IsNullOrEmpty(Path.GetExtension(outputFile))
                ? Path.ChangeExtension(outputFile, extension)
                : outputFile;

            File.WriteAllBytes(finalOutput, data);
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
                            result.Enqueue(reverseCodeTable[code]);
                            current = "";
                            break;
                        }
                    }
                });

                return (result.ToArray(), extension);
            }
        }

        // Directory compression (parallel implementation)
        public async Task CompressDirectory(string sourceDirectory, string outputFile, IProgress<int> progress = null)
        {
            var files = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories)
                .Select(f => (FullPath: f, RelativePath: Path.GetRelativePath(sourceDirectory, f)))
                .ToList();

            using (var fs = new FileStream(outputFile, FileMode.Create))
            using (var writer = new BinaryWriter(fs))
            {
                // Write directory marker
                writer.Write(DIRECTORY_HEADER_MARKER);

                // Write root directory name
                string rootName = Path.GetFileName(sourceDirectory);
                writer.Write(rootName);
                writer.Write(files.Count);

                // Write file list
                foreach (var file in files)
                {
                    byte[] pathBytes = Encoding.UTF8.GetBytes(file.RelativePath);
                    writer.Write((short)pathBytes.Length);
                    writer.Write(pathBytes);
                    writer.Write(new FileInfo(file.FullPath).Length);
                }

                // Parallel compression
                var compressedFiles = new ConcurrentDictionary<string, byte[]>();
                int processed = 0;

                await Parallel.ForEachAsync(files, async (file, token) =>
                {
                    byte[] data = File.ReadAllBytes(file.FullPath);
                    string ext = Path.GetExtension(file.FullPath);
                    byte[] compressed = await CompressBytesAsync(data, ext, progress);
                    compressedFiles[file.RelativePath] = compressed;

                    Interlocked.Increment(ref processed);
                    progress?.Report(processed * 100 / files.Count);
                });

                // Write compressed data
                foreach (var file in files)
                {
                    if (compressedFiles.TryGetValue(file.RelativePath, out byte[] data))
                    {
                        writer.Write(data.Length);
                        writer.Write(data);
                    }
                }
            }
        }

        // Directory decompression (parallel implementation)
        public void DecompressDirectory(string inputFile, string outputDirectory, IProgress<int> progress = null)
        {
            using (var fs = new FileStream(inputFile, FileMode.Open))
            using (var reader = new BinaryReader(fs))
            {
                // Verify marker
                int marker = reader.ReadInt32();
                if (marker != DIRECTORY_HEADER_MARKER)
                    throw new InvalidDataException("Invalid directory archive");

                // Read root name
                string rootName = reader.ReadString();
                string outputRoot = Path.Combine(outputDirectory, rootName);
                Directory.CreateDirectory(outputRoot);

                int fileCount = reader.ReadInt32();
                var files = new List<(string Path, long Size)>();

                // Read file list
                for (int i = 0; i < fileCount; i++)
                {
                    short pathLen = reader.ReadInt16();
                    string relPath = Encoding.UTF8.GetString(reader.ReadBytes(pathLen));
                    long size = reader.ReadInt64();
                    files.Add((relPath, size));
                }

                // Read compressed data
                var compressedData = new Dictionary<string, byte[]>();
                foreach (var file in files)
                {
                    int compSize = reader.ReadInt32();
                    compressedData[file.Path] = reader.ReadBytes(compSize);
                }

                // Parallel decompression
                int processed = 0;
                Parallel.ForEach(files, file =>
                {
                    string outputPath = Path.Combine(outputRoot, file.Path);
                    Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

                    var (data, _) = DecompressBytes(compressedData[file.Path], progress);
                    File.WriteAllBytes(outputPath, data);

                    Interlocked.Increment(ref processed);
                    progress?.Report(processed * 100 / files.Count);
                });
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

        private static void BuildShannonFanoTree(List<Node> nodes, int start, int end)
        {
            if (start >= end) return;

            int total = nodes.Skip(start).Take(end - start + 1).Sum(n => n.Frequency);
            int sum1 = 0, sum2 = 0, split = start;
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

            split = left - 1;

            for (int i = start; i <= end; i++)
            {
                nodes[i].Code += (i <= split) ? "1" : "0";
            }

            BuildShannonFanoTree(nodes, start, split);
            BuildShannonFanoTree(nodes, split + 1, end);
        }
    }
}