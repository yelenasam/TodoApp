using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows.Threading;
using TodoApp.Client.Services;
using TodoApp.Shared.Model;

namespace TodoApp.Client.Data
{
    public class TasksDataProvider : ITasksDataProvider
    {
        private readonly HttpClient m_httpClient;
        private HubConnection? m_hubConnection;
        private bool m_connected = false;
        private readonly DispatcherTimer m_reconnectTimer;
        private readonly IErrorHandler m_errorHandler;
        private readonly ILogger<TasksDataProvider> m_logger;

        // === Configuration Constants ===
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 3000;
        private const string BaseApiUrl = "https://localhost:7186";
        private const string HubUrl = BaseApiUrl + "/taskitemshub";

        public event Action<TaskItem>? TaskAdded;
        public event Action<TaskItem>? TaskUpdated;
        public event Action<int>? TaskDeleted;
        public event Action<int, string>? TaskLocked;
        public event Action<int>? TaskUnlocked;

        public TasksDataProvider(ILogger<TasksDataProvider> logger, IErrorHandler errorHandler)
        {
            m_logger = logger;
            m_errorHandler = errorHandler;
            m_httpClient = new HttpClient
            {
                BaseAddress = new Uri(BaseApiUrl)
            };

            m_reconnectTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            m_reconnectTimer.Tick += async (s, e) => await TryReconnectAsync();
        }

        public async Task ConnectToServerAsync()
        {
            try
            {
                m_hubConnection = new HubConnectionBuilder()
                .WithUrl(HubUrl)
                .WithAutomaticReconnect()
                .Build();

                m_hubConnection.On<TaskItem>("TaskAdded", task => TaskAdded?.Invoke(task));
                m_hubConnection.On<TaskItem>("TaskUpdated", updated => TaskUpdated?.Invoke(updated));
                m_hubConnection.On<int>("TaskDeleted", taskId => TaskDeleted?.Invoke(taskId));
                m_hubConnection.On<int, string>("TaskLocked", (taskId, user) => TaskLocked?.Invoke(taskId, user));
                m_hubConnection.On<int, string>("TaskUnlocked", (taskId, user) => TaskUnlocked?.Invoke(taskId));

                m_hubConnection.Closed += HandleHubConnectionClosed;
                await m_hubConnection.StartAsync();
                m_connected = true;
                m_logger.LogInformation("Connected to SignalR");
            }
            catch (Exception ex)
            {
                await HandleHubConnectionClosed(ex);
            }
        }

        private async Task HandleHubConnectionClosed(Exception? ex)
        {
            m_connected = false;
            m_logger.LogWarning("SignalR closed: " + ex?.Message);
            m_reconnectTimer.Start();
        }

        private async Task TryReconnectAsync()
        {
            if (m_connected || m_hubConnection == null) return;

            try
            {
                await m_hubConnection.StartAsync();
                m_connected = true;
                m_reconnectTimer.Stop();
                m_logger.LogInformation("Reconnected to SignalR");
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, $"Reconnect failed: {ex.Message}");
            }
        }

        public async Task<IEnumerable<TaskItem>?> GetAllAsync()
        {
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                try
                {
                    var response = await m_httpClient.GetAsync("api/tasks");
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadFromJsonAsync<IEnumerable<TaskItem>>();
                }
                catch (HttpRequestException ex)
                {
                    m_logger.LogError(ex, $"[Attempt {attempt}] HTTP error (GetAllAsync): {ex.Message}");
                    if (attempt == MaxRetries)
                    {
                        m_errorHandler.Handle(ex, "GetAllAsync", "Cannot reach the server.", ErrorSeverity.Error);
                        return null;
                    }
                    await Task.Delay(RetryDelayMs);
                }
                catch (Exception ex)
                {
                    m_errorHandler.Handle(ex, "GetAllAsync", "Unexpected client error.", ErrorSeverity.Error);
                    return null;
                }
            }
            return null;
        }

        public async Task Add(TaskItem task)
        {
            try
            {
                HttpResponseMessage response = await m_httpClient.PostAsJsonAsync("api/tasks", task);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                m_errorHandler.Handle(ex, "Add Task", "Add failed.", ErrorSeverity.Error);
            }
        }

        public async Task<bool> LockTaskAsync(int id, string user)
        {
            try
            {
                HttpResponseMessage response = await m_httpClient.PostAsJsonAsync($"api/tasks/{id}/lock", user);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                m_errorHandler.Handle(ex, "LockTaskAsync", "Edit failed.", ErrorSeverity.Error);
                return false;
            }
        }

        public async Task<bool> UnlockTaskAsync(int id, string user)
        {
            try
            {
                HttpResponseMessage response = await m_httpClient.PostAsJsonAsync($"api/tasks/{id}/unlock", user);
                response.EnsureSuccessStatusCode();
                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, $"UnlockTaskAsync failed");
                return false;
            }
        }

        public async Task Update(TaskItem task)
        {
            try
            {
                HttpResponseMessage response = await m_httpClient.PutAsJsonAsync($"api/tasks/{task.Id}", task);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                m_errorHandler.Handle(ex, "Update failed", "Failed to update task.", ErrorSeverity.Error);
            }
        }

        public async Task Delete(int id)
        {
            try
            {
                HttpResponseMessage response = await m_httpClient.DeleteAsync($"api/tasks/{id}");
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                m_errorHandler.Handle(ex, "Delete failed", "Failed to delete task.", ErrorSeverity.Error);
            }
        }

        public async Task UpdateTaskComplition(int id, bool isComplete)
        {
            try
            {
                HttpResponseMessage response = await m_httpClient.PutAsJsonAsync($"api/tasks/{id}/complete", isComplete);
                response.EnsureSuccessStatusCode();
            }
            catch (Exception ex)
            {
                m_errorHandler.Handle(ex, "Update task complition failed",
                                $"Failed to set task complition to '{isComplete}'.", ErrorSeverity.Error);
            }
        }
    }
}
