using System;

namespace App.Services;

/// <summary>
/// 统一日志服务（单例）
/// 所有日志输出的唯一入口
/// </summary>
public sealed class LogService
{
    private static LogService? _instance;
    private static readonly object _lock = new();

    public static LogService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new LogService();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// 日志消息事件（UI 订阅此事件显示日志）
    /// </summary>
    public event Action<string>? LogMessageReceived;

    private LogService() { }

    /// <summary>
    /// 输出日志（唯一入口）
    /// </summary>
    public void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var logLine = $"[{timestamp}] {message}";

        // 输出到 Debug 控制台
        System.Diagnostics.Debug.WriteLine(logLine);

        // 通知 UI
        LogMessageReceived?.Invoke(logLine);
    }
}
