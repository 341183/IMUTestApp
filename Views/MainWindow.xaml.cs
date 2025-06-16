using System.Windows;
using IMUTestApp.ViewModels;
using IMUTestApp.Services;
using System.ComponentModel;
using System.Threading.Tasks;

namespace IMUTestApp.Views
{
    public partial class MainWindow : Window
    {
        private MainViewModel? _mainViewModel;
        
        public MainWindow()
        {
            InitializeComponent();
            _mainViewModel = new MainViewModel();
            DataContext = _mainViewModel;
            
            // 订阅窗口关闭事件
            this.Closing += MainWindow_Closing;
        }
        
        private async void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // 取消默认关闭行为，先进行清理
            e.Cancel = true;
            
            try
            {
                // 如果有TestViewModel，进行清理
                if (_mainViewModel?.TestViewModel != null)
                {
                    await _mainViewModel.TestViewModel.CleanupOnApplicationExit();
                }
                
                // 清理完成后真正关闭程序
                this.Closing -= MainWindow_Closing; // 取消事件订阅避免递归
                Application.Current.Shutdown();
            }
            catch (System.Exception ex)
            {
                // 即使清理失败也要关闭程序
                MessageBox.Show($"程序关闭时清理资源失败: {ex.Message}", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Closing -= MainWindow_Closing;
                Application.Current.Shutdown();
            }
        }
    }
}