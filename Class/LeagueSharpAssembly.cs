#region LICENSE

// Copyright 2016-2016 LeagueSharp.Loader
// LeagueSharpAssembly.cs is part of LeagueSharp.Loader.
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

namespace LeagueSharp.Loader.Class
{
    #region

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Xml.Serialization;

    using LeagueSharp.Loader.Data;

    using Microsoft.Build.Evaluation;

    using PlaySharp.Service.Model;

    using RestSharp.Extensions.MonoHttp;

    #endregion

    public static class LeagueSharpAssemblies
    {
        public static LeagueSharpAssembly GetAssembly(string projectFile, string url = "")
        {
            LeagueSharpAssembly foundAssembly = null;
            try
            {
                var name = Path.GetFileNameWithoutExtension(projectFile);
                foundAssembly = new LeagueSharpAssembly(name, projectFile, url);
            }
            catch (Exception e)
            {
                Utility.Log(LogStatus.Error, "Updater GetAssembly", e.ToString(), Logs.MainLog);
            }

            return foundAssembly;
        }

        public static List<LeagueSharpAssembly> GetAssemblies(string directory, string url = "")
        {
            var projectFiles = new List<string>();
            var foundAssemblies = new List<LeagueSharpAssembly>();

            try
            {
                projectFiles.AddRange(Directory.GetFiles(directory, "*.csproj", SearchOption.AllDirectories));
                foreach (var projectFile in projectFiles)
                {
                    var name = Path.GetFileNameWithoutExtension(projectFile);
                    var assembly = new LeagueSharpAssembly(name, projectFile, url);

                    if (!string.IsNullOrEmpty(url))
                    {
                        var entry = Config.Instance.DatabaseAssemblies?.FirstOrDefault(a => a.Name == name)
                                    ?? Config.Instance.DatabaseAssemblies?.FirstOrDefault(a => Path.GetFileNameWithoutExtension(a.GithubUrl) == name);

                        if (entry != null)
                        {
                            assembly.Description = HttpUtility.HtmlDecode(entry.Description);
                            assembly.DisplayName = HttpUtility.HtmlDecode(entry.Name);
                        }
                    }

                    foundAssemblies.Add(assembly);
                }
            }
            catch (Exception e)
            {
                Utility.Log(LogStatus.Error, "Updater", e.ToString(), Logs.MainLog);
            }

            return foundAssemblies;
        }
    }

    public enum AssemblyType
    {
        Library = 3,

        Executable = 1,

        Unknown,
    }

    public enum AssemblyStatus
    {
        Ready,

        Updating,

        UpdatingError,

        CompilingError,

        Compiling,
    }

    [XmlType(AnonymousType = true)]
    [Serializable]
    public class LeagueSharpAssembly : INotifyPropertyChanged
    {
        private string displayName = "";

        private bool injectChecked;

        private bool installChecked;

        private string pathToBinary = null;

        private string pathToProjectFile = "";

        private string svnUrl;

        private AssemblyType? type = null;

        public LeagueSharpAssembly()
        {
            this.Status = AssemblyStatus.Ready;
        }

        public LeagueSharpAssembly(string name, string path, string svnUrl)
        {
            this.Name = name;
            this.PathToProjectFile = path;
            this.SvnUrl = svnUrl;
            this.Description = "";
            this.Status = AssemblyStatus.Ready;
        }

        public string Description { get; set; }

        public string DisplayName
        {
            get
            {
                return this.displayName == "" ? this.Name : this.displayName;
            }
            set
            {
                this.displayName = value;
            }
        }

        public bool InjectChecked
        {
            get
            {
                if (this.Type == AssemblyType.Library)
                {
                    return true;
                }

                return this.injectChecked;
            }
            set
            {
                this.injectChecked = value;
                this.OnPropertyChanged("InjectChecked");
            }
        }

        public bool InstallChecked
        {
            get
            {
                return this.installChecked;
            }
            set
            {
                this.installChecked = value;
                this.OnPropertyChanged("InstallChecked");
            }
        }

        public string Location => this.SvnUrl == "" ? "Local" : this.SvnUrl;

        public string Name { get; set; }

        public string PathToBinary
        {
            get
            {
                if (this.pathToBinary == null)
                {
                    this.pathToBinary = Path.Combine(
                        this.Type == AssemblyType.Library ? Directories.CoreDirectory : Directories.AssembliesDir,
                        (this.Type == AssemblyType.Library ? "" : this.PathToProjectFile.GetHashCode().ToString("X"))
                        + Path.GetFileName(Compiler.GetOutputFilePath(this.GetProject())));
                }

                return this.pathToBinary;
            }
        }

