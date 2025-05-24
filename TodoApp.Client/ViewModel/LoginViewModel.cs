using System.Windows.Input;
using TodoApp.Client.Command;

namespace TodoApp.Client.ViewModel
{
    public class LoginViewModel : ViewModelBase

    {
        private string m_userName = string.Empty;
        private string? m_error;

        public string UserName
        {
            get => m_userName;
            set
            {
                m_userName = value;
                OnPropertyChanged();
            }
        }

        public string? Error
        {
            get => m_error;
            private set
            {
                m_error = value;
                OnPropertyChanged();
            }
        }

        public ICommand LoginCommand { get; }

        public Action<string>? OnLoginSuccess;

        public LoginViewModel()
        {
            LoginCommand = new DelegateCommand(OnLogin);
        }

        private void OnLogin(object? _)
        {
            if (string.IsNullOrWhiteSpace(UserName))
            {
                Error = "Username is required.";
                return;
            }

            Error = null;
            OnLoginSuccess?.Invoke(UserName.Trim());
        }
    }
}
