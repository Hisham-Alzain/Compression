using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Compression
{
    public class Helper
    {
        public (List<(string FullPath, string RelativePath)> files, string distPath, string dirName) ReadDirectory(string folderPath)
        {
            // Get directory info
            string dirName = Path.GetFileName(folderPath);
            string distPath = folderPath.Replace(dirName, "");

            // Get all files including subdirectories
            List<(string FullPath, string RelativePath)> allFiles = Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories)
                .Select(filePath =>
                (
                    FullPath : filePath,
                    RelativePath : Path.GetRelativePath(folderPath, filePath)
                )).ToList();

            return (files: allFiles, distPath, dirName);
        }

        public async Task<(byte[] data, string ext, long size)> Readfile(string filePath)
        {
            // Get original file extension (without the dot)
            string originalExtension = Path.GetExtension(filePath).TrimStart('.');
            if (string.IsNullOrEmpty(originalExtension))
            {
                originalExtension = "bin"; // Default for files without extension
            }

            byte[] originalData = await File.ReadAllBytesAsync(filePath);
            long originalSize = originalData.Length;

            return (data: originalData, ext: originalExtension, size: originalSize);
        }

        public async Task<Dictionary<byte, int>> CalculateFrequencies(byte[] data)
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
