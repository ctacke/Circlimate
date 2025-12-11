using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit.Abstractions;

namespace Circlimate.Core.Tests;

public class TestLogger<T> : ILogger<T>
{
    private readonly ITestOutputHelper? _output;

    public TestLogger(ITestOutputHelper? output = null)
    {
        _output = output;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        var message = formatter(state, exception);
        var logMessage = $"[{logLevel}] {message}";

        // Write to Debug output (visible in Visual Studio Output window)
        Debug.WriteLine(logMessage);

        // Also write to test output if available
        _output?.WriteLine(logMessage);

        if (exception != null)
        {
            Debug.WriteLine(exception.ToString());
            _output?.WriteLine(exception.ToString());
        }
    }
}
