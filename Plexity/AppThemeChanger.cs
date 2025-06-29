using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using Wpf.Ui.Appearance;

namespace Plexity.Helpers
{
    public static class AppThemeChanger
    {
        private static bool _metadataOverridden = false;
        private static bool _disableAnimations = false;
        private static ApplicationTheme _currentTheme = ApplicationTheme.Dark;

        static AppThemeChanger()
        {
            if (!_metadataOverridden)
            {
                Timeline.DesiredFrameRateProperty.OverrideMetadata(
                    typeof(Timeline),
                    new FrameworkPropertyMetadata(120));
                _metadataOverridden = true;
            }
        }

        public static bool DisableAnimations
        {
            get => _disableAnimations;
            set => _disableAnimations = value;
        }

        public static ApplicationTheme CurrentTheme => _currentTheme;

        public static async Task ChangeThemeAsync(ApplicationTheme newTheme)
        {
            if (_currentTheme == newTheme)
                return;

            var mainWindow = Application.Current?.MainWindow;

            if (mainWindow == null || DisableAnimations)
            {
                ApplicationThemeManager.Apply(newTheme);
                _currentTheme = newTheme;
                return;
            }

            const int animationDurationMs = 150; // Slightly longer for smoother transition
            var duration = TimeSpan.FromMilliseconds(animationDurationMs);
            var easing = new SineEase { EasingMode = EasingMode.EaseInOut };

            // Clear previous animations before starting new ones to avoid memory leaks
            mainWindow.BeginAnimation(UIElement.OpacityProperty, null);

            // Fade out animation
            var fadeOut = new DoubleAnimation
            {
                To = 0.1,
                Duration = new Duration(duration),
                EasingFunction = easing,
                FillBehavior = FillBehavior.Stop // Use Stop to restore property value to base after animation
            };

            var fadeOutTcs = new TaskCompletionSource<bool>();
            fadeOut.Completed += (_, __) => fadeOutTcs.TrySetResult(true);

            mainWindow.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            await fadeOutTcs.Task;

            // Apply theme after fade out
            ApplicationThemeManager.Apply(newTheme);
            _currentTheme = newTheme;

            // Fade in animation
            var fadeIn = new DoubleAnimation
            {
                To = 1.0,
                Duration = new Duration(duration),
                EasingFunction = easing,
                FillBehavior = FillBehavior.Stop
            };

            var fadeInTcs = new TaskCompletionSource<bool>();
            fadeIn.Completed += (_, __) => fadeInTcs.TrySetResult(true);

            mainWindow.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            await fadeInTcs.Task;

            // Ensure opacity is exactly 1 after animation completes
            mainWindow.Opacity = 1.0;
        }

        public static void Initialize(ApplicationTheme initialTheme, bool disableAnimations = false)
        {
            _disableAnimations = disableAnimations;
            _currentTheme = initialTheme;
            ApplicationThemeManager.Apply(initialTheme);
        }
    }
}
