namespace Compression
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {

            try
            {
                ApplicationConfiguration.Initialize();
                string filePath = null;

                // Check if we have any arguments
                if (args.Length > 0)
                {
                    // Use the first argument as the file path
                    filePath = args[0];
                }
                Application.Run(new MainForm(filePath));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Startup error: {ex.Message}", "Error",
                                MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}