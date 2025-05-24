using System.Diagnostics;
using System.Net.Http.Json;
using System.Text.Json;
using System.Collections.Concurrent;
using TodoApp.Shared.Model;

namespace TodoApp.ClientSimulator
{
    public class ClientSimulator
    {
        private readonly HttpClient m_httpClient;
        private readonly string m_userName;
        private static readonly Random s_random = new();
        private readonly int m_totalRounds;
        private readonly int m_totalAdditions;
        private readonly int m_completionToggles;

        private static readonly ConcurrentBag<ClientStats> s_stats = new();

        public ClientSimulator(string baseUrl, string userName, int totalRounds = 3, int additions = 2, int completionToggles = 2)
        {
            m_httpClient = new HttpClient { BaseAddress = new Uri(baseUrl) };
            m_userName = userName;
            m_totalRounds = totalRounds;
            m_totalAdditions = additions;
            m_completionToggles = completionToggles;
        }

        public async Task RunAsync()
        {
            var stats = new ClientStats { UserName = m_userName };
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < m_totalAdditions; i++)
                await AddNewTaskAsync(stats);

            for (int round = 1; round <= m_totalRounds; round++)
            {
                var tasksResponse = await m_httpClient.GetAsync("api/tasks");
                if (!tasksResponse.IsSuccessStatusCode)
                {
                    stats.Failures++;
                    continue;
                }

                var json = await tasksResponse.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
                };

                var tasks = JsonSerializer.Deserialize<List<TaskItem>>(json, options);
                if (tasks == null || tasks.Count == 0) continue;

                foreach (var task in tasks.OrderBy(t => t.Id))
                {
                    var lockWatch = Stopwatch.StartNew();
                    var lockResp = await m_httpClient.PostAsJsonAsync($"api/tasks/{task.Id}/lock", m_userName);
                    lockWatch.Stop();
                    stats.TotalLockTime += lockWatch.ElapsedMilliseconds;

                    if (lockResp.IsSuccessStatusCode)
                    {
                        var updateWatch = Stopwatch.StartNew();
                        task.Description += $" (edited by {m_userName})";
                        var updateResp = await m_httpClient.PutAsJsonAsync($"api/tasks/{task.Id}", task);
                        updateWatch.Stop();
                        stats.TotalUpdateTime += updateWatch.ElapsedMilliseconds;

                        if (updateResp.IsSuccessStatusCode)
                            stats.Updated++;
                        else
                            stats.Failures++;
                    }
                    else if (!task.IsLocked)
                    {
                        for (int toggle = 0; toggle < m_completionToggles; toggle++)
                        {
                            var completeWatch = Stopwatch.StartNew();
                            task.IsComplete = !task.IsComplete;
                            var completeResp = await m_httpClient.PutAsJsonAsync($"api/tasks/{task.Id}/complete", task.IsComplete);
                            completeWatch.Stop();
                            stats.TotalCompleteTime += completeWatch.ElapsedMilliseconds;

                            if (completeResp.IsSuccessStatusCode)
                                stats.Completed++;
                            else
                                stats.Failures++;

                            await Task.Delay(s_random.Next(30, 100));
                        }
                    }

                    await Task.Delay(s_random.Next(50, 150));
                }
            }

            stopwatch.Stop();
            stats.Elapsed = stopwatch.ElapsedMilliseconds;
            s_stats.Add(stats);
        }

        private async Task AddNewTaskAsync(ClientStats stats)
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
            if (response.IsSuccessStatusCode)
                stats.Added++;
            else
                stats.Failures++;
        }

        public static async Task Main(string[] args)
        {
            const string baseUrl = "https://localhost:7186";
            const int clientCount = 10;
            const int totalRounds = 3;
            const int additionsPerClient = 2;
            const int completionToggles = 2;

            var tasks = new List<Task>();
            for (int i = 0; i < clientCount; i++)
            {
                string user = $"User{i}";
                var simulator = new ClientSimulator(baseUrl, user, totalRounds, additionsPerClient, completionToggles);
                tasks.Add(simulator.RunAsync());
            }

            await Task.WhenAll(tasks);

            Console.WriteLine("All client simulations completed.\n");
            var totalStats = new ClientStats();

            foreach (var stat in s_stats)
            {
                totalStats.Added += stat.Added;
                totalStats.Updated += stat.Updated;
                totalStats.Completed += stat.Completed;
                totalStats.Failures += stat.Failures;
                totalStats.TotalLockTime += stat.TotalLockTime;
                totalStats.TotalUpdateTime += stat.TotalUpdateTime;
                totalStats.TotalCompleteTime += stat.TotalCompleteTime;
                totalStats.Elapsed += stat.Elapsed;
            }

            Console.WriteLine($"Total clients: {clientCount}");
            Console.WriteLine($"Avg Added: {totalStats.Added / clientCount}");
            Console.WriteLine($"Avg Updated: {totalStats.Updated / clientCount}");
            Console.WriteLine($"Avg Completed: {totalStats.Completed / clientCount}");
            Console.WriteLine($"Avg Failures: {totalStats.Failures / clientCount}");
            Console.WriteLine($"Avg Lock Time: {totalStats.TotalLockTime / Math.Max(totalStats.Updated, 1)} ms");
            Console.WriteLine($"Avg Update Time: {totalStats.TotalUpdateTime / Math.Max(totalStats.Updated, 1)} ms");
            Console.WriteLine($"Avg Completion Time: {totalStats.TotalCompleteTime / Math.Max(totalStats.Completed, 1)} ms");
            Console.WriteLine($"Avg Elapsed Time Per Client: {totalStats.Elapsed / clientCount} ms");
        }
    }

    public class ClientStats
    {
        public string UserName { get; set; } = string.Empty;
        public int Added { get; set; } = 0;
        public int Updated { get; set; } = 0;
        public int Completed { get; set; } = 0;
        public int Failures { get; set; } = 0;
        public long TotalLockTime { get; set; } = 0;
        public long TotalUpdateTime { get; set; } = 0;
        public long TotalCompleteTime { get; set; } = 0;
        public long Elapsed { get; set; } = 0;
    }
}
