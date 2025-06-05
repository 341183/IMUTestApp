using System;

namespace IMUTestApp.Models
{
    public class IMUData
    {
        public DateTime Timestamp { get; set; }
        public double AccelX { get; set; }
        public double AccelY { get; set; }
        public double AccelZ { get; set; }
        public double GyroX { get; set; }
        public double GyroY { get; set; }
        public double GyroZ { get; set; }
        public double MagX { get; set; }
        public double MagY { get; set; }
        public double MagZ { get; set; }
        
        public override string ToString()
        {
            return $"{Timestamp:HH:mm:ss.fff} - Accel: ({AccelX:F3}, {AccelY:F3}, {AccelZ:F3}) Gyro: ({GyroX:F3}, {GyroY:F3}, {GyroZ:F3}) Mag: ({MagX:F3}, {MagY:F3}, {MagZ:F3})";
        }
    }
}