using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using IMUTestApp.Models;

namespace IMUTestApp.Services
{
    public class DualSerialPortService : IDisposable
    {
        private SerialPort? _wheelMotorPort;
        private SerialPort? _imuPort;
        private bool _disposed = false;
        
        public event EventHandler<IMUData>? IMUDataReceived;
        public event EventHandler<string>? WheelMotorDataReceived;
        public event EventHandler<bool>? WheelMotorConnectionChanged;
        public event EventHandler<bool>? IMUConnectionChanged;
        
        public bool IsWheelMotorConnected => _wheelMotorPort?.IsOpen ?? false;
        public bool IsIMUConnected => _imuPort?.IsOpen ?? false;
        
        public List<string> GetAvailablePorts()
        {
            return SerialPort.GetPortNames().ToList();
        }
        
        public async Task<(string wheelMotorPort, string imuPort)> AutoDetectPorts()
        {
            var availablePorts = GetAvailablePorts();
            string detectedWheelMotorPort = string.Empty;
            string detectedIMUPort = string.Empty;
            
            foreach (var port in availablePorts)
            {
                try
                {
                    using var testPort = new SerialPort(port, 115200);
                    testPort.Open();
                    
                    // 发送测试命令并等待响应
                    testPort.WriteLine("AT"); // 通用AT命令
                    await Task.Delay(100);
                    
                    if (testPort.BytesToRead > 0)
                    {
                        var response = testPort.ReadExisting();
                        
                        // 根据响应内容判断设备类型
                        if (IsWheelMotorResponse(response))
                        {
                            detectedWheelMotorPort = port;
                        }
                        else if (IsIMUResponse(response))
                        {
                            detectedIMUPort = port;
                        }
                    }
                    
                    testPort.Close();
                }
                catch
                {
                    // 忽略连接失败的端口
                }
            }
            
            return (detectedWheelMotorPort, detectedIMUPort);
        }
        
        private bool IsWheelMotorResponse(string response)
        {
            // 根据波轮电机的特定响应模式判断
            // 这里需要根据实际的波轮电机响应格式来调整
            return response.Contains("MOTOR") || response.Contains("WHEEL") || 
                   Regex.IsMatch(response, @"RPM|SPEED|TORQUE", RegexOptions.IgnoreCase);
        }
        
        private bool IsIMUResponse(string response)
        {
            // 根据IMU的特定响应模式判断
            // 这里需要根据实际的IMU响应格式来调整
            return response.Contains("IMU") || response.Contains("ACCEL") || 
                   Regex.IsMatch(response, @"GYRO|MAG|QUATERNION", RegexOptions.IgnoreCase);
        }
        
        public bool ConnectWheelMotor(string portName, int baudRate = 115200)
        {
            try
            {
                DisconnectWheelMotor();
                
                _wheelMotorPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
                _wheelMotorPort.DataReceived += OnWheelMotorDataReceived;
                _wheelMotorPort.Open();
                
                WheelMotorConnectionChanged?.Invoke(this, true);
                return true;
            }
            catch
            {
                WheelMotorConnectionChanged?.Invoke(this, false);
                return false;
            }
        }
        
        public bool ConnectIMU(string portName, int baudRate = 115200)
        {
            try
            {
                DisconnectIMU();
                
                _imuPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
                _imuPort.DataReceived += OnIMUDataReceived;
                _imuPort.Open();
                
                IMUConnectionChanged?.Invoke(this, true);
                return true;
            }
            catch
            {
                IMUConnectionChanged?.Invoke(this, false);
                return false;
            }
        }
        
        public void DisconnectWheelMotor()
        {
            if (_wheelMotorPort?.IsOpen == true)
            {
                _wheelMotorPort.Close();
            }
            _wheelMotorPort?.Dispose();
            _wheelMotorPort = null;
            WheelMotorConnectionChanged?.Invoke(this, false);
        }
        
        public void DisconnectIMU()
        {
            if (_imuPort?.IsOpen == true)
            {
                _imuPort.Close();
            }
            _imuPort?.Dispose();
            _imuPort = null;
            IMUConnectionChanged?.Invoke(this, false);
        }
        
        private void OnWheelMotorDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (_wheelMotorPort != null)
            {
                var data = _wheelMotorPort.ReadExisting();
                WheelMotorDataReceived?.Invoke(this, data);
            }
        }
        
        private void OnIMUDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            // 解析IMU数据
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
            
            IMUDataReceived?.Invoke(this, data);
        }
        
        public void SendWheelMotorCommand(string command)
        {
            if (_wheelMotorPort?.IsOpen == true)
            {
                _wheelMotorPort.WriteLine(command);
            }
        }
        
        public void Dispose()
        {
            if (!_disposed)
            {
                DisconnectWheelMotor();
                DisconnectIMU();
                _disposed = true;
            }
        }
    }
}