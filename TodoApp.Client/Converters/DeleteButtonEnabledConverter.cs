using System;
using System.Globalization;
using System.Windows.Data;

namespace TodoApp.Client.Converters
{
    public class DeleteButtonEnabledConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEditable = value is bool b && b;
            return isEditable;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
