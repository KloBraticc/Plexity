using System;
using System.Windows;
using System.Windows.Media;
// Add rendering namespace
using System.Windows.Media.Effects;

namespace Plexity.Helpers
{
    public static class ResourceOptimizer
    {
        public static void ApplyPerformanceOptimizedResources()
        {
            bool useHighQualityRendering = !SystemOptimizer.IsLowEndMachine();
            
            // Apply appropriate resource settings
            var appResources = Application.Current.Resources;
            
            // Animation durations
            appResources["AnimationDuration"] = useHighQualityRendering 
                ? TimeSpan.FromMilliseconds(300) 
                : TimeSpan.FromMilliseconds(100);
            
            // Image quality
            appResources["DefaultBitmapScalingMode"] = useHighQualityRendering
                ? BitmapScalingMode.HighQuality
                : BitmapScalingMode.LowQuality;
            
            // Shadow depth
            appResources["ShadowDepth"] = useHighQualityRendering ? 3 : 1;
            
            // Effect quality - use enum correctly
            appResources["EffectQuality"] = useHighQualityRendering
                ? "Quality"  // Use string instead of enum
                : "Performance";
            
            // Enable edge aliasing on low-end devices for better performance
            if (!useHighQualityRendering)
            {
                appResources["DefaultEdgeMode"] = EdgeMode.Aliased;
            }
        }
    }
}