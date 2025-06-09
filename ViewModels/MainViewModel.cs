using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IMUTestApp.Services;
using IMUTestApp.Models;

namespace IMUTestApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly SerialPortService _serialPortService;
        private readonly SettingsService _settingsService;
        private readonly ConfigService _configService;
        private string _currentView = "Test";
        private string _deviceStatus = "未连接";
        private string _dataStatus = "无数据";
        
        public MainViewModel()
        {
            _serialPortService = new SerialPortService();
            _settingsService = new SettingsService();
            _configService = new ConfigService();
            _serialPortService.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            TestViewModel = new TestViewModel(_serialPortService);
            ConfigViewModel = new ConfigViewModel(_serialPortService, _configService);
            SettingsViewModel = new SettingsViewModel(_settingsService);
            
            ShowTestViewCommand = new RelayCommand(() => CurrentView = "Test");
            ShowConfigViewCommand = new RelayCommand(() => CurrentView = "Config");
            ShowSettingsViewCommand = new RelayCommand(() => CurrentView = "Settings");
        }
        
        public TestViewModel TestViewModel { get; }
        public ConfigViewModel ConfigViewModel { get; }
        public SettingsViewModel SettingsViewModel { get; }
        
        public string CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }
        
        public string DeviceStatus
        {
            get => _deviceStatus;
            set => SetProperty(ref _deviceStatus, value);
        }
        
        public string DataStatus
        {
            get => _dataStatus;
            set => SetProperty(ref _dataStatus, value);
        }
        
        public ICommand ShowTestViewCommand { get; }
        public ICommand ShowConfigViewCommand { get; }
        public ICommand ShowSettingsViewCommand { get; }
        
        private void OnConnectionStatusChanged(object? sender, bool isConnected)
        {
            DeviceStatus = isConnected ? "已连接" : "未连接";
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
    }
}