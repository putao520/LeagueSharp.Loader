#region LICENSE

// Copyright 2014 LeagueSharp.Loader
// GitUpdater.cs is part of LeagueSharp.Loader.
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

using System.Linq;

namespace LeagueSharp.Loader.Class
{
    #region

    using System;
    using System.Collections.Generic;
    using System.IO;
    using LeagueSharp.Loader.Data;
    using LibGit2Sharp;

    #endregion

    internal class GitUpdater
    {
        public static string Update(string url, Log log, string directory, string repoDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                Utility.Log(LogStatus.Skipped, "Updater", string.Format("No Url specified - {0}", url), log);
            }
            else
            {
                try
                {
                    var dir = Path.Combine(directory, url.GetHashCode().ToString("X"), "trunk");

                    if (Repository.IsValid(dir))
                    {
                        using (var repo = new Repository(dir))
                        {
                            repo.Config.Set("user.name", Config.Instance.Username);
                            repo.Config.Set("user.email", Config.Instance.Username + "@joduska.me");
                            repo.Fetch("origin");

                            if (repoDirectory != null)
                            {
                                repo.CheckoutPaths("origin/master", new List<String>() { repoDirectory }, 
                                    new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
                            }
  
                            repo.Checkout("origin/master", new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });
                        }

                    }
                    else
                    {
                        var oldPath = Path.Combine(directory, url.GetHashCode().ToString("X"));

                        if (Directory.Exists(oldPath))
                        {
                            Directory.Delete(oldPath, true);
                        }

                        Repository.Clone(url, dir, new CloneOptions { Checkout = true });
                        using (var repo = new Repository(dir))
                        {
                            repo.Config.Set("user.name", Config.Instance.Username);
                            repo.Config.Set("user.email", Config.Instance.Username + "@joduska.me");
                        }
                    }

                    return dir;
                }
                catch (Exception ex)
                {
                    Utility.Log(LogStatus.Error, "Updater", string.Format("{0} - {1}", ex.Message, url), log);
                }
            }

            return string.Empty;
        }

        /// <summary>
        /// Clearing unused folders to reduce file space usage.
        /// </summary>
        /// <param name="repoDirectory">Path to unused folder</param>
        /// <param name="log">Log</param>

        public static void ClearUnusedRepoFolder(string repoDirectory, Log log)
        {
            try
            {
                var dir = repoDirectory.Remove(repoDirectory.LastIndexOf("\\"));
                if (dir.EndsWith("trunk"))
                {
                    return;
                }
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
                dir = repoDirectory.Remove(dir.LastIndexOf("\\"));
                Directory.GetFiles(dir).ToList().ForEach(File.Delete);
            }
            catch (Exception ex)
            {
                Utility.Log(LogStatus.Error, "Updater", string.Format("{0} - {1}", ex.Message, repoDirectory), log);
            }
        }

        public static void ClearUnusedRepos(List<LeagueSharpAssembly> assemblyList)
        {
            try
            {
                var usedRepos = new List<string>();
                foreach (var assembly in assemblyList.Where(a => !string.IsNullOrEmpty(a.SvnUrl)))
                {
                    usedRepos.Add(assembly.SvnUrl.GetHashCode().ToString("X"));
                }

                var dirs = new List<string>(Directory.EnumerateDirectories(Directories.RepositoryDir));

                foreach (var dir in dirs)
                {
                    if (!usedRepos.Contains(Path.GetFileName(dir)))
                    {
                        Utility.ClearDirectory(dir);
                        Directory.Delete(dir);
                    }
                }
            }
            catch (Exception) { }
        }
    }
}