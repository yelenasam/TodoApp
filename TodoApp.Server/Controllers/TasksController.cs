using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TodoApp.Server.Hubs;
using TodoApp.Server.Services;
using TodoApp.Shared.Model;

namespace TodoApp.Server.Controllers
{
    /// <summary>
    /// Web API controller - Exposes HTTP endpoints (GET, POST, PUT) to the clients
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly TaskItemsService m_service;
        private readonly IHubContext<TaskItemsHub> m_hubContext;

        public TasksController(TaskItemsService service, IHubContext<TaskItemsHub> hubContext)
        {
            m_service = service;
            m_hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IEnumerable<TaskItem>> GetAllTasks()
        {
            return await m_service.GetAllAsync();
        }

        [HttpPost]
        public async Task<IActionResult> AddTask(TaskItem task)
        {
            var saved = await m_service.AddAsync(task);
            await m_hubContext.Clients.All.SendAsync("TaskAdded", saved);
            return Ok(saved);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(int id, TaskItem updatedTask)
        {
            var saved = await m_service.UpdateAsync(id, updatedTask);
            if (saved == null)
                return NotFound();

            // Updating Clients by SignalR
            await m_hubContext.Clients.All.SendAsync("TaskUpdated", saved);
            // Unlock the task
            await UnlockTask(id, null);
            return Ok(saved);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(int id)
        {
            bool success = await m_service.DeleteAsync(id);
            if (!success)
                return NotFound();

            // Updating Clients by SignalR
            await m_hubContext.Clients.All.SendAsync("TaskDeleted", id);
            return Ok();
        }

        [HttpPut("{id}/complete")]
        public async Task<IActionResult> UpdateTaskComplition(int id, [FromBody] bool isCompleted)
        {
            var saved = await m_service.SetTaskCompletionAsync(id, isCompleted);
            if (saved == null)
                return NotFound();

            // Updating Clients by SignalR
            await m_hubContext.Clients.All.SendAsync("TaskUpdated", saved);
            return Ok(saved);
        }

        [HttpPost("{id}/lock")]
        public async Task<IActionResult> LockTask(int id, [FromBody] string user)
        {
            var success = await m_service.LockAsync(id, user);
            if (!success)
                return BadRequest("Task is already locked or not found.");

            await m_hubContext.Clients.All.SendAsync("TaskLocked", id, user);
            return Ok();
        }

        [HttpPost("{id}/unlock")]
        public async Task<IActionResult> UnlockTask(int id, [FromBody] string? user)
        {
            //TODO: check user
            var success = await m_service.UnlockAsync(id);
            if (!success)
                return BadRequest("Task is already unlocked or not found.");

            await m_hubContext.Clients.All.SendAsync("TaskUnlocked", id, user);
            return Ok();
        }
    }
}

