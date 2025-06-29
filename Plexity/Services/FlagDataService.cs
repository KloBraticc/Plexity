using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Plexity.Services
{
    public class FlagDataService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private readonly Dictionary<string, object> _allFlags = new();
        private readonly Dictionary<string, DateTime> _flagTimestamps = new();
        private readonly Dictionary<string, object> _previousFlags = new();
        
        private static readonly string[] FlagSourceUrls = 
        {
            "https://raw.githubusercontent.com/DynamicFastFlag/DynamicFastFlag/refs/heads/main/FvaribleV2.json",
            "https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCClientBootstrapper.json",
            "https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCStudioApp.json",
            "https://raw.githubusercontent.com/MaximumADHD/Roblox-FFlag-Tracker/refs/heads/main/PCDesktopClient",
            "https://raw.githubusercontent.com/MaximumADHD/Roblox-Client-Tracker/refs/heads/roblox/FVariables.txt",
            "https://raw.githubusercontent.com/SCR00M/froststap-shi/refs/heads/main/PCDesktopClient.json",
            "https://raw.githubusercontent.com/SCR00M/froststap-shi/refs/heads/main/FVariablesV2.json",
            "https://clientsettings.roblox.com/v2/settings/application/PCDesktopClient"
        };

        public async Task<bool> FetchAllFlagsAsync()
        {
            var fetchedFlags = new Dictionary<string, object>();
            var currentTime = DateTime.Now;

            foreach (var url in FlagSourceUrls)
            {
                try
                {
                    var content = await _httpClient.GetStringAsync(url);
                    var flags = ParseFlagContent(content, url);
                    
                    foreach (var flag in flags)
                    {
                        if (!fetchedFlags.ContainsKey(flag.Key))
                        {
                            fetchedFlags[flag.Key] = flag.Value;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to fetch from {url}: {ex.Message}");
                }
            }

            // Store previous flags before updating
            _previousFlags.Clear();
            foreach (var flag in _allFlags)
            {
                _previousFlags[flag.Key] = flag.Value;
            }

            // Update main flag collection and track new flags
            foreach (var flag in fetchedFlags)
            {
                bool isNewFlag = !_allFlags.ContainsKey(flag.Key);
                _allFlags[flag.Key] = flag.Value;
                
                // Only mark as new if it's truly new (not in previous collection)
                if (isNewFlag)
                {
                    _flagTimestamps[flag.Key] = currentTime;
                }
            }

            SaveFlagsToCache();
            return fetchedFlags.Count > 0;
        }

        private Dictionary<string, object> ParseFlagContent(string content, string url)
        {
            var flags = new Dictionary<string, object>();

            try
            {
                if (url.EndsWith(".txt") || !content.TrimStart().StartsWith("{"))
                {
                    // Parse FVariables.txt format
                    var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("#"))
                            continue;

                        var parts = trimmed.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 1)
                        {
                            var flagName = parts[0];
                            var flagValue = parts.Length > 1 ? parts[1] : "true";
                            flags[flagName] = NormalizeValue(flagValue);
                        }
                    }
                }
                else
                {
                    // Parse JSON format
                    var jsonDoc = JsonDocument.Parse(content);
                    foreach (var property in jsonDoc.RootElement.EnumerateObject())
                    {
                        flags[property.Name] = NormalizeValue(property.Value);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to parse content from {url}: {ex.Message}");
            }

            return flags;
        }

        private object NormalizeValue(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Number => value.TryGetInt32(out int intVal) ? intVal : value.GetDouble(),
                JsonValueKind.String => value.GetString() ?? "",
                _ => value.ToString()
            };
        }

        private object NormalizeValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";

            value = value.Trim().Trim('"');

            if (bool.TryParse(value, out bool boolValue))
                return boolValue;

            if (int.TryParse(value, out int intValue))
                return intValue;

            if (double.TryParse(value, out double doubleValue))
                return doubleValue;

            return value;
        }

        public Dictionary<string, object> GetAllFlags() => new(_allFlags);

        public Dictionary<string, object> GetFlagsAddedInLast24Hours()
        {
            var cutoff = DateTime.Now.AddHours(-24);
            return _flagTimestamps
                .Where(kvp => kvp.Value >= cutoff)
                .ToDictionary(kvp => kvp.Key, kvp => _allFlags.ContainsKey(kvp.Key) ? _allFlags[kvp.Key] : null)
                .Where(kvp => kvp.Value != null)
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public Dictionary<string, object> FilterFlags(Dictionary<string, object> flags, bool? valueFilter)
        {
            if (!valueFilter.HasValue) return flags;

            return flags.Where(kvp => 
            {
                var value = kvp.Value.ToString()?.ToLower();
                return valueFilter.Value ? value == "true" : value == "false";
            }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public Dictionary<string, object> SearchFlags(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new Dictionary<string, object>();

            return _allFlags
                .Where(kvp => kvp.Key.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        public bool ValidateFlag(string flagName) => _allFlags.ContainsKey(flagName);

        public ValidationResult ValidateFlags(Dictionary<string, object> inputFlags)
        {
            var result = new ValidationResult();
            var duplicates = new HashSet<string>();

            // Check for duplicates in input
            var flagNames = inputFlags.Keys.ToList();
            for (int i = 0; i < flagNames.Count; i++)
            {
                for (int j = i + 1; j < flagNames.Count; j++)
                {
                    if (flagNames[i].Equals(flagNames[j], StringComparison.OrdinalIgnoreCase))
                    {
                        duplicates.Add(flagNames[i]);
                    }
                }
            }

            result.Duplicates = duplicates.ToList();

            foreach (var flag in inputFlags)
            {
                if (_allFlags.ContainsKey(flag.Key))
                {
                    result.ValidFlags[flag.Key] = _allFlags[flag.Key];
                }
                else
                {
                    result.InvalidFlags[flag.Key] = flag.Value;
                }
            }

            return result;
        }

        public string RemoveInvalidFlags(string input, ValidationResult validationResult)
        {
            if (validationResult.InvalidFlags.Count == 0)
                return input;

            var lines = input.Split('\n');
            var validLines = new List<string>();

            if (input.TrimStart().StartsWith("{"))
            {
                // JSON format - reconstruct with only valid flags
                var validJson = new Dictionary<string, object>();
                foreach (var validFlag in validationResult.ValidFlags)
                {
                    validJson[validFlag.Key] = validFlag.Value;
                }

                if (validJson.Any())
                {
                    return JsonSerializer.Serialize(validJson, new JsonSerializerOptions { WriteIndented = true });
                }
                return "{}";
            }
            else
            {
                // Line-by-line format
                foreach (var line in lines)
                {
                    var trimmed = line.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;

                    string flagName = "";
                    if (trimmed.Contains('='))
                    {
                        flagName = trimmed.Split('=', 2)[0].Trim();
                    }
                    else if (trimmed.Contains(':'))
                    {
                        flagName = trimmed.Split(':', 2)[0].Trim().Trim('"');
                    }
                    else
                    {
                        flagName = trimmed;
                    }

                    if (!validationResult.InvalidFlags.ContainsKey(flagName))
                    {
                        validLines.Add(line);
                    }
                }

                return string.Join('\n', validLines);
            }
        }

        private void SaveFlagsToCache()
        {
            try
            {
                var flagCachePath = Path.Combine(Paths.Base, "FlagCache.json");
                var timestampCachePath = Path.Combine(Paths.Base, "FlagTimestamps.json");

                var flagContent = JsonSerializer.Serialize(_allFlags, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(flagCachePath, flagContent);

                var timestampContent = JsonSerializer.Serialize(_flagTimestamps, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(timestampCachePath, timestampContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save flag cache: {ex.Message}");
            }
        }

        public void LoadCachedFlags()
        {
            try
            {
                var flagCachePath = Path.Combine(Paths.Base, "FlagCache.json");
                var timestampCachePath = Path.Combine(Paths.Base, "FlagTimestamps.json");

                if (File.Exists(flagCachePath))
                {
                    var content = File.ReadAllText(flagCachePath);
                    var cache = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
                    if (cache != null)
                    {
                        _allFlags.Clear();
                        foreach (var flag in cache)
                        {
                            _allFlags[flag.Key] = flag.Value;
                        }
                    }
                }

                if (File.Exists(timestampCachePath))
                {
                    var content = File.ReadAllText(timestampCachePath);
                    var timestamps = JsonSerializer.Deserialize<Dictionary<string, DateTime>>(content);
                    if (timestamps != null)
                    {
                        _flagTimestamps.Clear();
                        foreach (var timestamp in timestamps)
                        {
                            _flagTimestamps[timestamp.Key] = timestamp.Value;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to load flag cache: {ex.Message}");
            }
        }
    }

    public class ValidationResult
    {
        public Dictionary<string, object> ValidFlags { get; set; } = new();
        public Dictionary<string, object> InvalidFlags { get; set; } = new();
        public List<string> Duplicates { get; set; } = new();
    }
}