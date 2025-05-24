using System.Windows.Input;

namespace TodoApp.Client.Command
{
    public class DelegateCommand : ICommand
    {
        private readonly Action<object?> m_execute;
        private readonly Func<object?, bool>? m_canExecute;

        public DelegateCommand(Action<object?> execute, Func<object?,bool>? canExecute = null)
        {
            ArgumentNullException.ThrowIfNull(execute);
            this.m_execute = execute;
            this.m_canExecute = canExecute;
        }

        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        
        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter) => m_canExecute is null || m_canExecute(parameter);

        public void Execute(object? parameter) => m_execute(parameter);
    }
}
