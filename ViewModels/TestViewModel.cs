using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using IMUTestApp.Models;
using IMUTestApp.Services;
using SimpleWifi; // 添加这行
using OxyPlot;
using OxyPlot.Series;
using OxyPlot.Axes;

namespace IMUTestApp.ViewModels
{
    // 在类的顶部添加模板数据字段
    public partial class TestViewModel : BaseViewModel
    {
        private readonly RetryConnectionService _retryService;
        //串口连接
        private readonly DualSerialPortService _dualSerialPortService;
        //配置文件
        private readonly ConfigService _configService;
        private readonly LoggingService _loggingService; // 添加日志服务

        
        // 模板数据相关字段
        private bool _hasTemplate = false;

        private IMUData? _templateData; 
        private readonly List<IMUData> _tcpTestData = new List<IMUData>();
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

        private string _testDateTime = string.Empty;

        private bool _autoSaveEnabled = true;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             

        // 🔥 新增：将图表数据作为 ViewModel 属性保存
        public ObservableCollection<DataPoint> ChartDataPoints { get; }
        public ObservableCollection<DataPoint> UpperLimitPoints { get; }
        public ObservableCollection<DataPoint> LowerLimitPoints { get; }
        public ObservableCollection<DataPoint> BaselinePoints { get; }
        
