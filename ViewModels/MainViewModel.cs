using System.Windows.Input;
using IMUTestApp.Services;
using IMUTestApp.Models;

namespace IMUTestApp.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly SerialPortService _serialPortService;
        private readonly SettingsService _settingsService;
        private string _currentView = "Test";
        private string _deviceStatus = "未连接";
        private string _dataStatus = "停止";
        
        public MainViewModel()
        {
            _serialPortService = new SerialPortService();
            _settingsService = new SettingsService();
            _serialPortService.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            TestViewModel = new TestViewModel(_serialPortService);
            ConfigViewModel = new ConfigViewModel(_serialPortService,_settingsService);
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
    }
}