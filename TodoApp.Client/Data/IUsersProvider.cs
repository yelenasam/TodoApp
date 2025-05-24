using TodoApp.Shared.Model;

namespace TodoApp.Client.Data
{
    public interface IUsersProvider
    {
        Task<List<User>> GetAllAsync();
    }
}
