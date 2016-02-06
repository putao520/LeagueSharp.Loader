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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;

    using LeagueSharp.Loader.Data;

    using PlaySharp.Service.Model;

    public class Dependency
    {
        public string Repository { get; set; }

        public string Name { get; set; }

        public string Project { get; set; }

        public string Description { get; set; }

        public static Dependency FromAssemblyEntry(AssemblyEntry assembly)
        {
            try
            {
                var repositoryMatch = Regex.Match(assembly.GithubUrl, @"^(http[s]?)://(?<host>.*?)/(?<author>.*?)/(?<repo>.*?)(/{1}|$)");
                var projectName = assembly.GithubUrl.Substring(assembly.GithubUrl.LastIndexOf("/") + 1);
                var repositoryUrl = $"https://{repositoryMatch.Groups["host"]}/{repositoryMatch.Groups["author"]}/{repositoryMatch.Groups["repo"]}";

                return new Dependency { Repository = repositoryUrl, Project = projectName, Name = assembly.Name, Description = assembly.Description };
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }

            return null;
        }

        public override string ToString()
        {
            return $"{this.Name} - {this.Project} - {this.Repository}";
        }

        public async Task<bool> InstallAsync()
        {
            var updateResult = string.Empty;
            var compileResult = false;

            await Task.Factory.StartNew(() => { updateResult = GitUpdater.Update(this.Repository); });
            if (string.IsNullOrEmpty(updateResult))
            {
                return false;
            }

            var fileSearchResult = Directory.EnumerateFiles(updateResult, this.Project, SearchOption.AllDirectories).FirstOrDefault();
            if (string.IsNullOrEmpty(fileSearchResult))
            {
                return false;
            }

            var di = new DependencyInstaller(new List<string> { fileSearchResult });
            await di.SatisfyAsync();

            var assembly = new LeagueSharpAssembly(this.Name, fileSearchResult, this.Repository) { Description = this.Description };

            await Task.Factory.StartNew(() => { compileResult = assembly.Compile(); });
            if (!compileResult)
            {
                return false;
            }

            Config.Instance.Profiles.First().InstalledAssemblies.Add(assembly);

            return true;
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

            var di = new DependencyInstaller(new List<string> { fileSearchResult });
            di.Satisfy();

            var assembly = new LeagueSharpAssembly(this.Name, fileSearchResult, this.Repository) { Description = this.Description };

            var compileResult = assembly.Compile();
            if (!compileResult)
            {
                return false;
            }

            Config.Instance.Profiles.First().InstalledAssemblies.Add(assembly);

            return true;
        }
    }
}