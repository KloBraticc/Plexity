using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Plexity
{


    /// <summary>
    /// Provides transition effects for <see cref="FrameworkElement"/>.
    /// </summary>
    public static class Transitions
    {
        private const int MaxDuration = 800;
        private const int MinDuration = 50;

        /// <summary>
        /// Applies a transition animation to the specified element.
        /// </summary>
        public static bool ApplyTransition(object element, TransitionType type, int duration)
        {
            if (element is not FrameworkElement frameworkElement)
                return false;

            // Clamp duration between 200 and MaxDuration for smooth animation speed
            duration = Math.Clamp(duration, 200, MaxDuration);
            var animationDuration = new Duration(TimeSpan.FromMilliseconds(duration));

            switch (type)
            {
                case TransitionType.FadeIn:
                    ApplyFade(frameworkElement, animationDuration);
                    break;

                case TransitionType.FadeInWithSlide:
                    ApplySlide(frameworkElement, animationDuration, 0, 30, fade: true);
                    break;

                case TransitionType.SlideBottom:
                    ApplySlide(frameworkElement, animationDuration, 0, 30);
                    break;

                case TransitionType.SlideRight:
                    ApplySlide(frameworkElement, animationDuration, 50, 0);
                    break;

                case TransitionType.SlideLeft:
                    ApplySlide(frameworkElement, animationDuration, -50, 0);
                    break;

                default:
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Fades in the element.
        /// </summary>
        private static void ApplyFade(FrameworkElement element, Duration duration)
        {
            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = duration,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
        }

        /// <summary>
        /// Applies a slide (and optional fade) animation.
        /// </summary>
        private static void ApplySlide(FrameworkElement element, Duration duration, double offsetX, double offsetY, bool fade = false)
        {
            if (element.RenderTransform is not TranslateTransform)
                element.RenderTransform = new TranslateTransform();

            element.RenderTransformOrigin = new Point(0.5, 0.5);

            var easing = new SineEase { EasingMode = EasingMode.EaseInOut };

            if (offsetX != 0)
            {
                var animX = new DoubleAnimation
                {
                    From = offsetX,
                    To = 0,
                    Duration = duration,
                    EasingFunction = easing
                };

                element.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animX);
            }

            if (offsetY != 0)
            {
                var animY = new DoubleAnimation
                {
                    From = offsetY,
                    To = 0,
                    Duration = duration,
                    EasingFunction = easing
                };

                element.RenderTransform.BeginAnimation(TranslateTransform.YProperty, animY);
            }

            if (fade)
            {
                var fadeIn = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = duration,
                    EasingFunction = easing
                };

                element.BeginAnimation(UIElement.OpacityProperty, fadeIn);
            }
        }
    }
}
