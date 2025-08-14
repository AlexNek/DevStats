using System.Diagnostics;
using System.IO;

namespace DevStats.Helpers
{
    public static class FileExplorerHelper
    {
        /// <summary>
        /// Opens the folder containing the file (does not select the file).
        /// </summary>
        public static void OpenContainingDirectory(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = directory,
                    UseShellExecute = true
                });
            }
        }

        /// <summary>
        /// Opens Windows Explorer and selects the specified file.
        /// </summary>
        public static void OpenAndSelectFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return;

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,\"{filePath}\"",
                UseShellExecute = true
            });
        }
    }
}