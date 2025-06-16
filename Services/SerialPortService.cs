using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using IMUTestApp.Models;

namespace IMUTestApp.Services
{
    public class SerialPortService
    {
        private readonly LoggingService _logger;
        
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
    }
}