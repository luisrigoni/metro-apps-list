using System;
using System.Windows;
using System.Windows.Data;

namespace MetroAppsList
{
    public class ForegroundTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch (value.ToString())
            {
                case "light":
                    return "#FFFFFF";
                default:
                    return "#000000";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}