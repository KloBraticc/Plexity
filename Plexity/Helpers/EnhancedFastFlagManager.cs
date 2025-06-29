using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Plexity.Enums.FlagPresets;

namespace Plexity.Helpers
{
    public class EnhancedFastFlagManager : IDisposable
    {
        private readonly ILogger<EnhancedFastFlagManager> _logger;
        private readonly ReaderWriterLockSlim _lock = new(LockRecursionPolicy.SupportsRecursion);
        private readonly ConcurrentDictionary<string, object> _flagValues = new();
        private readonly ConcurrentDictionary<string, DateTime> _flagTimestamps = new();
        private readonly Timer _autoSaveTimer;
        private volatile bool _hasUnsavedChanges;
        private readonly string _backupDirectory;
        private readonly int _maxBackups = 5;

        public EnhancedFastFlagManager(ILogger<EnhancedFastFlagManager> logger)
        {
            _logger = logger;
            _backupDirectory = Path.Combine(Paths.Base, "Backups", "FastFlags");
            Directory.CreateDirectory(_backupDirectory);
            
            // Auto-save every 30 seconds if there are changes
            _autoSaveTimer = new Timer(AutoSaveCallback, null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
        }

        public async Task<bool> SetFlagAsync(string key, object? value, bool createBackup = true)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            try
            {
                _lock.EnterWriteLock();

                if (createBackup && _flagValues.ContainsKey(key))
                {
                    await CreateBackupAsync($"Before_Change_{key}");
                }

                if (value == null)
                {
                    _flagValues.TryRemove(key, out _);
                    _flagTimestamps.TryRemove(key, out _);
                    _logger.LogInformation("Removed FastFlag: {Key}", key);
                }
                else
                {
                    var stringValue = value.ToString() ?? string.Empty;
                    _flagValues.AddOrUpdate(key, stringValue, (k, v) => stringValue);
                    _flagTimestamps.AddOrUpdate(key, DateTime.UtcNow, (k, v) => DateTime.UtcNow);
                    _logger.LogInformation("Set FastFlag: {Key} = {Value}", key, stringValue);
                }

                _hasUnsavedChanges = true;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set FastFlag: {Key}", key);
                return false;
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public string? GetFlag(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return null;

            try
            {
                _lock.EnterReadLock();
                return _flagValues.TryGetValue(key, out var value) ? value?.ToString() : null;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task<bool> SaveAsync()
        {
            try
            {
                _lock.EnterReadLock();
                
                if (!_hasUnsavedChanges)
                    return true;

                var data = _flagValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                
                var tempFile = App.FastFlags.FileLocation + ".tmp";
                await File.WriteAllTextAsync(tempFile, json);
                
                // Atomic move
                if (File.Exists(App.FastFlags.FileLocation))
                    File.Replace(tempFile, App.FastFlags.FileLocation, App.FastFlags.FileLocation + ".bak");
                else
                    File.Move(tempFile, App.FastFlags.FileLocation);

                _hasUnsavedChanges = false;
                _logger.LogInformation("Successfully saved {Count} FastFlags", data.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save FastFlags");
                return false;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        public async Task<bool> LoadAsync()
        {
            try
            {
                if (!File.Exists(App.FastFlags.FileLocation))
                    return true;

                var json = await File.ReadAllTextAsync(App.FastFlags.FileLocation);
                var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new();

                _lock.EnterWriteLock();
                _flagValues.Clear();
                _flagTimestamps.Clear();

                foreach (var kvp in data)
                {
                    _flagValues[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                    _flagTimestamps[kvp.Key] = DateTime.UtcNow;
                }

                _hasUnsavedChanges = false;
                _logger.LogInformation("Successfully loaded {Count} FastFlags", data.Count);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load FastFlags");
                return await RestoreFromBackupAsync();
            }
            finally
            {
                _lock.ExitWriteLock();
            }
        }

        public async Task<bool> CreateBackupAsync(string? reason = null)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var fileName = $"FastFlags_Backup_{timestamp}";
                if (!string.IsNullOrEmpty(reason))
                    fileName += $"_{reason}";
                fileName += ".json";

                var backupPath = Path.Combine(_backupDirectory, fileName);
                
                _lock.EnterReadLock();
                var data = _flagValues.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                var json = JsonConvert.SerializeObject(data, Formatting.Indented);
                
                await File.WriteAllTextAsync(backupPath, json);
                
                // Clean old backups
                await CleanOldBackupsAsync();
                
                _logger.LogInformation("Created FastFlag backup: {FileName}", fileName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create FastFlag backup");
                return false;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private async Task<bool> RestoreFromBackupAsync()
        {
            try
            {
                var backupFiles = Directory.GetFiles(_backupDirectory, "FastFlags_Backup_*.json")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .Take(3);

                foreach (var backupFile in backupFiles)
                {
                    try
                    {
                        var json = await File.ReadAllTextAsync(backupFile);
                        var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json) ?? new();
                        
                        _flagValues.Clear();
                        _flagTimestamps.Clear();

                        foreach (var kvp in data)
                        {
                            _flagValues[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
                            _flagTimestamps[kvp.Key] = DateTime.UtcNow;
                        }

                        _logger.LogInformation("Restored FastFlags from backup: {BackupFile}", Path.GetFileName(backupFile));
                        return true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to restore from backup: {BackupFile}", backupFile);
                    }
                }
                
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to restore from any backup");
                return false;
            }
        }

        private async Task CleanOldBackupsAsync()
        {
            try
            {
                var backupFiles = Directory.GetFiles(_backupDirectory, "FastFlags_Backup_*.json")
                    .OrderByDescending(f => File.GetCreationTime(f))
                    .Skip(_maxBackups);

                foreach (var oldBackup in backupFiles)
                {
                    File.Delete(oldBackup);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean old backups");
            }
        }

        public Dictionary<string, object> ResolveConflicts(Dictionary<string, object> incoming)
        {
            var resolved = new Dictionary<string, object>();

            _lock.EnterReadLock();
            try
            {
                foreach (var kvp in incoming)
                {
                    if (_flagValues.TryGetValue(kvp.Key, out var existingValue))
                    {
                        // Conflict resolution: use newer timestamp
                        if (_flagTimestamps.TryGetValue(kvp.Key, out var timestamp))
                        {
                            var hoursSinceLastChange = (DateTime.UtcNow - timestamp).TotalHours;
                            if (hoursSinceLastChange < 24) // Keep recent changes
                            {
                                resolved[kvp.Key] = existingValue;
                                _logger.LogInformation("Conflict resolved for {Key}: kept existing value due to recent change", kvp.Key);
                            }
                            else
                            {
                                resolved[kvp.Key] = kvp.Value;
                                _logger.LogInformation("Conflict resolved for {Key}: accepted incoming value", kvp.Key);
                            }
                        }
                        else
                        {
                            resolved[kvp.Key] = kvp.Value; // Default to incoming
                        }
                    }
                    else
                    {
                        resolved[kvp.Key] = kvp.Value; // New flag
                    }
                }
                
                return resolved;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        private void AutoSaveCallback(object? state)
        {
            if (_hasUnsavedChanges)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SaveAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Auto-save failed");
                    }
                });
            }
        }

        public void Dispose()
        {
            _autoSaveTimer?.Dispose();
            
            if (_hasUnsavedChanges)
            {
                try
                {
                    SaveAsync().Wait(TimeSpan.FromSeconds(5));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save during disposal");
                }
            }
            
            _lock?.Dispose();
        }
    }
}