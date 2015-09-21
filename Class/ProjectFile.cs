#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
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
    #region

    using System;
    using System.IO;

    using LeagueSharp.Loader.Data;

    using Microsoft.Build.Evaluation;

    #endregion

    [Serializable]
    internal class ProjectFile
    {
        public ProjectFile(string file, Log log)
        {
            try
            {
                this._log = log;

                if (File.Exists(file))
                {
                    ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
                    this.Project = new Project(file);
                }
            }
            catch (Exception ex)
            {
                Utility.Log(LogStatus.Error, "ProjectFile", string.Format("Error - {0}", ex.Message), this._log);
            }
        }

        public readonly Project Project;

        private readonly Log _log;

        public string Configuration { get; set; }

        public string PlatformTarget { get; set; }

        public bool PostbuildEvent { get; set; }

        public bool PrebuildEvent { get; set; }

        public string ReferencesPath { get; set; }

        public bool ResetOutputPath { get; set; }

        public bool UpdateReferences { get; set; }

        public void Change()
        {
            try
            {
                if (this.Project == null)
                {
                    return;
                }
                if (!string.IsNullOrWhiteSpace(this.Configuration))
                {
                    this.Project.SetProperty("Configuration", this.Configuration);
                    this.Project.Save();
                }
                if (this.PrebuildEvent)
                {
                    this.Project.SetProperty("PreBuildEvent", string.Empty);
                }
                if (this.PostbuildEvent)
                {
                    this.Project.SetProperty("PostBuildEvent", string.Empty);
                }
                if (!string.IsNullOrWhiteSpace(this.PlatformTarget))
                {
                    this.Project.SetProperty("PlatformTarget", this.PlatformTarget);
                }
                var outputPath = this.Project.GetProperty("OutputPath");
                if (this.ResetOutputPath || outputPath == null || string.IsNullOrWhiteSpace(outputPath.EvaluatedValue))
                {
                    this.Project.SetProperty("OutputPath", "bin\\" + this.Configuration);
                }
                if (this.UpdateReferences)
                {
                    foreach (var item in this.Project.GetItems("Reference"))
                    {
                        if (item == null)
                        {
                            continue;
                        }
                        var hintPath = item.GetMetadata("HintPath");
                        if (hintPath != null && !string.IsNullOrWhiteSpace(hintPath.EvaluatedValue))
                        {
                            item.SetMetadataValue(
                                "HintPath",
                                Path.Combine(this.ReferencesPath, Path.GetFileName(hintPath.EvaluatedValue)));
                        }
                    }
                }

                var targetFramework = this.Project.GetProperty("TargetFrameworkVersion").EvaluatedValue;

                switch (targetFramework)
                {
                    case "v4.5":
                    case "v4.5.1":
                        this.Project.SetProperty("TargetFrameworkVersion", "v4.5.2");
                        break;

                    case "v4.6":
                        break;
                }

                this.Project.Save();
                Utility.Log(
                    LogStatus.Ok,
                    "ProjectFile",
                    string.Format("File Updated - {0}", this.Project.FullPath),
                    this._log);
            }
            catch (Exception ex)
            {
                Utility.Log(LogStatus.Error, "ProjectFile", ex.Message, this._log);
            }
        }
    }
}