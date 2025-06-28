using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using Plexity.Enums.FlagPresets;

namespace Plexity.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        // Forward properties to FastFlagsViewModel
        private FastFlagsViewModel? _fastFlagsViewModel;
        
        public FastFlagsViewModel FastFlags => _fastFlagsViewModel ??= new FastFlagsViewModel();

        // UseFastFlagManager property
        public bool UseFastFlagManager
        {
            get => FastFlags.UseFastFlagManager;
            set => SetProperty(FastFlags.UseFastFlagManager, value, () => FastFlags.UseFastFlagManager = value);
        }

        // ResetConfiguration property
        public bool ResetConfiguration
        {
            get => FastFlags.ResetConfiguration;
            set => SetProperty(FastFlags.ResetConfiguration, value, () => FastFlags.ResetConfiguration = value);
        }

        // GPU properties
        public IReadOnlyDictionary<string, string?> GPUs => FastFlags.GPUs;
        public string SelectedGPU
        {
            get => FastFlags.SelectedGPU;
            set => SetProperty(FastFlags.SelectedGPU, value, () => FastFlags.SelectedGPU = value);
        }

        // CPU Thread properties
        public IReadOnlyDictionary<string, string?> CpuThreads => FastFlags.CpuThreads;
        public KeyValuePair<string, string?> SelectedCpuThreads
        {
            get => FastFlags.SelectedCpuThreads;
            set => SetProperty(FastFlags.SelectedCpuThreads, value, () => FastFlags.SelectedCpuThreads = value);
        }

        // Refresh Rate properties
        public IReadOnlyDictionary<RefreshRate, string> RefreshRates => FastFlags.RefreshRates;
        public RefreshRate SelectedRefreshRate
        {
            get => FastFlags.SelectedRefreshRate;
            set => SetProperty(FastFlags.SelectedRefreshRate, value, () => FastFlags.SelectedRefreshRate = value);
        }

        // Boolean toggle properties
        public bool SkipDiskChecks
        {
            get => FastFlags.SkipDiskChecks;
            set => SetProperty(FastFlags.SkipDiskChecks, value, () => FastFlags.SkipDiskChecks = value);
        }

        public bool Threading
        {
            get => FastFlags.Threading;
            set => SetProperty(FastFlags.Threading, value, () => FastFlags.Threading = value);
        }

        // SSAO properties
        public IReadOnlyDictionary<SSAOMode, string> SSAOLevels => FastFlags.SSAOLevels;
        public SSAOMode SelectedSSAOLevel
        {
            get => FastFlags.SelectedSSAOLevel;
            set => SetProperty(FastFlags.SelectedSSAOLevel, value, () => FastFlags.SelectedSSAOLevel = value);
        }

        // MSAA properties
        public IReadOnlyDictionary<MSAAMode, string> MSAALevels => FastFlags.MSAALevels;
        public MSAAMode SelectedMSAALevel
        {
            get => FastFlags.SelectedMSAALevel;
            set => SetProperty(FastFlags.SelectedMSAALevel, value, () => FastFlags.SelectedMSAALevel = value);
        }

        // Texture Quality properties
        public IReadOnlyDictionary<TextureQuality, string> TextureQualities => FastFlags.TextureQualities;
        public TextureQuality SelectedTextureQuality
        {
            get => FastFlags.SelectedTextureQuality;
            set => SetProperty(FastFlags.SelectedTextureQuality, value, () => FastFlags.SelectedTextureQuality = value);
        }

        // Rendering properties
        public bool MinimalRendering
        {
            get => FastFlags.MinimalRendering;
            set => SetProperty(FastFlags.MinimalRendering, value, () => FastFlags.MinimalRendering = value);
        }

        public bool NewFpsSystem
        {
            get => FastFlags.NewFpsSystem;
            set => SetProperty(FastFlags.NewFpsSystem, value, () => FastFlags.NewFpsSystem = value);
        }

        public int FramerateLimit
        {
            get => FastFlags.FramerateLimit;
            set => SetProperty(FastFlags.FramerateLimit, value, () => FastFlags.FramerateLimit = value);
        }

        public bool FixDisplayScaling
        {
            get => FastFlags.FixDisplayScaling;
            set => SetProperty(FastFlags.FixDisplayScaling, value, () => FastFlags.FixDisplayScaling = value);
        }

        // Rendering Modes
        public IReadOnlyDictionary<RenderingMode, string> RenderingModes => FastFlags.RenderingModes;
        public RenderingMode SelectedRenderingMode
        {
            get => FastFlags.SelectedRenderingMode;
            set => SetProperty(FastFlags.SelectedRenderingMode, value, () => FastFlags.SelectedRenderingMode = value);
        }

        public bool TaskSchedulerAvoidingSleep
        {
            get => FastFlags.TaskSchedulerAvoidingSleep;
            set => SetProperty(FastFlags.TaskSchedulerAvoidingSleep, value, () => FastFlags.TaskSchedulerAvoidingSleep = value);
        }

        public bool SkipHighResTextures
        {
            get => FastFlags.SkipHighResTextures;
            set => SetProperty(FastFlags.SkipHighResTextures, value, () => FastFlags.SkipHighResTextures = value);
        }

        // Telemetry properties
        public bool DisableTelemetry
        {
            get => FastFlags.DisableTelemetry;
            set => SetProperty(FastFlags.DisableTelemetry, value, () => FastFlags.DisableTelemetry = value);
        }

        public bool DisableWebview2Telemetry
        {
            get => FastFlags.DisableWebview2Telemetry;
            set => SetProperty(FastFlags.DisableWebview2Telemetry, value, () => FastFlags.DisableWebview2Telemetry = value);
        }

        public bool DisableVoiceChatTelemetry
        {
            get => FastFlags.DisableVoiceChatTelemetry;
            set => SetProperty(FastFlags.DisableVoiceChatTelemetry, value, () => FastFlags.DisableVoiceChatTelemetry = value);
        }

        public bool BlockTencent
        {
            get => FastFlags.BlockTencent;
            set => SetProperty(FastFlags.BlockTencent, value, () => FastFlags.BlockTencent = value);
        }

        // UI and visual properties
        public bool RainbowTheme
        {
            get => FastFlags.RainbowTheme;
            set => SetProperty(FastFlags.RainbowTheme, value, () => FastFlags.RainbowTheme = value);
        }

        public bool RedFont
        {
            get => FastFlags.RedFont;
            set => SetProperty(FastFlags.RedFont, value, () => FastFlags.RedFont = value);
        }

        public bool Pseudolocalization
        {
            get => FastFlags.Pseudolocalization;
            set => SetProperty(FastFlags.Pseudolocalization, value, () => FastFlags.Pseudolocalization = value);
        }

        public bool NewCamera
        {
            get => FastFlags.NewCamera;
            set => SetProperty(FastFlags.NewCamera, value, () => FastFlags.NewCamera = value);
        }

        public bool Layered
        {
            get => FastFlags.Layered;
            set => SetProperty(FastFlags.Layered, value, () => FastFlags.Layered = value);
        }

        public string? FakeVerify
        {
            get => FastFlags.FakeVerify;
            set => SetProperty(FastFlags.FakeVerify, value, () => FastFlags.FakeVerify = value);
        }

        // Graphics properties
        public bool DisablePostFX
        {
            get => FastFlags.DisablePostFX;
            set => SetProperty(FastFlags.DisablePostFX, value, () => FastFlags.DisablePostFX = value);
        }

        public bool DisablePlayerShadows
        {
            get => FastFlags.DisablePlayerShadows;
            set => SetProperty(FastFlags.DisablePlayerShadows, value, () => FastFlags.DisablePlayerShadows = value);
        }

        public bool DisableTerrainTextures
        {
            get => FastFlags.DisableTerrainTextures;
            set => SetProperty(FastFlags.DisableTerrainTextures, value, () => FastFlags.DisableTerrainTextures = value);
        }

        public bool RemoveGrass
        {
            get => FastFlags.RemoveGrass;
            set => SetProperty(FastFlags.RemoveGrass, value, () => FastFlags.RemoveGrass = value);
        }

        public bool DisableSky
        {
            get => FastFlags.DisableSky;
            set => SetProperty(FastFlags.DisableSky, value, () => FastFlags.DisableSky = value);
        }

        public bool EnableGraySky
        {
            get => FastFlags.EnableGraySky;
            set => SetProperty(FastFlags.EnableGraySky, value, () => FastFlags.EnableGraySky = value);
        }

        public bool GrayAvatar
        {
            get => FastFlags.GrayAvatar;
            set => SetProperty(FastFlags.GrayAvatar, value, () => FastFlags.GrayAvatar = value);
        }

        public bool DisableVignette
        {
            get => FastFlags.DisableVignette;
            set => SetProperty(FastFlags.DisableVignette, value, () => FastFlags.DisableVignette = value);
        }

        public bool BetterShadows
        {
            get => FastFlags.BetterShadows;
            set => SetProperty(FastFlags.BetterShadows, value, () => FastFlags.BetterShadows = value);
        }

        // Lighting properties
        public IReadOnlyDictionary<LightingMode, string> LightingModes => FastFlags.LightingModes;
        public LightingMode SelectedLightingMode
        {
            get => FastFlags.SelectedLightingMode;
            set => SetProperty(FastFlags.SelectedLightingMode, value, () => FastFlags.SelectedLightingMode = value);
        }

        public int UpdateRateMenu
        {
            get => FastFlags.UpdateRateMenu;
            set => SetProperty(FastFlags.UpdateRateMenu, value, () => FastFlags.UpdateRateMenu = value);
        }

        // Helper method for property setting
        private void SetProperty<T>(T currentValue, T newValue, System.Action setter, [CallerMemberName] string? propertyName = null)
        {
            if (!EqualityComparer<T>.Default.Equals(currentValue, newValue))
            {
                setter();
                OnPropertyChanged(propertyName);
            }
        }
    }
}
