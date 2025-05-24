using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Server.Data;
using TodoApp.Shared.Model;

namespace TodoApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly TodoDbContext m_context;

        public UsersController(TodoDbContext context)
        {
            m_context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetAll()
        {
            List<User> users = await m_context.Users.ToListAsync();
            return Ok(users);
        }
    }
}
