using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using Plexity.Enums.FlagPresets;
using Plexity.UI.ViewModels.Bootstrapper;
using SharpDX.DXGI;
using static ICSharpCode.SharpZipLib.Zip.ExtendedUnixData;



public static class SystemInfo
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_INFO
    {
        public ushort wProcessorArchitecture;
        public ushort wReserved;
        public uint dwPageSize;
        public IntPtr lpMinimumApplicationAddress;
        public IntPtr lpMaximumApplicationAddress;
        public IntPtr dwActiveProcessorMask;
        public uint dwNumberOfProcessors;
        public uint dwProcessorType;
        public uint dwAllocationGranularity;
        public ushort wProcessorLevel;
        public ushort wProcessorRevision;
    }

    [DllImport("kernel32.dll")]
    private static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

    public static int GetLogicalProcessorCount()
    {
        GetSystemInfo(out SYSTEM_INFO sysInfo);
        return (int)sysInfo.dwNumberOfProcessors;
    }

    public enum LOGICAL_PROCESSOR_RELATIONSHIP : uint
    {
        ProcessorCore = 0,
        NumaNode = 1,
        Cache = 2,
        ProcessorPackage = 3,
        Group = 4,
        All = 0xffff
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION
    {
        public UIntPtr ProcessorMask;
        public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
        public ProcessorInfoUnion ProcessorInformation;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct ProcessorInfoUnion
    {
        [FieldOffset(0)]
        public ProcessorCore ProcessorCore;

        [FieldOffset(0)]
        public NumaNode NumaNode;

        [FieldOffset(0)]
        public CacheDescriptor Cache;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ProcessorCore
    {
        public byte Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct NumaNode
    {
        public uint NodeNumber;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct CacheDescriptor
    {
        public byte Level;
        public byte Associativity;
        public ushort LineSize;
        public uint Size;
        public uint Type;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformation(IntPtr Buffer, ref uint ReturnLength);

    public static int GetPhysicalCoreCount()
    {
        uint returnLength = 0;
        GetLogicalProcessorInformation(IntPtr.Zero, ref returnLength);

        IntPtr ptr = Marshal.AllocHGlobal((int)returnLength);
        try
        {
            if (!GetLogicalProcessorInformation(ptr, ref returnLength))
            {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
            }

            int size = Marshal.SizeOf(typeof(SYSTEM_LOGICAL_PROCESSOR_INFORMATION));
            int count = (int)returnLength / size;

            int coreCount = 0;
            for (int i = 0; i < count; i++)
            {
                IntPtr current = IntPtr.Add(ptr, i * size);
                var info = Marshal.PtrToStructure<SYSTEM_LOGICAL_PROCESSOR_INFORMATION>(current);

                if (info.Relationship == LOGICAL_PROCESSOR_RELATIONSHIP.ProcessorCore)
                {
                    coreCount++;
                }
            }

            return coreCount;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}


namespace Plexity.ViewModels.Pages
{
    public class FastFlagsViewModel : NotifyPropertyChangedViewModel
    {
        private Dictionary<string, object>? _preResetFlags;

        public event EventHandler? RequestPageReloadEvent;
        public event EventHandler? OpenFlagEditorEvent;

        private void OpenFastFlagEditor() => OpenFlagEditorEvent?.Invoke(this, EventArgs.Empty);

        public ICommand OpenFastFlagEditorCommand => new RelayCommand(OpenFastFlagEditor);

        public const string Enabled = "True";
        public const string Disabled = "False";

        public bool ResetConfiguration
        {
            get => _preResetFlags is not null;
            set
            {
                if (value)
                {
                    _preResetFlags = new(App.FastFlags.Prop);
                    App.FastFlags.Prop.Clear();
                }
                else
                {
                    App.FastFlags.Prop = _preResetFlags!;
                    _preResetFlags = null;
                }

                RequestPageReloadEvent?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool DisableTelemetry
        {
            get => App.FastFlags?.GetPreset("Telemetry.TelemetryV2Url") == "0.0.0.0";
            set
            {
                App.FastFlags.SetPreset("Telemetry.TelemetryV2Url", value ? "0.0.0.0" : null);
                App.FastFlags.SetPreset("Telemetry.Protocol", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.GraphicsQualityUsage", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.GpuVsCpuBound", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.RenderFidelity", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.RenderDistance", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.AudioPlugin", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.FmodErrors", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.SoundLength", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.AssetRequestV1", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.DeviceRAM", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.V2FrameRateMetrics", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.GlobalSkipUpdating", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.CallbackSafety", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.V2PointEncoding", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.ReplaceSeparator", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.OpenTelemetry", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.FLogTelemetry", value ? "0" : null);
                App.FastFlags.SetPreset("Telemetry.TelemetryService", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.PropertiesTelemetry", value ? "False" : null);
            }
        }

        public string? FakeVerify
        {
            get => App.FastFlags.GetPreset("Fake.Verify");
            set => App.FastFlags.SetPreset("Fake.Verify", value);
        }

        public bool RedFont
        {
            get => App.FastFlags.GetPreset("UI.RedFont") == "rbxasset://fonts/families/BuilderSans.json";
            set => App.FastFlags.SetPreset("UI.RedFont", value ? "rbxasset://fonts/families/BuilderSans.json" : null);
        }


        public bool Pseudolocalization
        {
            get => App.FastFlags.GetPreset("UI.Pseudolocalization") == "True";
            set => App.FastFlags.SetPreset("UI.Pseudolocalization", value ? "True" : null);
        }

        public bool Layered
        {
            get => App.FastFlags.GetPreset("Layered.Clothing") == "-1";
            set => App.FastFlags.SetPreset("Layered.Clothing", value ? "-1" : null);
        }

        public bool RainbowTheme
        {
            get => App.FastFlags.GetPreset("UI.RainbowText") == "True";
            set => App.FastFlags.SetPreset("UI.RainbowText", value ? "True" : null);
        }


        public bool SmoothTextures
        {
            get => App.FastFlags.GetPreset("System.SmoothTerrain") == "True";
            set
            {
                App.FastFlags.SetPreset("System.SmoothTerrain", value ? "True" : null);
            }
        }



        public bool BetterShadows
        {
            get => App.FastFlags.GetPreset("System.BetterShadows") == "True";
            set
            {
                App.FastFlags.SetPreset("System.BetterShadows", value ? "True" : null);
            }
        }

        public bool DisableWebview2Telemetry
        {
            get => App.FastFlags?.GetPreset("Telemetry.Webview1") == "www.youtube-nocookie.com";
            set
            {
                App.FastFlags.SetPreset("Telemetry.Webview1", value ? "www.youtube-nocookie.com" : null);
                App.FastFlags.SetPreset("Telemetry.Webview2", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Webview3", value ? "0" : null);
                App.FastFlags.SetPreset("Telemetry.Webview4", value ? "0" : null);
                App.FastFlags.SetPreset("Telemetry.Webview5", value ? "0" : null);
                App.FastFlags.SetPreset("Telemetry.Webview6", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Webview7", value ? "False" : null);
            }
        }

        public bool DisableVoiceChatTelemetry
        {
            get => App.FastFlags?.GetPreset("Telemetry.Voicechat1") == "False";
            set
            {
                App.FastFlags.SetPreset("Telemetry.Voicechat1", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat2", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat3", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat4", value ? "0" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat5", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat6", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat7", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat8", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat9", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat10", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat11", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat12", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat13", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat14", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat15", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat16", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat17", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat18", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat19", value ? "0" : null);
                App.FastFlags.SetPreset("Telemetry.Voicechat20", value ? "-1" : null);
            }
        }

        public bool BlockTencent
        {
            get => App.FastFlags?.GetPreset("Telemetry.Tencent1") == "/tencent/";
            set
            {
                App.FastFlags.SetPreset("Telemetry.Tencent1", value ? "/tencent/" : null);
                App.FastFlags.SetPreset("Telemetry.Tencent2", value ? "/tencent/" : null);
                App.FastFlags.SetPreset("Telemetry.Tencent3", value ? "https://www.gov.cn" : null);
                App.FastFlags.SetPreset("Telemetry.Tencent4", value ? "https://www.gov.cn" : null);
                App.FastFlags.SetPreset("Telemetry.Tencent5", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Tencent6", value ? "False" : null);
                App.FastFlags.SetPreset("Telemetry.Tencent7", value ? "10000" : null);

            }
        }

        public bool UseFastFlagManager
        {
            get => App.Settings.Prop.UseClientAppSettings;
            set => App.Settings.Prop.UseClientAppSettings = value;
        }


        public bool MinimalRendering
        {
            get => App.FastFlags.GetPreset("Rendering.MinimalRendering") == "True";
            set => App.FastFlags.SetPreset("Rendering.MinimalRendering", value ? "True" : null);
        }

        public bool GrayAvatar
        {
            get => App.FastFlags.GetPreset("Rendering.GrayAvatar") == "0";
            set => App.FastFlags.SetPreset("Rendering.GrayAvatar", value ? "0" : null);
        }

        public bool EnableGraySky
        {
            get => App.FastFlags.GetPreset("Graphic.GraySky") == "True";
            set => App.FastFlags.SetPreset("Graphic.GraySky", value ? "True" : null);
        }

        public bool DisableSky
        {
            get => App.FastFlags.GetPreset("Rendering.FRMRefactor") == "False";
            set
            {
                App.FastFlags.SetPreset("Rendering.FRMRefactor", value ? "False" : null);
                App.FastFlags.SetPreset("Lighting.Bloom2", value ? "False" : null);
            }
        }

        public IReadOnlyDictionary<DynamicResolution, string> DynamicResolutions =>
Enum.GetValues(typeof(DynamicResolution))
.Cast<DynamicResolution>()
.ToDictionary(
mode => mode,
mode => mode.GetStaticName()
);

        public DynamicResolution SelectedDynamicResolution
        {
            get => DynamicResolutions.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.Dynamic.Resolution")).Key;
            set
            {
                if (value == DynamicResolution.Resolution2)
                {
                    App.FastFlags.SetPreset("Rendering.Dynamic.Resolution", null);
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.Dynamic.Resolution", DynamicResolutions[value]);
                }
            }
        }

        public bool Threading
        {
            get => App.FastFlags.GetPreset("Hyper.Threading1") == "True";
            set
            {
                App.FastFlags.SetPreset("Hyper.Threading1", value ? "True" : null);
            }
        }

        public IReadOnlyDictionary<RefreshRate, string> RefreshRates =>
            Enum.GetValues(typeof(RefreshRate))
                .Cast<RefreshRate>()
                .ToDictionary(
                    mode => mode,
                    mode => {
                        var name = mode.GetStaticName();
                        if (name.EndsWith(" Hz"))
                            name = name.Substring(0, name.Length - 3);
                        return name;
                    }
                );


        private RefreshRate _selectedRefreshRate = RefreshRate.Default;
        public RefreshRate SelectedRefreshRate
        {
            get
            {
                var preset = App.FastFlags.GetPreset("System.TargetRefreshRate1");
                var matchingPair = RefreshRates.FirstOrDefault(x => x.Value == preset);

                return RefreshRates.ContainsKey(matchingPair.Key) ? matchingPair.Key : RefreshRate.Default;
            }
            set
            {
                if (_selectedRefreshRate != value)
                {
                    _selectedRefreshRate = value;
                    OnPropertyChanged(nameof(SelectedRefreshRate));

                    if (value == RefreshRate.Default)
                    {
                        App.FastFlags.SetPreset("System.TargetRefreshRate1", null);
                        App.FastFlags.SetPreset("System.TargetRefreshRate2", null);
                        App.FastFlags.SetPreset("System.TargetRefreshRate3", null);
                        App.FastFlags.SetPreset("System.TargetRefreshRate4", null);
                    }
                    else if (RefreshRates.ContainsKey(value))
                    {
                        var displayValue = RefreshRates[value];
                        App.FastFlags.SetPreset("System.TargetRefreshRate1", displayValue);
                        App.FastFlags.SetPreset("System.TargetRefreshRate2", displayValue);
                        App.FastFlags.SetPreset("System.TargetRefreshRate3", displayValue);
                        App.FastFlags.SetPreset("System.TargetRefreshRate4", displayValue);
                    }
                }
            }
        }


        public bool FixDisplayScaling
        {
            get => App.FastFlags.GetPreset("Rendering.DisableScaling") == "True";
            set => App.FastFlags.SetPreset("Rendering.DisableScaling", value ? "True" : null);
        }

        public bool DisablePlayerShadows
        {
            get => App.FastFlags.GetPreset("Rendering.ShadowIntensity") == "0";
            set
            {
                App.FastFlags.SetPreset("Rendering.ShadowIntensity", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.Pause.Voxelizer", value ? "True" : null);
                App.FastFlags.SetPreset("Rendering.ShadowMapBias", value ? "-1" : null);
            }
        }

        public bool DisablePostFX
        {
            get => App.FastFlags.GetPreset("Rendering.DisablePostFX") == "True";
            set => App.FastFlags.SetPreset("Rendering.DisablePostFX", value ? "True" : null);
        }

        public bool SkipHighResTextures
        {
            get => App.FastFlags.GetPreset("System.HighResTextures") == "True";
            set => App.FastFlags.SetPreset("System.HighResTextures", value ? "True" : null);
        }

        public bool TaskSchedulerAvoidingSleep
        {
            get => App.FastFlags.GetPreset("Rendering.AvoidSleep") == "True";
            set => App.FastFlags.SetPreset("Rendering.AvoidSleep", value ? "True" : null);
        }

        public bool DisableTerrainTextures
        {
            get => App.FastFlags.GetPreset("Rendering.TerrainTextureQuality") == "0";
            set
            {
                App.FastFlags.SetPreset("Rendering.TerrainTextureQuality", value ? "0" : null);
            }
        }

        public bool RemoveGrass
        {
            get => App.FastFlags.GetPreset("Rendering.Nograss1") == "0";
            set
            {
                App.FastFlags.SetPreset("Rendering.Nograss1", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.Nograss2", value ? "0" : null);
                App.FastFlags.SetPreset("Rendering.Nograss3", value ? "0" : null);
            }
        }

        public int FramerateLimit
        {
            get => int.TryParse(App.FastFlags.GetPreset("Rendering.Framerate"), out int result) ? result : 0;
            set
            {
                App.FastFlags.SetPreset("Rendering.Framerate", value == 0 ? null : value);
                if (value > 240)
                {
                    DialogService.ShowMessage(
                        "Going above 240 FPS is not recommended, as this may cause latency issues.",
                        "Warning",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                    App.FastFlags.SetPreset("FpsFix.Log", "False");
                }
                else
                {
                    App.FastFlags.SetPreset("FpsFix.Log", null);
                }
            }
        }

        public int UpdateRateMenu
        {
            get
            {
                var preset = App.FastFlags.GetPreset("Rendering.UpdateRateMenu");
                return int.TryParse(preset?.ToString(), out int val) ? val : default;
            }
            set => App.FastFlags.SetPreset("Rendering.UpdateRateMenu", value);
        }


        public IReadOnlyDictionary<SSAOMode, string> SSAOLevels =>
            Enum.GetValues(typeof(SSAOMode))
                .Cast<SSAOMode>()
                .ToDictionary(
                    mode => mode,
                    mode => mode.GetStaticName2() ?? mode.ToString()
                );

        public SSAOMode SelectedSSAOLevel
        {
            get
            {
                var current = App.FastFlags.GetPreset("Render.SSAOMip");
                foreach (var pair in ClientAppSettings.SSAOModes)
                {
                    if (string.Equals(pair.Value, current, StringComparison.Ordinal) ||
                        (pair.Value == null && current == null))
                    {
                        return pair.Key;
                    }
                }
                return SSAOMode.Default;
            }
            set
            {
                var newValue = ClientAppSettings.SSAOModes.TryGetValue(value, out var val) ? val : null;
                App.FastFlags.SetPreset("Render.SSAOMip", newValue);
                App.FastFlags.SetPreset("Render.SSAOLVL", newValue);

                var currentForce = App.FastFlags.GetPreset("Render.SSAOForce");
                bool currentForceBool = string.Equals(currentForce, "True", StringComparison.OrdinalIgnoreCase);
                var newForceValue = currentForceBool ? null : "True";

                App.FastFlags.SetPreset("Render.SSAOForce", newForceValue);
            }
        }

        public IReadOnlyDictionary<MSAAMode, string> MSAALevels =>
    Enum.GetValues(typeof(MSAAMode))
        .Cast<MSAAMode>()
        .ToDictionary(
            mode => mode,
            mode => mode.GetStaticName()
        );


        public MSAAMode SelectedMSAALevel
        {
            get
            {
                var current = App.FastFlags.GetPreset("Rendering.MSAA1");
                foreach (var pair in ClientAppSettings.MSAAModes)
                {
                    if (pair.Value == current || (pair.Value == null && current == null))
                        return pair.Key;
                }
                return MSAAMode.Default;
            }
            set
            {
                var newValue = ClientAppSettings.MSAAModes.TryGetValue(value, out var val) ? val : null;
                App.FastFlags.SetPreset("Rendering.MSAA1", newValue);
                App.FastFlags.SetPreset("Rendering.MSAA2", newValue);

            }
        }

        public IReadOnlyDictionary<TextureQuality, string> TextureQualities =>
Enum.GetValues(typeof(TextureQuality))
.Cast<TextureQuality>()
.ToDictionary(
mode => mode,
mode => mode.GetStaticName()
);

        public TextureQuality SelectedTextureQuality
        {
            get => TextureQualities.FirstOrDefault(x => x.Value == App.FastFlags.GetPreset("Rendering.TextureQuality.Level")).Key;
            set
            {
                if (value == TextureQuality.Default)
                {
                    App.FastFlags.SetPreset("Rendering.TextureQuality", null);
                }
                else
                {
                    App.FastFlags.SetPreset("Rendering.TextureQuality.OverrideEnabled", "True");
                    App.FastFlags.SetPreset("Rendering.TextureQuality.Level", TextureQualities[value]);
                }
            }
        }

        public IReadOnlyDictionary<RenderingMode, string> RenderingModes =>
Enum.GetValues(typeof(RenderingMode))
.Cast<RenderingMode>()
.ToDictionary(
mode => mode,
mode => mode.GetStaticName()
);

        public RenderingMode SelectedRenderingMode
        {
            get => App.FastFlags.GetPresetEnum(RenderingModes, "Rendering.Mode", "True");
            set
            {
                RenderingMode[] DisableD3D11 = new RenderingMode[]
                {
                    RenderingMode.Vulkan,
                    RenderingMode.OpenGL
                };

                App.FastFlags.SetPresetEnum("Rendering.Mode", value.ToString(), "True");
                App.FastFlags.SetPreset("Rendering.Mode.DisableD3D11", DisableD3D11.Contains(value) ? "True" : null);
            }
        }

        public bool NewFpsSystem
        {
            get => App.FastFlags.GetPreset("Rendering.NewFpsSystem") == "True";
            set => App.FastFlags.SetPreset("Rendering.NewFpsSystem", value ? "True" : null);
        }

        public bool NewCamera
        {
            get => App.FastFlags.GetPreset("Camera.Controls") == "True";
            set => App.FastFlags.SetPreset("Camera.Controls", value ? "True" : null);
        }

        public bool DisableVignette
        {
            get => App.FastFlags.GetPreset("WOW.Vignette") == "False";
            set => App.FastFlags.SetPreset("WOW.Vignette", value ? "False" : null);
        }


        public bool SkipDiskChecks
        {
            get => App.FastFlags.GetPreset("System.DiskChecks") == "True";
            set => App.FastFlags.SetPreset("System.DiskChecks", value ? "True" : "False");
        }


        public IReadOnlyDictionary<LightingMode, string> LightingModes => ClientAppSettings.LightingModes;

        public LightingMode SelectedLightingMode
        {
            get => App.FastFlags.GetPresetEnum(LightingModes, "Rendering.Lighting", "True");
            set => App.FastFlags.SetPresetEnum("Rendering.Lighting", LightingModes[value], "True");
        }


        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T field, T newValue, [CallerMemberName] string? propertyName = null)
        {
            if (!Equals(field, newValue))
            {
                field = newValue;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                return true;
            }

            return false;
        }

        private System.Collections.IEnumerable? profileModes;

        public System.Collections.IEnumerable? ProfileModes { get => profileModes; set => SetProperty(ref profileModes, value); }

        private string selectedProfileMods = string.Empty;

        public string SelectedProfileMods { get => selectedProfileMods; set => SetProperty(ref selectedProfileMods, value); }

        public IReadOnlyDictionary<string, string?> GPUs => GetGPUs();

        public string SelectedGPU
        {
            get
            {
                var preset = App.FastFlags.GetPreset("System.PreferredGPU");
                return string.IsNullOrEmpty(preset) ? "Automatic" : preset;
            }
            set
            {
                // Store null internally if "Automatic" is selected
                var gpuValue = (value == "Automatic") ? null : value;

                App.FastFlags.SetPreset("System.PreferredGPU", gpuValue);
                App.FastFlags.SetPreset("System.DXT", gpuValue);
            }
        }

        public static IReadOnlyDictionary<string, string> GetGPUs()
        {
            const string LOG_IDENT = "FFlagPresets::GetGPUs";

            var GPUs = new Dictionary<string, string>
        {
            // Display and value both "Automatic" so combo box shows it
            { "Automatic", "Automatic" }
        };

            try
            {
                using (var factory = new Factory1())
                {
                    int adapterCount = factory.GetAdapterCount1();

                    for (int i = 0; i < adapterCount; i++)
                    {
                        var adapter = factory.GetAdapter1(i);
                        var desc = adapter.Description;

                        string gpuName = desc.Description.Trim();

                        if (!string.IsNullOrEmpty(gpuName) && !GPUs.ContainsKey(gpuName))
                        {
                            GPUs.Add(gpuName, gpuName);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to get GPU names: {ex.Message}");
            }

            return GPUs;
        }



        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public static IReadOnlyDictionary<string, string> GetCpuThreads()
        {
            const string LOG_IDENT = "FFlagPresets::GetCpuThreads";
            var cpuThreads = new Dictionary<string, string>();

            cpuThreads.Add(string.Empty, "Automatic");



            try
            {
                int logicalProcessorCount = SystemInfo.GetLogicalProcessorCount();

                for (int i = 1; i <= logicalProcessorCount; i++)
                {
                    // Use string key = number as string, value = number as string too for display
                    string threadCount = i.ToString();
                    cpuThreads.Add(threadCount, threadCount);
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to get CPU thread count: {ex.Message}");
            }

            return cpuThreads;
        }



        public IReadOnlyDictionary<string, string?> CpuThreads => GetCpuThreads();

        private void SetPresetAndNotify(string key, string? value)
        {
            App.FastFlags.SetPreset(key, value);
            // Do not call OnPropertyChanged here — we'll call it once after all sets
        }

        public KeyValuePair<string, string?> SelectedCpuThreads
        {
            get
            {
                string currentValue = App.FastFlags.GetPreset("System.CpuCore1");

                // Normalize "Automatic" or null to ""
                if (string.IsNullOrEmpty(currentValue) || currentValue == "Automatic")
                    currentValue = string.Empty;

                // Try to find matching key
                return CpuThreads?.FirstOrDefault(kvp => kvp.Key == currentValue)
                    ?? new KeyValuePair<string, string?>(string.Empty, "Automatic");
            }
            set
            {
                string val = value.Key;

                // Set all core flags
                SetPresetAndNotify("System.CpuCore1", val);
                SetPresetAndNotify("System.CpuCore2", val);
                SetPresetAndNotify("System.CpuCore3", val);
                SetPresetAndNotify("System.CpuCore4", val);
                SetPresetAndNotify("System.CpuCore5", val);
                SetPresetAndNotify("System.CpuCore6", val);
                SetPresetAndNotify("System.CpuCore7", val);
                SetPresetAndNotify("System.CpuCore8", val);
                SetPresetAndNotify("System.CpuCore9", val);

                if (!string.IsNullOrEmpty(val) && int.TryParse(val, out int parsedValue))
                {
                    int adjustedValue = Math.Max(parsedValue - 1, 1);
                    SetPresetAndNotify("System.CpuThreads", adjustedValue.ToString());
                    SetPresetAndNotify("System.CpuCore8", adjustedValue.ToString());
                }
                else
                {
                    SetPresetAndNotify("System.CpuThreads", null);
                    SetPresetAndNotify("System.CpuCore8", null);
                }

                OnPropertyChanged(nameof(SelectedCpuThreads));
            }
        }


        public static IReadOnlyDictionary<string, string> GetCpuCoreMinThreadCount()
        {
            const string LOG_IDENT = "FFlagPresets::GetCpuCoreMinThreadCount";
            var cpuCoreMinThreads = new Dictionary<string, string>();

            cpuCoreMinThreads.Add(string.Empty, "Automatic");

            try
            {
                int coreCount = SystemInfo.GetPhysicalCoreCount();

                for (int i = 1; i <= coreCount; i++)
                {
                    string coreCountStr = i.ToString();
                    cpuCoreMinThreads.Add(coreCountStr, coreCountStr);
                }
            }
            catch (Exception ex)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Failed to get CPU core min thread count: {ex.Message}");
            }

            return cpuCoreMinThreads;
        }


        public IReadOnlyDictionary<string, string?> CpuCoreMinThreadCount => GetCpuCoreMinThreadCount();

        public KeyValuePair<string, string?> SelectedCpuCoreMinThreadCount
        {
            get
            {
                string currentValue = App.FastFlags.GetPreset("System.CpuCoreMinThreadCount") ?? "Automatic";
                return CpuCoreMinThreadCount?.FirstOrDefault(kvp => kvp.Key == currentValue) ?? default;
            }
            set
            {
                string? val = value.Value;

                App.FastFlags.SetPreset("System.CpuCoreMinThreadCount", val);

                if (val != null && int.TryParse(val, out int parsedValue))
                {
                    int adjustedValue = Math.Max(parsedValue - 1, 1);
                    App.FastFlags.SetPreset("System.CpuCoreMinThreadCount", adjustedValue.ToString());
                }
                else
                {
                    App.FastFlags.SetPreset("System.CpuCoreMinThreadCount", null);
                }

                OnPropertyChanged(nameof(SelectedCpuCoreMinThreadCount));
            }
        }
    }
}
