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
        
        public SettingsService()
        {
            _settingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "IMUTestApp", "settings.json");
            _settings = LoadSettings();
        }
        
        public AppSettings Settings => _settings;
        
        public void SaveSettings()
        {
            try
            {
                var directory = Path.GetDirectoryName(_settingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var json = JsonSerializer.Serialize(_settings, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                // 可以添加日志记录
                throw new InvalidOperationException($"保存设置失败: {ex.Message}", ex);
            }
        }
        
        private AppSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    return settings ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                // 可以添加日志记录
                // 如果加载失败，返回默认设置
            }
            
            return new AppSettings();
        }
        
        public void ResetToDefaults()
        {
            _settings = new AppSettings();
            SaveSettings();
        }
        
        public bool ValidatePaths()
        {
            try
            {
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
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}