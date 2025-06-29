using System;
using System.IO;
using System.Text.Json;
using Plexity.Utility;

namespace Plexity
{
    public class JsonManager<T> where T : class, new()
    {
        public T OriginalProp { get; set; } = new();

        public T Prop { get; set; } = new();

        public virtual string ClassName => typeof(T).Name;
        public string? LastFileHash { get; private set; }

        public virtual string FileLocation => Path.Combine(Paths.Base, $"{ClassName}.json");

        public virtual string LOG_IDENT_CLASS => $"JsonManager<{ClassName}>";

        public virtual void Load(bool alertFailure = true)
        {
            string LOG_IDENT = $"{LOG_IDENT_CLASS}::Load";

            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Loading from {FileLocation}...");

            try
            {
                T? settings = JsonSerializer.Deserialize<T>(File.ReadAllText(FileLocation));

                if (settings is null)
                    throw new ArgumentNullException("Deserialization returned null");

                Prop = settings;

                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Loaded successfully!");
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Failed to load!");
                App.Logger.WriteException(LOG_IDENT, ex);
                Save();
            }
        }

        public virtual void Save()
        {
            string LOG_IDENT = $"{LOG_IDENT_CLASS}::Save";

            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Saving to {FileLocation}...");

            Directory.CreateDirectory(Path.GetDirectoryName(FileLocation)!);

            try
            {
                File.WriteAllText(FileLocation, JsonSerializer.Serialize(Prop, new JsonSerializerOptions { WriteIndented = true }));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Failed to save");
                App.Logger.WriteException(LOG_IDENT, ex);
                return;
            }

            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Save complete!");
        }

        public bool HasFileOnDiskChanged()
        {
            return LastFileHash != MD5Hash.FromFile(FileLocation);
        }
    }
}
