using System.Windows;

namespace TodoApp.Client.Services
{
    public interface IErrorHandler
    {
        void Handle(Exception? ex, string context, string userMessage, 
            ErrorSeverity severity = ErrorSeverity.Error, string title = "Error");
    }

}
