using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Diagnostics;
using TodoApp.Server.Data;
using TodoApp.Shared.Model;

namespace TodoApp.Server.Services
{
    /// <summary>
    /// 
    /// </summary>
    public class TaskItemsService
    {
        private readonly TodoDbContext m_context;
        private readonly ILogger<TaskItemsService> m_logger;

        public TaskItemsService(TodoDbContext context, ILogger<TaskItemsService> logger)
        {
            m_context = context;
            m_logger = logger;
        }

        public async Task<IEnumerable<TaskItem>> GetAllAsync()
        {
            m_logger.LogInformation("Retrieving all tasks...");
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                List<TaskItem> result = await m_context.TaskItems.Include(t => t.User)
                                                                 .Include(t => t.Tags)
                                                                 .ToListAsync();
                sw.Stop();
                m_logger.LogInformation($"Retrieved {result.Count} tasks in {sw.ElapsedMilliseconds} ms.");
                return result;
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to retrieve tasks.");
                throw;
            }
        }

        public async Task<TaskItem> AddAsync(TaskItem taskItem)
        {
            m_logger.LogInformation($"Adding task '{taskItem.Title}'...");
            try
            {
                if (taskItem.User != null)
                {
                    User? existingUser = await m_context.Users.FindAsync(taskItem.User.Id);
                    taskItem.User = existingUser;
                }

                HashSet<int> tagIds = taskItem.Tags.Select(t => t.Id).ToHashSet();
                List<Tag> existingTags = await m_context.Tags.Where(t => tagIds.Contains(t.Id))
                                                             .ToListAsync();

                taskItem.Tags = existingTags;

                m_context.TaskItems.Add(taskItem);
                await m_context.SaveChangesAsync();
                m_logger.LogInformation($"Task '{taskItem.Title}' added with ID '{taskItem.Id}'");
                return taskItem;
            }
            catch (Exception ex)
            {
                m_logger.LogError($"Failed adding task '{taskItem.Title}' with error: '{ex}'");
                throw;
            }
        }

