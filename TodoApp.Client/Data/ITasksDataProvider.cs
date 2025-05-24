using TodoApp.Shared.Model;

namespace TodoApp.Client.Data
{
    public interface ITasksDataProvider
    {
        Task<IEnumerable<TaskItem>?> GetAllAsync();
        Task Add(TaskItem task);
        Task Update(TaskItem task);
        Task Delete(int id);
        Task UpdateTaskComplition(int id, bool isComplete);
        Task<bool> LockTaskAsync(int id, string user);
        Task<bool> UnlockTaskAsync(int id, string user);

        event Action<TaskItem>? TaskAdded;
        event Action<TaskItem>? TaskUpdated;
        event Action<int, string>? TaskLocked;
        event Action<int>? TaskDeleted;
        event Action<int>? TaskUnlocked;

        Task ConnectToServerAsync();
    }
}