using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using TodoApp.Client.Command;
using TodoApp.Client.Data;
using TodoApp.Client.Services;
using TodoApp.Shared.Model;

namespace TodoApp.Client.ViewModel
{
    public class TasksViewModel : ViewModelBase
    {
        private readonly ITasksDataProvider m_tasksDataProvider;
        private readonly IUsersProvider m_usersProvider;
        private readonly ITagsProvider m_tagsProvider;
        private readonly IErrorHandler m_errorHandler;
        private TaskItemViewModel? m_previousSelectedTask;
        private TaskItemViewModel? m_selectedTask;
        private string m_userName;
        private TaskItemViewModel? m_pendingNewTask;

        public ObservableCollection<TaskItemViewModel> TaskItems { get; } = new();
        public ObservableCollection<User> AvailableUsers { get; } = new();
        public ObservableCollection<TagViewModel> AvailableTags { get; } = new();

        public DelegateCommand AddCommand { get; }
        public DelegateCommand DeleteCommand { get; }
        public DelegateCommand UpdateCommand { get; }
        public DelegateCommand SetCompleteCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand CancelEditCommand { get; }

        public TaskItemViewModel? SelectedTask
        {
            get => m_selectedTask;
            set
            {
                if (m_selectedTask != value)
                {
                    m_previousSelectedTask = m_selectedTask;
                    m_selectedTask = value;

                    if (m_selectedTask != null)
                    {
                        m_selectedTask.User = AvailableUsers.FirstOrDefault(u => u.Id == m_selectedTask.TaskItem.UserId);
                        m_selectedTask?.DisplayDetails();
                    }
                    OnPropertyChanged();
                }
            }
        }

        public string UserName => m_userName;

        public TasksViewModel(ITasksDataProvider tasksDataProvider, IUsersProvider usersProvider,
                                ITagsProvider tagsProvider, IErrorHandler errorHandler)
        {
            m_tasksDataProvider = tasksDataProvider;
            m_usersProvider = usersProvider;
            m_tagsProvider = tagsProvider;
            m_errorHandler = errorHandler;
            m_userName = "Anonymus";

            AddCommand = new DelegateCommand(Add);
            DeleteCommand = new DelegateCommand(Delete);
            UpdateCommand = new DelegateCommand(Update);
            EditCommand = new DelegateCommand(Edit);
            CancelEditCommand = new DelegateCommand(CancelEdit);
            SetCompleteCommand = new DelegateCommand(SetCompleteAsync);

            RegisterTaskEvents();
            m_errorHandler = errorHandler;
        }
        public void UserLogin(string username)
        {
            m_userName = username;
            ConnectAndLoadAsync();
        }

        private async void ConnectAndLoadAsync()
        {
            await m_tasksDataProvider.ConnectToServerAsync();
            Task usersLoad = LoadUsers();
            Task tagsLoad = LoadTags();
            Task tasksLoad = LoadTasks();

            await Task.WhenAll(tasksLoad, usersLoad, tagsLoad);
            LoadUiState();
        }

        private void RegisterTaskEvents()
        {
            m_tasksDataProvider.TaskAdded += OnTaskAdded;
            m_tasksDataProvider.TaskUpdated += OnTaskUpdated;
            m_tasksDataProvider.TaskDeleted += OnTaskDeleted;
            m_tasksDataProvider.TaskLocked += OnTaskLocked;
            m_tasksDataProvider.TaskUnlocked += OnTaskUnlocked;
        }