        public async Task<TaskItem?> UpdateAsync(int id, TaskItem updatedTask)
        {
            m_logger.LogInformation($"Updating task '{id}'...");
            try
            {
                TaskItem? taskItem = await m_context.TaskItems.Include(t => t.Tags)
                                                              .Include(t => t.User)
                                                              .FirstOrDefaultAsync(t => t.Id == id);

                if (taskItem == null)
                {
                    m_logger.LogInformation($"Task '{id}' was not found");
                    return null;
                }

                taskItem.Title = updatedTask.Title;
                taskItem.Description = updatedTask.Description;
                taskItem.Priority = updatedTask.Priority;
                taskItem.DueDate = updatedTask.DueDate;
                taskItem.IsComplete = updatedTask.IsComplete;

                // Handle user updates
                if (updatedTask.User != null)
                {
                    if (taskItem.User == null || taskItem.User.Id != updatedTask.User.Id)
                    {
                        User? existingUser = await m_context.Users.FindAsync(updatedTask.User.Id);
                        taskItem.User = existingUser;
                    }
                }
                else
                {
                    taskItem.User = updatedTask.User;
                }

                // Handle tags updates
                var updatedTagIds = updatedTask.Tags.Select(t => t.Id).ToHashSet();
                var currentTagIds = taskItem.Tags.Select(t => t.Id).ToHashSet();

                bool tagsChanged = !updatedTagIds.SetEquals(currentTagIds);
                if (tagsChanged)
                {
                    List<Tag> allRelevantTags = await m_context.Tags.Where(t => updatedTagIds.Contains(t.Id))
                                                                    .ToListAsync();

                    foreach (Tag tag in allRelevantTags)
                    {
                        if (!currentTagIds.Contains(tag.Id))
                        {
                            taskItem.Tags.Add(tag);
                        }
                    }
                    // Handle removed tags
                    var tagsToRemove = taskItem.Tags.Where(t => !updatedTagIds.Contains(t.Id)).ToList();
                    foreach (var tag in tagsToRemove)
                    {
                        taskItem.Tags.Remove(tag);
                    }
                }

                await m_context.SaveChangesAsync();
                m_logger.LogInformation($"Task '{taskItem.Title}', id='{id}' updated.");

                return taskItem;
            }
            catch (Exception ex)
            {
                m_logger.LogError($"Failed update task '{id}' with error: '{ex}'");
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            m_logger.LogInformation($"DeleteAsync:  Deleting task '{id}'");
            try
            {
                TaskItem? taskItem = await m_context.TaskItems.FindAsync(id);

                if (taskItem == null)
                {
                    m_logger.LogInformation($"DeleteAsync: Task '{id}' was not found");
                    return true;
                }

                m_context.TaskItems.Remove(taskItem);
                m_logger.LogInformation($"DeleteAsync: Task '{taskItem.Title}' id='{id}' - deleted.");
                await m_context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError($"Failed update complition state for task '{id}' with error: '{ex}'");
                throw;
            }
        }

        public async Task<TaskItem?> SetTaskCompletionAsync(int id, bool isCompleted)
        {
            m_logger.LogInformation($"Updating task '{id}' complition state to complited='{isCompleted}'");
            try
            {
                //TaskItem? taskItem = await m_context.TaskItems.FindAsync(id);
                TaskItem? taskItem = await m_context.TaskItems.Include(t => t.User)
                                                              .Include(t => t.Tags)
                                                              .FirstOrDefaultAsync(t => t.Id == id);

                if (taskItem == null)
                {
                    m_logger.LogInformation($"Task '{id}' was not found");
                    return null;
                }

                taskItem.IsComplete = isCompleted;

                await m_context.SaveChangesAsync();
                m_logger.LogInformation($"Task '{taskItem.Title}' complited='{isCompleted}', id='{id}' updated.");
                await UnlockAsync(id);
                return taskItem;
            }
            catch (Exception ex)
            {
                m_logger.LogError($"Failed update complition state for task '{id}' with error: '{ex}'");
                throw;
            }
        }

        /// <summary>
        /// Prevent simultaneous edits (task being edited will be locked)
        /// Support multiple concurrent clients
        /// Using Transaction Locking with UPDLOCK
        /// </summary>
        /// <param name="id"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<bool> LockAsync(int id, string user)
        {
            m_logger.LogInformation($"Locking task '{id}'...");
            try
            {
                using IDbContextTransaction transaction = await m_context.Database.BeginTransactionAsync();

                TaskItem? taskItem = await m_context.TaskItems
                                    .FromSqlRaw("SELECT * FROM TaskItems WITH (UPDLOCK) WHERE Id = {0}", id)
                                    .FirstOrDefaultAsync();

                if (taskItem == null)
                {
                    m_logger.LogInformation($"Task '{id}' was not found");
                    return false;
                }
                if (taskItem.IsLocked)
                {
                    m_logger.LogInformation($"Task '{id}' already locked by '{taskItem.LockedBy}'");
                    if (user != taskItem.LockedBy)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }

                taskItem.IsLocked = true;
                taskItem.LockedBy = user;
                taskItem.LockedAt = DateTime.UtcNow;

                await m_context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError($"Failed lock task '{id}' with error: {ex}");
                throw;
            }
        }

        public async Task<bool> UnlockAsync(int id)
        {
            m_logger.LogInformation($"Unocking task '{id}'...");
            try
            {
                TaskItem? taskItem = await m_context.TaskItems.FindAsync(id);

                if (taskItem == null)
                {
                    m_logger.LogInformation($"Task '{id}' was not found");
                    return false;
                }

                taskItem.IsLocked = false;
                taskItem.LockedBy = null;
                taskItem.LockedAt = null;
                await m_context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                m_logger.LogError($"Failed to unlock task '{id}' with error: {ex}");
                throw;
            }
        }
    }
}
