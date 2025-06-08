using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IMUTestApp.Models
{
    public class AppSettings : INotifyPropertyChanged
    {
        private string _dataPath = @"C:\IMUData";
        private string _logPath = @"C:\IMULogs";
        private bool _enableLogRotation = true;
        private int _logRotationDays = 7;
        private int _maxLogFileSize = 10; // MB
        private bool _autoCreateDirectories = true;
        private string _dataFileFormat = "csv";
        private bool _enableDataBackup = false;
        private string _backupPath = @"C:\IMUBackup";
        private string _wheelMotorPort = string.Empty;
        private string _imuPort = string.Empty;
        private int _wheelMotorBaudRate = 115200;
        private int _imuBaudRate = 115200;
        private bool _autoDetectPorts = true;
        
        public string DataPath
        {
            get => _dataPath;
            set => SetProperty(ref _dataPath, value);
        }
        
        public string LogPath
        {
            get => _logPath;
            set => SetProperty(ref _logPath, value);
        }
        
        public bool EnableLogRotation
        {
            get => _enableLogRotation;
            set => SetProperty(ref _enableLogRotation, value);
        }
        
        public int LogRotationDays
        {
            get => _logRotationDays;
            set => SetProperty(ref _logRotationDays, value);
        }
        
        public int MaxLogFileSize
        {
            get => _maxLogFileSize;
            set => SetProperty(ref _maxLogFileSize, value);
        }
        
        public bool AutoCreateDirectories
        {
            get => _autoCreateDirectories;
            set => SetProperty(ref _autoCreateDirectories, value);
        }
        
        public string DataFileFormat
        {
            get => _dataFileFormat;
            set => SetProperty(ref _dataFileFormat, value);
        }
        
        public bool EnableDataBackup
        {
            get => _enableDataBackup;
            set => SetProperty(ref _enableDataBackup, value);
        }
        
        public string BackupPath
        {
            get => _backupPath;
            set => SetProperty(ref _backupPath, value);
        }
        
        public event PropertyChangedEventHandler? PropertyChanged;
        
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        
        public string WheelMotorPort
        {
            get => _wheelMotorPort;
            set => SetProperty(ref _wheelMotorPort, value);
        }
        
        public string IMUPort
        {
            get => _imuPort;
            set => SetProperty(ref _imuPort, value);
        }
        
        public int WheelMotorBaudRate
        {
            get => _wheelMotorBaudRate;
            set => SetProperty(ref _wheelMotorBaudRate, value);
        }
        
        public int IMUBaudRate
        {
            get => _imuBaudRate;
            set => SetProperty(ref _imuBaudRate, value);
        }
        
        public bool AutoDetectPorts
        {
            get => _autoDetectPorts;
            set => SetProperty(ref _autoDetectPorts, value);
        }
    }
}