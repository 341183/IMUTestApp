using System.Windows.Controls;
using System.Windows.Input;
using IMUTestApp.ViewModels;

namespace IMUTestApp.Views
{
    public partial class TestView : UserControl
    {
        public TestView()
        {
            InitializeComponent();
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
            }
        }
    }
}