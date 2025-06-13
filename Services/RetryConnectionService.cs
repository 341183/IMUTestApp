using System;
using System.Threading.Tasks;
using IMUTestApp.Models;

namespace IMUTestApp.Services
{
    public class RetryConnectionService
    {
        private readonly LoggingService _logger;
        
        public RetryConnectionService(LoggingService logger)
        {
            _logger = logger;
        }
        
        /// <summary>
        /// 通用重试连接方法
        /// </summary>
        /// <typeparam name="T">连接参数类型</typeparam>
        /// <param name="connectionFunc">连接函数委托</param>
        /// <param name="config">连接参数</param>
        /// <param name="connectionName">连接名称（用于日志）</param>
        /// <param name="maxRetries">最大重试次数</param>
        /// <param name="timeoutSeconds">总超时时间（秒）</param>
        /// <param name="progressCallback">进度回调（可选）</param>
        /// <returns>连接是否成功</returns>
        public async Task<bool> RetryConnectionAsync<T>(
            Func<T, bool> connectionFunc,
            T config,
            string connectionName,
            int maxRetries = 3,
            int timeoutSeconds = 10,
            Action<string>? progressCallback = null)
        {
            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            
            _logger.LogInfo(LogCategory.System, $"开始尝试连接 {connectionName}，最大重试次数: {maxRetries}，超时时间: {timeoutSeconds}秒");
            progressCallback?.Invoke($"🔄 开始连接 {connectionName}...");
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                // 检查是否超时
                if (DateTime.Now - startTime > timeout)
                {
                    _logger.LogWarn(LogCategory.System, $"{connectionName} 连接超时 ({timeoutSeconds}秒)，停止重试");
                    progressCallback?.Invoke($"⏰ {connectionName} 连接超时 ({timeoutSeconds}秒)");
                    return false;
                }
                
                _logger.LogInfo(LogCategory.System, $"尝试连接 {connectionName} (第{attempt}/{maxRetries}次)");
                progressCallback?.Invoke($"🔄 连接尝试 {attempt}/{maxRetries}...");
                
                try
                {
                    bool success = connectionFunc(config);
                    
                    if (success)
                    {
                        _logger.LogInfo(LogCategory.System, $"{connectionName} 连接成功 (第{attempt}次尝试)");
                        progressCallback?.Invoke($"✅ {connectionName} 连接成功");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarn(LogCategory.System, $"{connectionName} 连接失败 (第{attempt}次尝试)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(LogCategory.System, $"{connectionName} 连接异常 (第{attempt}次尝试): {ex.Message}");
                }
                
                // 如果不是最后一次尝试，等待后重试
                if (attempt < maxRetries)
                {
                    var retryDelay = CalculateRetryDelay(attempt);
                    _logger.LogInfo(LogCategory.System, $"{connectionName} 连接失败，{retryDelay}ms后重试");
                    progressCallback?.Invoke($"⏳ {retryDelay}ms后重试...");
                    await Task.Delay(retryDelay);
                }
            }
            
            _logger.LogError(LogCategory.System, $"{connectionName} 连接失败，已达到最大重试次数 ({maxRetries})");
            progressCallback?.Invoke($"❌ {connectionName} 连接失败");
            return false;
        }
        
        /// <summary>
        /// 异步连接函数的重试方法
        /// </summary>
        public async Task<bool> RetryConnectionAsync<T>(
            Func<T, Task<bool>> connectionFunc,
            T config,
            string connectionName,
            int maxRetries = 3,
            int timeoutSeconds = 10,
            Action<string>? progressCallback = null)
        {
            var startTime = DateTime.Now;
            var timeout = TimeSpan.FromSeconds(timeoutSeconds);
            
            _logger.LogInfo(LogCategory.System, $"开始尝试异步连接 {connectionName}，最大重试次数: {maxRetries}，超时时间: {timeoutSeconds}秒");
            progressCallback?.Invoke($"🔄 开始连接 {connectionName}...");
            
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                // 检查是否超时
                if (DateTime.Now - startTime > timeout)
                {
                    _logger.LogWarn(LogCategory.System, $"{connectionName} 连接超时 ({timeoutSeconds}秒)，停止重试");
                    progressCallback?.Invoke($"⏰ {connectionName} 连接超时 ({timeoutSeconds}秒)");
                    return false;
                }
                
                _logger.LogInfo(LogCategory.System, $"尝试异步连接 {connectionName} (第{attempt}/{maxRetries}次)");
                progressCallback?.Invoke($"🔄 连接尝试 {attempt}/{maxRetries}...");
                
                try
                {
                    bool success = await connectionFunc(config);
                    
                    if (success)
                    {
                        _logger.LogInfo(LogCategory.System, $"{connectionName} 连接成功 (第{attempt}次尝试)");
                        progressCallback?.Invoke($"✅ {connectionName} 连接成功");
                        return true;
                    }
                    else
                    {
                        _logger.LogWarn(LogCategory.System, $"{connectionName} 连接失败 (第{attempt}次尝试)");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(LogCategory.System, $"{connectionName} 连接异常 (第{attempt}次尝试): {ex.Message}");
                }
                
                // 如果不是最后一次尝试，等待后重试
                if (attempt < maxRetries)
                {
                    var retryDelay = CalculateRetryDelay(1);
                    _logger.LogInfo(LogCategory.System, $"{connectionName} 连接失败，{retryDelay}ms后重试");
                    progressCallback?.Invoke($"⏳ {retryDelay}ms后重试...");
                    await Task.Delay(retryDelay);
                }
            }
            
            _logger.LogError(LogCategory.System, $"{connectionName} 连接失败，已达到最大重试次数 ({maxRetries})");
            progressCallback?.Invoke($"❌ {connectionName} 连接失败");
            return false;
        }
        
        /// <summary>
        /// 计算重试延迟时间（递增策略）
        /// </summary>
        /// <param name="attempt">当前尝试次数</param>
        /// <returns>延迟时间（毫秒）</returns>
        private int CalculateRetryDelay(int attempt)
        {
            // 递增延迟策略：1秒、2秒、3秒，最大不超过5秒
            return Math.Min(1000 * attempt, 5000);
        }
    }
}