        // 如果不需要，直接删除这个字段
        // 或者在构造函数中使用它
        public TestViewModel(DualSerialPortService dualSerialPortService, ConfigService configService, LoggingService loggingService) // 添加LoggingService参数
        {
            _dualSerialPortService = dualSerialPortService;
            _configService = configService;
            _loggingService = loggingService; // 初始化日志服务

            _retryService = new RetryConnectionService(loggingService);
            
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
        
        //绘制默认界面线条
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
        
        //存放接收到的数据
        public ObservableCollection<IMUData> TestData { get; }
        
        //绘制接收到的数据图表
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
        
        //返回测试状态
        public bool IsTestRunning
        {
            get => _isTestRunning;
            set => SetProperty(ref _isTestRunning, value);
        }
        
        //接收到的数据包数量
        public int PacketCount
        {
            get => _packetCount;
            set => SetProperty(ref _packetCount, value);
        }
        
        //运行时间
        public string RunTime
        {
            get => _runTime;
            set => SetProperty(ref _runTime, value);
        }
        
        //采样率 HZ
        public string SampleRate
        {
            get => _sampleRate;
            set => SetProperty(ref _sampleRate, value);
        }
        
        //界面文本框显示内容
        public string DataDisplay
        {
            get => _dataDisplay;
            set => SetProperty(ref _dataDisplay, value);
        }
        
        //产品编号
        public string ProductCode
        {
            get => _productCode;
            set => SetProperty(ref _productCode, value);
        }
        
        // 添加处理回车键的方法
        public void OnProductCodeEnterPressed()
        {
            // 检查字符长度
            if (string.IsNullOrWhiteSpace(ProductCode) || ProductCode.Length < 10)
            {
                // 弹框警告
                System.Windows.MessageBox.Show("产品编码错误，请重新输入！", "输入错误", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                return;
            }
            
            // 检查测试条件
            if (!_isTestRunning)
            {
                StartTest();
            }
            else if (_isTestRunning)
            {
                System.Windows.MessageBox.Show("测试正在进行中，请等待测试完成！", "提示", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            }
        }
        
        //测试结果
        public string TestResult
        {
            get => _testResult;
            set => SetProperty(ref _testResult, value);
        }
        
        //测试错误原因  PASS右侧显示
        public string TestResultDetails
        {
            get => _testResultDetails;
            set => SetProperty(ref _testResultDetails, value);
        }       
        
        //测试时间
        public string TestDateTime
        {
            get => _testDateTime;
            set => SetProperty(ref _testDateTime, value);
        }
        
        //自动保存
        public bool AutoSaveEnabled
        {
            get => _autoSaveEnabled;
            set => SetProperty(ref _autoSaveEnabled, value);
        }
        
        public ICommand ClearProductCodeCommand { get; }
        
        //清除产品代码
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
        
        //开始测试
        private async void StartTest()
        {
            _loggingService.LogInfo(LogCategory.UserAction, $"开始测试 - 产品编码: {ProductCode}");
            
            TestData.Clear();
            PacketCount = 0;
            IsTestRunning = true;
            _testStartTime = DateTime.Now;
            TestDateTime = _testStartTime.ToString("yyyy-MM-dd HH:mm:ss");
            TestResult = string.Empty;
            TestResultDetails = "测试进行中...";
            
            _loggingService.LogInfo(LogCategory.IMUData, $"测试开始时间: {_testStartTime:yyyy-MM-dd HH:mm:ss}");
            
            DataDisplay = $"产品编码: {ProductCode}\n测试开始时间: {_testStartTime:yyyy-MM-dd HH:mm:ss}\n\n";
            
            // 清空图表数据
            ChartDataPoints.Clear();
            PlotModel?.InvalidatePlot(true);
            
            try
            {
                _loggingService.LogDebug(LogCategory.IMUData, "开始执行测试步骤");
                
                // 步骤1：控制波轮电机转速为50%
                await Step1_ControlMotor();
                
                // 步骤2：连接IMU并获取设备信息
                var deviceInfo = await Step2_GetDeviceInfo();
                
                if (deviceInfo != null)
                {
                    _loggingService.LogInfo(LogCategory.IMUData, $"获取到设备信息 - AP名称: {deviceInfo.ApName}");
                    
                    // 步骤3：连接WiFi热点
                    await Step3_ConnectWiFi(deviceInfo.ApName);
                    
                    // 步骤4：TCP协议测试
                    await Step4_TcpProtocolTest();
                }
                else
                {
                    _loggingService.LogWarn(LogCategory.IMUData, "未能获取到设备信息");
                }
                
                // 启动定时器开始收集数据
                _timer.Start();
                _loggingService.LogDebug(LogCategory.IMUData, "数据收集定时器已启动");
                
                // 设置测试自动停止时间（例如30秒后自动停止）
                var autoStopTimer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromSeconds(30)
                };
                autoStopTimer.Tick += async (s, e) =>
                {
                    autoStopTimer.Stop();
                    if (IsTestRunning)
                    {
                        _loggingService.LogInfo(LogCategory.IMUData, "测试达到预设时间，自动停止");
                        await StopTestAsync();
                    }
                };
                autoStopTimer.Start();
            }
            catch (Exception ex)
            {
                TestResult = "NG";
                TestResultDetails = $"测试失败: {ex.Message}";
                DataDisplay += $"\n错误: {ex.Message}";
                IsTestRunning = false;
            }
        }
        
        // 步骤1：控制波轮电机
        private async Task Step1_ControlMotor()
        {
            _loggingService.LogDebug(LogCategory.IMUData, "步骤1: 开始启动波轮电机");
            DataDisplay += "步骤1: 启动波轮电机...\n";
            
            bool connected = await _retryService.RetryConnectionAsync(
            config => _dualSerialPortService.ConnectWheelMotor(config),
            _configService.WheelMotorPort,
            "波轮电机串口",
            maxRetries: 3,
            timeoutSeconds: 10,
            progressCallback: message => DataDisplay += $"{message}\n"
            );


            if (connected && _dualSerialPortService?.IsWheelMotorConnected == true)
            {
                // 发送电机控制指令
                await _dualSerialPortService.SendToWheelMotorAsync("fan pwm 50\r\n");
                _loggingService.LogInfo(LogCategory.IMUData, "已发送电机控制指令: fan pwm 50");
                DataDisplay += "已发送指令: fan pwm 50\n";
                
                // 等待电机启动
                await Task.Delay(2000);
                _loggingService.LogInfo(LogCategory.IMUData, "波轮电机已启动至50%转速");
                DataDisplay += "波轮电机已启动至50%转速\n";
            }
            else
            {
                _loggingService.LogError(LogCategory.IMUData, "波轮电机串口未连接");
                throw new Exception("波轮电机串口未连接");
            }
        }
        
        // 步骤2：获取IMU设备信息
        private async Task<DeviceInfo> Step2_GetDeviceInfo()
        {
            DataDisplay += "\n步骤2: 获取IMU设备信息...\n";

            bool connected = await _retryService.RetryConnectionAsync(
            config => _dualSerialPortService.ConnectIMU(config),
            _configService.IMUPort,
            "IMU串口",
            maxRetries: 3,
            timeoutSeconds: 15,
            progressCallback: message => DataDisplay += $"{message}\n"
            );
            
            if (connected && _dualSerialPortService?.IsIMUConnected == true)

            {
                // 发送设备信息请求
                var request = "{\"DevInfo\":{}}";
                await _dualSerialPortService.SendToIMUAsync(request + "\r\n");
                DataDisplay += $"已发送请求: {request}\n";
                
                // 等待响应（这里需要实现响应解析逻辑）
                var response = await WaitForDeviceInfoResponse();
                
                if (response != null)
                {
                    DataDisplay += $"设备信息: 产品={response.Product}, 固件版本={response.FwVer}\n";
                    DataDisplay += $"热点名称: {response.ApName}\n";
                    return response;
                }
                else
                {
                    throw new Exception("未收到设备信息响应");
                }
            }
            else
            {
                throw new Exception("IMU串口未连接");
            }
        }
        
        // 步骤3：连接WiFi热点
        private async Task Step3_ConnectWiFi(string apName)
        {
            DataDisplay += $"\n步骤3: 连接WiFi热点 {apName}...\n";
            
            try
            {
                // 需要添加密码参数，可以从配置中获取或设为空字符串
                string wifiPassword = ""; // 或者从配置文件中获取
                
                // 创建WiFi连接参数对象
                var wifiConfig = new { SSID = apName, Password = wifiPassword };
                
                // 使用重试服务连接WiFi
                bool connected = await _retryService.RetryConnectionAsync(
                    async config => await ConnectToWiFiAsync(config.SSID, config.Password),
                    wifiConfig,
                    $"WiFi热点 {apName}",
                    maxRetries: 3,
                    timeoutSeconds: 15,
                    progressCallback: message => DataDisplay += $"{message}\n"
                );
                
                if (connected)
                {
                    DataDisplay += $"已成功连接到热点: {apName}\n";
                }
                else
                {
                    throw new Exception($"经过重试后仍无法连接到WiFi热点: {apName}");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"WiFi连接失败: {ex.Message}");
            }
        }
        
        // 步骤4：TCP协议测试
        private async Task Step4_TcpProtocolTest()
        {
            DataDisplay += "\n步骤4: 开始TCP协议测试...\n";
            
            try
            {
                // 修复第450-470行的代码，将变量名统一：
                {
                    // 获取TCP配置
                    var config = _configService.Config;
                    string ipAddress = config.TcpConfig.IpAddress;
                    int port = config.TcpConfig.Port; 
                    
                    using (var tcpClient = new System.Net.Sockets.TcpClient())
                    {
                        await tcpClient.ConnectAsync(ipAddress, port); // 使用正确的变量名
                        DataDisplay += $"TCP连接已建立: {ipAddress}:{port}\n"; // 使用正确的变量名
                        
                        var stream = tcpClient.GetStream();
                        
                        // 发送空JSON请求获取IMU数据
                        string request = "{\"IMU\":{}}";
                        byte[] requestData = System.Text.Encoding.UTF8.GetBytes(request);
                        await stream.WriteAsync(requestData, 0, requestData.Length);
                        DataDisplay += $"发送请求: {request}\n";
                        
                        // 读取响应
                        byte[] buffer = new byte[1024];
                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        DataDisplay += $"收到响应: {response}\n";
                        
                        // 解析IMU数据
                        var imuData = ParseIMUResponse(response);
                        if (imuData != null)
                        {
                            // 检查是否有模板数据
                            if (!_hasTemplate)
                            {
                                _templateData = imuData;
                                _hasTemplate = true;
                                DataDisplay += "已保存第一组数据作为模板\n";
                            }
                            
                            // 开始连续接收IMU数据进行测试
                            await ContinuousIMUDataCollection(stream);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                DataDisplay += $"TCP连接失败: {ex.Message}\n";
                TestResult = "NG";
            }
        }
        
        // 执行连续IMU测试
        private async Task ContinuousIMUDataCollection(System.Net.Sockets.NetworkStream stream)
        {
            DataDisplay += "\n开始连续IMU测试...\n";
            _tcpTestData.Clear();
            
            // 设置测试时间（例如30秒）
            var testEndTime = DateTime.Now.AddSeconds(30);
            string request = "{\"IMU\":{}}";
            byte[] requestData = System.Text.Encoding.UTF8.GetBytes(request);
            
            while (DateTime.Now < testEndTime && IsTestRunning)
            {
                try
                {
                    // 发送请求
                    await stream.WriteAsync(requestData, 0, requestData.Length);
                    
                    // 读取响应
                    byte[] buffer = new byte[1024];
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    string response = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    
                    // 解析数据
                    var imuData = ParseIMUResponse(response);
                    if (imuData != null)
                    {
                        _tcpTestData.Add(imuData);
                        
                        // 更新显示
                        DataDisplay += $"IMU数据 - Roll: {imuData.Roll:F2}, Pitch: {imuData.Pitch:F2}, " +
                                     $"Yaw: {imuData.Yaw:F2}, GyroX: {imuData.GyroX:F2}, GyroY: {imuData.GyroY:F2}\n";
                    }
                    
                    // 等待一段时间再发送下一个请求
                    await Task.Delay(100);
                }
                catch (Exception ex)
                {
                    DataDisplay += $"读取IMU数据时出错: {ex.Message}\n";
                    break;
                }
            }
            
            // 分析测试结果
            AnalyzeIMUTestResults();
        }
        
        // 解析IMU响应数据
        private IMUData ParseIMUResponse(string response)
        {
            try
            {
                // 查找IMU数据部分
                var imuStartIndex = response.IndexOf("\"IMU\":");
                if (imuStartIndex == -1) return null;
                
                var imuSection = response.Substring(imuStartIndex);
                
                // 简单解析（实际项目中建议使用JSON库）
                var rollMatch = System.Text.RegularExpressions.Regex.Match(imuSection, @"""roll""\s*:\s*([+-]?\d*\.?\d+)");
                var pitchMatch = System.Text.RegularExpressions.Regex.Match(imuSection, @"""pitch""\s*:\s*([+-]?\d*\.?\d+)");
                var yawMatch = System.Text.RegularExpressions.Regex.Match(imuSection, @"""yaw""\s*:\s*([+-]?\d*\.?\d+)");
                var gyroXMatch = System.Text.RegularExpressions.Regex.Match(imuSection, @"""gyro_x""\s*:\s*([+-]?\d*\.?\d+)");
                var gyroYMatch = System.Text.RegularExpressions.Regex.Match(imuSection, @"""gyro_y""\s*:\s*([+-]?\d*\.?\d+)");
                
                if (rollMatch.Success && pitchMatch.Success && yawMatch.Success)
                {
                    return new IMUData
                    {
                        Roll = double.Parse(rollMatch.Groups[1].Value),
                        Pitch = double.Parse(pitchMatch.Groups[1].Value),
                        Yaw = double.Parse(yawMatch.Groups[1].Value),
                        GyroX = gyroXMatch.Success ? double.Parse(gyroXMatch.Groups[1].Value) : 0,
                        GyroY = gyroYMatch.Success ? double.Parse(gyroYMatch.Groups[1].Value) : 0,
                        Timestamp = DateTime.Now
                    };
                }
            }
            catch (Exception ex)
            {
                DataDisplay += $"解析IMU数据失败: {ex.Message}\n";
            }
            
            return null;
        }
        
        // 分析IMU测试结果
        private void AnalyzeIMUTestResults()
        {
            if (_tcpTestData.Count == 0)
            {
                DataDisplay += "\n测试结果: NG - 未获取到有效数据\n";
                TestResult = "NG";
                TestResultDetails = "未获取到有效的IMU数据";
                return;
            }
            
            DataDisplay += $"\n分析 {_tcpTestData.Count} 组IMU数据...\n";
            
            // 计算各参数的最大最小值
            var gyroXValues = _tcpTestData.Select(d => d.GyroX).ToList();
            var gyroYValues = _tcpTestData.Select(d => d.GyroY).ToList();
            var pitchValues = _tcpTestData.Select(d => d.Pitch).ToList();
            var rollValues = _tcpTestData.Select(d => d.Roll).ToList();
            
            var gyroXRange = gyroXValues.Max() - gyroXValues.Min();
            var gyroYRange = gyroYValues.Max() - gyroYValues.Min();
            var pitchRange = pitchValues.Max() - pitchValues.Min();
            var rollRange = rollValues.Max() - rollValues.Min();
            
            DataDisplay += $"GyroX 范围: {gyroXRange:F2} (最小: {gyroXValues.Min():F2}, 最大: {gyroXValues.Max():F2})\n";
            DataDisplay += $"GyroY 范围: {gyroYRange:F2} (最小: {gyroYValues.Min():F2}, 最大: {gyroYValues.Max():F2})\n";
            DataDisplay += $"Pitch 范围: {pitchRange:F2} (最小: {pitchValues.Min():F2}, 最大: {pitchValues.Max():F2})\n";
            DataDisplay += $"Roll 范围: {rollRange:F2} (最小: {rollValues.Min():F2}, 最大: {rollValues.Max():F2})\n";
            
            // 判断测试结果
            bool isPass = true;
            var failedParams = new List<string>();
            
            if (gyroXRange > 500)
            {
                isPass = false;
                failedParams.Add($"GyroX范围超标({gyroXRange:F2})");
            }
            
            if (gyroYRange > 500)
            {
                isPass = false;
                failedParams.Add($"GyroY范围超标({gyroYRange:F2})");
            }
            
            if (pitchRange > 500)
            {
                isPass = false;
                failedParams.Add($"Pitch范围超标({pitchRange:F2})");
            }
            
            if (rollRange > 500)
            {
                isPass = false;
                failedParams.Add($"Roll范围超标({rollRange:F2})");
            }
            
            // 设置测试结果
            if (isPass)
            {
                TestResult = "PASS";
                TestResultDetails = "所有IMU参数变化范围均在500以内，测试通过";
                DataDisplay += "\n测试结果: PASS - 所有参数变化范围正常\n";
            }
            else
            {
                TestResult = "NG";
                TestResultDetails = $"以下参数超出范围: {string.Join(", ", failedParams)}";
                DataDisplay += $"\n测试结果: NG - {TestResultDetails}\n";
            }
        }
        
        // 停止测试时关闭电机
        private async Task StopTestAsync()
        {
            _loggingService.LogInfo(LogCategory.IMUData, "开始停止测试");
            
            IsTestRunning = false;
            _timer.Stop();
            
            try
            {
                // 关闭波轮电机
                if (_dualSerialPortService?.IsWheelMotorConnected == true)
                {
                    await _dualSerialPortService.SendToWheelMotorAsync("fan pwm 0\r\n");
                    _loggingService.LogInfo(LogCategory.IMUData, "波轮电机已关闭");
                    DataDisplay += "\n波轮电机已关闭\n";
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError(LogCategory.IMUData, $"关闭电机时出错: {ex.Message}", ex);
                DataDisplay += $"\n关闭电机时出错: {ex.Message}\n";
            }
            
            // 执行测试结果判定逻辑
            EvaluateTestResult();
            
            var endTime = DateTime.Now;
            var testDuration = endTime - _testStartTime;
            _loggingService.LogInfo(LogCategory.IMUData, $"测试结束 - 结果: {TestResult}, 测试时长: {testDuration.TotalSeconds:F1}秒, 数据包数量: {PacketCount}");
            
            DataDisplay += $"\n测试结束时间: {endTime:yyyy-MM-dd HH:mm:ss}";
            DataDisplay += $"\n测试结果: {TestResult}";
            DataDisplay += $"\n{TestResultDetails}";
            
            // 如果启用自动保存，则自动保存数据
            if (AutoSaveEnabled && TestData.Count > 0)
            {
                _loggingService.LogDebug(LogCategory.FileIO, "开始自动保存测试数据");
                SaveData();
            }
        }
        
        private void EvaluateTestResult()
        {
            _loggingService.LogDebug(LogCategory.IMUData, "开始评估测试结果");
            
            // 示例测试判定逻辑
            bool isPass = true;
            var details = new StringBuilder();
            
            // 检查数据包数量
            if (PacketCount < 10)
            {
                isPass = false;
                details.AppendLine("数据包数量不足");
                _loggingService.LogWarn(LogCategory.IMUData, $"数据包数量不足: {PacketCount} < 10");
            }
            else
            {
                details.AppendLine($"数据包数量: {PacketCount} ✓");
                _loggingService.LogDebug(LogCategory.IMUData, $"数据包数量检查通过: {PacketCount}");
            }
            
            // 检查测试时长
            var testDuration = DateTime.Now - _testStartTime;
            if (testDuration.TotalSeconds < 5)
            {
                isPass = false;
                details.AppendLine("测试时长不足");
                _loggingService.LogWarn(LogCategory.IMUData, $"测试时长不足: {testDuration.TotalSeconds:F1}秒 < 5秒");
            }
            else
            {
                details.AppendLine($"测试时长: {testDuration.TotalSeconds:F1}秒 ✓");
                _loggingService.LogDebug(LogCategory.IMUData, $"测试时长检查通过: {testDuration.TotalSeconds:F1}秒");
            }
            
            // 检查采样率稳定性
            if (SampleRate.Contains("0 Hz"))
            {
                isPass = false;
                details.AppendLine("采样率异常");
                _loggingService.LogWarn(LogCategory.IMUData, $"采样率异常: {SampleRate}");
            }
            else
            {
                details.AppendLine($"采样率: {SampleRate} ✓");
                _loggingService.LogDebug(LogCategory.IMUData, $"采样率检查通过: {SampleRate}");
            }
            
            TestResult = isPass ? "PASS" : "NG";
            TestResultDetails = details.ToString().Trim();
            
            _loggingService.LogInfo(LogCategory.IMUData, $"测试结果评估完成 - 产品编码: {ProductCode}, 结果: {TestResult}");
            if (!isPass)
            {
                _loggingService.LogWarn(LogCategory.IMUData, $"测试失败详情: {TestResultDetails}");
            }
        }
        
        private void SaveData()
        {
            try
            {
                _loggingService.LogDebug(LogCategory.FileIO, $"开始保存测试数据 - 产品编码: {ProductCode}");
                
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
        
        private string? _pendingDeviceInfoResponse = null;
        private readonly object _responseLock = new object();
        
        private async Task<DeviceInfo> WaitForDeviceInfoResponse()
        {
            // 订阅轮子电机数据接收事件
            _dualSerialPortService.WheelMotorDataReceived += OnWheelMotorDataReceived;
            
            try
            {
                var timeout = TimeSpan.FromSeconds(10);
                var startTime = DateTime.Now;
                
                _loggingService?.LogInfo(LogCategory.SerialPort, "等待设备信息响应...");
                
                //间隔0.1s解析信息
                while (DateTime.Now - startTime < timeout)
                {
                    string? responseData = null;
                    
                    // 线程安全地检查是否收到响应数据
                    lock (_responseLock)
                    {
                        if (!string.IsNullOrEmpty(_pendingDeviceInfoResponse))
                        {
                            responseData = _pendingDeviceInfoResponse;
                            _pendingDeviceInfoResponse = null; // 清空缓存
                        }
                    }
                    
                    if (!string.IsNullOrEmpty(responseData))
                    {
                        try
                        {
                            // 解析JSON响应
                            var deviceInfoResponse = System.Text.Json.JsonSerializer.Deserialize<DeviceInfoResponse>(responseData);
                            
                            if (deviceInfoResponse?.Result == 0) // 假设0表示成功
                            {
                                _loggingService?.LogInfo(LogCategory.SerialPort, "设备信息获取成功");
                                return deviceInfoResponse.DevInfo;
                            }
                            else
                            {
                                _loggingService?.LogError(LogCategory.SerialPort, $"设备信息获取失败，错误码: {deviceInfoResponse?.Result}");
                            }
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            _loggingService?.LogError(LogCategory.SerialPort, $"JSON解析失败: {ex.Message}, 原始数据: {responseData}");
                        }
                    }
                    
                    await Task.Delay(100); // 每100ms检查一次
                }
                
                _loggingService?.LogError(LogCategory.SerialPort, "等待设备信息响应超时");
                return null; // 超时返回null
            }
            finally
            {
                // 取消订阅事件
                _dualSerialPortService.WheelMotorDataReceived -= OnWheelMotorDataReceived;
            }
        }

        private void OnWheelMotorDataReceived(object? sender, string data)
        {
            if (string.IsNullOrEmpty(data)) return;
            
            _loggingService?.LogDebug(LogCategory.SerialPort, $"收到轮子电机数据: {data}");
            
            // 检查是否是设备信息响应（可以根据JSON结构或特定字段判断）
            if (data.Contains("DevInfo") || data.Contains("product") || data.Contains("fw_ver"))
            {
                lock (_responseLock)
                {
                    _pendingDeviceInfoResponse = data;
                }
                
                _loggingService?.LogInfo(LogCategory.SerialPort, "检测到设备信息响应数据");
            }
        }

        private async Task<bool> ConnectToWiFiAsync(string ssid, string password)
        {
            try
            {
                _loggingService?.LogInfo(LogCategory.TCP, $"开始连接WiFi: {ssid}");
                
                var wifi = new Wifi();
                var accessPoints = await Task.Run(() => wifi.GetAccessPoints());
                
                var targetAP = accessPoints.FirstOrDefault(ap => ap.Name == ssid);
                if (targetAP == null)
                {
                    _loggingService?.LogError(LogCategory.TCP, $"未找到WiFi网络: {ssid}");
                    return false;
                }
        
                var authRequest = new AuthRequest(targetAP)
                {
                    Password = password
                };
        
                bool connected = await Task.Run(() => targetAP.Connect(authRequest));
                
                if (connected)
                {
                    _loggingService?.LogInfo(LogCategory.TCP, $"成功连接到WiFi: {ssid}");
                    return true;
                }
                else
                {
                    _loggingService?.LogError(LogCategory.TCP, $"连接WiFi失败: {ssid}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService?.LogError(LogCategory.TCP, $"WiFi连接异常: {ex.Message}");
                return false;
            }
        }
    }
}