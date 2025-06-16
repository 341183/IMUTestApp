using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using IMUTestApp.Services;
using IMUTestApp.Models;

namespace IMUTestApp.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly DualSerialPortService _dualSerialPortService;
        private readonly ConfigService _configService;
        private readonly LoggingService _loggingService;
        private string _currentView = "Test";
        private string _deviceStatus = "未连接";
        
        public MainViewModel()
        {
            // 按正确的依赖顺序初始化服务
            _configService = new ConfigService();
            _loggingService = new LoggingService(_configService);
            _configService.SetLogger(_loggingService);
            _dualSerialPortService = new DualSerialPortService(_loggingService); // 添加这行

            _loggingService.LogInfo(LogCategory.System, "正在初始化应用程序主视图模型...");
            
            // 记录应用启动日志            
            _loggingService.LogInfo(LogCategory.System, "应用程序主视图模型已初始化");

            TestViewModel = new TestViewModel(_dualSerialPortService, _configService, _loggingService);
            ConfigViewModel = new ConfigViewModel( _configService, _loggingService);
            SettingsViewModel = new SettingsViewModel(_configService);
            
            ShowTestViewCommand = new RelayCommand(() => {
                //_loggingService.LogDebug(LogCategory.UserOperation, "用户切换到测试视图");
                CurrentView = "Test";
            });
            ShowConfigViewCommand = new RelayCommand(() => {
                //_loggingService.LogDebug(LogCategory.UserOperation, "用户切换到配置视图");
                CurrentView = "Config";
            });
            ShowSettingsViewCommand = new RelayCommand(() => {
                //_loggingService.LogDebug(LogCategory.UserOperation, "用户切换到设置视图");
                CurrentView = "Settings";
            });
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
        
        public ICommand ShowTestViewCommand { get; }
        public ICommand ShowConfigViewCommand { get; }
        public ICommand ShowSettingsViewCommand { get; }
        
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