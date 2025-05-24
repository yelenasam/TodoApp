using System.ComponentModel.DataAnnotations;

namespace TodoApp.Shared.Model
{
    public class Tag
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
        public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();
    }
}
