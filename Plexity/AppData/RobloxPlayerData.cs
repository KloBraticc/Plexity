using Plexity.AppData;
using Plexity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Plexity.Models.Persistable;

namespace Plexity.AppData
{
    public class RobloxPlayerData : CommonAppData, IAppData
    {
        public string ProductName => "Roblox";

        public string BinaryType => "WindowsPlayer";

        public string RegistryName => "RobloxPlayer";

        public override string ExecutableName => "RobloxPlayerBeta.exe";

        public override AppState State => App.State.Prop.Player;

        private static readonly IReadOnlyDictionary<string, string> _packageDirectoryMap =
            new ReadOnlyDictionary<string, string>(
                new Dictionary<string, string>
                {
                    { "Roblox.zip", string.Empty }
                });

        public override IReadOnlyDictionary<string, string> PackageDirectoryMap => _packageDirectoryMap;
    }
}
