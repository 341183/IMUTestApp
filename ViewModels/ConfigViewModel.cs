using System;
using System.Collections.ObjectModel;
using System.IO.Ports;
using System.Linq;
using System.Windows.Input;
using IMUTestApp.Models;
using IMUTestApp.Services;

namespace IMUTestApp.ViewModels
{
    public class ConfigViewModel : BaseViewModel
    {
        private readonly SerialPortService _serialPortService;
        private readonly ConfigService _configService;
        private SerialPortConfig _config = new SerialPortConfig();
        private SerialPortConfig _secondConfig = new SerialPortConfig();
        private string _tcpIpAddress = "192.168.1.1";  // 第一次定义
        private bool _isConnected;
        // 删除这行重复定义
        // private string _tcpIpAddress;  // 重复定义，需要删除
        private int _tcpPort;
        
        public ConfigViewModel(SerialPortService serialPortService, ConfigService configService)
        {
            _serialPortService = serialPortService;
            _configService = configService;
            _serialPortService.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            // 从配置文件加载配置
            LoadFromConfig();
            AvailablePorts = new ObservableCollection<string>();
            BaudRates = new ObservableCollection<int> { 9600, 19200, 38400, 57600, 115200 };
            DataBitsList = new ObservableCollection<int> { 7, 8 };
            StopBitsList = new ObservableCollection<int> { 1, 2 };
            SampleFrequencies = new ObservableCollection<string> { "10 Hz", "50 Hz", "100 Hz", "200 Hz" };
            Ranges = new ObservableCollection<string> { "±2g", "±4g", "±8g", "±16g" };
            
            ConnectCommand = new RelayCommand(Connect, () => !_isConnected && !string.IsNullOrEmpty(_config.PortName));
            DisconnectCommand = new RelayCommand(Disconnect, () => _isConnected);
            RefreshPortsCommand = new RelayCommand(RefreshPorts);
            
            RefreshPorts();
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
                SaveToConfig();
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
            }
        }
        
        public int SelectedBaudRate
        {
            get => _config.BaudRate;
            set
            {
                _config.BaudRate = value;
                OnPropertyChanged();
                SaveToConfig();
            }
        }
        
        public int SelectedDataBits
        {
            get => _config.DataBits;
            set
            {
                _config.DataBits = value;
                OnPropertyChanged();
                SaveToConfig();
            }
        }
        
        public int SelectedStopBits
        {
            get => _config.StopBits;
            set
            {
                _config.StopBits = value;
                OnPropertyChanged();
                SaveToConfig();
            }
        }
        
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                SetProperty(ref _isConnected, value);
                ((RelayCommand)ConnectCommand).RaiseCanExecuteChanged();
                ((RelayCommand)DisconnectCommand).RaiseCanExecuteChanged();
            }
        }
        
        public ICommand ConnectCommand { get; }
        public ICommand DisconnectCommand { get; }
        public ICommand RefreshPortsCommand { get; }
        
        private void Connect()
        {
            _serialPortService.Connect(_config);
        }
        
        private void Disconnect()
        {
            _serialPortService.Disconnect();
        }
        
        private void RefreshPorts()
        {
            AvailablePorts.Clear();
            var ports = _serialPortService.GetAvailablePorts();
            foreach (var port in ports)
            {
                AvailablePorts.Add(port);
            }
            
            if (AvailablePorts.Any() && string.IsNullOrEmpty(SelectedPort))
            {
                SelectedPort = AvailablePorts.First();
            }
        }
        
        private void OnConnectionStatusChanged(object? sender, bool isConnected)
        {
            IsConnected = isConnected;
        }
        
        // 第二串口属性
        public string SecondSelectedPort
        {
            get => _secondConfig.PortName;
            set
            {
                _secondConfig.PortName = value;
                OnPropertyChanged();
                SaveToConfig();
            }
        }
        
        public int SecondSelectedBaudRate
        {
            get => _secondConfig.BaudRate;
            set
            {
                _secondConfig.BaudRate = value;
                OnPropertyChanged();
                SaveToConfig();
            }
        }
        
        // TCP属性
        public string TcpIpAddress
        {
            get => _tcpIpAddress;
            set
            {
                SetProperty(ref _tcpIpAddress, value);
                SaveToConfig();
            }
        }
        
        public int TcpPort
        {
            get => _tcpPort;
            set
            {
                SetProperty(ref _tcpPort, value);
                SaveToConfig();
            }
        }
        
        private void LoadFromConfig()
        {
            var config = _configService.Config;
            _config = config.SerialPortConfig;
            _secondConfig = config.SecondSerialPortConfig;
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
        }
        
        private void SaveToConfig()
        {
            var config = _configService.Config;
            config.SerialPortConfig = _config;
            config.SecondSerialPortConfig = _secondConfig;
            config.TcpConfig.IpAddress = _tcpIpAddress;
            config.TcpConfig.Port = _tcpPort;
            _configService.SaveConfig();
        }
    }
}