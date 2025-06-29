using System;
using System.Globalization;
using System.Windows.Data;
using Wpf.Ui.Appearance;

namespace Plexity.Helpers
{
    /// <summary>
    /// Converts an <see cref="ApplicationTheme"/> enum value to a boolean based on a parameter string
    /// and vice versa. Useful for binding enum values to UI elements like radio buttons or toggles.
    /// </summary>
    internal class EnumToBooleanConverter : IValueConverter
    {
        /// <summary>
        /// Converts an enum value to a boolean by comparing it with the enum name provided as a parameter.
        /// </summary>
        /// <param name="value">The current enum value (expected to be of type <see cref="ApplicationTheme"/>).</param>
        /// <param name="targetType">The target binding type (ignored).</param>
        /// <param name="parameter">The enum name string to compare with.</param>
        /// <param name="culture">The culture info (ignored).</param>
        /// <returns>True if <paramref name="value"/> matches the enum name given in <paramref name="parameter"/>; otherwise, false.</returns>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string enumString || string.IsNullOrWhiteSpace(enumString))
            {
                throw new ArgumentException("Converter parameter must be a non-empty enum name string.", nameof(parameter));
            }

            if (value == null || !Enum.IsDefined(typeof(ApplicationTheme), value))
            {
                throw new ArgumentException($"Value must be a defined {nameof(ApplicationTheme)} enum.", nameof(value));
            }

            if (!Enum.TryParse<ApplicationTheme>(enumString, out var enumValue))
            {
                throw new ArgumentException($"Parameter '{enumString}' is not a valid {nameof(ApplicationTheme)} enum name.", nameof(parameter));
            }

            return enumValue.Equals(value);
        }

        /// <summary>
        /// Converts a boolean back to an enum value based on the enum name provided as a parameter.
        /// </summary>
        /// <param name="value">The boolean value from the UI element.</param>
        /// <param name="targetType">The target binding type (expected to be <see cref="ApplicationTheme"/>).</param>
        /// <param name="parameter">The enum name string to convert to.</param>
        /// <param name="culture">The culture info (ignored).</param>
        /// <returns>The enum value corresponding to the enum name if <paramref name="value"/> is true; otherwise, returns Binding.DoNothing.</returns>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter is not string enumString || string.IsNullOrWhiteSpace(enumString))
            {
                throw new ArgumentException("Converter parameter must be a non-empty enum name string.", nameof(parameter));
            }

            if (value is not bool boolValue)
            {
                return Binding.DoNothing;
            }

            if (!boolValue)
            {
                // If the checkbox/radio button is unchecked, do not change the source
                return Binding.DoNothing;
            }

            if (!Enum.TryParse<ApplicationTheme>(enumString, out var enumValue))
            {
                throw new ArgumentException($"Parameter '{enumString}' is not a valid {nameof(ApplicationTheme)} enum name.", nameof(parameter));
            }

            return enumValue;
        }
    }
}
