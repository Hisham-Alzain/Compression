namespace Compression
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            //ApplicationConfiguration.Initialize();
            //Application.Run(new MainForm());

            ShannonFano sf = new ShannonFano();
            sf.CompressDirectory(@"D:\projects\C#\Compression\Compression\kk\test.txt", @"D:\projects\C#\Compression\Compression\tttt.sf");

            // Decompress a directory
            //sf.DecompressDirectory(@"D:\projects\C#\Compression\Compression\archive.sf", @"D:\projects\C#\Compression\Compression\");
        }
    }
}