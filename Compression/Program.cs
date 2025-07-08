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
                registerContextMenuToolStripMenuItem_Click();
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
        static void registerContextMenuToolStripMenuItem_Click()
        {
            try
            {
                RegistryHelper.RegisterContextMenu();
            }
            catch (UnauthorizedAccessException)
            {
                MessageBox.Show("Admin rights required. Run as administrator.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}");
            }
        }
    }
}