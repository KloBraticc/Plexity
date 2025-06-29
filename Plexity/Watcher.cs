using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Plexity.Integrations;
using Plexity.Models;
using Plexity.Utility;
using Plexity.UI;
using Plexity.AppData;

namespace Plexity
{
    public class Watcher : IDisposable
    {
        private readonly InterProcessLock _lock = new("Watcher");

        private WatcherData? _watcherData;

        private readonly NotifyIconWrapper? _notifyIcon;

        public readonly ActivityWatcher? ActivityWatcher;

        public readonly IntegrationWatcher? IntegrationWatcher;

        private bool _disposed;

        public Watcher()
        {
            const string LOG_IDENT = "Watcher";

            if (!_lock.IsAcquired)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Watcher instance already exists");
                return;
            }

            string? watcherDataArg = App.LaunchSettings.WatcherFlag.Data;

#if DEBUG
            if (string.IsNullOrEmpty(watcherDataArg))
            {
                string path = new RobloxPlayerData().ExecutablePath;

                // Start process and wait shortly to ensure it's started
                var gameClientProcess = Process.Start(path);
                if (gameClientProcess == null)
                    throw new Exception("Failed to start Roblox player process");

                _watcherData = new WatcherData { ProcessId = gameClientProcess.Id };
            }
#else
            if (string.IsNullOrEmpty(watcherDataArg))
                throw new Exception("Watcher data not specified");
#endif

            if (!string.IsNullOrEmpty(watcherDataArg))
            {
                try
                {
                    var json = Encoding.UTF8.GetString(Convert.FromBase64String(watcherDataArg));
                    _watcherData = JsonSerializer.Deserialize<WatcherData>(json);
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to deserialize watcher data", ex);
                }
            }

            if (_watcherData is null)
                throw new Exception("Watcher data is invalid");
        }

        public void KillRobloxProcess()
        {
            if (_watcherData != null)
                CloseProcess(_watcherData.ProcessId, true);
        }

        public void CloseProcess(int pid, bool force = false)
        {
            const string LOG_IDENT = "Watcher::CloseProcess";

            try
            {
                using var process = Process.GetProcessById(pid);

                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Killing process '{process.ProcessName}' (pid={pid}, force={force})");

                if (process.HasExited)
                {
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"PID {pid} has already exited");
                    return;
                }

                if (force)
                    process.Kill();
                else
                    process.CloseMainWindow();
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"PID {pid} could not be closed");
                App.Logger.WriteException(LOG_IDENT, ex);
            }
        }

        public async Task Run(CancellationToken cancellationToken = default)
        {
            if (!_lock.IsAcquired || _watcherData is null)
                return;

            ActivityWatcher?.Start();

            try
            {
                while (!cancellationToken.IsCancellationRequested &&
                       Utilities.GetProcessesSafe().Any(x => x.Id == _watcherData.ProcessId))
                {
                    await Task.Delay(500, cancellationToken);
                }
            }
            catch (TaskCanceledException)
            {
                // Task was cancelled gracefully
            }

            if (_watcherData.AutoclosePids is not null)
            {
                foreach (int pid in _watcherData.AutoclosePids)
                    CloseProcess(pid);
            }

        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                App.Logger.WriteLine(LogLevel.Info, "Watcher::Dispose", "Disposing Watcher");

                IntegrationWatcher?.Dispose();
                _notifyIcon?.Dispose();
                ActivityWatcher?.Dispose();
                _lock.Dispose();
            }

            _disposed = true;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
