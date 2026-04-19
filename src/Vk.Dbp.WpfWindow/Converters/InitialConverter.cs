using System;
using System.Globalization;
using System.Windows.Data;

namespace Dabp.WpfWindow.Converters
{
    /// <summary>
    /// 将用户名转换为首字母（用于头像显示）
    /// </summary>
    public class InitialConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string username && !string.IsNullOrEmpty(username))
            {
                // 取用户名第一个字符作为头像显示
                return username[0].ToString().ToUpper();
            }
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}