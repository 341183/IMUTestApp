using System.Windows;
using System;
using System.Windows.Controls;
using System.Windows.Input;
using IMUTestApp.ViewModels;

namespace IMUTestApp.Views
{
    public partial class TestView : UserControl
    {
        // 添加一个计时器字段
        private System.Windows.Threading.DispatcherTimer _scanTimer;
        
        // 在构造函数中初始化计时器
        public TestView()
        {
            InitializeComponent();
            
            // 初始化扫码延迟计时器
            _scanTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = System.TimeSpan.FromMilliseconds(500) // 500ms延迟
            };
            _scanTimer.Tick += ScanTimer_Tick;
            
            // 设置加载完成后的焦点
            this.Loaded += (sender, e) => 
            {
                ProductCodeTextBox.Focus();
            };
        }
        
        private void ProductCodeTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // 获取 ViewModel 并调用回车处理方法
                if (DataContext is TestViewModel viewModel)
                {
                    viewModel.OnProductCodeEnterPressed();
                }
                
                // 防止回车键产生换行
                e.Handled = true;
            }
        }
        
        // 添加文本变化事件处理
        private void ProductCodeTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 重置计时器
            _scanTimer.Stop();
            
            // 如果文本长度达到要求，启动计时器
            if (ProductCodeTextBox.Text.Length >= 10)
            {
                _scanTimer.Start();
            }
        }
        
        // 计时器触发事件
        private void ScanTimer_Tick(object sender, EventArgs e)
        {
            _scanTimer.Stop();
            
            // 自动触发测试
            if (DataContext is TestViewModel viewModel)
            {
                viewModel.OnProductCodeEnterPressed();
            }
        }
        
        // 新增：当文本内容改变时自动滚动到底部
        private void DataDisplayTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // 滚动到最底部
                textBox.ScrollToEnd();
            }
        }
    }
}