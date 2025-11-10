using System;
using System.Globalization;
using System.Windows.Data;

namespace KoreanSoftKeyboard.WPF
{
    public class RatioConverter : IValueConverter
    {
        // 把高度 * 比例 = 宽度
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double height)
            {
                double ratio = 1.8; // 你希望的宽高比，可自行调整
                return height * ratio;
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
