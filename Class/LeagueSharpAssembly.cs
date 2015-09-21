#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
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
    using System.Windows;
    using System.Xml.Serialization;

    using LeagueSharp.Loader.Data;

    using Microsoft.Build.Evaluation;

    #endregion

    public static class LeagueSharpAssemblies
    {
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
                    foundAssemblies.Add(new LeagueSharpAssembly(name, projectFile, url));
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
        Library,

        Executable,

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

        private string _displayName = "";

        private bool _injectChecked;

        private bool _installChecked;

        private string _pathToBinary = null;

        private string _pathToProjectFile = "";

        private string _svnUrl;

        private AssemblyType? _type = null;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Description { get; set; }

        public string DisplayName
        {
            get
            {
                return this._displayName == "" ? this.Name : this._displayName;
            }
            set
            {
                this._displayName = value;
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

                return this._injectChecked;
            }
            set
            {
                this._injectChecked = value;
                this.OnPropertyChanged("InjectChecked");
            }
        }

        public bool InstallChecked
        {
            get
            {
                return this._installChecked;
            }
            set
            {
                this._installChecked = value;
                this.OnPropertyChanged("InstallChecked");
            }
        }

        public string Location
        {
            get
            {
                return this.SvnUrl == "" ? "Local" : this.SvnUrl;
            }
        }

        public string Name { get; set; }

        public string PathToBinary
        {
            get
            {
                if (this._pathToBinary == null)
                {
                    this._pathToBinary =
                        Path.Combine(
                            (this.Type == AssemblyType.Library ? Directories.CoreDirectory : Directories.AssembliesDir),
                            (this.Type == AssemblyType.Library ? "" : this.PathToProjectFile.GetHashCode().ToString("X"))
                            + Path.GetFileName(Compiler.GetOutputFilePath(this.GetProject())));
                }

                return this._pathToBinary;
            }
        }

        public string PathToProjectFile
        {
            get
            {
                if (File.Exists(this._pathToProjectFile))
                {
                    return this._pathToProjectFile;
                }

                try
                {
                    var folderToSearch = Path.Combine(
                        Directories.RepositoryDir,
                        this.SvnUrl.GetHashCode().ToString("X"),
                        "trunk");
                    var projectFile =
                        Directory.GetFiles(folderToSearch, "*.csproj", SearchOption.AllDirectories)
                                 .FirstOrDefault(file => Path.GetFileNameWithoutExtension(file) == this.Name);
                    if (projectFile != default(string))
                    {
                        this._pathToProjectFile = projectFile;
                        return projectFile;
                    }
                }
                catch
                {
                    // ignored
                }

                return this._pathToProjectFile;
            }
            set
            {
                if (!value.Contains("%AppData%"))
                {
                    this._pathToProjectFile = value;
                }
                else
                {
                    this._pathToProjectFile = value.Replace(
                        "%AppData%",
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
                }
            }
        }

        public AssemblyStatus Status { get; set; }

        public string SvnUrl
        {
            get
            {
                return this._svnUrl;
            }
            set
            {
                this._svnUrl = value;
                this.OnPropertyChanged("SvnUrl");
            }
        }

        public AssemblyType Type
        {
            get
            {
                if (this._type == null)
                {
                    var project = this.GetProject();
                    if (project != null)
                    {
                        this._type = project.GetPropertyValue("OutputType").ToLower().Contains("exe")
                                         ? AssemblyType.Executable
                                         : AssemblyType.Library;
                    }
                }

                return this._type ?? AssemblyType.Unknown;
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

        public bool Compile()
        {
            this.Status = AssemblyStatus.Compiling;
            this.OnPropertyChanged("Version");
            var project = this.GetProject();

            if (Compiler.Compile(project, Path.Combine(Directories.LogsDir, this.Name + ".txt"), Logs.MainLog))
            {
                var result = Utility.OverwriteFile(Compiler.GetOutputFilePath(project), this.PathToBinary);

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
                            Configuration = "Release",
                            PlatformTarget = "x86",
                            ReferencesPath = Directories.CoreDirectory,
                            UpdateReferences = true,
                            PostbuildEvent = true,
                            PrebuildEvent = true,
                            ResetOutputPath = true
                        };
                    pf.Change();

                    /* _pathToBinary =
                        Path.Combine(
                            (Type == AssemblyType.Library ? Directories.CoreDirectory : Directories.AssembliesDir),
                            (Type == AssemblyType.Library ? "" : PathToProjectFile.GetHashCode().ToString("X")) +
                            Path.GetFileName(Compiler.GetOutputFilePath(pf.Project)));

                    _type = pf.Project.GetPropertyValue("OutputType").ToLower().Contains("exe")
                        ? AssemblyType.Executable
                        : AssemblyType.Library;*/

                    return pf.Project;
                }
                catch (Exception e)
                {
                    Utility.Log(LogStatus.Error, "Builder", "Error: " + e, Logs.MainLog);
                }
            }
            return null;
        }

        /// <summary>
        ///     Returns the relative path to the Project after the trunk folder.
        /// </summary>
        /// <returns></returns>
        public string GetProjectPathRelative()
        {
            var dir = this.PathToProjectFile.Remove(this.PathToProjectFile.LastIndexOf("\\"));
            return dir.Remove(0, dir.LastIndexOf("trunk\\") + "trunk\\".Length);
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
                GitUpdater.Update(this.SvnUrl, Logs.MainLog, Directories.RepositoryDir, this.GetProjectPathRelative());
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
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}