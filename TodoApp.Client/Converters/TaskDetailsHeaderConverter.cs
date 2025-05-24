using System.Globalization;
using System.Windows.Data;

namespace TodoApp.Client.Converters
{
    public class TaskDetailsHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isNew)
                return isNew ? "New Task Details" : "Task Details";

            return "Task Details";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

