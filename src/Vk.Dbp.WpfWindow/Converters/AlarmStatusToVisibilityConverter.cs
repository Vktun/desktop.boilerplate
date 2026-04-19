using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Vk.Dbp.Contracts.Events;

namespace Vk.Dbp.WpfWindow.Converters
{
    /// <summary>
    /// 告警状态转可见性转换器（Active状态显示确认按钮）
    /// </summary>
    public class AlarmStatusToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AlarmStatus status)
            {
                return status == AlarmStatus.Active ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}