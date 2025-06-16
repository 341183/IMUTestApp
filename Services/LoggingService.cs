using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using IMUTestApp.Models;
using System.Collections.Generic;
using System.Linq;

namespace IMUTestApp.Services
{
    public class LoggingService : IDisposable
    {
        private readonly ConfigService _configService;
        private readonly ConcurrentQueue<LogEntry> _logQueue;
        private readonly Timer _flushTimer;
        private readonly SemaphoreSlim _flushSemaphore;
        private readonly object _fileLock = new object();
        private bool _disposed = false;
        private string _currentLogFile = string.Empty;

        public LoggingService(ConfigService configService)
        {
            _configService = configService;
            _logQueue = new ConcurrentQueue<LogEntry>();
            _flushSemaphore = new SemaphoreSlim(1, 1);
            
            // 每5秒刷新一次日志
            _flushTimer = new Timer(FlushLogs, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
            
            InitializeLogDirectory();
            LogInfo(LogCategory.System, "日志服务已启动");
        }

        private void InitializeLogDirectory()
        {
            try
            {
                // 使用 ConfigService 中的日志路径
                var logPath = _configService.Config.GeneralSettings.LogPath;
                if (!Directory.Exists(logPath))
                {
                    Directory.CreateDirectory(logPath);
                }
                
                _currentLogFile = Path.Combine(logPath, $"IMUTestApp_{DateTime.Now:yyyyMMdd}.log");
                
                // 清理旧日志文件
                CleanupOldLogs();
            }
            catch (Exception ex)
            {
                // 如果无法创建日志目录，记录到控制台
                Console.WriteLine($"无法初始化日志目录: {ex.Message}");
            }
        }

        private void CleanupOldLogs()
        {
            try
            {
                var logPath = _configService.Config.GeneralSettings.LogPath;
                var retentionDays = _configService.Config.GeneralSettings.LogRotationDays;
                var cutoffDate = DateTime.Now.AddDays(-retentionDays);

                var logFiles = Directory.GetFiles(logPath, "*.log")
                    .Where(file => File.GetCreationTime(file) < cutoffDate);

                foreach (var file in logFiles)
                {
                    File.Delete(file);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"清理旧日志文件失败: {ex.Message}");
            }
        }

        public void LogDebug(LogCategory category, string message, Exception? exception = null)
        {
            Log(LogLevel.Debug, category, message, exception);
        }

        public void LogInfo(LogCategory category, string message, Exception? exception = null)
        {
            Log(LogLevel.Info, category, message, exception);
        }

        public void LogWarn(LogCategory category, string message, Exception? exception = null)
        {
            Log(LogLevel.Warn, category, message, exception);
        }

        public void LogError(LogCategory category, string message, Exception? exception = null)
        {
            Log(LogLevel.Error, category, message, exception);
        }

        public void LogFatal(LogCategory category, string message, Exception? exception = null)
        {
            Log(LogLevel.Fatal, category, message, exception);
        }

        private void Log(LogLevel level, LogCategory category, string message, Exception? exception = null)
        {
            if (!ShouldLog(level, category))
                return;

            var logEntry = exception != null 
                ? new LogEntry(level, category, message, exception)
                : new LogEntry(level, category, message);

            _logQueue.Enqueue(logEntry);

            // 如果是严重错误，立即刷新
            if (level >= LogLevel.Error)
            {
                Task.Run(() => FlushLogs(null));
            }

            // 控制台输出
            if (_configService.Config.GeneralSettings.EnableConsoleLog)
            {
                Console.WriteLine(logEntry.ToString());
            }
        }

        private bool ShouldLog(LogLevel level, LogCategory category)
        {
            // 检查全局日志级别
            if (!Enum.TryParse<LogLevel>(_configService.Config.GeneralSettings.LogLevel, out var globalLevel))
            {
                globalLevel = LogLevel.Info;
            }

            if (level < globalLevel)
                return false;

            // 检查分类特定的日志级别
            var categoryName = category.ToString();
            if (_configService.Config.GeneralSettings.LogCategories.TryGetValue(categoryName, out var categoryLevelStr))
            {
                if (Enum.TryParse<LogLevel>(categoryLevelStr, out var categoryLevel))
                {
                    return level >= categoryLevel;
                }
            }

            return true;
        }

        private async void FlushLogs(object? state)
        {
            if (_disposed || !await _flushSemaphore.WaitAsync(100))
                return;

            try
            {
                var entriesToWrite = new List<LogEntry>();
                
                while (_logQueue.TryDequeue(out var entry))
                {
                    entriesToWrite.Add(entry);
                }

                if (entriesToWrite.Count == 0)
                    return;

                await WriteToFile(entriesToWrite);
            }
            finally
            {
                _flushSemaphore.Release();
            }
        }

        private async Task WriteToFile(List<LogEntry> entries)
        {
            try
            {
                // 检查文件大小，如果超过限制则创建新文件
                CheckAndRotateLogFile();

                var lines = entries.Select(entry => entry.ToString()).ToArray();
                
                lock (_fileLock)
                {
                    File.AppendAllLines(_currentLogFile, lines);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"写入日志文件失败: {ex.Message}");
            }
        }

        private void CheckAndRotateLogFile()
        {
            try
            {
                if (!File.Exists(_currentLogFile))
                    return;

                var fileInfo = new FileInfo(_currentLogFile);
                var maxSizeMB = _configService.Config.GeneralSettings.MaxLogFileSize;
                
                if (fileInfo.Length > maxSizeMB * 1024 * 1024)
                {
                    var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    var logPath = _configService.Config.GeneralSettings.LogPath;
                    _currentLogFile = Path.Combine(logPath, $"IMUTestApp_{timestamp}.log");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"日志文件轮转失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            
            LogInfo(LogCategory.System, "日志服务正在关闭");
            
            _flushTimer?.Dispose();
            
            // 最后一次刷新
            FlushLogs(null);
            
            _flushSemaphore?.Dispose();
        }
    }
}