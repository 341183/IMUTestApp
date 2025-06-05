using System.Windows;
using IMUTestApp.ViewModels;

namespace IMUTestApp.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }
    }
}