using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using IMUTestApp.Models;
using IMUTestApp.Services;
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

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

        // 图表相关属性
        private PlotModel _plotModel = new PlotModel();

        // 🔥 新增：将图表数据作为 ViewModel 属性保存
        public ObservableCollection<DataPoint> ChartDataPoints { get; }
        public ObservableCollection<DataPoint> UpperLimitPoints { get; }
        public ObservableCollection<DataPoint> LowerLimitPoints { get; }
        public ObservableCollection<DataPoint> BaselinePoints { get; }
        
        // 如果不需要，直接删除这个字段
        // 或者在构造函数中使用它
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
            
            // 🔥 初始化图表数据集合
            ChartDataPoints = new ObservableCollection<DataPoint>();
            UpperLimitPoints = new ObservableCollection<DataPoint>();
            LowerLimitPoints = new ObservableCollection<DataPoint>();
            BaselinePoints = new ObservableCollection<DataPoint>();
            
            // 初始化固定线条的数据点
            InitializeFixedLines();
            
            ClearProductCodeCommand = new RelayCommand(ClearProductCode);
        }
        
        private void InitializeFixedLines()
        {
            // 上限线
            UpperLimitPoints.Add(new DataPoint(0, 500));
            UpperLimitPoints.Add(new DataPoint(30, 500));
            
            // 下限线
            LowerLimitPoints.Add(new DataPoint(0, -500));
            LowerLimitPoints.Add(new DataPoint(30, -500));
            
            // 基准线
            BaselinePoints.Add(new DataPoint(0, 0));
            BaselinePoints.Add(new DataPoint(30, 0));
        }
        
        public ObservableCollection<IMUData> TestData { get; }
        
        public PlotModel PlotModel
        {
            get
            {
                // 🔥 每次访问时重新创建 PlotModel，并从 ViewModel 数据源同步数据
                var plotModel = new PlotModel
                {
                    Title = "IMU传感器数据相对于基准值的偏差",
                    Background = OxyColors.White
                };
                
                // 配置坐标轴
                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "采样点",
                    Minimum = 0,
                    Maximum = 30,
                    MajorStep = 5,
                    MinorStep = 1
                });
                
                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "偏差值",
                    Minimum = -600,
                    Maximum = 600,
                    MajorStep = 200,
                    MinorStep = 100
                });
                
                // 🔥 从 ViewModel 数据源创建系列
                var dataSeries = new LineSeries
                {
                    Title = "传感器数据",
                    Color = OxyColors.Blue,
                    StrokeThickness = 2
                };
                foreach (var point in ChartDataPoints)
                {
                    dataSeries.Points.Add(point);
                }
                plotModel.Series.Add(dataSeries);
                
                var upperLimitSeries = new LineSeries
                {
                    Title = "上限",
                    Color = OxyColors.Red,
                    StrokeThickness = 1,
                    LineStyle = LineStyle.Dash
                };
                foreach (var point in UpperLimitPoints)
                {
                    upperLimitSeries.Points.Add(point);
                }
                plotModel.Series.Add(upperLimitSeries);
                
                var lowerLimitSeries = new LineSeries
                {
                    Title = "下限",
                    Color = OxyColors.Red,
                    StrokeThickness = 1,
                    LineStyle = LineStyle.Dash
                };
                foreach (var point in LowerLimitPoints)
                {
                    lowerLimitSeries.Points.Add(point);
                }
                plotModel.Series.Add(lowerLimitSeries);
                
                var baselineSeries = new LineSeries
                {
                    Title = "基准线",
                    Color = OxyColors.Green,
                    StrokeThickness = 1,
                    LineStyle = LineStyle.Dot
                };
                foreach (var point in BaselinePoints)
                {
                    baselineSeries.Points.Add(point);
                }
                plotModel.Series.Add(baselineSeries);
                
                return plotModel;
            }
        }
        
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
            TestData.Clear();
            PacketCount = 0;
            TestResult = string.Empty;
            TestResultDetails = string.Empty;
            DataDisplay = "等待输入产品编码...";
            
            // 清空图表数据
            ChartDataPoints.Clear();
            // 🔥 添加空值检查
            PlotModel?.InvalidatePlot(true);
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
            
            // 清空图表数据
            ChartDataPoints.Clear();
            // 🔥 添加空值检查
            PlotModel?.InvalidatePlot(true);
            
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
        
        // 🔥 新增：清除图表数据的方法
        public void ClearChartData()
        {
            ChartDataPoints.Clear();
            OnPropertyChanged(nameof(PlotModel));
        }
        
        // 🔥 新增：重置测试数据的方法
        private void ResetTestData()
        {
            TestData.Clear();
            ClearChartData();
            PacketCount = 0;
            RunTime = "00:00:00";
            SampleRate = "0 Hz";
            DataDisplay = "等待输入产品编码...";
        }
        
        private void OnDataReceived(object? sender, IMUData data)
        {
            if (!IsTestRunning) return;
            
            // 使用Dispatcher确保在UI线程执行
            Application.Current.Dispatcher.Invoke(() =>
            {
                TestData.Add(data);
                PacketCount++;
                
                // 计算相对于基准值的偏差（这里以AccelX为例）
                double baselineValue = 0.0; // 基准值
                double deviation = (data.AccelX - baselineValue) * 1000; // 转换为合适的单位
                
                // 🔥 添加到 ViewModel 的数据集合中
                ChartDataPoints.Add(new DataPoint(PacketCount, deviation));
                
                // 限制图表数据点数量（保持最近30个点）
                if (ChartDataPoints.Count > 30)
                {
                    ChartDataPoints.RemoveAt(0);
                    
                    // 重新调整X轴坐标
                    for (int i = 0; i < ChartDataPoints.Count; i++)
                    {
                        ChartDataPoints[i] = new DataPoint(i + 1, ChartDataPoints[i].Y);
                    }
                }
                
                // 🔥 通知 PlotModel 属性更新，触发图表重新绘制
                OnPropertyChanged(nameof(PlotModel));
                
                // 更新数据显示
                DataDisplay += $"[{DateTime.Now:HH:mm:ss.fff}] X:{data.AccelX:F3} Y:{data.AccelY:F3} Z:{data.AccelZ:F3}\n";
                
                // 计算采样率
                if (TestData.Count > 1)
                {
                    var timeSpan = DateTime.Now - _testStartTime;
                    var rate = PacketCount / timeSpan.TotalSeconds;
                    SampleRate = $"{rate:F1} Hz";
                }
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