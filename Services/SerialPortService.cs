using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using IMUTestApp.Models;
using IMUTestApp.Services;

namespace IMUTestApp.Services
{
    public class SerialPortService : IDisposable
    {
        private readonly LoggingService _logger;
        private SerialPort? _serialPort;
        private bool _disposed = false;
        
        public event EventHandler<IMUData>? DataReceived;
        public event EventHandler<bool>? ConnectionStatusChanged;
        
        public bool IsConnected => _serialPort?.IsOpen ?? false;
        
        public SerialPortService(LoggingService logger)
        {
            _logger = logger;
            _logger.LogInfo(LogCategory.System, "串口服务已初始化");
        }
        
        //获取串口列表
        public List<string> GetAvailablePorts()
        {
            try
            {
                var ports = SerialPort.GetPortNames().ToList();
                _logger.LogDebug(LogCategory.SerialPort, $"获取可用串口列表，共{ports.Count}个端口: {string.Join(", ", ports)}");
                return ports;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.SerialPort, "获取可用串口列表失败", ex);
                return new List<string>();
            }
        }
        
        public bool Connect(SerialPortConfig config)
        {
            try
            {
                _logger.LogInfo(LogCategory.SerialPort, $"尝试连接串口: {config.PortName}, 波特率: {config.BaudRate}, 数据位: {config.DataBits}");
                
                //断开连接
                Disconnect();
                
                _serialPort = new SerialPort(config.PortName, config.BaudRate, 
                    Parity.None, config.DataBits, StopBits.One);
                //_serialPort.DataReceived += OnDataReceived;
                _serialPort.Open();
                
                _logger.LogInfo(LogCategory.SerialPort, $"串口连接成功: {config.PortName}");
                ConnectionStatusChanged?.Invoke(this, true);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.SerialPort, $"串口连接失败: {config.PortName}", ex);
                ConnectionStatusChanged?.Invoke(this, false);
                return false;
            }
        }
        
        //断开串口连接
        public void Disconnect()
        {
            try
            {
                if (_serialPort?.IsOpen == true)
                {
                    _logger.LogInfo(LogCategory.SerialPort, $"断开串口连接: {_serialPort.PortName}");
                    _serialPort.Close();
                }
                _serialPort?.Dispose();
                _serialPort = null;
                ConnectionStatusChanged?.Invoke(this, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(LogCategory.SerialPort, "断开串口连接时发生错误", ex);
            }
        }
        
                
        public void Dispose()
        {
            if (!_disposed)
            {
                _logger.LogInfo(LogCategory.System, "串口服务正在释放资源");
                Disconnect();
                _disposed = true;
            }
        }
    }
}