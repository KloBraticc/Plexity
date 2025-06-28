using System;
using System.Windows;
using Plexity.Models.Persistable;

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

        public static void ApplyDensityMode(DensityMode mode)
        {
            var application = Application.Current;
            if (application == null)
                return;

            var resourceDictionaries = application.Resources.MergedDictionaries;

            // Define the density resource paths
            string compactResourcePath = "pack://application:,,,/Resources/Density/CompactDensity.xaml";
            string regularResourcePath = "pack://application:,,,/Resources/Density/RegularDensity.xaml";
            string expandedResourcePath = "pack://application:,,,/Resources/Density/ExpandedDensity.xaml";

            // Remove existing density dictionaries
            for (int i = resourceDictionaries.Count - 1; i >= 0; i--)
            {
                var dictionary = resourceDictionaries[i];
                if (dictionary.Source != null && 
                    (dictionary.Source.ToString() == compactResourcePath ||
                     dictionary.Source.ToString() == regularResourcePath ||
                     dictionary.Source.ToString() == expandedResourcePath))
                {
                    resourceDictionaries.RemoveAt(i);
                }
            }

            // Add the appropriate density dictionary
            ResourceDictionary newDictionary = new ResourceDictionary();
            switch (mode)
            {
                case DensityMode.Compact:
                    newDictionary.Source = new Uri(compactResourcePath);
                    break;
                case DensityMode.Regular:
                    newDictionary.Source = new Uri(regularResourcePath);
                    break;
                case DensityMode.Expanded:
                    newDictionary.Source = new Uri(expandedResourcePath);
                    break;
            }

            application.Resources.MergedDictionaries.Add(newDictionary);
        }
    }
}