namespace IMUTestApp.Models
{
    public class ConnectionConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Port { get; set; } = string.Empty;
        public int BaudRate { get; set; } = 115200;
        public int DataBits { get; set; } = 8;
        public int StopBits { get; set; } = 1;
        public string Parity { get; set; } = "None";
        public ConnectionType Type { get; set; } = ConnectionType.Serial;
        
        // TCP专用属性
        public string IpAddress { get; set; } = "192.168.4.1";
        public int TcpPort { get; set; } = 12024;
    }
    
    public enum ConnectionType
    {
        Serial,
        Tcp
    }
}