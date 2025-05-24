using Microsoft.EntityFrameworkCore;
using TodoApp.Shared.Model;

namespace TodoApp.Server.Data
{
    public class TodoDbContext : DbContext
    {
        public TodoDbContext(DbContextOptions<TodoDbContext> options) : base(options) { }
        public DbSet<TaskItem> TaskItems { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Many-to-many relationship between TaskItem and Tag
            modelBuilder.Entity<TaskItem>()
                .HasMany(t => t.Tags)
                .WithMany(tag => tag.TaskItems)
                .UsingEntity<Dictionary<string, object>>("TaskTags",
                right => right.HasOne<Tag>().WithMany()
                                            .HasForeignKey("TagId")
                                            .OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<TaskItem>().WithMany()
                                               .HasForeignKey("TaskItemId")
                                               .OnDelete(DeleteBehavior.Cascade))
                                               .HasKey("TaskItemId", "TagId"); // composite PK


            // TaskItem to User (many-to-one)
            modelBuilder.Entity<TaskItem>()
                .HasOne(t => t.User)
                .WithMany(u => u.Tasks)
                .HasForeignKey(t => t.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
