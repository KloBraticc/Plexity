using System;
using System.Windows;
using System.Windows.Media;

namespace Plexity.Helpers
{
    public static class ThemeHelper
    {
        public static void ApplyLightTheme(Application app)
        {
            // Update brushes for light mode
            app.Resources["BackgroundPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(248, 248, 248));
            app.Resources["BackgroundSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(237, 237, 237));
            app.Resources["BackgroundTertiaryBrush"] = new SolidColorBrush(Color.FromRgb(224, 224, 224));
            
            app.Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(26, 26, 26));
            app.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 45));
            app.Resources["TextTertiaryBrush"] = new SolidColorBrush(Color.FromRgb(102, 102, 102));
            
            app.Resources["BorderPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(192, 192, 192));
            app.Resources["BorderSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(208, 208, 208));
        }

        public static void ApplyDarkTheme(Application app)
        {
            // Update brushes for dark mode
            app.Resources["BackgroundPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            app.Resources["BackgroundSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(45, 45, 48));
            app.Resources["BackgroundTertiaryBrush"] = new SolidColorBrush(Color.FromRgb(58, 58, 60));
            
            app.Resources["TextPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(255, 255, 240));
            app.Resources["TextSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(232, 232, 232));
            app.Resources["TextTertiaryBrush"] = new SolidColorBrush(Color.FromRgb(200, 200, 200));
            
            app.Resources["BorderPrimaryBrush"] = new SolidColorBrush(Color.FromRgb(92, 92, 95));
            app.Resources["BorderSecondaryBrush"] = new SolidColorBrush(Color.FromRgb(64, 64, 64));
        }
    }
}