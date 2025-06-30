using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Plexity.Views.Pages
{
    // Enhance the converter to support animation and smooth transitions
    public class ProgressToWidthConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values?.Length == 3 &&
                values[0] is double width &&
                values[1] is double height &&
                values[2] is CornerRadius cornerRadius &&
                width > 0 && height > 0)
            {
                var radius = Math.Max(0, cornerRadius.TopLeft);
                // Use the correct constructor overload
                return new RectangleGeometry(new Rect(0, 0, width, height), radius, radius);
            }

            return DependencyProperty.UnsetValue;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
