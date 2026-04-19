using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Vk.Dbp.Contracts.Events;

namespace Vk.Dbp.WpfWindow.Converters
{
    /// <summary>
    /// 告警状态转颜色转换器
    /// </summary>
    public class AlarmStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AlarmStatus status)
            {
                return status switch
                {
                    AlarmStatus.Active => new SolidColorBrush(Color.FromRgb(220, 53, 69)),     // Red
                    AlarmStatus.Acknowledged => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // Yellow
                    AlarmStatus.Resolved => new SolidColorBrush(Color.FromRgb(40, 167, 69)),   // Green
                    AlarmStatus.Ignored => new SolidColorBrush(Color.FromRgb(108, 117, 125)),  // Gray
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}