using System;
using System.Globalization;
using System.Windows.Data;

namespace SlideExtractor.WPF.Resources.Styles;

public class NullOrEmptyToBoolConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
		string.IsNullOrWhiteSpace(value as string) ? false : true;

	public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
		throw new NotSupportedException();
}
