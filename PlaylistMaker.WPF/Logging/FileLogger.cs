using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace PlaylistMaker.WPF.Logging;

public sealed class FileLogger : ILogger
{
    private readonly string _categoryName;
    private readonly string _filePath;
    private IExternalScopeProvider? _scopeProvider;
    private static readonly object WriterLock = new();

    public FileLogger(string categoryName, string filePath)
    {
        _categoryName = categoryName;
        _filePath = filePath;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _scopeProvider?.Push(state!) ?? NullScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel) || formatter is null)
        {
            return;
        }

        var builder = new StringBuilder()
            .Append(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz"))
            .Append(" [").Append(logLevel).Append("] ")
            .Append(_categoryName).Append(" ")
            .AppendLine(formatter(state, exception));

        _scopeProvider?.ForEachScope((scope, sb) =>
        {
            sb.Append("  => scope: ").Append(scope).AppendLine();
        }, builder);

        if (exception is not null)
        {
            builder.AppendLine(exception.ToString());
        }

        lock (WriterLock)
        {
            File.AppendAllText(_filePath, builder.ToString(), Encoding.UTF8);
        }
    }

    public void SetScopeProvider(IExternalScopeProvider? scopeProvider) => _scopeProvider = scopeProvider;

    private sealed class NullScope : IDisposable
    {
        public static readonly NullScope Instance = new();
        public void Dispose() { }
    }
}
