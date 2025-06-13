using System.Windows;
using IMUTestApp.ViewModels;
using IMUTestApp.Services;

namespace IMUTestApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            // 按正确的依赖顺序创建服务实例
            var configService = new ConfigService();
            var settingsService = new SettingsService();
            var loggingService = new LoggingService(configService, settingsService);
            
            // 设置ConfigService的日志服务关联
            configService.SetLogger(loggingService);
            
            // 创建需要LoggingService的其他服务
            //var serialPortService = new SerialPortService(loggingService);
            var dualSerialPortService = new DualSerialPortService(loggingService);
            
            // 创建MainViewModel并传递所有必需的服务
            DataContext = new MainViewModel(settingsService, configService, loggingService, dualSerialPortService);
        }
    }
}