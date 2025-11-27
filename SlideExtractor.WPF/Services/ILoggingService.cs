namespace SlideExtractor.WPF.Services;

public interface ILoggingService
{
	void Trace(string message);
	void Debug(string message);
	void Info(string message);
	void Warn(string message);
	void Error(string message, Exception? ex = null);
	void Fatal(string message, Exception? ex = null);
}

public class LoggingService : ILoggingService
{
	public void Trace(string message) => Serilog.Log.Verbose(message);
	public void Debug(string message) => Serilog.Log.Debug(message);
	public void Info(string message) => Serilog.Log.Information(message);
	public void Warn(string message) => Serilog.Log.Warning(message);
	public void Error(string message, Exception? ex = null) => Serilog.Log.Error(ex, message);
	public void Fatal(string message, Exception? ex = null) => Serilog.Log.Fatal(ex, message);
}
