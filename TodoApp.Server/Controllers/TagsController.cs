using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApp.Server.Data;
using TodoApp.Shared.Model;

namespace TodoApp.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly TodoDbContext m_context;

        public TagsController(TodoDbContext context)
        {
            m_context = context;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tag>>> GetAll()
        {
            List<Tag> tags = await m_context.Tags.ToListAsync();
            return Ok(tags);
        }
    }
}
