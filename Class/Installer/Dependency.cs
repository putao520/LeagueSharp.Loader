#region LICENSE

// Copyright 2016-2016 LeagueSharp.Loader
// Dependency.cs is part of LeagueSharp.Loader.
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

namespace LeagueSharp.Loader.Class.Installer
{
    using System.IO;
    using System.Linq;

    using LeagueSharp.Loader.Data;

    public class Dependency
    {
        public string Repository { get; set; }

        public string Name { get; set; }

        public string Project { get; set; }

        public override string ToString()
        {
            return $"{this.Name} - {this.Project} - {this.Repository}";
        }

        public bool Install()
        {
            var updateResult = GitUpdater.Update(this.Repository);
            if (string.IsNullOrEmpty(updateResult))
            {
                return false;
            }

            var fileSearchResult = Directory.EnumerateFiles(updateResult, this.Project, SearchOption.AllDirectories).FirstOrDefault();
            if (string.IsNullOrEmpty(fileSearchResult))
            {
                return false;
            }

            var assembly = new LeagueSharpAssembly(this.Name, fileSearchResult, this.Repository);

            var compileResult = assembly.Compile();
            if (!compileResult)
            {
                return false;
            }

            Config.Instance.SelectedProfile.InstalledAssemblies.Add(assembly);

            return true;
        }
    }
}