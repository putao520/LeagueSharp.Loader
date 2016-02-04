#region LICENSE

// Copyright 2016-2016 LeagueSharp.Loader
// ProjectFile.cs is part of LeagueSharp.Loader.
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
    using System;
    using System.IO;

    using LeagueSharp.Loader.Data;

    using Microsoft.Build.Evaluation;

    [Serializable]
    internal class ProjectFile
    {
        private readonly Log log;

        public readonly Project Project;

        public ProjectFile(string file, Log log)
        {
            try
            {
                this.log = log;

                if (File.Exists(file))
                {
                    ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
                    this.Project = new Project(file);
                }
            }
            catch (Exception ex)
            {
                Utility.Log(LogStatus.Error, "ProjectFile", $"Error - {ex.Message}", this.log);
            }
        }

        public string Configuration { get; set; }

        public string PlatformTarget { get; set; }

        public string ReferencesPath { get; set; }

        public void Change()
        {
            try
            {
                if (this.Project == null)
                {
                    return;
                }

                this.Project.SetGlobalProperty("Configuration", this.Configuration);
                this.Project.SetGlobalProperty("Platform", this.PlatformTarget);
                this.Project.SetGlobalProperty("PlatformTarget", this.PlatformTarget);

                this.Project.SetGlobalProperty("PreBuildEvent", string.Empty);
                this.Project.SetGlobalProperty("PostBuildEvent", string.Empty);

                this.Project.SetGlobalProperty("DebugSymbols", this.Configuration == "Release" ? "false" : "true");
                this.Project.SetGlobalProperty("DebugType", this.Configuration == "Release" ? "None" : "full");
                this.Project.SetGlobalProperty("Optimize", this.Configuration == "Release" ? "true" : "false");
                this.Project.SetGlobalProperty("DefineConstants", this.Configuration == "Release" ? "TRACE" : "DEBUG;TRACE");

                this.Project.SetGlobalProperty("OutputPath", "bin\\" + this.Configuration + "\\");

                foreach (var item in this.Project.GetItems("Reference"))
                {
                    var hintPath = item?.GetMetadata("HintPath");

                    if (!string.IsNullOrWhiteSpace(hintPath?.EvaluatedValue))
                    {
                        item.SetMetadataValue("HintPath", Path.Combine(this.ReferencesPath, Path.GetFileName(hintPath.EvaluatedValue)));
                    }
                }

                this.Project.Save();
            }
            catch (Exception ex)
            {
                Utility.Log(LogStatus.Error, "ProjectFile", ex.Message, this.log);
            }
        }
    }
}