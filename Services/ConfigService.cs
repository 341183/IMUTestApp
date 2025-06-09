using System;
using System.IO;
using System.Text.Json;
using IMUTestApp.Models;

namespace IMUTestApp.Services
{
    public class ConfigService
    {
        private readonly string _configFilePath;
        private AppConfig _config;
        
        public ConfigService()
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "config.json");
            _config = LoadConfig();
        }
        
        public AppConfig Config => _config;
        
        public void SaveConfig()
        {
            try
            {
                var directory = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(_configFilePath, json);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"保存配置失败: {ex.Message}", ex);
            }
        }
        
        private AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var config = JsonSerializer.Deserialize<AppConfig>(json, options);
                    return config ?? new AppConfig();
                }
            }
            catch (Exception)
            {
                // 处理异常但不使用异常变量
                return new AppConfig(); // 直接返回新实例，而不是调用不存在的方法
            }
            
            return new AppConfig();
        }
        
        // 或者添加 GetDefaultConfig 方法
        private AppConfig GetDefaultConfig()
        {
            return new AppConfig();
        }
        
        public void ResetToDefaults()
        {
            _config = new AppConfig();
            SaveConfig();
        }
    }
}