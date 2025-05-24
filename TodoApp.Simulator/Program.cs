using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using TodoApp.Shared.Model;

namespace TodoApp.ClientSimulator
{
    public class ClientSimulator
    {
        private readonly HttpClient m_httpClient;
        private readonly string m_userName;
        private readonly StreamWriter m_logWriter;
        private static readonly Random s_random = new();
        private int m_tasksAdded = 0;
        private int m_tasksUpdated = 0;
        private int m_tasksCompleted = 0;
        private int m_totalRounds;
        private int m_totalAdditions;
        private int m_completionToggles;
        private long m_totalLockTime = 0;
        private long m_totalUpdateTime = 0;
        private long m_totalCompleteTime = 0;

        public ClientSimulator(string baseUrl, string userName, int totalRounds = 3, int additions = 2, int completionToggles = 2)
        {
            m_httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            m_userName = userName;
            m_totalRounds = totalRounds;
            m_totalAdditions = additions;
            m_completionToggles = completionToggles;
            m_logWriter = new StreamWriter($"log_{userName}.txt", append: false) { AutoFlush = true };
        }

        public async Task RunAsync()
        {
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < m_totalAdditions; i++)
                await AddNewTaskAsync();

            for (int round = 1; round <= m_totalRounds; round++)
            {
                await LogAsync($"Round {round} started");

                //var tasks = await m_httpClient.GetFromJsonAsync<List<TaskItem>>("api/tasks");
                HttpResponseMessage response = await m_httpClient.GetAsync("api/tasks");
                if (!response.IsSuccessStatusCode)
                {
                    string errorContent = await response.Content.ReadAsStringAsync();
                    await LogAsync($"Failed to fetch tasks: {response.StatusCode} - {errorContent}");
                    return;
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                };

                string json = await response.Content.ReadAsStringAsync();
                var tasks = JsonSerializer.Deserialize<List<TaskItem>>(json, options);
                if (tasks == null || tasks.Count == 0)
                    continue;

                foreach (var task in tasks.OrderBy(t => t.Id))
                {
                    var lockWatch = Stopwatch.StartNew();
                    var lockResp = await m_httpClient.PostAsJsonAsync($"api/tasks/{task.Id}/lock", m_userName);
                    lockWatch.Stop();
                    m_totalLockTime += lockWatch.ElapsedMilliseconds;

                    if (lockResp.IsSuccessStatusCode)
                    {
                        var updateWatch = Stopwatch.StartNew();
                        task.Description += $" (edited by {m_userName})";
                        await m_httpClient.PutAsJsonAsync($"api/tasks/{task.Id}", task);
                        updateWatch.Stop();
                        m_totalUpdateTime += updateWatch.ElapsedMilliseconds;

                        await LogAsync($"Updated task {task.Id}");
                        m_tasksUpdated++;
                    }
                    else if (!task.IsLocked)
                    {
                        for (int toggle = 0; toggle < m_completionToggles; toggle++)
                        {
                            var completeWatch = Stopwatch.StartNew();
                            task.IsComplete = !task.IsComplete;
                            await m_httpClient.PutAsJsonAsync($"api/tasks/{task.Id}/complete", task.IsComplete);
                            completeWatch.Stop();
                            m_totalCompleteTime += completeWatch.ElapsedMilliseconds;

                            await LogAsync($"Toggled completion for task {task.Id} to {task.IsComplete}");
                            m_tasksCompleted++;
                            await Task.Delay(s_random.Next(30, 100));
                        }
                    }

                    await Task.Delay(s_random.Next(50, 150));
                }
            }

            stopwatch.Stop();
            await LogAsync($"Client {m_userName} finished in {stopwatch.ElapsedMilliseconds} ms");
            await LogAsync($"Added: {m_tasksAdded}, Updated: {m_tasksUpdated}, Completion toggles: {m_tasksCompleted}");
            await LogAsync($"Avg Lock Time: {(m_tasksUpdated > 0 ? m_totalLockTime / m_tasksUpdated : 0)} ms");
            await LogAsync($"Avg Update Time: {(m_tasksUpdated > 0 ? m_totalUpdateTime / m_tasksUpdated : 0)} ms");
            await LogAsync($"Avg Completion Time: {(m_tasksCompleted > 0 ? m_totalCompleteTime / m_tasksCompleted : 0)} ms");
            m_logWriter.Close();
        }

        private async Task AddNewTaskAsync()
        {
            var task = new TaskItem
            {
                Title = $"Task from {m_userName}",
                Description = "Auto-generated task",
                DueDate = DateTime.UtcNow.AddDays(s_random.Next(1, 10)),
                IsComplete = false,
                Priority = s_random.Next(1, 4),
                //Tags = new List<Tag> { new Tag { Name = "simulated" } },
                //User = new User { Username = m_userName }
            };

            var response = await m_httpClient.PostAsJsonAsync("api/tasks", task);
            if (response.IsSuccessStatusCode) m_tasksAdded++;
            await LogAsync(response.IsSuccessStatusCode ? $"Created task successfully" : $"Failed to create task: {response.StatusCode}");
        }

        private async Task LogAsync(string message)
        {
            var logEntry = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Console.WriteLine(logEntry);
            await m_logWriter.WriteLineAsync(logEntry);
        }

        public static async Task Main(string[] args)
        {
            const string baseUrl = "https://localhost:7186";
            const int clientCount = 10;
            const int totalRounds = 5;
            const int additionsPerClient = 1;
            const int completionToggles = 5;

            var tasks = new List<Task>();
            for (int i = 0; i < clientCount; i++)
            {
                string user = $"User{i}";
                var simulator = new ClientSimulator(baseUrl, user, totalRounds, additionsPerClient, completionToggles);
                tasks.Add(simulator.RunAsync());
            }

            await Task.WhenAll(tasks);
            Console.WriteLine("All client simulations completed.");
        }
    }
}
