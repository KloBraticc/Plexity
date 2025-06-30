using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace Plexity.Services
{
    public class DiscordService
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private const string DISCORD_INVITE_URL = "https://discord.gg/wdmYT9WKTX";
        private const string GUILD_ID = "1234567890123456789"; // You'll need to replace this with the actual server ID
        
        static DiscordService()
        {
            // Set a user agent to avoid being blocked by Discord
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Plexity/1.0");
        }

        /// <summary>
        /// Checks if the user is in the Discord server and auto-joins if not
        /// </summary>
        /// <returns>True if user is in server or successfully joined, false otherwise</returns>
        public async Task<bool> CheckAndJoinDiscordServerAsync()
        {
            const string LOG_IDENT = "DiscordService::CheckAndJoinDiscordServer";
            
            try
            {
                App.Logger?.WriteLine(LogLevel.Info, LOG_IDENT, "Checking Discord server membership...");
                
                // Check if Discord is running
                var discordProcesses = Process.GetProcessesByName("Discord");
                if (discordProcesses.Length == 0)
                {
                    App.Logger?.WriteLine(LogLevel.Info, LOG_IDENT, "Discord not running, attempting to open invite link...");
                    return await OpenDiscordInviteAsync();
                }

                // For now, we'll assume the user needs to join and open the invite
                // In a real implementation, you'd need Discord OAuth or bot token to check membership
                App.Logger?.WriteLine(LogLevel.Info, LOG_IDENT, "Discord is running, opening invite link...");
                return await OpenDiscordInviteAsync();
            }
            catch (Exception ex)
            {
                App.Logger?.WriteLine(LogLevel.Error, LOG_IDENT, $"Error checking Discord server: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Opens the Discord invite link to join the server
        /// </summary>
        private async Task<bool> OpenDiscordInviteAsync()
        {
            const string LOG_IDENT = "DiscordService::OpenDiscordInvite";
            
            try
            {
                App.Logger?.WriteLine(LogLevel.Info, LOG_IDENT, $"Opening Discord invite: {DISCORD_INVITE_URL}");
                
                // Open the Discord invite link
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = DISCORD_INVITE_URL,
                    UseShellExecute = true
                };
                
                Process.Start(processStartInfo);
                
                // Show notification to user using Application dispatcher
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    if (Application.Current.MainWindow is Views.Windows.MainWindow mainWindow)
                    {
                        // Call the public method through reflection or make it public
                        var showNotificationMethod = typeof(Views.Windows.MainWindow).GetMethod("ShowNotification", 
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        showNotificationMethod?.Invoke(mainWindow, new object[] { "Please join our Discord server to continue using Plexity!", 5000 });
                    }
                });

                App.Logger?.WriteLine(LogLevel.Info, LOG_IDENT, "Discord invite opened successfully");
                return true;
            }
            catch (Exception ex)
            {
                App.Logger?.WriteLine(LogLevel.Error, LOG_IDENT, $"Failed to open Discord invite: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Validates the Discord invite link
        /// </summary>
        public async Task<bool> ValidateInviteLinkAsync()
        {
            const string LOG_IDENT = "DiscordService::ValidateInviteLink";
            
            try
            {
                // Extract invite code from URL
                var inviteCode = DISCORD_INVITE_URL.Split('/').Last();
                var apiUrl = $"https://discord.com/api/v10/invites/{inviteCode}";
                
                var response = await _httpClient.GetAsync(apiUrl);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var inviteData = JsonDocument.Parse(content);
                    
                    if (inviteData.RootElement.TryGetProperty("guild", out var guild))
                    {
                        var guildName = guild.GetProperty("name").GetString();
                        App.Logger?.WriteLine(LogLevel.Info, LOG_IDENT, $"Valid invite for server: {guildName}");
                        return true;
                    }
                }
                
                App.Logger?.WriteLine(LogLevel.Warning, LOG_IDENT, "Invalid Discord invite link");
                return false;
            }
            catch (Exception ex)
            {
                App.Logger?.WriteLine(LogLevel.Error, LOG_IDENT, $"Error validating invite link: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Shows a dialog asking user to join Discord server
        /// </summary>
        public void ShowJoinServerDialog()
        {
            const string message = "To use Plexity, you need to join our Discord server. Click OK to open the invite link.";
            
            var result = ConfirmDialog.Show(message, false);
            
            if (result)
            {
                _ = Task.Run(OpenDiscordInviteAsync);
            }
        }

        /// <summary>
        /// Checks if user has joined the server (simplified version)
        /// In a real implementation, this would require proper Discord API authentication
        /// </summary>
        public async Task<bool> IsUserInServerAsync()
        {
            // This is a placeholder - in reality you'd need:
            // 1. Discord OAuth to get user token
            // 2. Check user's guilds via Discord API
            // 3. Look for the specific guild ID
            
            // For now, we'll always return false to trigger the join process
            await Task.Delay(100); // Simulate API call
            return false;
        }
    }
}