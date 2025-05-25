
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;
using TodoApp.Server.Data;
using TodoApp.Server.Hubs;
using TodoApp.Server.Services;
using TodoApp.Shared.Model;

namespace TodoApp.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            ConfigureServices(builder.Services, builder.Configuration);

            WebApplication app = builder.Build();

            // Configure the HTTP request pipeline
            ConfigurePipeline(app);

            app.Run();
        }

        private static void ConfigureServices(IServiceCollection services, ConfigurationManager config)
        {
            // Set reference handling for the 
            services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.JsonSerializerOptions.WriteIndented = true;
            });
            services.AddSignalR().AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
                options.PayloadSerializerOptions.WriteIndented = true;
            });

            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
            services.AddSignalR();

            services.AddDbContext<TodoDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));

            services.AddScoped<TaskItemsService>();
            services.AddSingleton<ConcurrentQueue<TaskItem>>();
            // services.AddHostedService<TaskOperationsWorker>(); // Optional worker
        }

        private static void ConfigurePipeline(WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // app.UseHttpsRedirection();
            // app.UseAuthorization();

            app.MapControllers();
            app.MapHub<TaskItemsHub>("/taskitemshub");
        }
    }
}
