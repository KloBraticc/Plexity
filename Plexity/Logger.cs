using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Plexity
{
    public enum LogLevel { Trace, Debug, Info, Warning, Error, Critical }

    public sealed class Logger : IDisposable, IAsyncDisposable
    {
        private readonly Channel<string> _channel;
        private readonly ConcurrentQueue<string> _history = new();
        private const int MaxHistoryEntries = 180;

        private CancellationTokenSource _cts = new();
        private FileStream? _fileStream;
        private Task? _backgroundTask;

        private readonly TimeSpan _writeInterval;
        private readonly int _batchSize;
        private DateTime _lastWriteTime = DateTime.MinValue;

        private readonly Process _currentProcess = Process.GetCurrentProcess();
        private TimeSpan _lastTotalProcessorTime = TimeSpan.Zero;
        private DateTime _lastCpuCheckTime = DateTime.UtcNow;

        private long _maxFileSizeBytes = 10 * 1024 * 1024;
        private int _fileIndex;
        private string? _directory;
        private string? _baseFilename;

        public bool Initialized { get; private set; }
        public bool NoWriteMode { get; private set; }
        public string? FileLocation { get; private set; }
        public long MaxFileSizeBytes { get => _maxFileSizeBytes; set => _maxFileSizeBytes = value > 0 ? value : _maxFileSizeBytes; }
        public TimeSpan WriteInterval => _writeInterval;
        public int BatchSize => _batchSize;
        public bool EnableExtendedInfo { get; set; } = true;
        public bool UseJsonFormat { get; set; } = false;

        private static readonly ConcurrentBag<StringBuilder> _sbPool = new();

        public Logger(int batchSize = 50, TimeSpan? writeInterval = null)
        {
            _batchSize = Math.Max(batchSize, 1);
            _writeInterval = writeInterval ?? TimeSpan.FromSeconds(2);
            _channel = Channel.CreateUnbounded<string>(new UnboundedChannelOptions { SingleWriter = false, SingleReader = true });
        }

        public string AsDocument => string.Join(Environment.NewLine, _history);

        public void Initialize(bool useTempDir = false, long? maxFileSizeBytes = null)
        {
            if (Initialized) return;

            try
            {
                _directory = useTempDir ? Paths.TempLogs : Path.Combine(Paths.Base, "Logs");
                Directory.CreateDirectory(_directory);

                _baseFilename = $"{App.ProjectName}_{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}.log";
                FileLocation = Path.Combine(_directory, _baseFilename);
                _fileStream = new FileStream(FileLocation, FileMode.Create, FileAccess.Write, FileShare.Read, 8192, useAsync: true);
                _maxFileSizeBytes = maxFileSizeBytes.GetValueOrDefault(_maxFileSizeBytes);

                Initialized = true;
                NoWriteMode = false;

                _backgroundTask = Task.Run(() => BackgroundWriterAsync(_cts.Token));
                WriteLine(LogLevel.Info, "Logger", $"Logger initialized at {FileLocation}");

                CleanupOldLogs(_directory);
            }
            catch
            {
                NoWriteMode = true;
            }
        }

        private async Task BackgroundWriterAsync(CancellationToken token)
        {
            var batch = new List<string>(_batchSize);

            try
            {
                while (await _channel.Reader.WaitToReadAsync(token).ConfigureAwait(false))
                {
                    batch.Clear();
                    while (_channel.Reader.TryRead(out var msg))
                    {
                        batch.Add(msg);
                        if (batch.Count >= _batchSize) break;
                    }

                    if (batch.Count < _batchSize)
                    {
                        var delay = _writeInterval - (DateTime.UtcNow - _lastWriteTime);
                        if (delay > TimeSpan.Zero)
                            await Task.Delay(delay, token);
                    }

                    if (batch.Count > 0 && _fileStream is not null)
                        await WriteLogBatchAsync(batch, token).ConfigureAwait(false);
                }

                await FlushPendingWritesAsync();
            }
            catch (OperationCanceledException) { await FlushPendingWritesAsync(); }
            catch (Exception ex) { Debug.WriteLine($"Logger background failed: {ex}"); }
        }

        private async Task WriteLogBatchAsync(List<string> batch, CancellationToken token)
        {
            if (_fileStream == null) return;

            var sb = GetPooledStringBuilder();
            try
            {
                foreach (var msg in batch)
                    sb.AppendLine(msg);

                var data = Encoding.UTF8.GetBytes(sb.ToString());

                if (_fileStream.Length + data.Length > _maxFileSizeBytes)
                    await RollOverFileAsync(token);

                await _fileStream.WriteAsync(data, token);
                await _fileStream.FlushAsync(token);
                _lastWriteTime = DateTime.UtcNow;
            }
            finally { ReturnStringBuilder(sb); }
        }

        private async Task RollOverFileAsync(CancellationToken token)
        {
            if (_fileStream == null || _directory == null || _baseFilename == null) return;

            try { await _fileStream.DisposeAsync(); } catch { }

            _fileIndex++;
            var newName = Path.Combine(_directory, $"{Path.GetFileNameWithoutExtension(_baseFilename)}_{_fileIndex}.log");
            _fileStream = new FileStream(newName, FileMode.Create, FileAccess.Write, FileShare.Read, 8192, useAsync: true);
            FileLocation = newName;
            WriteLine(LogLevel.Info, "Logger", $"Rolled over to {newName}");
        }

        public async Task FlushPendingWritesAsync()
        {
            if (_fileStream == null) return;

            var sb = GetPooledStringBuilder();
            try
            {
                while (_channel.Reader.TryRead(out var msg))
                    sb.AppendLine(msg);

                if (sb.Length > 0)
                {
                    var data = Encoding.UTF8.GetBytes(sb.ToString());
                    await _fileStream.WriteAsync(data);
                    await _fileStream.FlushAsync();
                    _lastWriteTime = DateTime.UtcNow;
                }
            }
            finally { ReturnStringBuilder(sb); }
        }

        private void CleanupOldLogs(string directory, int maxFiles = 10)
        {
            try
            {
                var files = new DirectoryInfo(directory).GetFiles("*.log")
                    .OrderByDescending(f => f.CreationTimeUtc).Skip(maxFiles);

                foreach (var file in files)
                {
                    try { file.Delete(); } catch { }
                }
            }
            catch (Exception ex) { Debug.WriteLine($"Cleanup failed: {ex}"); }
        }

        public void WriteLine(LogLevel level, string identifier, string message)
        {
            string formatted = FormatLog(level, identifier, message);
            _history.Enqueue(formatted);
            while (_history.Count > MaxHistoryEntries) _history.TryDequeue(out _);
            if (Initialized && !NoWriteMode) _channel.Writer.TryWrite(formatted);
        }

        private string FormatLog(LogLevel level, string identifier, string message)
        {
            var timestamp = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            int tid = Environment.CurrentManagedThreadId;
            int pid = _currentProcess.Id;
            long mem = _currentProcess.PrivateMemorySize64 / 1024;
            double cpu = GetCpuUsage();

            if (UseJsonFormat)
            {
                return $"{{\"ts\":\"{timestamp}\",\"lvl\":\"{level}\",\"pid\":{pid},\"tid\":{tid},\"cpu\":{cpu:F2},\"mem_kb\":{mem},\"id\":\"{identifier}\",\"msg\":\"{message.Replace("\"", "\\\"")}\"}}";
            }

            return $"[{timestamp}] [{level}] [PID:{pid}] [TID:{tid}] [CPU:{cpu:F2}%] [Mem:{mem} KB] [{identifier}] {message}";
        }

        private double GetCpuUsage()
        {
            try
            {
                var now = DateTime.UtcNow;
                var totalProc = _currentProcess.TotalProcessorTime;

                double cpuUsedMs = (totalProc - _lastTotalProcessorTime).TotalMilliseconds;
                double intervalMs = (now - _lastCpuCheckTime).TotalMilliseconds;

                _lastTotalProcessorTime = totalProc;
                _lastCpuCheckTime = now;

                return intervalMs > 0 ? Math.Clamp((cpuUsedMs / (intervalMs * Environment.ProcessorCount)) * 100, 0, 100) : 0;
            }
            catch { return 0; }
        }

        private static StringBuilder GetPooledStringBuilder()
            => _sbPool.TryTake(out var sb) ? sb : new StringBuilder();

        private static void ReturnStringBuilder(StringBuilder sb)
        {
            sb.Clear();
            if (sb.Capacity < 32_768)
                _sbPool.Add(sb);
        }

        public void WriteException(string identifier, Exception ex)
            => WriteLine(LogLevel.Error, identifier, $"Exception: {ex.Message}\n{ex}");

        public void Dispose() => DisposeAsync().AsTask().Wait();

        public async ValueTask DisposeAsync()
        {
            if (_cts.IsCancellationRequested) return;

            _cts.Cancel();
            _channel.Writer.TryComplete();
            try { if (_backgroundTask != null) await _backgroundTask.ConfigureAwait(false); } catch { }
            if (_fileStream != null) try { await _fileStream.DisposeAsync().ConfigureAwait(false); } catch { }
            _cts.Dispose();
        }
    }
}
