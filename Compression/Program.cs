namespace Compression
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            ApplicationConfiguration.Initialize();

            // Handle command-line operations
            if (args.Length > 1)
            {
                string operation = args[0].ToLower();
                string path = args[1];

                if (operation == "compress")
                {
                    Application.Run(new MainForm("path"));
                }
                else if (operation == "decompress")
                {
                    Application.Run(new MainForm("path"));
                }
            }
            else
            {
                // Normal GUI mode
                Application.Run(new MainForm());
            }
        }

        //private static void RunHeadlessCompression(string path)
        //{
        //    try
        //    {
        //        // Simplified compression logic
        //        // You'll need to adapt your existing compression code here
        //        MessageBox.Show($"Compression completed for: {path}");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Compression error: {ex.Message}");
        //    }
        //    finally
        //    {
        //        Application.Exit();
        //    }
        //}

        //private static void RunHeadlessDecompression(string path)
        //{
        //    try
        //    {
        //        // Simplified decompression logic
        //        // You'll need to adapt your existing decompression code here
        //        MessageBox.Show($"Decompression completed for: {path}");
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"Decompression error: {ex.Message}");
        //    }
        //    finally
        //    {
        //        Application.Exit();
        //    }
        //}
    }
}