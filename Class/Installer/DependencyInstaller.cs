#region LICENSE

// Copyright 2016-2016 LeagueSharp.Loader
// DependencyInstaller.cs is part of LeagueSharp.Loader.
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
    using System.Threading.Tasks;

    using LeagueSharp.Loader.Data;

    using Newtonsoft.Json;

    using PlaySharp.Service.Model;

    public class DependencyInstaller
    {
        static DependencyInstaller()
        {
            UpdateReferenceCache();
        }

        public DependencyInstaller(IEnumerable<string> projects)
        {
            this.Projects = projects;
        }

        public static List<Dependency> Cache { get; set; } = new List<Dependency>();

        public IEnumerable<string> Projects { get; set; }

        private bool IsInstalled(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return File.Exists(Path.Combine(Directories.CoreDirectory, $"{name}.dll"));
        }

        private bool IsKnown(string name)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            return Cache.Any(d => d.Name == name);
        }

        public async Task<bool> SatisfyAsync()
        {
            var successful = true;

            foreach (var project in this.Projects)
            {
                try
                {
                    var projectReferences = this.ParseReferences(project);
                    var missingReferences = projectReferences.Where(r => this.IsKnown(r) && !this.IsInstalled(r)).Select(r => Cache.First(d => r == d.Name));

                    foreach (var dependency in missingReferences)
                    {
                        if (!await dependency.InstallAsync().ConfigureAwait(true))
                        {
                            successful = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    successful = false;
                    Console.WriteLine(e);
                }
            }

            return successful;
        }

        public bool Satisfy()
        {
            var successful = true;

            foreach (var project in this.Projects)
            {
                try
                {
                    var projectReferences = this.ParseReferences(project);
                    var missingReferences = projectReferences.Where(r => this.IsKnown(r) && !this.IsInstalled(r)).Select(r => Cache.First(d => r == d.Name));

                    foreach (var dependency in missingReferences)
                    {
                        if (!dependency.Install())
                        {
                            successful = false;
                        }
                    }
                }
                catch (Exception e)
                {
                    successful = false;
                    Console.WriteLine(e);
                }
            }

            return successful;
        }

        private static void UpdateReferenceCache()
        {
            var assemblies = new List<AssemblyEntry>();

            try
            {
                assemblies = WebService.Client.Assemblies().Where(a => a.Type == 3).ToList();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            Cache.Clear();
            foreach (var lib in assemblies)
            {
                Cache.Add(ParseAssemblyName(lib));
            }
            Cache.RemoveAll(a => a == null);
        }

        private static Dependency ParseAssemblyName(AssemblyEntry assembly)
        {
            if (assembly == null)
            {
                throw new ArgumentNullException(nameof(assembly));
            }

            try
            {
                var project = assembly.GithubUrl;
                project = project.Replace("https://github.com/", "https://raw.githubusercontent.com/");
                project = project.Replace("/blob/master/", "/master/");

                using (var client = new WebClientEx())
                {
                    var dependency = Dependency.FromAssemblyEntry(assembly);
                    var content = client.DownloadString(project);
                    var assemblyNameMatch = Regex.Match(content, "<AssemblyName>(?<name>.*?)</AssemblyName>");
                    dependency.Name = assemblyNameMatch.Groups["name"].Value;

                    return dependency;
                }
            }
            catch
            {
                Console.WriteLine($"Invalid: {assembly.Id} - {assembly.GithubUrl}");
            }

            return null;
        }

        private List<string> ParseReferences(string project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var projectReferences = new List<string>();

            try
            {
                var matches = Regex.Matches(File.ReadAllText(project), "<Reference Include=\"(?<assembly>.*?)\"(?<space>.*?)>");

                foreach (Match match in matches)
                {
                    var m = match.Groups["assembly"].Value;

                    if (m.Contains(","))
                    {
                        m = m.Split(',')[0];
                    }

                    projectReferences.Add(m);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return projectReferences;
        }
    }
}