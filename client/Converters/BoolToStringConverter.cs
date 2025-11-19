using System.Globalization;
using System.Windows.Data;

namespace client.Converters
{
    public class BoolToStringConverter : IValueConverter
    {
        public string TrueValue { get; set; } = "Отключиться";
        public string FalseValue { get; set; } = "Подключиться";

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
