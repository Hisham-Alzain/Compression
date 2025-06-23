using System;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using System.Threading.Tasks;
using System.Security.Principal;
using Microsoft.Win32;

namespace Compression
{
    public static class RegistryHelper
    {
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static void RegisterContextMenu()
        {
            if (!IsAdministrator())
                throw new UnauthorizedAccessException("Admin rights required");

            // FIX: Get the actual EXE path
            string appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            // For files - pass only the file path
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"*\shell\Compress with CompressionApp"))
            {
                key.SetValue("", "Compress with CompressionApp");
                key.SetValue("Icon", appPath);

                using (RegistryKey commandKey = key.CreateSubKey("command"))
                {
                    // FIX: Pass only the file path without operation verb
                    commandKey.SetValue("", $"\"{appPath}\" \"%1\"");
                }
            }

            // For folders - same as above
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@"Directory\shell\Compress with CompressionApp"))
            {
                key.SetValue("", "Compress with CompressionApp");
                key.SetValue("Icon", appPath);

                using (RegistryKey commandKey = key.CreateSubKey("command"))
                {
                    commandKey.SetValue("", $"\"{appPath}\" \"%1\"");
                }
            }

            // For compressed files (.sf) - pass only the file path
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@".sf\shell\Decompress with CompressionApp"))
            {
                key.SetValue("", "Decompress with CompressionApp");
                key.SetValue("Icon", appPath);

                using (RegistryKey commandKey = key.CreateSubKey("command"))
                {
                    // FIX: Pass only the file path
                    commandKey.SetValue("", $"\"{appPath}\" \"%1\"");
                }
            }

            // For compressed files (.huff) - same as above
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(@".huff\shell\Decompress with CompressionApp"))
            {
                key.SetValue("", "Decompress with CompressionApp");
                key.SetValue("Icon", appPath);

                using (RegistryKey commandKey = key.CreateSubKey("command"))
                {
                    commandKey.SetValue("", $"\"{appPath}\" \"%1\"");
                }
            }
        }


        //public static void UnregisterContextMenu()
        //{
        //    if (!IsAdministrator())
        //        throw new UnauthorizedAccessException("Admin rights required");

        //    string[] keysToDelete = {
        //    @"*\shell\Compress with CompressionApp",
        //    @"Directory\shell\Compress with CompressionApp",
        //    @".sf\shell\Decompress with CompressionApp",
        //    @".huff\shell\Decompress with CompressionApp"
        //};

        //    foreach (var key in keysToDelete)
        //    {
        //        try
        //        {
        //            Registry.ClassesRoot.DeleteSubKeyTree(key, false);
        //        }
        //        catch { /* Ignore missing keys */ }
        //    }
        //}
    }
}
