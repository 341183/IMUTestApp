using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using IMUTestApp.Models;
using IMUTestApp.Services;

namespace IMUTestApp.ViewModels
{
    public class ConfigViewModel : BaseViewModel
    {
        private readonly SerialPortService _serialPortService;
        private SerialPortConfig _config;
        private bool _isConnected;
        
        public ConfigViewModel(SerialPortService serialPortService)
        {
            _serialPortService = serialPortService;
            _serialPortService.ConnectionStatusChanged += OnConnectionStatusChanged;
            
            _config = new SerialPortConfig();
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
    }
}