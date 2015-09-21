#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// Configuration.cs is part of LeagueSharp.Loader.
// 
// LeagueSharp.Loader is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LeagueSharp.Loader is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LeagueSharp.Loader. If not, see <http://www.gnu.org/licenses/>.

#endregion

namespace LeagueSharp.Sandbox.Shared
{
    using System.Runtime.Serialization;
    using System.Security;

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
        public bool SendStatistics { get; set; }

        [DataMember]
        public bool ShowDrawing { get; set; }

        [DataMember]
        public bool ShowPing { get; set; }

        [DataMember]
        public bool TowerRange { get; set; }

        [DataMember]
        public int UnloadKey { get; set; }

        public override string ToString()
        {
            return
                string.Format(
                    "DataDirectory:{0}\n" + "LeagueSharpDllPath:{1}\n" + "LibrariesDirectory:{2}\n" + "MenuKey:{3}\n"
                    + "MenuToggleKey:{4}\n" + "AntiAfk:{5}\n" + "Console:{6}\n" + "ExtendedZoom:{7}\n"
                    + "TowerRange:{8}\n" + "ReloadKey:{9}\n" + "ReloadAndRecompileKey:{10}\n"
                    + "SelectedLanguage:{11}\n" + "UnloadKey:{12}\n",
                    this.DataDirectory,
                    this.LeagueSharpDllPath,
                    this.LibrariesDirectory,
                    this.MenuKey,
                    this.MenuToggleKey,
                    this.AntiAfk,
                    this.Console,
                    this.ExtendedZoom,
                    this.TowerRange,
                    this.ReloadKey,
                    this.ReloadAndRecompileKey,
                    this.SelectedLanguage,
                    this.UnloadKey);
        }
    }
}