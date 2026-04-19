using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Vk.Dbp.WpfWindow.Converters
{
    /// <summary>
    /// 数量转可见性转换器（数量为0时显示空状态提示）
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}