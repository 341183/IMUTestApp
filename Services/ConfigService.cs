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
        private LoggingService? _logger; // 可选的日志服务，因为ConfigService可能在LoggingService之前初始化
        
        public ConfigService()
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "config.json");
            _config = LoadConfig();
        }
        
        public void SetLogger(LoggingService logger)
        {
            _logger = logger;
            _logger.LogInfo(LogCategory.System, "配置服务已关联日志服务");
        }
        
        public AppConfig Config => _config;

        public SerialPortConfig WheelMotorPort
        {
            get => _config.WheelMotorConfig;
        }
        
        public SerialPortConfig IMUPort
        {
            get => _config.IMUConfig;
        }

        public void SaveConfig()
        {
            try
            {
                _logger?.LogInfo(LogCategory.System, "开始保存配置文件");
                
                var directory = Path.GetDirectoryName(_configFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger?.LogDebug(LogCategory.System, $"创建配置目录: {directory}");
                }
                
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };
                
                var json = JsonSerializer.Serialize(_config, options);
                File.WriteAllText(_configFilePath, json);
                
                _logger?.LogInfo(LogCategory.System, $"配置文件保存成功: {_configFilePath}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(LogCategory.System, "保存配置文件失败", ex);
                throw new InvalidOperationException($"保存配置失败: {ex.Message}", ex);
            }
        }
        
        private AppConfig LoadConfig()
        {
            try
            {
                if (File.Exists(_configFilePath))
                {
                    _logger?.LogInfo(LogCategory.System, $"加载配置文件: {_configFilePath}");
                    
                    var json = File.ReadAllText(_configFilePath);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    };
                    var config = JsonSerializer.Deserialize<AppConfig>(json, options);
                    
                    _logger?.LogInfo(LogCategory.System, "配置文件加载成功");
                    return config ?? new AppConfig();
                }
                else
                {
                    _logger?.LogWarn(LogCategory.System, $"配置文件不存在，使用默认配置: {_configFilePath}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(LogCategory.System, "加载配置文件失败，使用默认配置", ex);
            }
            
            return new AppConfig();
        }
        
        public void ResetToDefaults()
        {
            _logger?.LogInfo(LogCategory.UserOperation, "重置配置为默认值");
            _config = new AppConfig();
            SaveConfig();
        }
    }
}