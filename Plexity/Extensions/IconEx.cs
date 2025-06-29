using System;
using System.Drawing;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Plexity.Enums;

namespace Plexity.Extensions
{
    public static class IconEx
    {
        public static ImageSource GetImageSource(bool handleException = true)
        {
            using MemoryStream stream = new();
            stream.Position = 0; // Reset stream position before reading

            if (handleException)
            {
                try
                {
                    return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                }
                catch (Exception ex)
                {
                    App.Logger.WriteException("IconEx::GetImageSource", ex);
                    return null;  // Return null or a fallback ImageSource here
                }
            }
            else
            {
                return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
            }
        }
    }
}
