using System;

namespace IMUTestApp.Models
{
    public class LogEntry
    {
        public DateTime Timestamp { get; set; }
        public LogLevel Level { get; set; }
        public LogCategory Category { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Exception { get; set; }
        // 移除不常用的字段
        // public string? StackTrace { get; set; }
        // public string? AdditionalData { get; set; }

        public LogEntry()
        {
            Timestamp = DateTime.Now;
        }

        public LogEntry(LogLevel level, LogCategory category, string message) : this()
        {
            Level = level;
            Category = category;
            Message = message;
        }

        public LogEntry(LogLevel level, LogCategory category, string message, Exception ex) : this(level, category, message)
        {
            Exception = ex.Message;
        }

        public override string ToString()
        {
            var timestamp = Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var result = $"[{Level.ToString().ToUpper()}] {timestamp} {Category} - {Message}";
            
            if (!string.IsNullOrEmpty(Exception))
            {
                result += $" | Exception: {Exception}";
            }
            
            return result;
        }
    }
}