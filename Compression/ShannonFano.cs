namespace Compression
{
    public class ShannonFano
    {
        private Helper helper;
        private const int BUFFER_SIZE = 4096; // For handling large files
        public ShannonFano()
        {
            helper = new Helper();
        }

        public async void Compress(string inputFile, string outputFile)
        {
            byte[] data = File.ReadAllBytes(inputFile);
            Dictionary<byte, int> frequencies = await helper.CalculateFrequencies(data);
            List<Node> symbols = CreateSymbol(frequencies);

            BuildShannonFanoTree(symbols, 0, symbols.Count - 1);

            //important
            Dictionary<byte, string> codeTable = symbols.ToDictionary(n => n.Symbol, n => n.Code);

            string encodedBits = string.Join("", data.Select(b => codeTable[b]));

            // Pad the bits to make them divisible by 8
            int padding = (8 - (encodedBits.Length % 8)) % 8;
            encodedBits += new string('0', padding);

            // Convert bits to bytes
            byte[] compressedData = new byte[encodedBits.Length / 8];
            for (int i = 0; i < compressedData.Length; i++)
            {
                string byteStr = encodedBits.Substring(i * 8, 8);
                compressedData[i] = Convert.ToByte(byteStr, 2);
            }

            // Get original file extension (without the dot)
            string originalExtension = Path.GetExtension(inputFile).TrimStart('.');
            if (string.IsNullOrEmpty(originalExtension))
            {
                originalExtension = "bin"; // Default for files without extension
            }

            // Write metadata(header) and compressed data
            using (FileStream input = new FileStream(inputFile, FileMode.Open))
            using (FileStream output = new FileStream(outputFile, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(output))
            {
                // Write header
                // 1. Original extension length (1 byte)
                writer.Write((byte)originalExtension.Length);
                // 2. Original extension (ASCII)
                writer.Write(originalExtension.ToCharArray());
                // 3. Symbol count (2 bytes)
                writer.Write((ushort)symbols.Count);
                // 4. Symbol-frequency pairs
                foreach (var symbol in symbols)
                {
                    writer.Write(symbol.Symbol);
                    writer.Write(symbol.Frequency);
                }

                // Write compressed data
                BitWriter bitWriter = new BitWriter(writer);
                byte[] buffer = new byte[BUFFER_SIZE];
                int bytesRead;
                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    for (int i = 0; i < bytesRead; i++)
                    {
                        string code = codeTable[buffer[i]];
                        foreach (char c in code)
                        {
                            bitWriter.WriteBit(c == '1');
                        }
                    }
                }
                bitWriter.Flush();
            }
        }

        public void Decompress(string inputFile, string outputFile)
        {
            string finalOutputFile = outputFile; // Store the original output filename

            using (FileStream input = new FileStream(inputFile, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(input))
            {
                // Read header
                // 1. Original extension length
                byte extensionLength = reader.ReadByte();
                // 2. Original extension
                string originalExtension = new string(reader.ReadChars(extensionLength));
                // 3. Symbol count
                ushort symbolCount = reader.ReadUInt16();
                // 4. Symbol-frequency pairs
                Dictionary<byte, int> frequencies = new Dictionary<byte, int>();
                for (int i = 0; i < symbolCount; i++)
                {
                    byte symbol = reader.ReadByte();
                    int frequency = reader.ReadInt32();
                    frequencies[symbol] = frequency;
                }

                // Restore original file extension if outputFile doesn't have one
                if (string.IsNullOrEmpty(Path.GetExtension(outputFile)))
                {
                    finalOutputFile = Path.ChangeExtension(outputFile, originalExtension);
                }

                // Now create the output file with the correct name
                using (FileStream output = new FileStream(finalOutputFile, FileMode.Create))
                {
                    List<Node> symbols = CreateSymbol(frequencies);
                    BuildShannonFanoTree(symbols, 0, symbols.Count - 1);
                    Dictionary<string, byte> reverseCodeTable = symbols.ToDictionary(n => n.Code, n => n.Symbol);

                    // Read compressed data
                    BitReader bitReader = new BitReader(reader);
                    string currentCode = "";
                    while (true)
                    {
                        bool? bit = bitReader.ReadBit();
                        if (!bit.HasValue) break;

                        currentCode += bit.Value ? "1" : "0";
                        if (reverseCodeTable.TryGetValue(currentCode, out byte character))
                        {
                            output.WriteByte(character);
                            currentCode = "";
                        }
                    }
                }
            }
        }

        private static List<Node> CreateSymbol(Dictionary<byte, int> frequencies)
        {
            //kvp:key value pair
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
            if (start >= end)
                return;

            //takes total from start to end to calculate all the current subset
            int total = nodes.Skip(start).Take(end - start + 1).Sum(n => n.Frequency);
            int sum = 0;
            int split = start;

            // the beneeth method is wrong to do (fix the for loop into two pointers from start and end)

            // Find the best split point
            for (int i = start; i <= end; i++)
            {
                sum += nodes[i].Frequency;
                if (sum * 2 >= total)
                {
                    split = i;
                    break;
                }
            }

            // Assign codes
            for (int i = start; i <= end; i++)
            {
                nodes[i].Code += (i <= split) ? "1" : "0";
            }

            // Recursively process both parts
            BuildShannonFanoTree(nodes, start, split);
            BuildShannonFanoTree(nodes, split + 1, end);
        }
    }
}