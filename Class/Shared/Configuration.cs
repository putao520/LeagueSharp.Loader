using System.Runtime.Serialization;
using System.Security;

namespace LeagueSharp.Sandbox.Shared
{
    [DataContract]
    public class Configuration
    {
        [DataMember]
        public bool AntiAfk { get; set; }

        [DataMember]
        public bool Console { get; set; }

        [DataMember]
        public string DataDirectory { get; set; }

        [DataMember]
        public bool ExtendedZoom { get; set; }

        [DataMember]
        public string LeagueSharpDllPath { get; set; }

        [DataMember]
        public string LibrariesDirectory { get; set; }

        [DataMember]
        public int MenuKey { get; set; }

        [DataMember]
        public int MenuToggleKey { get; set; }

        [DataMember]
        public PermissionSet Permissions { get; set; }

        [DataMember]
        public int ReloadAndRecompileKey { get; set; }

        [DataMember]
        public int ReloadKey { get; set; }

        [DataMember]
        public string SelectedLanguage { get; set; }

        [DataMember]
        public bool TowerRange { get; set; }

        [DataMember]
        public int UnloadKey { get; set; }

        [DataMember]
        public bool ShowPing { get; set; }

        [DataMember]
        public bool ShowDrawing { get; set; }

        [DataMember]
        public bool SendStatistics { get; set; }

        public override string ToString()
        {
            return string.Format("DataDirectory:{0}\n" +
                                 "LeagueSharpDllPath:{1}\n" +
                                 "LibrariesDirectory:{2}\n" +
                                 "MenuKey:{3}\n" +
                                 "MenuToggleKey:{4}\n" +
                                 "AntiAfk:{5}\n" +
                                 "Console:{6}\n" +
                                 "ExtendedZoom:{7}\n" +
                                 "TowerRange:{8}\n" +
                                 "ReloadKey:{9}\n" +
                                 "ReloadAndRecompileKey:{10}\n" +
                                 "SelectedLanguage:{11}\n" +
                                 "UnloadKey:{12}\n",
                DataDirectory,
                LeagueSharpDllPath,
                LibrariesDirectory,
                MenuKey,
                MenuToggleKey,
                AntiAfk,
                Console,
                ExtendedZoom,
                TowerRange,
                ReloadKey,
                ReloadAndRecompileKey,
                SelectedLanguage,
                UnloadKey);
        }
    }
}