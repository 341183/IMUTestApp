namespace IMUTestApp.Models
{
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        Fatal = 4,
        Off = 5
    }

    public enum LogCategory
    {
        System,
        SerialPort,
        TCP,
        IMUData,
        FileIO,
        UserAction,
        UserOperation,    // 新增：用户操作类别
    }
}