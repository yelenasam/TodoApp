using System.ComponentModel.DataAnnotations;

namespace TodoApp.Shared.Model
{
    public class User
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = string.Empty;
        [MaxLength(256)]
        public string? PasswordHash { get; set; } // TODO:
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