        public string PathToProjectFile
        {
            get
            {
                if (File.Exists(this.pathToProjectFile))
                {
                    return this.pathToProjectFile;
                }

                try
                {
                    var folderToSearch = Path.Combine(Directories.RepositoryDir, this.SvnUrl.GetHashCode().ToString("X"), "trunk");

                    if (Directory.Exists(folderToSearch))
                    {
                        var projectFile =
                            Directory.GetFiles(folderToSearch, "*.csproj", SearchOption.AllDirectories)
                                     .FirstOrDefault(file => Path.GetFileNameWithoutExtension(file) == this.Name);

                        if (!string.IsNullOrEmpty(projectFile))
                        {
                            this.pathToProjectFile = projectFile;
                            return projectFile;
                        }
                    }
                }
                catch
                {
                    // ignored
                }

                return this.pathToProjectFile;
            }
            set
            {
                if (!value.Contains("%AppData%"))
                {
                    this.pathToProjectFile = value;
                }
                else
                {
                    this.pathToProjectFile = value.Replace("%AppData%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                }
            }
        }

        public AssemblyStatus Status { get; set; }

        public string SvnUrl
        {
            get
            {
                return this.svnUrl;
            }
            set
            {
                this.svnUrl = value;
                this.OnPropertyChanged("SvnUrl");
            }
        }

        public AssemblyType Type
        {
            get
            {
                if (this.type == null)
                {
                    var project = this.GetProject();
                    if (project != null)
                    {
                        this.type = project.GetPropertyValue("OutputType").ToLower().Contains("exe") ? AssemblyType.Executable : AssemblyType.Library;
                    }
                }

                return this.type ?? AssemblyType.Unknown;
            }
        }

        public string Version
        {
            get
            {
                if (this.Status != AssemblyStatus.Ready)
                {
                    return this.Status.ToString();
                }

                if (!string.IsNullOrEmpty(this.PathToBinary) && File.Exists(this.PathToBinary))
                {
                    return AssemblyName.GetAssemblyName(this.PathToBinary).Version.ToString();
                }

                return "?";
            }
        }

        internal bool IsBlocked
        {
            get
            {
                if (string.IsNullOrEmpty(this.SvnUrl))
                {
                    return false; // just to make sure :^)
                }

                return Config.Instance.BlockedRepositories.Any(x => this.SvnUrl.StartsWith(x));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static LeagueSharpAssembly FromAssemblyEntry(AssemblyEntry entry)
        {
            try
            {
                var repositoryMatch = Regex.Match(entry.GithubUrl, @"^(http[s]?)://(?<host>.*?)/(?<author>.*?)/(?<repo>.*?)(/{1}|$)");
                var repositoryUrl = $"https://{repositoryMatch.Groups["host"]}/{repositoryMatch.Groups["author"]}/{repositoryMatch.Groups["repo"]}";
                var repositoryDirectory = Path.Combine(Directories.RepositoryDir, repositoryUrl.GetHashCode().ToString("X"), "trunk");
                var path = Path.Combine(
                    repositoryDirectory,
                    entry.GithubUrl.Replace(repositoryUrl, "").Replace("/blob/master/", "").Replace("/", "\\"));

                return new LeagueSharpAssembly(entry.Name, path, repositoryUrl);
            }
            catch
            {
                return null;
            }
        }

        public bool Compile()
        {
            this.Status = AssemblyStatus.Compiling;
            this.OnPropertyChanged("Version");
            var project = this.GetProject();

            if (Compiler.Compile(project, Path.Combine(Directories.LogsDir, this.Name + ".txt"), Logs.MainLog))
            {
                var result = true;
                var assemblySource = Compiler.GetOutputFilePath(project);
                var assemblyDestination = this.PathToBinary;
                var pdbSource = Path.ChangeExtension(assemblySource, ".pdb");
                var pdbDestination = Path.ChangeExtension(assemblyDestination, ".pdb");

                if (File.Exists(assemblySource))
                {
                    result = Utility.OverwriteFile(assemblySource, assemblyDestination);
                }

                if (File.Exists(pdbSource))
                {
                    Utility.OverwriteFile(pdbSource, pdbDestination);
                }

                Utility.ClearDirectory(Path.Combine(project.DirectoryPath, "bin"));
                Utility.ClearDirectory(Path.Combine(project.DirectoryPath, "obj"));

                if (result)
                {
                    this.Status = AssemblyStatus.Ready;
                }
                else
                {
                    this.Status = AssemblyStatus.CompilingError;
                }

                this.OnPropertyChanged("Version");
                this.OnPropertyChanged("Type");
                return result;
            }

            this.Status = AssemblyStatus.CompilingError;
            this.OnPropertyChanged("Version");
            return false;
        }

        public LeagueSharpAssembly Copy()
        {
            return new LeagueSharpAssembly(this.Name, this.PathToProjectFile, this.SvnUrl);
        }

        public override bool Equals(object obj)
        {
            if (obj is LeagueSharpAssembly)
            {
                return ((LeagueSharpAssembly)obj).PathToProjectFile == this.PathToProjectFile;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return this.PathToProjectFile.GetHashCode();
        }

        public Project GetProject()
        {
            if (File.Exists(this.PathToProjectFile))
            {
                try
                {
                    var pf = new ProjectFile(this.PathToProjectFile, Logs.MainLog)
                        {
                            Configuration = Config.Instance.EnableDebug ? "Debug" : "Release",
                            PlatformTarget = "x86",
                            ReferencesPath = Directories.CoreDirectory
                        };
                    pf.Change();

                    return pf.Project;
                }
                catch (Exception e)
                {
                    Utility.Log(LogStatus.Error, "Builder", "Error: " + e, Logs.MainLog);
                }
            }
            return null;
        }

        public void Update()
        {
            if (this.Status == AssemblyStatus.Updating || this.SvnUrl == "")
            {
                return;
            }

            this.Status = AssemblyStatus.Updating;
            this.OnPropertyChanged("Version");
            try
            {
                GitUpdater.Update(this.SvnUrl);
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }

            this.Status = AssemblyStatus.Ready;
            this.OnPropertyChanged("Version");
        }

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}