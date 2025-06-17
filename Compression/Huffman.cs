using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compression
{
    public class Huffman
    {
        private Helper helper;

        public Huffman()
        {
            helper = new Helper();
        }

        public byte[] Compress(byte[] data, string ext)
        {
            if (data == null || data.Length == 0)
                return new byte[0];

            // Calculate frequencies
            Dictionary<byte, int> frequencies = helper.CalculateFrequencies(data);

            // Build Huffman tree
            var root = BuildHuffmanTree(frequencies);

            // Generate Huffman codes
            var codes = new Dictionary<byte, string>();
            GenerateCodes(root, "", codes);

            // Encode the data
            var bitString = new StringBuilder();
            foreach (byte b in data)
            {
                bitString.Append(codes[b]);
            }

            // Convert bit string to bytes with exact bit count
            int totalBits = bitString.Length;
            int totalBytes = (totalBits + 7) / 8;
            byte[] compressedBytes = new byte[totalBytes];
            int byteIndex = 0, bitIndex = 0;

            for (int i = 0; i < totalBits; i++)
            {
                if (bitString[i] == '1')
                {
                    compressedBytes[byteIndex] |= (byte)(1 << (7 - bitIndex));
                }

                bitIndex++;
                if (bitIndex == 8)
                {
                    bitIndex = 0;
                    byteIndex++;
                }
            }

            // Prepare final output (header + compressed data)
            using (var ms = new MemoryStream())
            using (var writer = new BinaryWriter(ms))
            {
                // Write header
                // 1. Original extension length (1 byte)
                writer.Write((byte)ext.Length);
                // 2. Original extension (ASCII)
                writer.Write(ext.ToCharArray());
                // 3. frequency table and total bits
                writer.Write(frequencies.Count);
                foreach (var pair in frequencies)
                {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value);
                }
                writer.Write(totalBits); // Store exact number of bits

                // Write compressed data
                writer.Write(compressedBytes);

                return ms.ToArray();
            }
        }

        public (byte[] data, string ext) Decompress(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0)
                return (data: new byte[0], ext: null);

            using (var ms = new MemoryStream(compressedData))
            using (var reader = new BinaryReader(ms))
            {
                // Read header
                // 1. Original extension length
                byte extensionLength = reader.ReadByte();
                // 2. Original extension
                string originalExtension = new string(reader.ReadChars(extensionLength));
                // 3. Read frequency table
                int count = reader.ReadInt32();
                var frequencies = new Dictionary<byte, int>();
                for (int i = 0; i < count; i++)
                {
                    byte symbol = reader.ReadByte();
                    int frequency = reader.ReadInt32();
                    frequencies[symbol] = frequency;
                }

                // Read exact number of bits
                int totalBits = reader.ReadInt32();

                // Rebuild Huffman tree
                var root = BuildHuffmanTree(frequencies);

                // Read compressed data
                byte[] compressedBytes = reader.ReadBytes((int)(ms.Length - ms.Position));

                // Convert bytes back to bits
                var bitString = new StringBuilder();
                for (int i = 0; i < compressedBytes.Length && bitString.Length < totalBits; i++)
                {
                    for (int j = 7; j >= 0 && bitString.Length < totalBits; j--)
                    {
                        bitString.Append((compressedBytes[i] & (1 << j)) != 0 ? '1' : '0');
                    }
                }

                // Decode the data
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

                return (data: decompressedData.ToArray(), ext: originalExtension);
            }
        }

        private Node BuildHuffmanTree(Dictionary<byte, int> frequencies)
        {
            var priorityQueue = new PriorityQueue<Node>();

            foreach (var symbol in frequencies)
            {
                priorityQueue.Enqueue(new Node()
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

                var parent = new Node()
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
        private List<T> data;

        public PriorityQueue()
        {
            this.data = new List<T>();
        }

        public void Enqueue(T item)
        {
            data.Add(item);
            int ci = data.Count - 1;
            while (ci > 0)
            {
                int pi = (ci - 1) / 2;
                if (data[ci].CompareTo(data[pi]) >= 0)
                    break;
                T tmp = data[ci]; data[ci] = data[pi]; data[pi] = tmp;
                ci = pi;
            }
        }

        public T Dequeue()
        {
            if (data.Count == 0)
                throw new InvalidOperationException("Queue is empty");

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
                if (rc <= li && data[rc].CompareTo(data[ci]) < 0)
                    ci = rc;
                if (data[pi].CompareTo(data[ci]) <= 0) break;
                T tmp = data[pi]; data[pi] = data[ci]; data[ci] = tmp;
                pi = ci;
            }
            return frontItem;
        }

        public int Count
        {
            get { return data.Count; }
        }
    }
}
