using System.ComponentModel;
using System.Windows;
using TodoApp.Client.View;
using TodoApp.Client.ViewModel;

namespace TodoApp.Client;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly LoginViewModel m_loginViewModel;
    private readonly TasksViewModel m_tasksViewModel;
    private readonly TasksView m_tasksView;


    public MainWindow(LoginViewModel loginVM, TasksViewModel tasksVM, TasksView tasksView)
    {
        InitializeComponent();
        m_loginViewModel = loginVM;
        m_tasksViewModel = tasksVM;
        m_tasksView = tasksView;

        m_loginViewModel.OnLoginSuccess = OnLoginSuccess;

        this.Loaded += (s, e) =>
        {
            MainContent.Content = new LoginView { DataContext = m_loginViewModel };
        };
        this.Closing += OnMainWindowClosing;
    }

    private void OnLoginSuccess(string userName)
    {
        m_tasksViewModel.UserLogin(userName);
        m_tasksView.DataContext = m_tasksViewModel;
        MainContent.Content = m_tasksView;
        DataContext = m_tasksViewModel;
    }

    private void OnMainWindowClosing(object? sender, CancelEventArgs e)
    {
        m_tasksViewModel.SaveUiState();     
    }
}
