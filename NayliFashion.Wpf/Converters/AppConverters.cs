using System.Globalization;
using System.Windows;
using System.Windows.Data;
using NayliFashion.Services.Helpers;

namespace NayliFashion.Wpf.Converters;

public class CurrencyFormatterConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return $"{d:N2} دج";
        if (value is double db)
            return $"{db:N2} دج";
        return "0.00 دج";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class PopularCentimesConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal d)
            return $"[{ArabicTafqeetHelper.ToPopularCentimes(d)}]";
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class BooleanToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = false;
        if (value is bool flag)
            b = flag;
        else if (value is string str)
            b = !string.IsNullOrWhiteSpace(str);
        else if (value != null)
            b = true;

        if (parameter is string p && p == "Inverse")
            b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool isNotNull = value != null;
        if (value is string s)
            isNotNull = !string.IsNullOrWhiteSpace(s);
        if (parameter is string p && p == "Inverse")
            isNotNull = !isNotNull;
        return isNotNull ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}
