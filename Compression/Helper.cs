using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;


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

        public double CalculateRatio(long compressedSize, long decompressedSize)
        {
            return (double)compressedSize / (double)decompressedSize * 100;
        }


        /// <summary>
        /// Finds the longest common directory path shared by all input paths.
        /// </summary>
        /// <param name="paths">Array of file paths to analyze</param>
        /// <returns>The common base directory path ending with directory separator, or empty string if no common path exists</returns>
        public string GetCommonPath(string[] paths)
        {
            // Handle edge cases
            if (paths.Length == 0)
                return string.Empty;  // No paths provided

            if (paths.Length == 1)
                return Path.GetDirectoryName(paths[0]);  // Single path - return its directory

            // Start with the directory of the first path as initial common path
            // Example: if first path is "C:\Folder\Subfolder\file.txt"
            // commonPath becomes "C:\Folder\Subfolder"
            string commonPath = Path.GetDirectoryName(paths[0]);

            // Compare with each subsequent path
            for (int i = 1; i < paths.Length; i++)
            {
                // Keep moving up the directory tree until we find a match
                while (!paths[i].StartsWith(commonPath, StringComparison.OrdinalIgnoreCase))
                {
                    // Move up one directory level
                    // Example: "C:\Folder\Subfolder" → "C:\Folder"
                    commonPath = Path.GetDirectoryName(commonPath);

                    // If we've reached the root and still no match
                    if (commonPath == null)
                        return string.Empty;  // No common path exists
                }
            }

            // Ensure the path ends with a directory separator
            // Example converts "C:\Folder" to "C:\Folder\"
            return commonPath + Path.DirectorySeparatorChar;
        }
    }
}
