using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Plexity.Helpers
{
    public static class RobustFileOperations
    {
        private static ILogger? _logger;

        static RobustFileOperations()
        {
            _logger = App.Services?.GetService(typeof(ILogger<App>)) as ILogger;
        }

        public static async Task<bool> SafeCopyAsync(string sourcePath, string destinationPath, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (!File.Exists(sourcePath))
                    {
                        _logger?.LogWarning("Source file does not exist: {SourcePath}", sourcePath);
                        return false;
                    }

                    var destinationDir = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(destinationDir))
                    {
                        Directory.CreateDirectory(destinationDir);
                    }

                    var tempPath = destinationPath + ".tmp";
                    
                    using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    using (var destStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        await sourceStream.CopyToAsync(destStream);
                        await destStream.FlushAsync();
                    }

                    // Atomic move
                    if (File.Exists(destinationPath))
                        File.Replace(tempPath, destinationPath, destinationPath + ".bak");
                    else
                        File.Move(tempPath, destinationPath);

                    _logger?.LogDebug("Successfully copied file: {SourcePath} -> {DestinationPath}", sourcePath, destinationPath);
                    return true;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger?.LogWarning(ex, "Copy attempt {Attempt}/{MaxRetries} failed: {SourcePath} -> {DestinationPath}", 
                        attempt, maxRetries, sourcePath, destinationPath);
                    
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Final copy attempt failed: {SourcePath} -> {DestinationPath}", sourcePath, destinationPath);
                    return false;
                }
            }

            return false;
        }

        public static async Task<bool> SafeDeleteAsync(string path, int maxRetries = 3)
        {
            if (!File.Exists(path) && !Directory.Exists(path))
                return true;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (File.Exists(path))
                    {
                        File.SetAttributes(path, FileAttributes.Normal);
                        File.Delete(path);
                    }
                    else if (Directory.Exists(path))
                    {
                        Directory.Delete(path, true);
                    }

                    _logger?.LogDebug("Successfully deleted: {Path}", path);
                    return true;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger?.LogWarning(ex, "Delete attempt {Attempt}/{MaxRetries} failed: {Path}", 
                        attempt, maxRetries, path);
                    
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Final delete attempt failed: {Path}", path);
                    return false;
                }
            }

            return false;
        }

        public static async Task<bool> SafeWriteTextAsync(string path, string content, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var directory = Path.GetDirectoryName(path);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    var tempPath = path + ".tmp";
                    await File.WriteAllTextAsync(tempPath, content);
                    
                    // Atomic move
                    if (File.Exists(path))
                        File.Replace(tempPath, path, path + ".bak");
                    else
                        File.Move(tempPath, path);

                    _logger?.LogDebug("Successfully wrote file: {Path}", path);
                    return true;
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger?.LogWarning(ex, "Write attempt {Attempt}/{MaxRetries} failed: {Path}", 
                        attempt, maxRetries, path);
                    
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Final write attempt failed: {Path}", path);
                    return false;
                }
            }

            return false;
        }

        public static async Task<string?> SafeReadTextAsync(string path, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    if (!File.Exists(path))
                        return null;

                    return await File.ReadAllTextAsync(path);
                }
                catch (Exception ex) when (attempt < maxRetries)
                {
                    _logger?.LogWarning(ex, "Read attempt {Attempt}/{MaxRetries} failed: {Path}", 
                        attempt, maxRetries, path);
                    
                    await Task.Delay(TimeSpan.FromMilliseconds(500 * attempt));
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Final read attempt failed: {Path}", path);
                    return null;
                }
            }

            return null;
        }
    }
}