using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using IMUTestApp.Models;

namespace IMUTestApp.Services
{
    public class SerialPortService : IDisposable
    {
        private SerialPort? _serialPort;
        private bool _disposed = false;
        
        public event EventHandler<IMUData>? DataReceived;
        public event EventHandler<bool>? ConnectionStatusChanged;
        
        public bool IsConnected => _serialPort?.IsOpen ?? false;
        
        public List<string> GetAvailablePorts()
        {
            return SerialPort.GetPortNames().ToList();
        }
        
        public bool Connect(SerialPortConfig config)
        {
            try
            {
                Disconnect();
                
                _serialPort = new SerialPort(config.PortName, config.BaudRate, 
                    Parity.None, config.DataBits, StopBits.One);
                _serialPort.DataReceived += OnDataReceived;
                _serialPort.Open();
                
                ConnectionStatusChanged?.Invoke(this, true);
                return true;
            }
            catch
            {
                ConnectionStatusChanged?.Invoke(this, false);
                return false;
            }
        }
        
        public void Disconnect()
        {
            if (_serialPort?.IsOpen == true)
            {
                _serialPort.Close();
            }
            _serialPort?.Dispose();
            _serialPort = null;
            ConnectionStatusChanged?.Invoke(this, false);
        }
        
        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // 模拟IMU数据解析
            var random = new Random();
            var data = new IMUData
            {
                Timestamp = DateTime.Now,
                AccelX = (random.NextDouble() - 0.5) * 4,
                AccelY = (random.NextDouble() - 0.5) * 4,
                AccelZ = (random.NextDouble() - 0.5) * 4,
                GyroX = (random.NextDouble() - 0.5) * 500,
                GyroY = (random.NextDouble() - 0.5) * 500,
                GyroZ = (random.NextDouble() - 0.5) * 500,
                MagX = (random.NextDouble() - 0.5) * 100,
                MagY = (random.NextDouble() - 0.5) * 100,
                MagZ = (random.NextDouble() - 0.5) * 100
            };
            
            DataReceived?.Invoke(this, data);
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                Disconnect();
                _disposed = true;
            }
        }
    }
}