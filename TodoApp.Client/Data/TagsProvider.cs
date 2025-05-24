using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;
using TodoApp.Shared.Model;

namespace TodoApp.Client.Data
{
    public class TagsProvider : ITagsProvider
    {
        private readonly HttpClient m_httpClient;
        private readonly ILogger<TagsProvider> m_logger;

        public TagsProvider(ILogger<TagsProvider> logger)
        {
            m_logger = logger;
            m_httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:7186") }; 
        }

        public async Task<List<Tag>> GetAllAsync()
        {
            try
            {
                return await m_httpClient.GetFromJsonAsync<List<Tag>>("api/tags") ?? new List<Tag>(); 
            }
            catch (Exception ex)
            {
                m_logger.LogError(ex, "Failed to load tags");
                return new List<Tag>();
            }
        }
    }
}
