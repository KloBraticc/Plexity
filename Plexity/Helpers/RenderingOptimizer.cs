using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Plexity.Helpers
{
    public static class RenderingOptimizer
    {
        public static void OptimizeElement(FrameworkElement element)
        {
            // Cache rendered content when possible
            RenderOptions.SetCachingHint(element, CachingHint.Cache);
            
            // Apply to ScrollViewers for better scrolling performance
            if (element is ScrollViewer scrollViewer)
            {
                ScrollViewer.SetIsDeferredScrollingEnabled(scrollViewer, true);
            }
            
            // Optimize image rendering
            if (element is Image image)
            {
                RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.LowQuality);
            }
        }
        
        public static void OptimizeContainer(Panel container)
        {
            OptimizeElement(container);
            
            // Apply hardware acceleration when appropriate
            if (container.Children.Count > 10)
            {
                RenderOptions.SetEdgeMode(container, EdgeMode.Aliased);
            }
        }
    }
}