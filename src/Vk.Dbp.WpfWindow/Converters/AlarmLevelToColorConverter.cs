using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using Vk.Dbp.Contracts.Events;

namespace Vk.Dbp.WpfWindow.Converters
{
    /// <summary>
    /// 告警等级转颜色转换器
    /// </summary>
    public class AlarmLevelToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AlarmLevel level)
            {
                return level switch
                {
                    AlarmLevel.Critical => new SolidColorBrush(Color.FromRgb(220, 53, 69)), // Red
                    AlarmLevel.Warning => new SolidColorBrush(Color.FromRgb(255, 153, 0)), // Orange
                    AlarmLevel.Info => new SolidColorBrush(Color.FromRgb(23, 162, 184)),   // Blue
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