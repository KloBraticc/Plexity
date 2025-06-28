using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Plexity.Models.Persistable;
using Plexity.Utility;

namespace Plexity.Services
{
    public class ThreadSafeSettingsManager
    {
        private readonly ILogger<ThreadSafeSettingsManager> _logger;
        private readonly SemaphoreSlim _semaphore = new(1, 1);
        private readonly object _lock = new();
        private AppSettings? _settings;

        public ThreadSafeSettingsManager(ILogger<ThreadSafeSettingsManager> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public AppSettings Settings
        {
            get
            {
                lock (_lock)
                {
                    return _settings ??= new AppSettings();
                }
            }
        }

        public async Task LoadAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.LogInformation("Loading settings...");
                
                // Use existing App.Settings to load
                await Task.Run(() => App.Settings.Load());
                
                lock (_lock)
                {
                    _settings = App.Settings.Prop;
                }
                
                _logger.LogInformation("Settings loaded successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
                lock (_lock)
                {
                    _settings = new AppSettings();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task SaveAsync()
        {
            await _semaphore.WaitAsync();
            try
            {
                _logger.LogInformation("Saving settings...");
                
                await Task.Run(() => App.Settings.Save());
                
                _logger.LogInformation("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                throw;
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task UpdateSettingsAsync<T>(Func<AppSettings, T> updater)
        {
            await _semaphore.WaitAsync();
            try
            {
                lock (_lock)
                {
                    if (_settings != null)
                    {
                        updater(_settings);
                    }
                }
                await SaveAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _semaphore?.Dispose();
        }
    }
}