using System;
using System.IO;
using System.Text.Json;
using IMUTestApp.Models;

namespace IMUTestApp.Services
{
    public class SettingsService
    {
        private readonly string _settingsFilePath;
        private AppSettings _settings;
        private LoggingService? _logger;
        
        public SettingsService()
        {
            _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IMUTestApp", "settings.json");
            _settings = LoadSettings();
        }
        
        public void SetLogger(LoggingService logger)
        {
            _logger = logger;
            _logger.LogInfo(LogCategory.System, "设置服务已关联日志服务");
        }
        
        public AppSettings Settings => _settings;
        
        public void SaveSettings()
        {
            try
            {
                _logger?.LogInfo(LogCategory.UserOperation, "开始保存用户设置");
                
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger?.LogDebug(LogCategory.System, $"创建设置目录: {directory}");
                }
                
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(_settingsFilePath, json);
                _logger?.LogInfo(LogCategory.UserOperation, $"用户设置保存成功: {_settingsFilePath}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(LogCategory.System, "保存用户设置失败", ex);
                throw new InvalidOperationException($"保存设置失败: {ex.Message}", ex);
            }
        }
        
        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    _logger?.LogInfo(LogCategory.System, $"加载用户设置: {_settingsFilePath}");
                    
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    
                    _logger?.LogInfo(LogCategory.System, "用户设置加载成功");
                    return settings ?? new AppSettings();
                }
                else
                {
                    _logger?.LogInfo(LogCategory.System, $"用户设置文件不存在，使用默认设置: {_settingsFilePath}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(LogCategory.System, "加载用户设置失败，使用默认设置", ex);
            }
            
            return new AppSettings();
        }
        
        public void ResetToDefaults()
        {
            _logger?.LogInfo(LogCategory.UserOperation, "重置用户设置为默认值");
            _settings = new AppSettings();
            SaveSettings();
        }
        
        public bool ValidatePaths()
        {
            try
            {
                _logger?.LogDebug(LogCategory.System, "开始验证路径设置");
                
                // 验证数据路径
                if (!string.IsNullOrEmpty(_settings.DataPath))
                {
                    var dataDir = new DirectoryInfo(_settings.DataPath);
                    if (_settings.AutoCreateDirectories && !dataDir.Exists)
                    {
                        dataDir.Create();
                    }
                }
                
                // 验证日志路径
                if (!string.IsNullOrEmpty(_settings.LogPath))
                {
                    var logDir = new DirectoryInfo(_settings.LogPath);
                    if (_settings.AutoCreateDirectories && !logDir.Exists)
                    {
                        logDir.Create();
                    }
                }
                
                // 验证备份路径
                if (_settings.EnableDataBackup && !string.IsNullOrEmpty(_settings.BackupPath))
                {
                    var backupDir = new DirectoryInfo(_settings.BackupPath);
                    if (_settings.AutoCreateDirectories && !backupDir.Exists)
                    {
                        backupDir.Create();
                    }
                }
                
                _logger?.LogInfo(LogCategory.System, "路径验证完成");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(LogCategory.System, "路径验证失败", ex);
                return false;
            }
        }
    }
}