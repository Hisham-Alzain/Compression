using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compression
{
    public class Helper
    {
        public (byte[] data, string ext, long size) Readfile(string filePath)
        {
            // Get original file extension (without the dot)
            string originalExtension = Path.GetExtension(filePath).TrimStart('.');
            if (string.IsNullOrEmpty(originalExtension))
            {
                originalExtension = "bin"; // Default for files without extension
            }

            byte[] originalData = File.ReadAllBytes(filePath);
            long originalSize = originalData.Length;

            return (data: originalData, ext: originalExtension, size: originalSize);
        }

        public void Writefile(string filePath, byte[] data)
        {
            File.WriteAllBytes(filePath, data);
        }

        public Dictionary<byte, int> CalculateFrequencies(byte[] data)
        {
            Dictionary<byte, int> frequencies = new Dictionary<byte, int>();
            foreach (byte b in data)
            {
                if (frequencies.ContainsKey(b))
                    frequencies[b]++;
                else
                    frequencies[b] = 1;
            }
            return frequencies;
        }
    }
}
