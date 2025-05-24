using Microsoft.Extensions.Logging;
using System.Windows;
using TodoApp.Client.Services;

namespace TodoApp.Client.Services
{
    public enum ErrorSeverity
    {
        Info,
        Warning,
        Error,      // App can continue running
        Critical    // Closing the app
    }
}

public class ErrorHandler : IErrorHandler
{
    private readonly ILogger<ErrorHandler> m_logger;

    public ErrorHandler(ILogger<ErrorHandler> logger)
    {
        m_logger = logger;
    }

    public void Handle(Exception? ex, string context, string userMessage,
                        ErrorSeverity severity = ErrorSeverity.Error, string title = "Error")
    {
        m_logger.LogError(ex, "{Context} | Severity: {Severity}", context, severity);

        if (severity != ErrorSeverity.Info)
        {
            MessageBox.Show(userMessage, title,
                            MessageBoxButton.OK,
                            severity == ErrorSeverity.Critical ? MessageBoxImage.Stop : MessageBoxImage.Warning);
        }

        if (severity == ErrorSeverity.Critical)
        {
            Application.Current.Shutdown();
        }
    }
}
