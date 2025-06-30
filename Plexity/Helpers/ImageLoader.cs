using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Plexity.Helpers
{
    public static class ImageLoader
    {
        private static readonly Dictionary<string, BitmapImage> _imageCache = new();
        
        public static async Task<BitmapImage> LoadImageAsync(string uri, bool useCache = true)
        {
            if (useCache && _imageCache.TryGetValue(uri, out var cachedImage))
            {
                return cachedImage;
            }
            
            return await Task.Run(() => 
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = new Uri(uri);
                image.EndInit();
                image.Freeze(); // Make it thread-safe
                
                if (useCache)
                {
                    _imageCache[uri] = image;
                }
                
                return image;
            });
        }
        
        public static void ClearCache()
        {
            _imageCache.Clear();
        }
    }
}