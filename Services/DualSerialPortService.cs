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
                _logger.LogInfo(LogCategory.SerialPort, $"轮子电机串口连接成功: {config.PortName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.SerialPort, $"轮子电机串口连接失败: {ex.Message}");
                return false;
            }
        }
        
        public bool ConnectIMU(SerialPortConfig config)
        {
            try
            {
                _imuPort = new SerialPort(config.PortName, config.BaudRate);
                _imuPort.Open();
                _logger.LogInfo(LogCategory.SerialPort, $"IMU串口连接成功: {config.PortName}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.SerialPort, $"IMU串口连接失败: {ex.Message}");
                return false;
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
        
        public void Dispose()
        {
            if (!_disposed)
            {
                _wheelMotorPort?.Close();
                _wheelMotorPort?.Dispose();
                _imuPort?.Close();
                _imuPort?.Dispose();
                _disposed = true;
            }
        }
    }
}