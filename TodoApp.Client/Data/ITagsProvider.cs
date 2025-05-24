using TodoApp.Shared.Model;

namespace TodoApp.Client.Data
{
    public interface ITagsProvider
    {
        Task<List<Tag>> GetAllAsync();
    }
}
