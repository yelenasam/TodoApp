using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using TodoApp.Shared.Model;

namespace TodoApp.Client.Data
{
    public class UsersProvider : IUsersProvider
    {
        private readonly HttpClient m_httpClient;
        private readonly ILogger<UsersProvider> m_logger;

        public UsersProvider(ILogger<UsersProvider> logger)
        {
            m_logger = logger;
            m_httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7186") };
        }

        public async Task<List<User>> GetAllAsync()
        {
            try
            {
                return await m_httpClient.GetFromJsonAsync<List<User>>("api/users") ?? new List<User>();
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to load users");
                return new List<User>();
            }
        }
    }
}
