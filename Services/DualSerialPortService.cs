using System;
using System.IO.Ports;
using System.Threading.Tasks;
using IMUTestApp.Models;
using IMUTestApp.Services;

namespace IMUTestApp.Services
{
    public class DualSerialPortService : IDisposable
    {
        private readonly LoggingService _logger;
        private SerialPort? _wheelMotorPort;
        private SerialPort? _imuPort; 
        private bool _disposed = false;
        
        public event EventHandler<IMUData>? IMUDataReceived;
        public event EventHandler<string>? WheelMotorDataReceived;
        
        // 添加缺少的属性
        public bool IsWheelMotorConnected => _wheelMotorPort?.IsOpen ?? false;
        public bool IsIMUConnected => _imuPort?.IsOpen ?? false;
        
        public DualSerialPortService(LoggingService logger)
        {
            _logger = logger;
        }
        
        //连接波轮电机
        public bool ConnectWheelMotor(SerialPortConfig config)
        {
            try
            {
                _wheelMotorPort = new SerialPort(config.PortName, config.BaudRate);
                _wheelMotorPort.Open();
                _disposed = false;
                if(!_wheelMotorPort.IsOpen)
                {
                    throw new Exception("Open Failed");
                }
                _logger.LogInfo(LogCategory.SerialPort, $"轮子电机串口连接成功: {config.PortName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.SerialPort, $"轮子电机串口连接失败: {ex.Message}");
                return false;
            }
        }
        
        // 🔥 新增：专门用于设备信息响应的事件
        public event EventHandler<string>? DeviceInfoReceived;
        
        // 连接IMU
        public bool ConnectIMU(SerialPortConfig config)
        {
            try
            {
                _imuPort = new SerialPort(config.PortName, config.BaudRate);
                _imuPort.DataReceived += OnIMUPortDataReceived;  // 添加这行
                _imuPort.Open();
                _disposed = false;
                
                _logger.LogInfo(LogCategory.SerialPort, $"IMU串口连接成功: {config.PortName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.SerialPort, $"IMU串口连接失败: {ex.Message}");
                return false;
            }
        }
        
        // 🔥 新增：处理IMU串口数据接收
        private void OnIMUPortDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                var port = sender as SerialPort;
                if (port != null && port.IsOpen)
                {
                    string data = port.ReadExisting();
                    if (!string.IsNullOrEmpty(data))
                    {
                        _logger.LogDebug(LogCategory.SerialPort, $"IMU串口接收到数据: {data}");
                        
                        // 判断是DeviceInfo响应
                        if (data.Contains("DevInfo") || data.Contains("product") || data.Contains("fw_ver"))
                        {
                            DeviceInfoReceived?.Invoke(this, data);
                        }
                        // 其他IMU数据可以触发IMUDataReceived事件
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.SerialPort, $"读取IMU数据失败: {ex.Message}");
            }
        }
        
        // 开启和关闭电机
        public async Task SendToWheelMotorAsync(string data)
        {
            if (_wheelMotorPort?.IsOpen == true)
            {
                await Task.Run(() => _wheelMotorPort.Write(data));
                _logger.LogDebug(LogCategory.SerialPort, $"发送到轮子电机: {data}");
            }
            else
            {
                _logger.LogWarn(LogCategory.SerialPort, "轮子电机串口未连接"); // 修复：LogWarning -> LogWarn
            }
        }
        
        //发送指令获取imu数据
        public async Task SendToIMUAsync(string data)
        {
            if (_imuPort?.IsOpen == true)
            {
                await Task.Run(() => _imuPort.Write(data));
                _logger.LogDebug(LogCategory.SerialPort, $"发送到IMU: {data}");
            }
            else
            {
                _logger.LogWarn(LogCategory.SerialPort, "IMU串口未连接"); // 修复：LogWarning -> LogWarn
            }
        }
        
        // 新增：安全关闭轮子电机连接
        public async Task DisconnectWheelMotorSafelyAsync()
        {
            try
            {
                if (_wheelMotorPort?.IsOpen == true)
                {
                    // 发送停止指令
                    await SendToWheelMotorAsync("{\"cmd\":\"stop\"}");
                    await Task.Delay(500); // 等待电机停止
                    
                    _wheelMotorPort.Close();
                    _logger.LogInfo(LogCategory.SerialPort, "轮子电机串口已安全关闭");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.SerialPort, $"安全关闭轮子电机串口失败: {ex.Message}");
            }
        }
        
        // 新增：安全关闭IMU连接
        public void DisconnectIMUSafely()
        {
            try
            {
                if (_imuPort?.IsOpen == true)
                {
                    _imuPort.Close();
                    _logger.LogInfo(LogCategory.SerialPort, "IMU串口已安全关闭");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.SerialPort, $"安全关闭IMU串口失败: {ex.Message}");
            }
        }
        
        // 改进现有的 Dispose 方法
        public void Dispose()
        {
            if (!_disposed)
            {
                try
                {
                    // 尝试安全关闭
                    DisconnectIMUSafely();
                    
                    // 对于轮子电机，如果可能的话发送停止指令
                    if (_wheelMotorPort?.IsOpen == true)
                    {
                        try
                        {
                            _wheelMotorPort.Write("{\"fan pwm 0\"}");
                            System.Threading.Thread.Sleep(200); // 简单等待
                        }
                        catch { /* 忽略发送停止指令的异常 */ }
                        
                        _wheelMotorPort.Close();
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(LogCategory.SerialPort, $"Dispose过程中发生异常: {ex.Message}");
                }
                finally
                {
                    _wheelMotorPort?.Dispose();
                    _imuPort?.Dispose();
                    _disposed = true;
                }
            }
        }
    }
}