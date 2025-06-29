using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Plexity.Resources
{
    public enum TransitionType
    {
        None,
        FadeIn,
        FadeInWithSlide,
        SlideBottom,
        SlideRight,
        SlideLeft
    }

    public static class Transitions
    {
        private const int MaxDuration = 800;
        private const int MinDuration = 50;

        public static bool Apply(UIElement element, TransitionType type, int duration)
        {
            if (element is not FrameworkElement fe || type == TransitionType.None || duration < MinDuration)
                return false;

            duration = Math.Clamp(duration, MinDuration, MaxDuration);
            var animationDuration = new Duration(TimeSpan.FromMilliseconds(duration));

            // Set initial states for animation
            switch (type)
            {
                case TransitionType.FadeIn:
                    fe.Opacity = 0;
                    ApplyFade(fe, animationDuration);
                    break;

                case TransitionType.FadeInWithSlide:
                    PrepareTransform(fe, 0, 30);
                    fe.Opacity = 0;
                    ApplySlide(fe, animationDuration, 0, 30, fade: true);
                    break;

                case TransitionType.SlideBottom:
                    PrepareTransform(fe, 0, 30);
                    ApplySlide(fe, animationDuration, 0, 30);
                    break;

                case TransitionType.SlideRight:
                    PrepareTransform(fe, 50, 0);
                    ApplySlide(fe, animationDuration, 50, 0);
                    break;

                case TransitionType.SlideLeft:
                    PrepareTransform(fe, -50, 0);
                    ApplySlide(fe, animationDuration, -50, 0);
                    break;

                default:
                    return false;
            }

            return true;
        }

        private static void PrepareTransform(FrameworkElement element, double offsetX, double offsetY)
        {
            if (element.RenderTransform is not TranslateTransform)
                element.RenderTransform = new TranslateTransform();

            element.RenderTransformOrigin = new Point(0.5, 0.5);
            var transform = (TranslateTransform)element.RenderTransform;
            transform.X = offsetX;
            transform.Y = offsetY;
        }

        private static void ApplyFade(UIElement element, Duration duration)
        {
            var fadeAnimation = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = duration,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            };

            element.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
        }

        private static void ApplySlide(UIElement element, Duration duration, double fromX, double fromY, bool fade = false)
        {
            var easing = new SineEase { EasingMode = EasingMode.EaseInOut };

            if (fromX != 0 && element.RenderTransform is TranslateTransform transformX)
            {
                var animX = new DoubleAnimation
                {
                    From = fromX,
                    To = 0,
                    Duration = duration,
                    EasingFunction = easing
                };
                transformX.BeginAnimation(TranslateTransform.XProperty, animX);
            }

            if (fromY != 0 && element.RenderTransform is TranslateTransform transformY)
            {
                var animY = new DoubleAnimation
                {
                    From = fromY,
                    To = 0,
                    Duration = duration,
                    EasingFunction = easing
                };
                transformY.BeginAnimation(TranslateTransform.YProperty, animY);
            }

            if (fade)
                ApplyFade(element, duration);
        }
    }
}
