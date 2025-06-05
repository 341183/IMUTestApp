using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows.Input;
using System.Windows.Threading;
using IMUTestApp.Models;
using IMUTestApp.Services;

namespace IMUTestApp.ViewModels
{
    public class TestViewModel : BaseViewModel
    {
        private readonly SerialPortService _serialPortService;
        private readonly DispatcherTimer _timer;
        private DateTime _testStartTime;
        private bool _isTestRunning;
        private int _packetCount;
        private string _runTime = "00:00:00";
        private string _sampleRate = "0 Hz";
        private string _dataDisplay = "等待开始测试...";
        
        public TestViewModel(SerialPortService serialPortService)
        {
            _serialPortService = serialPortService;
            _serialPortService.DataReceived += OnDataReceived;
            
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += OnTimerTick;
            
            TestData = new ObservableCollection<IMUData>();
            
            StartTestCommand = new RelayCommand(StartTest, () => !_isTestRunning && _serialPortService.IsConnected);
            StopTestCommand = new RelayCommand(StopTest, () => _isTestRunning);
            SaveDataCommand = new RelayCommand(SaveData, () => TestData.Count > 0);
        }
        
        public ObservableCollection<IMUData> TestData { get; }
        
        public bool IsTestRunning
        {
            get => _isTestRunning;
            set
            {
                SetProperty(ref _isTestRunning, value);
                ((RelayCommand)StartTestCommand).RaiseCanExecuteChanged();
                ((RelayCommand)StopTestCommand).RaiseCanExecuteChanged();
            }
        }
        
        public int PacketCount
        {
            get => _packetCount;
            set => SetProperty(ref _packetCount, value);
        }
        
        public string RunTime
        {
            get => _runTime;
            set => SetProperty(ref _runTime, value);
        }
        
        public string SampleRate
        {
            get => _sampleRate;
            set => SetProperty(ref _sampleRate, value);
        }
        
        public string DataDisplay
        {
            get => _dataDisplay;
            set => SetProperty(ref _dataDisplay, value);
        }
        
        public ICommand StartTestCommand { get; }
        public ICommand StopTestCommand { get; }
        public ICommand SaveDataCommand { get; }
        
        private void StartTest()
        {
            if (!_serialPortService.IsConnected) return;
            
            IsTestRunning = true;
            _testStartTime = DateTime.Now;
            PacketCount = 0;
            TestData.Clear();
            DataDisplay = "测试进行中...\n";
            _timer.Start();
        }
        
        private void StopTest()
        {
            IsTestRunning = false;
            _timer.Stop();
            DataDisplay += "\n测试已停止。";
        }
        
        private void SaveData()
        {
            try
            {
                var fileName = $"IMU_Data_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), fileName);
                
                var sb = new StringBuilder();
                sb.AppendLine("IMU测试数据");
                sb.AppendLine($"测试时间: {_testStartTime:yyyy-MM-dd HH:mm:ss}");
                sb.AppendLine($"数据包数量: {PacketCount}");
                sb.AppendLine("时间戳\t\t\t加速度X\t加速度Y\t加速度Z\t陀螺仪X\t陀螺仪Y\t陀螺仪Z\t磁力计X\t磁力计Y\t磁力计Z");
                
                foreach (var data in TestData)
                {
                    sb.AppendLine($"{data.Timestamp:HH:mm:ss.fff}\t{data.AccelX:F3}\t{data.AccelY:F3}\t{data.AccelZ:F3}\t{data.GyroX:F3}\t{data.GyroY:F3}\t{data.GyroZ:F3}\t{data.MagX:F3}\t{data.MagY:F3}\t{data.MagZ:F3}");
                }
                
                File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
                DataDisplay += $"\n数据已保存到: {filePath}";
            }
            catch (Exception ex)
            {
                DataDisplay += $"\n保存失败: {ex.Message}";
            }
        }
        
        private void OnDataReceived(object? sender, IMUData data)
        {
            if (!IsTestRunning) return;
            
            App.Current.Dispatcher.Invoke(() =>
            {
                TestData.Add(data);
                PacketCount++;
                DataDisplay += data.ToString() + "\n";
                
                // 计算采样率
                var elapsed = DateTime.Now - _testStartTime;
                if (elapsed.TotalSeconds > 0)
                {
                    SampleRate = $"{PacketCount / elapsed.TotalSeconds:F1} Hz";
                }
                
                ((RelayCommand)SaveDataCommand).RaiseCanExecuteChanged();
            });
        }
        
        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (IsTestRunning)
            {
                var elapsed = DateTime.Now - _testStartTime;
                RunTime = elapsed.ToString(@"hh\:mm\:ss");
            }
        }
    }
}