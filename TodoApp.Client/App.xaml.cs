using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using System.Windows;
using System.Windows.Threading;
using TodoApp.Client.Data;
using TodoApp.Client.Services;
using TodoApp.Client.View;
using TodoApp.Client.ViewModel;

namespace TodoApp.Client;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private readonly ServiceProvider m_serviceProvider;
    public static new App Current => (App)Application.Current;
    public static ServiceProvider Services => ((App)Current).m_serviceProvider;

    public App()
    {
        CreateLogger();

        ServiceCollection services = new ServiceCollection();
        ConfigureServices(services);
        m_serviceProvider = services.BuildServiceProvider();
    }

    private static void CreateLogger()
    {
        // identify each client uniquely
        string clientId = Guid.NewGuid().ToString().Substring(0, 4);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File($"logs/client_{clientId}.log", rollingInterval: RollingInterval.Day)
            .CreateLogger();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.AddSerilog();
        });
        services.AddSingleton<IErrorHandler, ErrorHandler>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<TasksViewModel>();
        services.AddTransient<ITasksDataProvider, TasksDataProvider>();
        services.AddTransient<IUsersProvider, UsersProvider>();
        services.AddTransient<ITagsProvider, TagsProvider>();
        services.AddTransient<MainWindow>();
        services.AddTransient<TasksView>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        MainWindow mainWindow = Services.GetRequiredService<MainWindow>();
        if (mainWindow != null)
        {
            mainWindow.Show();
            mainWindow.Loaded += (s, e) =>
            {
                Services.GetRequiredService<TasksViewModel>().LoadUiState();
            };
        }

        GlobalExceptionsHandling();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        TasksViewModel vm = Services.GetRequiredService<TasksViewModel>();
        vm.SaveUiState();
    }

    private void GlobalExceptionsHandling()
    {
        this.DispatcherUnhandledException += OnUiException;
        AppDomain.CurrentDomain.UnhandledException += OnDomainException;
        TaskScheduler.UnobservedTaskException += OnTaskException;
    }
    private void OnUiException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Services.GetRequiredService<IErrorHandler>()
                .Handle(e.Exception,
                        "DispatcherUnhandledException",
                        "A critical UI error occurred.",
                        ErrorSeverity.Critical);

        // e.Handled left false - app will shut down after handler.
    }
    private void OnDomainException(object sender, UnhandledExceptionEventArgs e)
    {
        Exception ex = e.ExceptionObject as Exception ?? new Exception("Unknown domain exception");
        Services.GetRequiredService<IErrorHandler>()
                .Handle(ex,
                        "AppDomain.UnhandledException",
                        "A fatal error occurred.",
                        ErrorSeverity.Critical);
    }

    private void OnTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Services.GetRequiredService<IErrorHandler>()
                .Handle(e.Exception,
                        "UnobservedTaskException",
                        "Background task error.",
                        ErrorSeverity.Error);
        e.SetObserved();
    }
}

