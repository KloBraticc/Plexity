using System;
using System.Windows;

namespace Plexity.Helpers
{
    public static class UIDensityManager
    {
        public enum DensityMode
        {
            Compact,
            Regular,
            Expanded
        }

        // Paths to the resource dictionaries for each density mode
        private static readonly Uri CompactResourceUri = new Uri("pack://application:,,,/Resources/Density/CompactDensity.xaml");
        private static readonly Uri RegularResourceUri = new Uri("pack://application:,,,/Resources/Density/RegularDensity.xaml");
        private static readonly Uri ExpandedResourceUri = new Uri("pack://application:,,,/Resources/Density/ExpandedDensity.xaml");

        public static void ApplyDensityMode(DensityMode mode)
        {
            var application = Application.Current;
            if (application == null)
                return;

            var resourceDictionaries = application.Resources.MergedDictionaries;

            // Remove any existing density resource dictionaries
            for (int i = resourceDictionaries.Count - 1; i >= 0; i--)
            {
                var dictionary = resourceDictionaries[i];
                if (dictionary.Source == CompactResourceUri ||
                    dictionary.Source == RegularResourceUri ||
                    dictionary.Source == ExpandedResourceUri)
                {
                    resourceDictionaries.RemoveAt(i);
                }
            }

            // Create and add the new density dictionary based on mode
            ResourceDictionary newDictionary = new ResourceDictionary();

            switch (mode)
            {
                case DensityMode.Compact:
                    newDictionary.Source = CompactResourceUri;
                    break;
                case DensityMode.Regular:
                    newDictionary.Source = RegularResourceUri;
                    break;
                case DensityMode.Expanded:
                    newDictionary.Source = ExpandedResourceUri;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mode), mode, null);
            }

            resourceDictionaries.Add(newDictionary);
        }
    }
}
