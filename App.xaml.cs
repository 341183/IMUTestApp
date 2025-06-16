using System.Configuration;
using System.Data;
using System.Windows;
using System.Threading.Tasks;

namespace IMUTestApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // 订阅应用程序退出事件
            this.Exit += App_Exit;
            
            // 订阅未处理异常事件
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }
        
        private void App_Exit(object sender, ExitEventArgs e)
        {
            // 应用程序退出时的清理工作
            // 这里可以添加全局清理逻辑
        }
        
        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            // 处理未捕获的异常
            MessageBox.Show($"程序发生未处理的异常: {e.Exception.Message}\n\n程序将尝试安全关闭。", 
                          "严重错误", MessageBoxButton.OK, MessageBoxImage.Error);
            
            // 标记异常已处理，避免程序崩溃
            e.Handled = true;
            
            // 安全关闭应用程序
            this.Shutdown();
        }
    }
}
