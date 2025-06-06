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
        private string _dataDisplay = "等待输入产品编码...";
        private string _productCode = string.Empty;
        private string _testResult = string.Empty;
        private string _testResultDetails = string.Empty;
        private string _testItems = "IMU传感器数据采集测试";
        private string _testStandards = "数据采集稳定性测试\n采样率稳定性测试\n数据完整性测试";
        private string _testDateTime = string.Empty;
        private string _operator = "系统操作员";
        private bool _autoSaveEnabled = true;
        
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
            
            ClearProductCodeCommand = new RelayCommand(ClearProductCode);
        }
        
        public ObservableCollection<IMUData> TestData { get; }
        
        public bool IsTestRunning
        {
            get => _isTestRunning;
            set => SetProperty(ref _isTestRunning, value);
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
        
        public string ProductCode
        {
            get => _productCode;
            set
            {
                SetProperty(ref _productCode, value);
                // 当产品编码输入完毕且不为空时自动开始测试
                if (!string.IsNullOrWhiteSpace(value) && !_isTestRunning && _serialPortService.IsConnected)
                {
                    StartTest();
                }
            }
        }
        
        public string TestResult
        {
            get => _testResult;
            set => SetProperty(ref _testResult, value);
        }
        
        public string TestResultDetails
        {
            get => _testResultDetails;
            set => SetProperty(ref _testResultDetails, value);
        }
        
        public string TestItems
        {
            get => _testItems;
            set => SetProperty(ref _testItems, value);
        }
        
        public string TestStandards
        {
            get => _testStandards;
            set => SetProperty(ref _testStandards, value);
        }
        
        public string TestDateTime
        {
            get => _testDateTime;
            set => SetProperty(ref _testDateTime, value);
        }
        
        public string Operator
        {
            get => _operator;
            set => SetProperty(ref _operator, value);
        }
        
        public bool AutoSaveEnabled
        {
            get => _autoSaveEnabled;
            set => SetProperty(ref _autoSaveEnabled, value);
        }
        
        public ICommand ClearProductCodeCommand { get; }
        
        private void ClearProductCode()
        {
            ProductCode = string.Empty;
            TestResult = string.Empty;
            TestResultDetails = string.Empty;
            TestDateTime = string.Empty;
            DataDisplay = "等待输入产品编码...";
        }
        
        private void StartTest()
        {
            TestData.Clear();
            PacketCount = 0;
            IsTestRunning = true;
            _testStartTime = DateTime.Now;
            TestDateTime = _testStartTime.ToString("yyyy-MM-dd HH:mm:ss");
            TestResult = string.Empty;
            TestResultDetails = "测试进行中...";
            
            DataDisplay = $"产品编码: {ProductCode}\n测试开始时间: {_testStartTime:yyyy-MM-dd HH:mm:ss}\n\n";
            
            _timer.Start();
            
            // 设置测试自动停止时间（例如30秒后自动停止）
            var autoStopTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30)
            };
            autoStopTimer.Tick += (s, e) =>
            {
                autoStopTimer.Stop();
                if (IsTestRunning)
                {
                    StopTest();
                }
            };
            autoStopTimer.Start();
        }
        
        private void StopTest()
        {
            IsTestRunning = false;
            _timer.Stop();
            
            // 执行测试结果判定逻辑
            EvaluateTestResult();
            
            DataDisplay += $"\n测试结束时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
            DataDisplay += $"\n测试结果: {TestResult}";
            DataDisplay += $"\n{TestResultDetails}";
            
            // 如果启用自动保存，则自动保存数据
            if (AutoSaveEnabled && TestData.Count > 0)
            {
                SaveData();
            }
        }
        
        private void EvaluateTestResult()
        {
            // 示例测试判定逻辑
            bool isPass = true;
            var details = new StringBuilder();
            
            // 检查数据包数量
            if (PacketCount < 10)
            {
                isPass = false;
                details.AppendLine("数据包数量不足");
            }
            else
            {
                details.AppendLine($"数据包数量: {PacketCount} ✓");
            }
            
            // 检查测试时长
            var testDuration = DateTime.Now - _testStartTime;
            if (testDuration.TotalSeconds < 5)
            {
                isPass = false;
                details.AppendLine("测试时长不足");
            }
            else
            {
                details.AppendLine($"测试时长: {testDuration.TotalSeconds:F1}秒 ✓");
            }
            
            // 检查采样率稳定性
            if (SampleRate.Contains("0 Hz"))
            {
                isPass = false;
                details.AppendLine("采样率异常");
            }
            else
            {
                details.AppendLine($"采样率: {SampleRate} ✓");
            }
            
            TestResult = isPass ? "PASS" : "NG";
            TestResultDetails = details.ToString().Trim();
        }
        
        private void SaveData()
        {
            try
            {
                var fileName = $"IMU_Test_{ProductCode}_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                var filePath = Path.Combine("Data", fileName);
                
                // 确保Data目录存在
                Directory.CreateDirectory("Data");
                
                var content = new StringBuilder();
                content.AppendLine($"产品编码: {ProductCode}");
                content.AppendLine($"测试时间: {TestDateTime}");
                content.AppendLine($"测试结果: {TestResult}");
                content.AppendLine($"测试详情: {TestResultDetails}");
                content.AppendLine($"数据包总数: {PacketCount}");
                content.AppendLine($"采样率: {SampleRate}");
                content.AppendLine($"运行时间: {RunTime}");
                content.AppendLine();
                content.AppendLine("详细数据:");
                content.AppendLine(DataDisplay);
                
                File.WriteAllText(filePath, content.ToString(), Encoding.UTF8);
                
                DataDisplay += $"\n\n数据已自动保存到: {filePath}";
            }
            catch (Exception ex)
            {
                DataDisplay += $"\n\n保存失败: {ex.Message}";
            }
        }
        
        private void OnDataReceived(object sender, IMUData data)
        {
            if (!IsTestRunning) return;
            
            TestData.Add(data);
            PacketCount++;
            
            // 更新数据显示
            DataDisplay += $"[{DateTime.Now:HH:mm:ss.fff}] X:{data.AccelX:F3} Y:{data.AccelY:F3} Z:{data.AccelZ:F3}\n";
            
            // 计算采样率
            if (TestData.Count > 1)
            {
                var timeSpan = DateTime.Now - _testStartTime;
                var rate = PacketCount / timeSpan.TotalSeconds;
                SampleRate = $"{rate:F1} Hz";
            }
        }
        
        private void OnTimerTick(object sender, EventArgs e)
        {
            if (IsTestRunning)
            {
                var elapsed = DateTime.Now - _testStartTime;
                RunTime = elapsed.ToString(@"hh\:mm\:ss");
            }
        }
    }
}