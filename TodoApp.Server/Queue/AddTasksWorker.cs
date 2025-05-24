using System.Collections.Concurrent;
using TodoApp.Server.Data;
using TodoApp.Shared.Model;

namespace TodoApp.Server.WorkersQueue;

public class AddingTasksWorker : BackgroundService
{
    private readonly ConcurrentQueue<TaskItem> m_jobs;
    private readonly IServiceScopeFactory m_scopeFactory;
    private readonly ILogger<AddingTasksWorker> m_logger;

    public AddingTasksWorker(IServiceScopeFactory scopeFactory, ILogger<AddingTasksWorker> logger)
    {
        m_jobs = new ConcurrentQueue<TaskItem>();
        m_scopeFactory = scopeFactory;
        m_logger = logger;
    }

    public void AddTask(TaskItem newTask)
    {
        m_logger.LogInformation($"Adding new taskItem to the queue '{newTask.Title}'...");
        m_jobs.Enqueue(newTask);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        m_logger.LogInformation("Task adding worker started");

        while (!stoppingToken.IsCancellationRequested)
        {
            if (m_jobs.TryDequeue(out TaskItem? taskToAdd))
            {
                if (taskToAdd == null)
                    continue;

                try
                {
                    using var scope = m_scopeFactory.CreateScope();
                    var context = scope.ServiceProvider.GetRequiredService<TodoDbContext>();

                    context.TaskItems.Add(taskToAdd);

                    await context.SaveChangesAsync(stoppingToken);
                    m_logger.LogInformation("Add job Completed");
                }
                catch (Exception ex)
                {
                    m_logger.LogError(ex, "Failed add job");
                }
            }
            else
            {
                await Task.Delay(500, stoppingToken);
            }
        }
    }
}

