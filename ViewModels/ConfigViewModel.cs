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
        private readonly SettingsService _settingsService;
        private SerialPortConfig _config;
        private SerialPortConfig _secondConfig;
        private bool _isConnected;
        private string _tcpIpAddress = "192.168.4.1";
        private int _tcpPort = 12024;
        
        public ConfigViewModel(SerialPortService serialPortService, SettingsService settingsService)
        {
            _serialPortService = serialPortService;
            _settingsService = settingsService;
            _serialPortService.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            _config = new SerialPortConfig();
            _secondConfig = new SerialPortConfig { PortName = "COM3", BaudRate = 115200 };
            
            // 从设置中加载配置
            LoadFromSettings();
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
                SaveToSettings();
            }
        }
        
        public int SecondSelectedBaudRate
        {
            get => _secondConfig.BaudRate;
            set
            {
                _secondConfig.BaudRate = value;
                OnPropertyChanged();
                SaveToSettings();
            }
        }
        
        // TCP属性
        public string TcpIpAddress
        {
            get => _tcpIpAddress;
            set
            {
                SetProperty(ref _tcpIpAddress, value);
                SaveToSettings();
            }
        }
        
        public int TcpPort
        {
            get => _tcpPort;
            set
            {
                SetProperty(ref _tcpPort, value);
                SaveToSettings();
            }
        }
        
        private void LoadFromSettings()
        {
            var settings = _settingsService.Settings;
            _secondConfig.PortName = settings.SecondSerialPort;
            _secondConfig.BaudRate = settings.SecondSerialBaudRate;
            _tcpIpAddress = settings.TcpIpAddress;
            _tcpPort = settings.TcpPort;
        }
        
        private void SaveToSettings()
        {
            var settings = _settingsService.Settings;
            settings.SecondSerialPort = _secondConfig.PortName;
            settings.SecondSerialBaudRate = _secondConfig.BaudRate;
            settings.TcpIpAddress = _tcpIpAddress;
            settings.TcpPort = _tcpPort;
            _settingsService.SaveSettings();
        }
    }
}