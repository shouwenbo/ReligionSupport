using System;
using System.Collections.Concurrent;
using System.IO;
using Microsoft.Extensions.Logging;

namespace PlaylistMaker.WPF.Logging;

public sealed class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
    private readonly ConcurrentDictionary<string, FileLogger> _loggers = new(StringComparer.OrdinalIgnoreCase);
    private readonly string _logDirectory;
    private IExternalScopeProvider? _scopeProvider;

    public FileLoggerProvider(string logDirectory)
    {
        _logDirectory = logDirectory;
        Directory.CreateDirectory(_logDirectory);
    }

    public ILogger CreateLogger(string categoryName)
        => _loggers.GetOrAdd(categoryName, name =>
        {
            var filePath = Path.Combine(_logDirectory, $"{DateTime.UtcNow:yyyyMMdd}.log");
            var logger = new FileLogger(name, filePath);
            logger.SetScopeProvider(_scopeProvider);
            return logger;
        });

    public void Dispose() => _loggers.Clear();

    public void SetScopeProvider(IExternalScopeProvider scopeProvider)
    {
        _scopeProvider = scopeProvider;
        foreach (var logger in _loggers.Values)
        {
            logger.SetScopeProvider(scopeProvider);
        }
    }
}
