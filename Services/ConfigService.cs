using System;
using System.IO;
using System.Text.Json;
using IMUTestApp.Models;
using System.ComponentModel;

namespace IMUTestApp.Services
{
    public class ConfigService
    {
        private readonly string _configFilePath;
        private AppConfig _config;
        private LoggingService? _logger;
        private readonly object _saveLock = new object();
        private bool _isLoading = false;
        
        public ConfigService()
        {   
            //exe所在文件夹
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs", "config.json");
            _isLoading = true;
            _config = LoadConfig();
            _isLoading = false;
            
            // 订阅配置更改事件，实现自动保存
            _config.PropertyChanged += OnConfigChanged;
        }
        
        public void SetLogger(LoggingService logger)
        {
            _logger = logger;
            _logger.LogInfo(LogCategory.System, "配置服务已关联日志服务");
        }
        
        public AppConfig Config => _config;

        public SerialPortConfig WheelMotorPort
        {
            get => Config.WheelMotorConfig;
            set
            {
                Config.WheelMotorConfig = value;
            }
        }
        
        public SerialPortConfig IMUPort
        {
            get => _config.IMUConfig;
            set
            {
                _config.IMUConfig = value;
            }
        }

        // 配置更改事件处理器 - 自动保存
        private void OnConfigChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 避免在加载配置时触发保存
            if (_isLoading) return;
            
            // 异步保存配置，避免阻塞UI
            System.Threading.Tasks.Task.Run(() => SaveConfigInternal());
        }

        // 内部保存方法，带锁保护
        private void SaveConfigInternal()
        {
            lock (_saveLock)
            {
                try
                {
                    _logger?.LogDebug(LogCategory.System, "检测到配置更改，开始自动保存");
                    
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
                    
                    _logger?.LogDebug(LogCategory.System, "配置文件自动保存成功");
                }
                catch (Exception ex)
                {
                    _logger?.LogError(LogCategory.System, "自动保存配置文件失败", ex);
                }
            }
        }

        //将config变量写入json文件中（手动保存方法保留）
        public void SaveConfig()
        {
            try
            {
                _logger?.LogInfo(LogCategory.System, "开始手动保存配置文件");
                
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
                
                _logger?.LogInfo(LogCategory.System, $"配置文件手动保存成功: {_configFilePath}");
            }
            catch (Exception ex)
            {
                _logger?.LogError(LogCategory.System, "手动保存配置文件失败", ex);
                throw new InvalidOperationException($"保存配置失败: {ex.Message}", ex);
            }
        }
        
        //从json文件中读取参数到config
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
        
    }
}