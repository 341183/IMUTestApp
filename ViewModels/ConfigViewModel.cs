using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using IMUTestApp.Models;
using IMUTestApp.Services;

namespace IMUTestApp.ViewModels
{
    public class ConfigViewModel : BaseViewModel
    {
        private readonly ConfigService _configService;
        private readonly SerialPortService _serialPortService;

        private SerialPortConfig _config;
        private SerialPortConfig _secondConfig;
        private string _tcpIpAddress ;  // 第一次定义

        private int _tcpPort;
        
        public ConfigViewModel(ConfigService configService,LoggingService logger)
        {
            _configService = configService;

            _serialPortService = new SerialPortService(logger);
            
            // 初始化集合
            AvailablePorts = new ObservableCollection<string>();
            BaudRates = new ObservableCollection<int> { 9600, 19200, 38400, 57600, 115200 };
            DataBitsList = new ObservableCollection<int> { 7, 8 };
            StopBitsList = new ObservableCollection<int> { 1, 2 };
            SampleFrequencies = new ObservableCollection<string> { "10 Hz", "50 Hz", "100 Hz", "200 Hz" };
            Ranges = new ObservableCollection<string> { "±2g", "±4g", "±8g", "±16g" };
            RefreshPortsCommand = new RelayCommand(RefreshPorts);
            
            // 从配置文件加载配置（这会调用RefreshPorts）
            LoadFromConfig();
        }
        
        public ObservableCollection<string> AvailablePorts { get; }
        public ObservableCollection<int> BaudRates { get; }
        public ObservableCollection<int> DataBitsList { get; }
        public ObservableCollection<int> StopBitsList { get; }
        public ObservableCollection<string> SampleFrequencies { get; }
        public ObservableCollection<string> Ranges { get; }
        
        public string SelectedPort
        {
            get => _config.PortName;
            set
            {
                _config.PortName = value;
                OnPropertyChanged();
            }
        }
        
        public int SelectedBaudRate
        {
            get => _config.BaudRate;
            set
            {
                _config.BaudRate = value;

                OnPropertyChanged();
            }
        }
        
        public int SelectedDataBits
        {
            get => _config.DataBits;
            set
            {
                _config.DataBits = value;
                OnPropertyChanged();
            }
        }
        
        public int SelectedStopBits
        {
            get => _config.StopBits;
            set
            {
                _config.StopBits = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshPortsCommand { get; }
        
        private void RefreshPorts()
        {
            AvailablePorts.Clear();
            var ports = _serialPortService.GetAvailablePorts();
            foreach (var port in ports)
            {
                AvailablePorts.Add(port);
            }
            
            // 改进的串口选择逻辑
            if (AvailablePorts.Any())
            {
                // 检查配置文件中的串口是否仍然可用
                if (!string.IsNullOrEmpty(SelectedPort) && AvailablePorts.Contains(SelectedPort))
                {
                    // 配置的串口仍然可用，保持当前选择
                    // 不需要做任何操作，因为绑定会自动保持选择
                }
                else
                {
                    // 配置的串口不存在或为空，选择第一个可用串口
                    SelectedPort = AvailablePorts.First();
                }
                
                // 对第二串口执行相同的逻辑
                if (!string.IsNullOrEmpty(SecondSelectedPort) && AvailablePorts.Contains(SecondSelectedPort))
                {
                    // 第二串口配置仍然可用
                }
                else
                {
                    // 为第二串口选择一个不同的可用串口（如果有多个）
                    var availableForSecond = AvailablePorts.Where(p => p != SelectedPort).ToList();
                    if (availableForSecond.Count > 0)
                    {
                        SecondSelectedPort = availableForSecond.First();
                    }
                    else if (AvailablePorts.Count == 1)
                    {
                        // 只有一个串口可用，两个配置使用同一个串口
                        SecondSelectedPort = AvailablePorts.First();
                    }
                }
            }
            else
            {
                // 没有可用串口时，清空选择
                SelectedPort = string.Empty;
                SecondSelectedPort = string.Empty;
            }
        }
        
        // 第二串口属性
        public string SecondSelectedPort
        {
            get => _secondConfig.PortName;
            set
            {
                _secondConfig.PortName = value;
                OnPropertyChanged();
            }
        }
        
        public int SecondSelectedBaudRate
        {
            get => _secondConfig.BaudRate;
            set
            {
                _secondConfig.BaudRate = value;
                OnPropertyChanged();
            }
        }
        
        // TCP属性
        public string TcpIpAddress
        {
            get => _tcpIpAddress;
            set
            {
                SetProperty(ref _tcpIpAddress, value);
            }
        }
        
        public int TcpPort
        {
            get => _tcpPort;
            set
            {
                SetProperty(ref _tcpPort, value);
            }
        }
        
        private void LoadFromConfig()
        {
            var config = _configService.Config;
            _config = config.WheelMotorConfig;
            _secondConfig = config.IMUConfig;
            _tcpIpAddress = config.TcpConfig.IpAddress;
            _tcpPort = config.TcpConfig.Port;
            
            // 触发属性更新通知
            OnPropertyChanged(nameof(SelectedPort));
            OnPropertyChanged(nameof(SelectedBaudRate));
            OnPropertyChanged(nameof(SelectedDataBits));
            OnPropertyChanged(nameof(SelectedStopBits));
            OnPropertyChanged(nameof(SecondSelectedPort));
            OnPropertyChanged(nameof(SecondSelectedBaudRate));
            OnPropertyChanged(nameof(TcpIpAddress));
            OnPropertyChanged(nameof(TcpPort));
            
            // 加载配置后刷新串口，确保选择有效的串口
            RefreshPorts();
        }
    }
}