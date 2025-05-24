using System.ComponentModel.DataAnnotations;

namespace TodoApp.Shared.Model
{
    public class TaskItem
    {
        public int Id { get; set; }
        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? Priority { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsComplete { get; set; }
        public bool IsLocked { get; set; }
        public string? LockedBy { get; set; }
        public DateTime? LockedAt { get; set; }
        // The user, task belongs to
        public int? UserId { get; set; }
        public User? User { get; set; }
        public ICollection<Tag> Tags { get; set; } = new List<Tag>();
    }
}
