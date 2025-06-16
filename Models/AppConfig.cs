using System.Runtime.CompilerServices;
using System.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace IMUTestApp.Models
{
    public class AppConfig : INotifyPropertyChanged
    {
        private SerialPortConfig _wheelMotorConfig = new();
        private SerialPortConfig _imuConfig = new();
        private TcpConfig _tcpConfig = new();
        private GeneralSettings _generalSettings = new();

        public SerialPortConfig WheelMotorConfig 
        { 
            get => _wheelMotorConfig;
            set
            {
                if (_wheelMotorConfig != value)
                {
                    if (_wheelMotorConfig != null)
                        _wheelMotorConfig.PropertyChanged -= OnChildPropertyChanged;
                    _wheelMotorConfig = value;
                    if (_wheelMotorConfig != null)
                        _wheelMotorConfig.PropertyChanged += OnChildPropertyChanged;
                    OnPropertyChanged();
                }
            }
        }
        
        public SerialPortConfig IMUConfig 
        { 
            get => _imuConfig;
            set
            {
                if (_imuConfig != value)
                {
                    if (_imuConfig != null)
                        _imuConfig.PropertyChanged -= OnChildPropertyChanged;
                    _imuConfig = value;
                    if (_imuConfig != null)
                        _imuConfig.PropertyChanged += OnChildPropertyChanged;
                    OnPropertyChanged();
                }
            }
        }
        
        public TcpConfig TcpConfig 
        { 
            get => _tcpConfig;
            set
            {
                if (_tcpConfig != value)
                {
                    if (_tcpConfig != null)
                        _tcpConfig.PropertyChanged -= OnChildPropertyChanged;
                    _tcpConfig = value;
                    if (_tcpConfig != null)
                        _tcpConfig.PropertyChanged += OnChildPropertyChanged;
                    OnPropertyChanged();
                }
            }
        }
        
        public GeneralSettings GeneralSettings 
        { 
            get => _generalSettings;
            set
            {
                if (_generalSettings != value)
                {
                    if (_generalSettings != null)
                        _generalSettings.PropertyChanged -= OnChildPropertyChanged;
                    _generalSettings = value;
                    if (_generalSettings != null)
                        _generalSettings.PropertyChanged += OnChildPropertyChanged;
                    OnPropertyChanged();
                }
            }
        }

        public AppConfig()
        {
            // 订阅子对象的属性更改事件
            _wheelMotorConfig.PropertyChanged += OnChildPropertyChanged;
            _imuConfig.PropertyChanged += OnChildPropertyChanged;
            _tcpConfig.PropertyChanged += OnChildPropertyChanged;
            _generalSettings.PropertyChanged += OnChildPropertyChanged;
        }

        private void OnChildPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // 当子对象属性更改时，触发AppConfig的PropertyChanged事件
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(sender)));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class TcpConfig : INotifyPropertyChanged
    {
        private string _ipAddress = "192.168.4.1";
        private int _port = 12024;
        
        public string IpAddress
        {
            get => _ipAddress;
            set
            {
                if (_ipAddress != value)
                {
                    _ipAddress = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public int Port
        {
            get => _port;
            set
            {
                if (_port != value)
                {
                    _port = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
     
    public class GeneralSettings : INotifyPropertyChanged
    {
        private string _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        private string _logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
        private bool _enableLogRotation = true;
        private int _logRotationDays = 7;
        private int _maxLogFileSize = 10;
        private bool _autoCreateDirectories = true;
        private string _dataFileFormat = "csv";
        private bool _enableDataBackup = false;
        private string _backupPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backup");
        private string _logLevel = "Info";
        private bool _enableConsoleLog = false;
        private Dictionary<string, string> _logCategories = new()
        {
            { "SerialPort", "Info" },
            { "TCP", "Info" },
            { "IMUData", "Warn" },
            { "FileIO", "Error" },
            { "System", "Info" },
            { "UserAction", "Info" }
        };

        //保存数据路径       
        public string DataPath
        {
            get => _dataPath;
            set
            {
                if (_dataPath != value)
                {
                    _dataPath = value;
                    OnPropertyChanged();
                }
            }
        }
        
        //日志路径
        public string LogPath
        {
            get => _logPath;
            set
            {
                if (_logPath != value)
                {
                    _logPath = value;
                    OnPropertyChanged();
                }
            }
        }
        
        //启用日志轮转
        public bool EnableLogRotation
        {
            get => _enableLogRotation;
            set
            {
                if (_enableLogRotation != value)
                {
                    _enableLogRotation = value;
                    OnPropertyChanged();
                }
            }
        }
        
        //日志论转天数
        public int LogRotationDays
        {
            get => _logRotationDays;
            set
            {
                if (_logRotationDays != value)
                {
                    _logRotationDays = value;
                    OnPropertyChanged();
                }
            }
        }
        
        //最大文件尺寸
        public int MaxLogFileSize
        {
            get => _maxLogFileSize;
            set
            {
                if (_maxLogFileSize != value)
                {
                    _maxLogFileSize = value;
                    OnPropertyChanged();
                }
            }
        }
        
        //自动创建文件夹
        public bool AutoCreateDirectories
        {
            get => _autoCreateDirectories;
            set
            {
                if (_autoCreateDirectories != value)
                {
                    _autoCreateDirectories = value;
                    OnPropertyChanged();
                }
            }
        }
        
        //数据文件格式
        public string DataFileFormat
        {
            get => _dataFileFormat;
            set
            {
                if (_dataFileFormat != value)
                {
                    _dataFileFormat = value;
                    OnPropertyChanged();
                }
            }
        }
        
        //启用数据备份
        public bool EnableDataBackup
        {
            get => _enableDataBackup;
            set
            {
                if (_enableDataBackup != value)
                {
                    _enableDataBackup = value;
                    OnPropertyChanged();
                }
            }
        }
        
        //备份路径
        public string BackupPath
        {
            get => _backupPath;
            set
            {
                if (_backupPath != value)
                {
                    _backupPath = value;
                    OnPropertyChanged();
                }
            }
        }
        
        //日志等级
        public string LogLevel
        {
            get => _logLevel;
            set
            {
                if (_logLevel != value)
                {
                    _logLevel = value;
                    OnPropertyChanged();
                }
            }
        }

        //其余命令行日志
        public bool EnableConsoleLog
        {
            get => _enableConsoleLog;
            set
            {
                if (_enableConsoleLog != value)
                {
                    _enableConsoleLog = value;
                    OnPropertyChanged();
                }
            }
        }  

        //日志类别
        public Dictionary<string, string> LogCategories
        {
            get => _logCategories;
            set
            {
                if (_logCategories != value)
                {
                    _logCategories = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}