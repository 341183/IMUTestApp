using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IMUTestApp.Models
{
    public class SerialPortConfig : INotifyPropertyChanged
    {
        private string _portName = string.Empty;
        private int _baudRate = 9600;
        private int _dataBits = 8;
        private int _stopBits = 1;
        private string _parity = "None";

        public string PortName 
        { 
            get => _portName;
            set
            {
                if (_portName != value)
                {
                    _portName = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public int BaudRate 
        { 
            get => _baudRate;
            set
            {
                if (_baudRate != value)
                {
                    _baudRate = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public int DataBits 
        { 
            get => _dataBits;
            set
            {
                if (_dataBits != value)
                {
                    _dataBits = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public int StopBits 
        { 
            get => _stopBits;
            set
            {
                if (_stopBits != value)
                {
                    _stopBits = value;
                    OnPropertyChanged();
                }
            }
        }
        
        public string Parity 
        { 
            get => _parity;
            set
            {
                if (_parity != value)
                {
                    _parity = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}