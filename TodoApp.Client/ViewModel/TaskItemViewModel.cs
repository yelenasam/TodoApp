using System.Collections.ObjectModel;
using TodoApp.Shared.Model;

namespace TodoApp.Client.ViewModel
{
    public class TaskItemViewModel : ViewModelBase
    {
        private readonly TaskItem m_taskItem;
        private readonly string m_currentUser;
        private bool m_isInEditMode;
        private readonly ObservableCollection<TagViewModel> m_availableTags;


        public bool IsNew { get; set; } = false;
        public TaskItem TaskItem => m_taskItem;
        public ObservableCollection<TagViewModel> Tags => m_availableTags;

        public TaskItemViewModel(TaskItem taskItem, string currentUser, ObservableCollection<TagViewModel> availableTags, 
                                                    bool isNew = false)
        {
            m_taskItem = taskItem;
            m_currentUser = currentUser;
            m_availableTags = availableTags;
            IsNew = isNew;
            Refresh();
        }

        // Indicates state when the task already locked by the client and he can edit the data and send update
        public bool IsInEditMode
        {
            get => m_isInEditMode;
            set
            {
                m_isInEditMode = value;
                OnPropertyChanged();
                Refresh();
            }
        }
        public int Id => m_taskItem.Id;
        public string TagsText => string.Join(", ", m_taskItem.Tags.Select(t => t.Name));
        public string LockedByDisplayText
        {
            get
            {
                if (!m_taskItem.IsLocked)
                    return string.Empty;

                return IsInEditMode ? "Locked by you" : $"Locked by: {m_taskItem.LockedBy}";
            }
        }
        public string? UserName => m_taskItem.User?.Username;
        public string? LockedBy => m_taskItem.LockedBy;
        public bool IsEditable => !m_taskItem.IsLocked || (m_taskItem.IsLocked && m_taskItem.LockedBy == m_currentUser);
        public bool CanDelete => !IsNew && IsEditable;
        public string? Title
        {
            get => m_taskItem.Title;
            set
            {
                m_taskItem.Title = value ?? string.Empty;
                OnPropertyChanged();
            }
        }
        public string? Description
        {
            get => m_taskItem.Description;
            set
            {
                m_taskItem.Description = value;
                OnPropertyChanged();
            }
        }
        public int? Priority
        {
            get => m_taskItem.Priority;
            set
            {
                m_taskItem.Priority = value;
                OnPropertyChanged();
            }
        }
        public DateTime? DueDate
        {
            get => m_taskItem.DueDate;
            set
            {
                m_taskItem.DueDate = value;
                OnPropertyChanged();
            }
        }
        public bool IsComplete
        {
            get => m_taskItem.IsComplete;
            set
            {
                m_taskItem.IsComplete = value;
                OnPropertyChanged();
            }
        }
        public User? User
        {
            get => m_taskItem.User;
            set
            {
                m_taskItem.User = value;
                m_taskItem.UserId = value?.Id;
                OnPropertyChanged();
                OnPropertyChanged(nameof(UserName));
            }
        }
        public string RowBackgroundColor
        {
            get
            {
                if (!m_taskItem.IsLocked)
                    return "White";
                else if (IsEditable)
                    return "MediumAquamarine";
                else
                    return "LightGray";
            }
        }

        public void UpdateTaskItem(TaskItem updatedTask, IEnumerable<User> availableUsers)
        {
            Title = updatedTask.Title;
            Description = updatedTask.Description;
            Priority = updatedTask.Priority;
            DueDate = updatedTask.DueDate;
            IsComplete = updatedTask.IsComplete;

            User = availableUsers.FirstOrDefault(u => u.Id == updatedTask.UserId);
            m_taskItem.IsLocked = updatedTask.IsLocked;
            m_taskItem.LockedBy = updatedTask.LockedBy;
            m_taskItem.LockedAt = updatedTask.LockedAt;

            m_taskItem.Tags = updatedTask.Tags.ToList();

            Refresh();
        }

        public void DisplayDetails()
        {
            IsInEditMode = IsNew || m_taskItem.IsLocked && LockedBy == m_currentUser;
            SyncTagsSelection();
        }

        public void SyncTagsSelection()
        {
            HashSet<int> selectedTagIds = m_taskItem.Tags.Select(t => t.Id).ToHashSet();

            foreach (TagViewModel tagVm in m_availableTags)
            {
                tagVm.IsSelected = selectedTagIds.Contains(tagVm.Tag.Id);
            }

            OnPropertyChanged(nameof(TagsText));
        }

        public void Refresh()
        {
            OnPropertyChanged(nameof(IsEditable));
            OnPropertyChanged(nameof(LockedBy));
            OnPropertyChanged(nameof(RowBackgroundColor));
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(LockedByDisplayText));
            OnPropertyChanged(nameof(TagsText));
        }
    }
}
