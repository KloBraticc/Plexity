using System;
using System.IO;

namespace Plexity.Utility
{
    internal static class Filesystem
    {
        /// <summary>
        /// Gets the available free disk space for the drive containing the specified path.
        /// </summary>
        /// <param name="path">The file or directory path.</param>
        /// <returns>Free space in bytes, or -1 if unavailable.</returns>
        internal static long GetFreeDiskSpace(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return -1;

            try
            {
                foreach (var drive in DriveInfo.GetDrives())
                {
                    if (path.StartsWith(drive.Name, StringComparison.OrdinalIgnoreCase))
                        return drive.AvailableFreeSpace;
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, "Filesystem::GetFreeDiskSpace", $"Error: {ex.Message}");
            }

            return -1;
        }

        /// <summary>
        /// Removes the read-only attribute from a file if set.
        /// </summary>
        internal static void AssertReadOnly(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            try
            {
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Exists && fileInfo.IsReadOnly)
                {
                    fileInfo.IsReadOnly = false;
                    App.Logger.WriteLine(LogLevel.Info, "Filesystem::AssertReadOnly", $"Removed read-only attribute: {filePath}");
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, "Filesystem::AssertReadOnly", $"Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Recursively removes read-only attributes from all files and folders in a directory.
        /// </summary>
        internal static void AssertReadOnlyDirectory(string directoryPath)
        {
            if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                return;

            try
            {
                var rootDir = new DirectoryInfo(directoryPath) { Attributes = FileAttributes.Normal };

                foreach (var info in rootDir.GetFileSystemInfos("*", SearchOption.AllDirectories))
                {
                    info.Attributes = FileAttributes.Normal;
                }

                App.Logger.WriteLine(LogLevel.Info, "Filesystem::AssertReadOnlyDirectory", $"Cleared read-only attributes: {directoryPath}");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, "Filesystem::AssertReadOnlyDirectory", $"Error: {ex.Message}");
            }
        }
    }
}
