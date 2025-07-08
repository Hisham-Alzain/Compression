using System.Diagnostics;
using System.Reflection;

namespace Compression
{
    internal static class Program
    {
        [STAThread] // This can be kept if you use the solution below
        static void Main(string[] args)
        {
            // Wrap the async code in a synchronous context
            MainAsync(args).GetAwaiter().GetResult();
        }

        static async Task MainAsync(string[] args)
        {
            try
            {
                ApplicationConfiguration.Initialize();

                if (args.Length > 0)
                {
                    string operation = args[0];

                    // Add this block: check if admin, and elevate if not
                    if (!RegistryHelper.IsAdministrator())
                    {
                        try
                        {
                            var startInfo = new ProcessStartInfo
                            {
                                FileName = Assembly.GetEntryAssembly().Location,
                                Arguments = string.Join(" ", args.Select(arg => $"\"{arg}\"")),
                                Verb = "runas", // <- triggers UAC
                                UseShellExecute = true
                            };

                            Process.Start(startInfo);
                            return; // Exit current process
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Failed to elevate: {ex.Message}", "Error",
                                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                            Environment.Exit(1);
                        }
                    }

                    // Now safe to continue with compression
                    string filePath = args.Length > 1 ? args[1] : null;
                    var cts = new CancellationTokenSource();
                    var pauseEvent = new ManualResetEventSlim(true);
                    await Task.Run(async () =>
                    {
                        switch (operation)
                        {
                            case "huffman_compress":
                                Huffman compressor1 = new Huffman(pauseEvent);
                                await compressor1.menuCompress(filePath);
                                break;

                            case "huffman_decompress":
                                Huffman decompressor1 = new Huffman(pauseEvent);
                                await decompressor1.menuDecompress(filePath);
                                break;

                            case "shannon_compress":
                                ShannonFano compressor2 = new ShannonFano(pauseEvent);
                                await compressor2.menuCompress(filePath);
                                break;

                            case "shannon_decompress":
                                ShannonFano decompressor2 = new ShannonFano(pauseEvent);
                                await decompressor2.menuDecompress(filePath);
                                break;

                            default:
                                MessageBox.Show("Unknown operation.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                break;
                        }
                    });
                    return;
                }

                // Run main form if no args
                RegistryHelper.RegisterContextMenu(true);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}