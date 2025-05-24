using TodoApp.Shared.Model;

namespace TodoApp.Client.ViewModel
{
    public class TagViewModel : ViewModelBase
    {
        public Tag Tag { get; }
        private bool m_isSelected;

        public bool IsSelected
        {
            get => m_isSelected;
            set
            {
                if (m_isSelected != value)
                {
                    m_isSelected = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Name => Tag.Name;

        public TagViewModel(Tag tag, bool isSelected = false)
        {
            Tag = tag;
            m_isSelected = isSelected;
        }
    }

}
