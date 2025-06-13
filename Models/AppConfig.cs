using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.IO;
using System;
using System.Collections.Generic;

namespace IMUTestApp.Models
{
    public class AppConfig : INotifyPropertyChanged
    {
        public SerialPortConfig WheelMotorConfig { get; set; } = new();
        public SerialPortConfig IMUConfig { get; set; } = new();  // 改为SerialPortConfig类型   
        public TcpConfig TcpConfig { get; set; } = new TcpConfig();
        public GeneralSettings GeneralSettings { get; set; } = new GeneralSettings();
        
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
                _ipAddress = value;
                OnPropertyChanged();
            }
        }
        
        public int Port
        {
            get => _port;
            set
            {
                _port = value;
                OnPropertyChanged();
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    
    public class IMUConfig : INotifyPropertyChanged
    {
        private string _portName = string.Empty;
        private int _baudRate = 115200;
        
        public string PortName
        {
            get => _portName;
            set
            {
                _portName = value;
                OnPropertyChanged();
            }
        }
        
        public int BaudRate
        {
            get => _baudRate;
            set
            {
                _baudRate = value;
                OnPropertyChanged();
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
        private bool _autoDetectPorts = true;
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
        private string _logFormat = "[{Level}] {Timestamp} {Category} - {Message}";
        private Dictionary<string, string> _logCategories = new Dictionary<string, string>
        {
            { "SerialPort", "Info" },
            { "TCP", "Info" },
            { "IMUData", "Warn" },
            { "FileIO", "Error" },
            { "System", "Info" },
            { "UserAction", "Info" }
        };
        
        public bool AutoDetectPorts
        {
            get => _autoDetectPorts;
            set
            {
                _autoDetectPorts = value;
                OnPropertyChanged();
            }
        }
        
        public string DataPath
        {
            get => _dataPath;
            set
            {
                _dataPath = value;
                OnPropertyChanged();
            }
        }
        
        public string LogPath
        {
            get => _logPath;
            set
            {
                _logPath = value;
                OnPropertyChanged();
            }
        }
        
        public bool EnableLogRotation
        {
            get => _enableLogRotation;
            set
            {
                _enableLogRotation = value;
                OnPropertyChanged();
            }
        }
        
        public int LogRotationDays
        {
            get => _logRotationDays;
            set
            {
                _logRotationDays = value;
                OnPropertyChanged();
            }
        }
        
        public int MaxLogFileSize
        {
            get => _maxLogFileSize;
            set
            {
                _maxLogFileSize = value;
                OnPropertyChanged();
            }
        }
        
        public bool AutoCreateDirectories
        {
            get => _autoCreateDirectories;
            set
            {
                _autoCreateDirectories = value;
                OnPropertyChanged();
            }
        }
        
        public string DataFileFormat
        {
            get => _dataFileFormat;
            set
            {
                _dataFileFormat = value;
                OnPropertyChanged();
            }
        }
        
        public bool EnableDataBackup
        {
            get => _enableDataBackup;
            set
            {
                _enableDataBackup = value;
                OnPropertyChanged();
            }
        }
        
        public string BackupPath
        {
            get => _backupPath;
            set
            {
                _backupPath = value;
                OnPropertyChanged();
            }
        }
        
        public string LogLevel
        {
            get => _logLevel;
            set
            {
                _logLevel = value;
                OnPropertyChanged();
            }
        }

        public bool EnableConsoleLog
        {
            get => _enableConsoleLog;
            set
            {
                _enableConsoleLog = value;
                OnPropertyChanged();
            }
        }

        public string LogFormat
        {
            get => _logFormat;
            set
            {
                _logFormat = value;
                OnPropertyChanged();
            }
        }

        public Dictionary<string, string> LogCategories
        {
            get => _logCategories;
            set
            {
                _logCategories = value;
                OnPropertyChanged();
            }
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}