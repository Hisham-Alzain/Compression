using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Windows.Forms;

namespace Compression
{
    public static class RegistryHelper
    {
        public static bool IsAdministrator()
        {
            using (var identity = WindowsIdentity.GetCurrent())
            {
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        public static bool RegisterContextMenu(bool silent = false)
        {
            if (!IsAdministrator())
            {
                if (!silent)
                {
                    var result = MessageBox.Show("This operation requires administrator privileges. Do you want to elevate now?",
                        "Admin Rights Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                        return false;
                }

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = Assembly.GetEntryAssembly().Location,
                        Arguments = "--register",
                        Verb = "runas",
                        UseShellExecute = true
                    };

                    var process = Process.Start(startInfo);
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    if (!silent)
                        MessageBox.Show($"Failed to elevate: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            string appPath = Process.GetCurrentProcess().MainModule.FileName;

            try
            {
                // Register for all file types
                using (var key = Registry.ClassesRoot.CreateSubKey(@"*\shell\CompressionApp"))
                {
                    key.SetValue("MUIVerb", "Compression Options");
                    key.SetValue("SubCommands", "");

                    using (var shellKey = key.CreateSubKey("shell"))
                    {
                        AddCompressionCommands(shellKey, appPath);
                    }
                }

                // Register for directories
                using (var key = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\CompressionApp"))
                {
                    key.SetValue("MUIVerb", "Compression Options");
                    key.SetValue("SubCommands", "");

                    using (var shellKey = key.CreateSubKey("shell"))
                    {
                        AddCompressionCommands(shellKey, appPath);
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!silent)
                    MessageBox.Show($"Registration failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private static void AddCompressionCommands(RegistryKey shellKey, string appPath)
        {
            // Compress commands
            AddCommand(shellKey, "shannon_compress", "Compress with Shannon", appPath, "shannon_compress");
            AddCommand(shellKey, "huffman_compress", "Compress with Huffman", appPath, "huffman_compress");

            // Decompress commands
            AddCommand(shellKey, "shannon_decompress", "Decompress with Shannon", appPath, "shannon_decompress");
            AddCommand(shellKey, "huffman_decompress", "Decompress with Huffman", appPath, "huffman_decompress");
        }

        private static void AddCommand(RegistryKey parent, string name, string label, string appPath, string command)
        {
            using (var key = parent.CreateSubKey(name))
            {
                key.SetValue("", label);
                using (var cmdKey = key.CreateSubKey("command"))
                {
                    cmdKey.SetValue("", $"\"{appPath}\" \"{command}\" \"%1\"");
                }
            }
        }

        public static bool UnregisterContextMenu(bool silent = false)
        {
            if (!IsAdministrator())
            {
                if (!silent)
                {
                    var result = MessageBox.Show("This operation requires administrator privileges. Do you want to elevate now?",
                        "Admin Rights Required", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                        return false;
                }

                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = Assembly.GetEntryAssembly().Location,
                        Arguments = "--unregister",
                        Verb = "runas",
                        UseShellExecute = true
                    };

                    var process = Process.Start(startInfo);
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
                catch (Exception ex)
                {
                    if (!silent)
                        MessageBox.Show($"Failed to elevate: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            try
            {
                string[] keysToDelete = {
                    @"*\shell\CompressionApp",
                    @"Directory\shell\CompressionApp",
                    @".sf",
                    @".huff",
                    @".sf_auto_file",
                    @".huff_auto_file",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\CommandStore\shell\CompressionApp"
                };

                foreach (var key in keysToDelete)
                {
                    try
                    {
                        if (key.StartsWith(@"SOFTWARE\"))
                        {
                            Registry.LocalMachine.DeleteSubKeyTree(key, false);
                        }
                        else
                        {
                            Registry.ClassesRoot.DeleteSubKeyTree(key, false);
                        }
                    }
                    catch { /* Ignore missing keys */ }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (!silent)
                    MessageBox.Show($"Unregistration failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}