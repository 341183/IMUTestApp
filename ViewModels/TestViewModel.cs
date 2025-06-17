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
        private TaskCompletionSource<string>? _deviceInfoTaskSource = null;
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
        
        // 添加清理标志
        private bool _isCleanupInProgress = false;                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             

        // 🔥 新增：将图表数据作为 ViewModel 属性保存
        public ObservableCollection<DataPoint> ChartDataPoints { get; }
        public ObservableCollection<DataPoint> UpperLimitPoints { get; }
        public ObservableCollection<DataPoint> LowerLimitPoints { get; }
        public ObservableCollection<DataPoint> BaselinePoints { get; }
        
        // 添加多个数据系列
        public ObservableCollection<DataPoint> RollDataPoints { get; }
        public ObservableCollection<DataPoint> PitchDataPoints { get; }
        public ObservableCollection<DataPoint> YawDataPoints { get; }
        public ObservableCollection<DataPoint> GyroXDataPoints { get; }
        public ObservableCollection<DataPoint> GyroYDataPoints { get; }
        public ObservableCollection<DataPoint> GyroZDataPoints { get; }
        
        // 如果不需要，直接删除这个字段
        // 或者在构造函数中使用它
        public TestViewModel(DualSerialPortService dualSerialPortService, ConfigService configService, LoggingService loggingService)
        {
            _dualSerialPortService = dualSerialPortService;
            _configService = configService;
            _loggingService = loggingService;

            _retryService = new RetryConnectionService(loggingService);
            
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _timer.Tick += OnTimerTick;
            
            TestData = new ObservableCollection<IMUData>();
            
            // 初始化所有数据系列
            RollDataPoints = new ObservableCollection<DataPoint>();
            PitchDataPoints = new ObservableCollection<DataPoint>();
            YawDataPoints = new ObservableCollection<DataPoint>();
            GyroXDataPoints = new ObservableCollection<DataPoint>();
            GyroYDataPoints = new ObservableCollection<DataPoint>();
            GyroZDataPoints = new ObservableCollection<DataPoint>();
            
            // 保留原有的其他集合
            ChartDataPoints = new ObservableCollection<DataPoint>();
            UpperLimitPoints = new ObservableCollection<DataPoint>();
            LowerLimitPoints = new ObservableCollection<DataPoint>();
            BaselinePoints = new ObservableCollection<DataPoint>();
            
            InitializeFixedLines();
            ClearProductCodeCommand = new RelayCommand(ClearProductCode);
            StopTestCommand = new RelayCommand(async () => await StopTestAsync(), () => IsTestRunning);
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
                var plotModel = new PlotModel
                {
                    Title = "IMU传感器数据实时监控",
                    Background = OxyColors.White
                };
                
                // 配置坐标轴
                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Title = "数据点",
                    Minimum = 0,
                    Maximum = 30,
                    MajorStep = 5,
                    MinorStep = 1
                });
                
                plotModel.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Title = "数值",
                    MajorGridlineStyle = LineStyle.Solid,
                    MinorGridlineStyle = LineStyle.Dot
                });
                
                // 🔥 新增：添加偏差范围带
                if (_tcpTestData != null && _tcpTestData.Count > 0)
                {
                    AddDeviationBands(plotModel);
                }
                
                // 添加Roll数据系列
                var rollSeries = new LineSeries
                {
                    Title = "Roll",
                    Color = OxyColors.Red,
                    StrokeThickness = 2
                };
                foreach (var point in RollDataPoints ?? new ObservableCollection<DataPoint>())
                {
                    rollSeries.Points.Add(point);
                }
                plotModel.Series.Add(rollSeries);
                
                // 添加Pitch数据系列
                var pitchSeries = new LineSeries
                {
                    Title = "Pitch",
                    Color = OxyColors.Green,
                    StrokeThickness = 2
                };
                foreach (var point in PitchDataPoints ?? new ObservableCollection<DataPoint>())
                {
                    pitchSeries.Points.Add(point);
                }
                plotModel.Series.Add(pitchSeries);
                
                // 添加Yaw数据系列
                var yawSeries = new LineSeries
                {
                    Title = "Yaw",
                    Color = OxyColors.Blue,
                    StrokeThickness = 2
                };
                foreach (var point in YawDataPoints ?? new ObservableCollection<DataPoint>())
                {
                    yawSeries.Points.Add(point);
                }
                plotModel.Series.Add(yawSeries);
                
                // 添加GyroX数据系列
                var gyroXSeries = new LineSeries
                {
                    Title = "GyroX",
                    Color = OxyColors.Orange,
                    StrokeThickness = 1,
                    LineStyle = LineStyle.Dash
                };
                foreach (var point in GyroXDataPoints ?? new ObservableCollection<DataPoint>())
                {
                    gyroXSeries.Points.Add(point);
                }
                plotModel.Series.Add(gyroXSeries);
                
                // 添加GyroY数据系列
                var gyroYSeries = new LineSeries
                {
                    Title = "GyroY",
                    Color = OxyColors.Purple,
                    StrokeThickness = 1,
                    LineStyle = LineStyle.Dash
                };
                foreach (var point in GyroYDataPoints ?? new ObservableCollection<DataPoint>())
                {
                    gyroYSeries.Points.Add(point);
                }
                plotModel.Series.Add(gyroYSeries);
                
                // 添加GyroZ数据系列
                var gyroZSeries = new LineSeries
                {
                    Title = "GyroZ",
                    Color = OxyColors.Brown,
                    StrokeThickness = 1,
                    LineStyle = LineStyle.Dash
                };
                foreach (var point in GyroZDataPoints ?? new ObservableCollection<DataPoint>())
                {
                    gyroZSeries.Points.Add(point);
                }
                plotModel.Series.Add(gyroZSeries);
                
                // 修复图例设置 - 使用正确的属性
                plotModel.IsLegendVisible = true;
                
                return plotModel;
            }
        }
        
        // 🔥 新增：添加偏差范围带的方法
        private void AddDeviationBands(PlotModel plotModel)
        {
            if (_tcpTestData == null || _tcpTestData.Count == 0) return;
            
            // 计算各参数的偏差值
            var gyroXValues = _tcpTestData.Select(d => d.GyroX).ToList();
            var gyroYValues = _tcpTestData.Select(d => d.GyroY).ToList();
            var pitchValues = _tcpTestData.Select(d => d.Pitch).ToList();
            var rollValues = _tcpTestData.Select(d => d.Roll).ToList();
            
            var gyroXMin = gyroXValues.Min();
            var gyroXMax = gyroXValues.Max();
            var gyroYMin = gyroYValues.Min();
            var gyroYMax = gyroYValues.Max();
            var pitchMin = pitchValues.Min();
            var pitchMax = pitchValues.Max();
            var rollMin = rollValues.Min();
            var rollMax = rollValues.Max();
            
            // 添加Roll偏差范围带
            var rollDeviationArea = new AreaSeries
            {
                Title = "Roll偏差范围",
                Color = OxyColor.FromArgb(50, 255, 0, 0), // 半透明红色
                Fill = OxyColor.FromArgb(30, 255, 0, 0),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dot
            };
            
            // 为整个数据范围添加偏差带
            for (int i = 0; i <= 30; i++)
            {
                rollDeviationArea.Points.Add(new DataPoint(i, rollMax));
            }
            for (int i = 30; i >= 0; i--)
            {
                rollDeviationArea.Points2.Add(new DataPoint(i, rollMin));
            }
            plotModel.Series.Add(rollDeviationArea);
            
            // 添加Pitch偏差范围带
            var pitchDeviationArea = new AreaSeries
            {
                Title = "Pitch偏差范围",
                Color = OxyColor.FromArgb(50, 0, 255, 0), // 半透明绿色
                Fill = OxyColor.FromArgb(30, 0, 255, 0),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dot
            };
            
            for (int i = 0; i <= 30; i++)
            {
                pitchDeviationArea.Points.Add(new DataPoint(i, pitchMax));
            }
            for (int i = 30; i >= 0; i--)
            {
                pitchDeviationArea.Points2.Add(new DataPoint(i, pitchMin));
            }
            plotModel.Series.Add(pitchDeviationArea);
            
            // 添加GyroX偏差范围带
            var gyroXDeviationArea = new AreaSeries
            {
                Title = "GyroX偏差范围",
                Color = OxyColor.FromArgb(50, 255, 165, 0), // 半透明橙色
                Fill = OxyColor.FromArgb(30, 255, 165, 0),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dot
            };
            
            for (int i = 0; i <= 30; i++)
            {
                gyroXDeviationArea.Points.Add(new DataPoint(i, gyroXMax));
            }
            for (int i = 30; i >= 0; i--)
            {
                gyroXDeviationArea.Points2.Add(new DataPoint(i, gyroXMin));
            }
            plotModel.Series.Add(gyroXDeviationArea);
            
            // 添加GyroY偏差范围带
            var gyroYDeviationArea = new AreaSeries
            {
                Title = "GyroY偏差范围",
                Color = OxyColor.FromArgb(50, 128, 0, 128), // 半透明紫色
                Fill = OxyColor.FromArgb(30, 128, 0, 128),
                StrokeThickness = 1,
                LineStyle = LineStyle.Dot
            };
            
            for (int i = 0; i <= 30; i++)
            {
                gyroYDeviationArea.Points.Add(new DataPoint(i, gyroYMax));
            }
            for (int i = 30; i >= 0; i--)
            {
                gyroYDeviationArea.Points2.Add(new DataPoint(i, gyroYMin));
            }
            plotModel.Series.Add(gyroYDeviationArea);
        }
        
        //返回测试状态
        public bool IsTestRunning
        {
            get => _isTestRunning;
            set 
            {
                SetProperty(ref _isTestRunning, value);
                // 通知停止测试命令状态变更
                (StopTestCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
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
        public ICommand StopTestCommand { get; }
        
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
            ClearChartData();
        }
        
        DeviceInfo? deviceInfo;
        //开始测试
        private async void StartTest()
        {
            if (IsTestRunning) return;
            
            ProductCode = "";
            IsTestRunning = true;
            _testStartTime = DateTime.Now;
            TestDateTime = _testStartTime.ToString("yyyy-MM-dd HH:mm:ss");
            
            // 重置清理标志
            _isCleanupInProgress = false;
            
            _dualSerialPortService.DisconnectIMUSafely();
            await _dualSerialPortService.DisconnectWheelMotorSafelyAsync();
            TestData.Clear();
            PacketCount = 0;
            TestResult = string.Empty;
            TestResultDetails = "测试进行中...";
            
            // 清空图表数据
            ClearChartData();
            
            try
            {
                DataDisplay = "开始测试...\n";
                _loggingService.LogInfo(LogCategory.IMUData, $"开始测试 - 产品编码: {ProductCode}");
                
                // Step 1: 控制电机
                if (!await Step1_ControlMotor())
                {
                    await CleanupAfterFailure("电机控制失败");
                    return;
                }
                
                // Step 2: 获取设备信息
                deviceInfo = await Step2_GetDeviceInfo();
                if (deviceInfo == null)
                {
                    await CleanupAfterFailure("获取设备信息失败");
                    return;
                }
                
                // Step 3: 连接WiFi
                if (!await Step3_ConnectWiFi(deviceInfo.ApName))
                {
                    if(await ConnectToOpenWiFiUsingNetshAsync(deviceInfo.ApName) == false)
                    {
                        await CleanupAfterFailure("WiFi连接失败");
                        return;
                    }
                }
                
                // Step 4: TCP协议测试
                if (!await Step4_TcpProtocolTest())
                {
                    await CleanupAfterFailure("TCP测试失败");
                    return;
                }
                
                // 启动运行时间显示定时器
                _timer.Start();
            }
            catch (Exception ex)
            {
                _loggingService.LogError(LogCategory.IMUData, $"测试过程中发生异常: {ex.Message}");
                await CleanupAfterFailure($"测试异常: {ex.Message}");
            }
        }
        

        
        // 步骤1：控制波轮电机
        private async Task<bool> Step1_ControlMotor()
        {
            if (!IsTestRunning) return false; // 添加状态检查
            
            try
            {
                _loggingService.LogDebug(LogCategory.IMUData, "步骤1: 开始启动波轮电机");
                DataDisplay += "步骤1: 启动波轮电机...\n";
                
                bool connected = await _retryService.RetryConnectionAsync(
                config => _dualSerialPortService.ConnectWheelMotor(config),
                _configService.WheelMotorPort,
                "波轮电机串口",
                maxRetries: 30,
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
                    await _dualSerialPortService.SendToWheelMotorAsync("fan pwm 50\r\n");
                    return true;
                }
                else
                {
                    _loggingService.LogError(LogCategory.IMUData, "波轮电机串口未连接");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError(LogCategory.IMUData, $"电机控制失败: {ex.Message}");
                return false;
            }
        }
        
        // 步骤2：获取IMU设备信息
        private async Task<DeviceInfo> Step2_GetDeviceInfo()
        {
            DataDisplay += "\n步骤2: 获取IMU设备信息...\n";

            //防止上一步出现问题，重复向轮子发信息
            await _dualSerialPortService.SendToWheelMotorAsync("fan pwm 50\r\n");

            bool connected = await _retryService.RetryConnectionAsync(
            config => _dualSerialPortService.ConnectIMU(config),
            _configService.IMUPort,
            "IMU串口",
            maxRetries: 100,
            timeoutSeconds: 1500,
            progressCallback: message => DataDisplay += $"{message}\n"
            );

            DeviceInfo? response;

            if (connected && _dualSerialPortService?.IsIMUConnected == true)
            {
                // 发送设备信息请求
                var request = "{\"DevInfo\":{}}";
                await _dualSerialPortService.SendToIMUAsync(request + "\r\n");
                DataDisplay += $"已发送请求: {request}\n";
                
                // 等待响应（这里需要实现响应解析逻辑）
                 response = await WaitForDeviceInfoResponse();
                
                //解析结果不对
                if (response != null && response.ApName != "")
                {
                    DataDisplay += $"设备信息: 产品={response.Product}, 固件版本={response.FwVer}\n";
                    DataDisplay += $"热点名称: {response.ApName}\n";
                    
                    return response;
                }
                else
                {
                    _dualSerialPortService.DisconnectIMUSafely();
                     response = await Step2_GetDeviceInfo();
                     return response;
                }
            }
            else
            {
                response = await Step2_GetDeviceInfo();
                return response;
            }
        }
        
        // 步骤3：连接WiFi热点
        private async Task<bool> Step3_ConnectWiFi(string apName)
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
                    maxRetries: 100,
                    timeoutSeconds: 1500,
                    progressCallback: message => DataDisplay += $"{message}\n"
                );
                
                if (connected)
                {
                    DataDisplay += $"已成功连接到热点: {apName}\n";
                    return true;
                }
                else
                {
                    _loggingService.LogError(LogCategory.TCP, $"经过重试后仍无法连接到WiFi热点: {apName}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.LogError(LogCategory.TCP, $"WiFi连接失败: {ex.Message}");
                return false;
            }
        }
        
        // 步骤4：TCP协议测试
        private async Task<bool> Step4_TcpProtocolTest()
        {
            DataDisplay += "\n步骤4: 开始TCP协议测试...\n";
            
            // 获取TCP配置
            var config = _configService.Config;
            string ipAddress = config.TcpConfig.IpAddress;
            int port = config.TcpConfig.Port; 
            DataDisplay += $"IP地址: {ipAddress}, 端口: {port}\n";

            try
            {
                using (var tcpClient = new System.Net.Sockets.TcpClient())
                {
                    // 设置超时
                    tcpClient.ReceiveTimeout = 10000;
                    tcpClient.SendTimeout = 10000;
                    
                    // 使用重试服务进行连接
                bool connected = await _retryService.RetryConnectionAsync(
                    async config => 
                    {
                        try 
                        {
                            await tcpClient.ConnectAsync(config.IPAddress, config.Port);
                            return true;
                        }
                        catch
                        {
                            return false;
                        }
                    },
                    new { IPAddress = ipAddress, Port = port },
                    $"TCP连接 {ipAddress}:{port}",
                    maxRetries: 50,
                    timeoutSeconds: 80,
                    progressCallback: message => DataDisplay += $"{message}\n"
                );
                    
                    if (!connected)
                    {
                        throw new Exception("TCP连接失败");
                    }
                    
                    DataDisplay += $"TCP连接已建立: {ipAddress}:{port}\n";
                    
                    var stream = tcpClient.GetStream();
                    
                    // 发送空JSON请求获取IMU数据
                    string request = "{\"IMU\":{}}"+ "\r\n";
                    _loggingService.LogInfo(LogCategory.TCP, $"发送请求: {request}");
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
                        return true;
                    }
                    else
                    {
                        _loggingService.LogError(LogCategory.TCP, "无法解析IMU数据");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                DataDisplay += $"TCP连接失败: {ex.Message}\n";
                _loggingService.LogError(LogCategory.TCP, $"TCP测试失败: {ex.Message}");
                return false;
            }
        }
        
        // 执行连续IMU测试
        private async Task ContinuousIMUDataCollection(System.Net.Sockets.NetworkStream stream)
        {
            DataDisplay += "\n开始连续IMU测试（30次数据采集）...\n";
            _tcpTestData.Clear();
            
            // 清空所有图表数据
            Application.Current.Dispatcher.Invoke(() =>
            {
                ClearChartData();
            });
            
            // 设置循环30次
            int totalRequests = 30;
            string request = "{\"IMU\":{}}";
            byte[] requestData = System.Text.Encoding.UTF8.GetBytes(request);
            
            for (int i = 0; i < totalRequests && IsTestRunning; i++)
            {
                try
                {
                    DataDisplay += $"发送第 {i + 1} 次请求...\n";
                    
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
                        DataDisplay += $"第 {i + 1} 次 - IMU数据 - Roll: {imuData.Roll:F2}, Pitch: {imuData.Pitch:F2}, " +
                                     $"Yaw: {imuData.Yaw:F2}, GyroX: {imuData.GyroX:F2}, GyroY: {imuData.GyroY:F2}\n";
                        
                        // 每获取到一个数据就更新图表
                        UpdateChart(imuData);
                    }
                    else
                    {
                        DataDisplay += $"第 {i + 1} 次请求未获取到有效数据\n";
                    }
                    
                    // 等待一段时间再发送下一个请求
                    await Task.Delay(1000);
                }
                catch (Exception ex)
                {
                    DataDisplay += $"第 {i + 1} 次请求时出错: {ex.Message}\n";
                    break;
                }
            }
            
            DataDisplay += $"\n完成 {_tcpTestData.Count} 次有效数据采集\n";
            
            // 分析测试结果
            AnalyzeIMUTestResults();

            if (IsTestRunning)
            {
                DataDisplay += "\n测试完成，正在停止...\n";
                await StopTestAsync();
            }
        }
        
        // 添加更新图表的方法
        private void UpdateChart(IMUData imuData)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    int dataPointIndex = _tcpTestData.Count;
                    
                    // 添加所有数据系列的数据点
                    RollDataPoints.Add(new DataPoint(dataPointIndex, imuData.Roll));
                    PitchDataPoints.Add(new DataPoint(dataPointIndex, imuData.Pitch));
                    YawDataPoints.Add(new DataPoint(dataPointIndex, imuData.Yaw));
                    GyroXDataPoints.Add(new DataPoint(dataPointIndex, imuData.GyroX));
                    GyroYDataPoints.Add(new DataPoint(dataPointIndex, imuData.GyroY));
                    GyroZDataPoints.Add(new DataPoint(dataPointIndex, imuData.GyroZ));
                    
                    // 限制数据点数量（保持最近30个点）
                    const int maxPoints = 30;
                    if (RollDataPoints.Count > maxPoints)
                    {
                        RollDataPoints.RemoveAt(0);
                        PitchDataPoints.RemoveAt(0);
                        YawDataPoints.RemoveAt(0);
                        GyroXDataPoints.RemoveAt(0);
                        GyroYDataPoints.RemoveAt(0);
                        GyroZDataPoints.RemoveAt(0);
                    }
                    
                    // 通知PlotModel属性更新
                    OnPropertyChanged(nameof(PlotModel));
                    
                    _loggingService?.LogInfo(LogCategory.TCP, $"图表已更新 - 数据点: {dataPointIndex}");
                }
                catch (Exception ex)
                {
                    _loggingService?.LogError(LogCategory.TCP, $"更新图表时出错: {ex.Message}");
                }
            });
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
            if (_tcpTestData.Count < 30)
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
                SaveData();
            }
            else
            {
                TestResult = "NG";
                TestResultDetails = $"以下参数超出范围: {string.Join(", ", failedParams)}";
                DataDisplay += $"\n测试结果: NG - {TestResultDetails}\n";
            }
        }
        
        // 新增：测试失败后的清理方法
        private async Task CleanupAfterFailure(string reason)
        {
            if (_isCleanupInProgress) return;
            _isCleanupInProgress = true;
            
            try
            {
                _loggingService.LogWarn(LogCategory.IMUData, $"开始清理资源 - 原因: {reason}");
                
                // 停止测试
                IsTestRunning = false;
                _timer?.Stop();
                
                // 关闭电机
                try
                {
                    if (_dualSerialPortService?.IsWheelMotorConnected == true)
                    {
                        await _dualSerialPortService.SendToWheelMotorAsync("fan pwm 0\r\n");
                        await Task.Delay(500); // 等待电机停止
                        _loggingService.LogInfo(LogCategory.SerialPort, "电机已停止");
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError(LogCategory.SerialPort, $"停止电机失败: {ex.Message}");
                }
                
                // 断开串口连接
                try
                {
                    _dualSerialPortService?.Dispose();
                    _loggingService.LogInfo(LogCategory.SerialPort, "串口连接已断开");
                }
                catch (Exception ex)
                {
                    _loggingService.LogError(LogCategory.SerialPort, $"断开串口连接失败: {ex.Message}");
                }
                
                // 更新UI显示
                DataDisplay += $"\n\n测试失败: {reason}\n清理完成";
                TestResult = "NG";
                TestResultDetails = reason;
                
                _loggingService.LogInfo(LogCategory.IMUData, "资源清理完成");
            }
            catch (Exception ex)
            {
                _loggingService.LogError(LogCategory.IMUData, $"清理过程中发生异常: {ex.Message}");
            }
            finally
            {
                _isCleanupInProgress = false;
            }
        }
        
        // 改进现有的 StopTestAsync 方法
        public async Task StopTestAsync()
        {
            deviceInfo = null;
            wifi = null;
            if (!IsTestRunning && !_isCleanupInProgress) return;
            
            _loggingService.LogInfo(LogCategory.IMUData, "停止测试");
            
            IsTestRunning = false;
            _timer?.Stop();
            
            try
            {
                // 发送停止电机指令
                if (_dualSerialPortService?.IsWheelMotorConnected == true)
                {
                    await _dualSerialPortService.SendToWheelMotorAsync("fan pwm 0\r\n");
                    await Task.Delay(500);
                    _loggingService.LogInfo(LogCategory.IMUData, "波轮电机已关闭");
                    DataDisplay += "\n波轮电机已关闭\n";
                }
                
                // 断开串口连接
                try
                {
                    if (_dualSerialPortService != null)
                    {
                        _dualSerialPortService.Dispose();
                        _loggingService.LogInfo(LogCategory.SerialPort, "串口连接已断开");
                        DataDisplay += "串口连接已断开\n";
                    }
                }
                catch (Exception ex)
                {
                    _loggingService.LogError(LogCategory.SerialPort, $"断开串口连接失败: {ex.Message}");
                }
                
                // 清理测试数据
                _tcpTestData.Clear();
                
                // 更新UI显示
                DataDisplay += "\n测试已停止，所有资源已释放\n";
                TestResult = string.Empty;
                TestResultDetails = "测试停止";
                
                _loggingService.LogInfo(LogCategory.IMUData, "测试停止完成，资源已释放");
     
            }
            catch (Exception ex)
            {
                _loggingService.LogError(LogCategory.IMUData, $"停止测试时发生异常: {ex.Message}");
            }
        }
        
        // 新增：程序关闭时的清理方法
        public async Task CleanupOnApplicationExit()
        {
            try
            {
                _loggingService?.LogInfo(LogCategory.IMUData, "程序关闭，开始清理资源");
                
                // 如果测试正在运行，先停止测试
                if (IsTestRunning)
                {
                    await StopTestAsync();
                }
                
                // 确保电机停止
                if (_dualSerialPortService?.IsWheelMotorConnected == true)
                {
                    await _dualSerialPortService.SendToWheelMotorAsync("fan pwm 0\r\n");
                    await Task.Delay(1000); // 给电机足够时间停止
                }
                
                // 释放串口资源
                _dualSerialPortService?.Dispose();
                
                _loggingService?.LogInfo(LogCategory.IMUData, "程序关闭清理完成");
            }
            catch (Exception ex)
            {
                _loggingService?.LogError(LogCategory.IMUData, $"程序关闭清理异常: {ex.Message}");
            }
        }
        
        private void SaveData()
        {
            try
            {
                _loggingService.LogDebug(LogCategory.FileIO, $"开始保存测试数据 - 产品编码: {ProductCode}");
                
                var dataPath = _configService.Config.GeneralSettings.DataPath;
                if (!Directory.Exists(dataPath))
                {
                    Directory.CreateDirectory(dataPath);
                }
                
                // 生成CSV文件名，包含产品编码和时间戳
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var csvFileName = $"{ProductCode}_{timestamp}.csv";
                var csvFilePath = Path.Combine(dataPath, csvFileName);
                
                var csvContent = new StringBuilder();
                
                // 添加CSV头部信息
                csvContent.AppendLine("# IMU测试数据报告");
                csvContent.AppendLine($"# 产品编码: {ProductCode}");
                csvContent.AppendLine($"# 测试时间: {TestDateTime}");
                csvContent.AppendLine($"# 测试结果: {TestResult}");
                csvContent.AppendLine($"# 测试详情: {TestResultDetails}");
                csvContent.AppendLine($"# 设备信息: {deviceInfo}");
                csvContent.AppendLine($"# 连接信息: {wifi}");
                csvContent.AppendLine("#");
                
                // 添加CSV列标题
                csvContent.AppendLine("Timestamp,AccelX,AccelY,AccelZ,GyroX,GyroY,GyroZ,MagX,MagY,MagZ,Roll,Pitch,Yaw");
                
                // 添加TCP测试数据
                if (_tcpTestData != null && _tcpTestData.Count > 0)
                {
                    foreach (var data in _tcpTestData)
                    {
                        csvContent.AppendLine($"{data.Timestamp:yyyy-MM-dd HH:mm:ss.fff}," +
                                            $"{data.AccelX:F6},{data.AccelY:F6},{data.AccelZ:F6}," +
                                            $"{data.GyroX:F6},{data.GyroY:F6},{data.GyroZ:F6}," +
                                            $"{data.MagX:F6},{data.MagY:F6},{data.MagZ:F6}," +
                                            $"{data.Roll:F6},{data.Pitch:F6},{data.Yaw:F6}");
                    }
                }
                else
                {
                    csvContent.AppendLine("# 无TCP测试数据");
                }
                
                // 保存CSV文件
                File.WriteAllText(csvFilePath, csvContent.ToString(), Encoding.UTF8);
                
                // 同时保存一个包含详细日志的文本文件
                var logFileName = $"{ProductCode}_{timestamp}_log.txt";
                var logFilePath = Path.Combine(dataPath, logFileName);
                
                var logContent = new StringBuilder();
                logContent.AppendLine($"产品编码: {ProductCode}");
                logContent.AppendLine($"测试时间: {TestDateTime}");
                logContent.AppendLine($"测试结果: {TestResult}");
                logContent.AppendLine($"测试详情: {TestResultDetails}");
                logContent.AppendLine($"数据包总数: {PacketCount}");
                logContent.AppendLine($"采样率: {SampleRate}");
                logContent.AppendLine($"运行时间: {RunTime}");
                logContent.AppendLine();
                logContent.AppendLine("详细测试日志:");
                logContent.AppendLine(DataDisplay);
                
                File.WriteAllText(logFilePath, logContent.ToString(), Encoding.UTF8);
                
                DataDisplay += $"\n\nCSV数据已保存到: {csvFilePath}";
                DataDisplay += $"\n详细日志已保存到: {logFilePath}";
                
                _loggingService.LogInfo(LogCategory.FileIO, $"数据保存完成 - CSV: {csvFilePath}, 日志: {logFilePath}");
            }
            catch (Exception ex)
            {
                var errorMsg = $"保存失败: {ex.Message}";
                DataDisplay += $"\n\n{errorMsg}";
                _loggingService.LogError(LogCategory.FileIO, errorMsg);
            }
        }
        
        // 🔥 新增：清除图表数据的方法
        public void ClearChartData()
        {
            ChartDataPoints?.Clear();
            RollDataPoints?.Clear();
            PitchDataPoints?.Clear();
            YawDataPoints?.Clear();
            GyroXDataPoints?.Clear();
            GyroYDataPoints?.Clear();
            GyroZDataPoints?.Clear();
            OnPropertyChanged(nameof(PlotModel));
        }
        
        private void OnTimerTick(object? sender, EventArgs e)
        {
            if (IsTestRunning)
            {
                var elapsed = DateTime.Now - _testStartTime;
                RunTime = elapsed.ToString(@"hh\:mm\:ss");
            }
        }
        
        
        private async Task<DeviceInfo?> WaitForDeviceInfoResponse()
        {
            try
            {
                _deviceInfoTaskSource = new TaskCompletionSource<string>();
                _dualSerialPortService.DeviceInfoReceived += OnDeviceInfoReceived;
                
                _loggingService?.LogInfo(LogCategory.SerialPort, "等待设备信息响应...");
                
                var timeout = TimeSpan.FromSeconds(10);
                using var cts = new System.Threading.CancellationTokenSource(timeout);
                
                var responseData = await _deviceInfoTaskSource.Task.WaitAsync(cts.Token);
                
                var deviceInfoResponse = System.Text.Json.JsonSerializer.Deserialize<DeviceInfoResponse>(responseData);
                return deviceInfoResponse?.Result == 0 ? deviceInfoResponse.DevInfo : null;
            }
            catch (OperationCanceledException)
            {
                _loggingService?.LogError(LogCategory.SerialPort, "等待设备信息响应超时");
                return null;
            }
            finally
            {
                _dualSerialPortService.DeviceInfoReceived -= OnDeviceInfoReceived;
                _deviceInfoTaskSource = null;
            }
        }


        private void OnDeviceInfoReceived(object? sender, string data)
        {
            _loggingService?.LogInfo(LogCategory.SerialPort, "收到设备信息响应");
            _deviceInfoTaskSource?.TrySetResult(data);
        }

        Wifi? wifi;
        private async Task<bool> ConnectToWiFiAsync(string ssid, string password = null)
        {
            try
            {
                _loggingService?.LogInfo(LogCategory.TCP, $"开始连接WiFi: {ssid}");
                
                wifi = new Wifi();
                var accessPoints = await Task.Run(() => wifi.GetAccessPoints());
                
                if (accessPoints == null || !accessPoints.Any())
                {
                    _loggingService?.LogError(LogCategory.TCP, "未找到任何WiFi接入点");
                    return false;
                }
                
                var targetAP = accessPoints.FirstOrDefault(ap => ap.Name == ssid);
                if (targetAP == null)
                {
                    _loggingService?.LogError(LogCategory.TCP, $"未找到WiFi网络: {ssid}");
                    _loggingService?.LogInfo(LogCategory.TCP, $"可用网络: {string.Join(", ", accessPoints.Select(ap => ap.Name))}");
                    return false;
                }
                
                _loggingService?.LogInfo(LogCategory.TCP, $"找到目标网络: {targetAP.Name}, 信号强度: {targetAP.SignalStrength}, 是否需要密码: {targetAP.IsSecure}");

                var authRequest = new AuthRequest(targetAP);

                // 检查是否为开放网络
                if (!targetAP.IsSecure)
                {
                    // 开放网络，直接连接
                    bool connected = await Task.Run(() => targetAP.Connect(authRequest));
                    
                    if (connected)
                    {
                        _loggingService?.LogInfo(LogCategory.TCP, $"成功连接到开放WiFi: {ssid}");
                        return true;
                    }
                    else
                    {
                        _loggingService?.LogError(LogCategory.TCP, $"连接开放WiFi失败: {ssid}");
                        return false;
                    }
                }
                else
                {
                    // 加密网络，需要密码
                    if (string.IsNullOrEmpty(password))
                    {
                        _loggingService?.LogError(LogCategory.TCP, $"WiFi网络 {ssid} 需要密码但未提供");
                        return false;
                    }
                    
                    
                    authRequest.Password = password;
                    
                    bool connected = await Task.Run(() => targetAP.Connect(authRequest, true));
                    
                    if (connected)
                    {
                        _loggingService?.LogInfo(LogCategory.TCP, $"成功连接到加密WiFi: {ssid}");
                        return true;
                    }
                    else
                    {
                        _loggingService?.LogError(LogCategory.TCP, $"连接加密WiFi失败: {ssid}");
                        return false;
                    }
                }
            }
            catch (ArgumentNullException ex)
            {
                _loggingService?.LogError(LogCategory.TCP, $"WiFi连接参数异常 (SimpleWifi库问题): {ex.Message}");
                _loggingService?.LogError(LogCategory.TCP, "尝试使用netsh替代方案");
                return await ConnectToOpenWiFiUsingNetshAsync(ssid);
            }
            catch (UnauthorizedAccessException ex)
            {
                _loggingService?.LogError(LogCategory.TCP, $"WiFi连接权限不足: {ex.Message}");
                _loggingService?.LogError(LogCategory.TCP, "请以管理员身份运行程序");
                return false;
            }
            catch (Exception ex)
            {
                _loggingService?.LogError(LogCategory.TCP, $"WiFi连接异常: {ex.Message}");
                _loggingService?.LogError(LogCategory.TCP, $"异常类型: {ex.GetType().Name}");
                if (ex.InnerException != null)
                {
                    _loggingService?.LogError(LogCategory.TCP, $"内部异常: {ex.InnerException.Message}");
                }
                return false;
            }
        }
        
        private async Task<bool> ConnectToOpenWiFiUsingNetshAsync(string ssid)
        {
            try
            {
                _loggingService?.LogInfo(LogCategory.TCP, $"使用netsh连接开放WiFi: {ssid}");
                
                // 转义SSID中的特殊字符
                var escapedSsid = EscapeXmlString(ssid);
                
                // 开放WiFi的配置文件XML
                var profileXml = $@"<?xml version=""1.0""?>
<WLANProfile xmlns=""http://www.microsoft.com/networking/WLAN/profile/v1"">
    <name>{escapedSsid}</name>
    <SSIDConfig>
        <SSID>
            <name>{escapedSsid}</name>
        </SSID>
    </SSIDConfig>
    <connectionType>ESS</connectionType>
    <connectionMode>auto</connectionMode>
    <MSM>
        <security>
            <authEncryption>
                <authentication>open</authentication>
                <encryption>none</encryption>
                <useOneX>false</useOneX>
            </authEncryption>
        </security>
    </MSM>
</WLANProfile>";
                
                var tempFile = Path.GetTempFileName();
                _loggingService?.LogInfo(LogCategory.TCP, $"创建临时配置文件: {tempFile}");
                
                await File.WriteAllTextAsync(tempFile, profileXml);
                
                // 记录XML内容用于调试
                _loggingService?.LogInfo(LogCategory.TCP, $"WiFi配置文件内容:\n{profileXml}");
                
                try
                {
                    // 检查WiFi服务状态
                    var serviceResult = await RunNetshCommandAsync("wlan show profiles");
                    if (!serviceResult.Success)
                    {
                        _loggingService?.LogError(LogCategory.TCP, $"WiFi服务可能未启动: {serviceResult.Output}");
                        return false;
                    }
                    
                    // 删除可能存在的同名配置文件
                    var deleteResult = await RunNetshCommandAsync($"wlan delete profile name=\"{escapedSsid}\"");
                    _loggingService?.LogInfo(LogCategory.TCP, $"删除旧配置文件结果: {deleteResult.Output}");
                    
                    // 添加新的配置文件
                    var addResult = await RunNetshCommandAsync($"wlan add profile filename=\"{tempFile}\"");
                    _loggingService?.LogInfo(LogCategory.TCP, $"添加配置文件完整输出: {addResult.Output}");
                    
                    if (!addResult.Success)
                    {
                        _loggingService?.LogError(LogCategory.TCP, $"添加WiFi配置文件失败: {addResult.Output}");
                        
                        // 尝试不同的添加方式
                        var addResult2 = await RunNetshCommandAsync($"wlan add profile filename=\"{tempFile}\" user=all");
                        if (!addResult2.Success)
                        {
                            _loggingService?.LogError(LogCategory.TCP, $"使用user=all参数也失败: {addResult2.Output}");
                            return false;
                        }
                        else
                        {
                            _loggingService?.LogInfo(LogCategory.TCP, "使用user=all参数成功添加配置文件");
                        }
                    }
                    
                    // 连接到网络
                    var connectResult = await RunNetshCommandAsync($"wlan connect name=\"{escapedSsid}\"");
                    _loggingService?.LogInfo(LogCategory.TCP, $"连接命令输出: {connectResult.Output}");
                    
                    if (connectResult.Success || connectResult.Output.Contains("连接请求已成功完成") || connectResult.Output.Contains("Connection request was completed successfully"))
                    {
                        _loggingService?.LogInfo(LogCategory.TCP, $"成功连接到开放WiFi: {ssid}");
                        
                        // 等待连接建立
                        await Task.Delay(3000);
                        
                        // 验证连接状态
                        var statusResult = await RunNetshCommandAsync("wlan show interfaces");
                        _loggingService?.LogInfo(LogCategory.TCP, $"连接状态: {statusResult.Output}");
                        
                        return statusResult.Output.Contains(escapedSsid) || statusResult.Output.Contains("已连接") || statusResult.Output.Contains("connected");
                    }
                    else
                    {
                        _loggingService?.LogError(LogCategory.TCP, $"连接WiFi失败: {connectResult.Output}");
                        return false;
                    }
                }
                finally
                {
                    if (File.Exists(tempFile))
                    {
                        File.Delete(tempFile);
                        _loggingService?.LogInfo(LogCategory.TCP, "已删除临时配置文件");
                    }
                }
            }
            catch (Exception ex)
            {
                _loggingService?.LogError(LogCategory.TCP, $"netsh开放WiFi连接异常: {ex.Message}");
                _loggingService?.LogError(LogCategory.TCP, $"异常堆栈: {ex.StackTrace}");
                return false;
            }
        }
        
        private string EscapeXmlString(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;
                
            return input
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
        
        private async Task<(bool Success, string Output)> RunNetshCommandAsync(string arguments)
        {
            try
            {
                _loggingService?.LogInfo(LogCategory.TCP, $"执行netsh命令: netsh {arguments}");
                
                // 创建批处理命令，设置代码页为UTF-8
                var batchCommand = $"chcp 65001 >nul && netsh {arguments}";
                
                var startInfo = new ProcessStartInfo
                {
                    FileName = "cmd",
                    Arguments = $"/c {batchCommand}",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8,
                    StandardErrorEncoding = Encoding.UTF8
                };
                
                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return (false, "无法启动cmd进程");
                }
                
                var output = await process.StandardOutput.ReadToEndAsync();
                var error = await process.StandardError.ReadToEndAsync();
                await process.WaitForExitAsync();
                
                var success = process.ExitCode == 0;
                var result = success ? output : $"ExitCode: {process.ExitCode}, Output: {output}, Error: {error}";
                
                _loggingService?.LogInfo(LogCategory.TCP, $"netsh命令结果 - 成功: {success}, 输出: {result}");
                
                return (success, result);
            }
            catch (Exception ex)
            {
                var errorMsg = $"执行netsh命令异常: {ex.Message}";
                _loggingService?.LogError(LogCategory.TCP, errorMsg);
                return (false, errorMsg);
            }
        }
    }
}