        private void OnTaskDeleted(int id)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TaskItemViewModel? existing = TaskItems.FirstOrDefault(t => t.Id == id);
                if (existing != null)
                {
                    if (SelectedTask?.Id == id)
                    {
                        SelectedTask = m_previousSelectedTask;
                    }
                    TaskItems.Remove(existing);
                }
            });
        }

        private void OnTaskAdded(TaskItem newTask)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var taskVm = new TaskItemViewModel(newTask, UserName, AvailableTags);
                TaskItems.Add(taskVm);

                if (m_pendingNewTask != null && m_pendingNewTask.Title == newTask.Title)
                {
                    m_pendingNewTask = null;
                    SelectedTask = taskVm;
                }
            });
        }

        private void OnTaskUpdated(TaskItem updatedTask)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TaskItemViewModel? existing = TaskItems.FirstOrDefault(t => t.Id == updatedTask.Id);
                if (existing != null)
                {
                    existing.UpdateTaskItem(updatedTask, AvailableUsers);
                }
            });
        }

        private void OnTaskLocked(int taskId, string user)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TaskItemViewModel? existing = TaskItems.FirstOrDefault(t => t.Id == taskId);
                if (existing != null)
                {
                    existing.TaskItem.IsLocked = true;
                    existing.TaskItem.LockedBy = user;
                    existing.Refresh();
                    OnPropertyChanged();
                }
            });
        }

        private void OnTaskUnlocked(int taskId)
        {
            Application.Current.Dispatcher.InvokeAsync(() =>
            {
                TaskItemViewModel? existing = TaskItems.FirstOrDefault(t => t.Id == taskId);
                if (existing != null)
                {
                    existing.TaskItem.IsLocked = false;
                    existing.TaskItem.LockedBy = null;
                    existing.Refresh();
                    OnPropertyChanged();
                }
            });
        }

        private async Task LoadTasks()
        {
            IEnumerable<TaskItem>? tasks = await m_tasksDataProvider.GetAllAsync();
            if (tasks != null)
            {
                foreach (var task in tasks)
                {
                    TaskItems.Add(new TaskItemViewModel(task, UserName, AvailableTags));
                }
            }
        }

        private async Task LoadUsers()
        {
            List<User> users = await m_usersProvider.GetAllAsync();

            AvailableUsers.Clear();
            foreach (User user in users)
            {
                AvailableUsers.Add(user);
            }
        }

        private async Task LoadTags()
        {
            List<Tag> tags = await m_tagsProvider.GetAllAsync();

            AvailableTags.Clear();
            foreach (Tag tag in tags)
            {
                AvailableTags.Add(new TagViewModel(tag)); ;
            }
        }

        private async void Edit(object? obj)
        {
            if (SelectedTask is null)
                return;

            bool locked = await m_tasksDataProvider.LockTaskAsync(SelectedTask.Id, m_userName);
            if (locked)
            {
                SelectedTask.IsInEditMode = true;
            }
            else
            {
                m_errorHandler.Handle(null, "TasksViewModel - Lock failed",
                        "Task already locked by someone else.", ErrorSeverity.Warning);
            }
        }

        private async void CancelEdit(object? obj)
        {
            if (SelectedTask is null)
                return;

            if (SelectedTask.IsNew)
            {
                m_pendingNewTask = null;
                SelectedTask = m_previousSelectedTask;
                return;
            }

            bool unlocked = await m_tasksDataProvider.UnlockTaskAsync(SelectedTask.Id, m_userName);

            if (unlocked)
            {
                SelectedTask.IsInEditMode = false;
            }
        }

        private void Add(object? obj)
        {
            TaskItem newTask = new TaskItem() { Title = "New Task" };
            m_pendingNewTask = new TaskItemViewModel(newTask, UserName, AvailableTags, isNew: true);
            m_previousSelectedTask = SelectedTask;
            SelectedTask = m_pendingNewTask;
        }

        private void Update(object? obj)
        {
            if (SelectedTask is null || string.IsNullOrWhiteSpace(SelectedTask.Title))
                return;

            SelectedTask.TaskItem.Tags = AvailableTags.Where(vm => vm.IsSelected)
                                                        .Select(vm => vm.Tag)
                                                        .ToList();

            if (SelectedTask.IsNew)
            {
                m_tasksDataProvider.Add(SelectedTask.TaskItem);
            }
            else
            {
                m_tasksDataProvider.Update(SelectedTask.TaskItem);
            }
            SelectedTask.IsInEditMode = false;
        }

        private async void SetCompleteAsync(object? obj)
        {
            if (SelectedTask is null)
                return;

            SelectedTask.IsComplete = !SelectedTask.IsComplete;
            await m_tasksDataProvider.UpdateTaskComplition(SelectedTask.Id, SelectedTask.IsComplete);
        }

        private void Delete(object? obj)
        {
            if (SelectedTask != null)
            {
                m_tasksDataProvider.Delete(SelectedTask.Id);
            }
        }

        private const string SettingsFile = "ui_state.json";

        public void SaveUiState()
        {
            UiState state = new UiState
            {
                SelectedTaskId = SelectedTask?.Id,
                IsEditing = SelectedTask?.IsInEditMode ?? false
            };

            string json = JsonSerializer.Serialize(state,
                           new JsonSerializerOptions { WriteIndented = true });

            File.WriteAllText(SettingsFile, json);
        }

        public void LoadUiState()
        {
            if (!File.Exists(SettingsFile))
                return;

            UiState? state =
                JsonSerializer.Deserialize<UiState>(File.ReadAllText(SettingsFile));

            if (state?.SelectedTaskId is int taskId)
            {
                SelectedTask = TaskItems.FirstOrDefault(t => t.Id == taskId);

                if (state.IsEditing && SelectedTask is not null)
                    SelectedTask.IsInEditMode = true;
            }
        }
    }

    internal sealed class UiState
    {
        public int? SelectedTaskId { get; set; }
        public bool IsEditing { get; set; }
    }
}

