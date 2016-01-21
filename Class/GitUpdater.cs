#region LICENSE

// Copyright 2016-2016 LeagueSharp.Loader
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

namespace LeagueSharp.Loader.Class
{
    #region

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    using LeagueSharp.Loader.Data;

    using LibGit2Sharp;

    #endregion

    internal class GitUpdater
    {
        /// <summary>
        ///     Clearing unused folders to reduce file space usage.
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
                Utility.Log(LogStatus.Error, "Clear Unused", $"{ex.Message} - {repoDirectory}", log);
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
            catch (Exception ex)
            {
                Utility.Log(LogStatus.Error, "Clear Unused", ex.Message, Logs.MainLog);
            }
        }

        private static bool IsValid(string directory)
        {
            try
            {
                if (Repository.IsValid(directory))
                {
                    using (var repo = new Repository(directory))
                    {
                        if (repo.Info.IsHeadDetached)
                        {
                            return false;
                        }

                        if (repo.Info.IsBare)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception e)
            {
                Utility.Log(LogStatus.Error, "IsValid", e.Message, Logs.MainLog);
                return false;
            }

            return true;
        }

        private static bool Pull(string directory)
        {
            try
            {
                using (var repo = new Repository(directory))
                {
                    repo.Reset(ResetMode.Hard);
                    repo.RemoveUntrackedFiles();
                    repo.Network.Pull(
                        new Signature(Config.Instance.Username, $"{Config.Instance.Username}@joduska.me", DateTimeOffset.Now),
                        new PullOptions
                            {
                                MergeOptions =
                                    new MergeOptions
                                        {
                                            FastForwardStrategy = FastForwardStrategy.Default,
                                            FileConflictStrategy = CheckoutFileConflictStrategy.Theirs,
                                            MergeFileFavor = MergeFileFavor.Theirs,
                                            CommitOnSuccess = true
                                        }
                            });

                    repo.Checkout(repo.Head, new CheckoutOptions { CheckoutModifiers = CheckoutModifiers.Force });

                    if (repo.Info.IsHeadDetached)
                    {
                        Utility.Log(LogStatus.Error, "Pull", "Update+Detached", Logs.MainLog);
                    }
                }

                return true;
            }
            catch (Exception e)
            {
                Utility.Log(LogStatus.Error, "Pull", e.Message, Logs.MainLog);
                return false;
            }
        }

        private static bool Clone(string url, string directory)
        {
            try
            {
                if (Directory.Exists(directory))
                {
                    Directory.Delete(directory, true);
                }

                Repository.Clone(url, directory);
                return true;
            }
            catch (Exception e)
            {
                Utility.Log(LogStatus.Error, "Clone", e.Message, Logs.MainLog);
                return false;
            }
        }

        internal static string Update(string url)
        {
            var root = Path.Combine(Directories.RepositoryDir, url.GetHashCode().ToString("X"), "trunk");

            if (!IsValid(root))
            {
                var cloneResult = Clone(url, root);

                if (!cloneResult)
                {
                    Utility.Log(LogStatus.Error, "Updater", $"Failed to Clone - {url}", Logs.MainLog);
                    return root;
                }
            }

            var pullResult = Pull(root);

            if (!pullResult)
            {
                Utility.Log(LogStatus.Error, "Updater", $"Failed to Pull Updates - {url}", Logs.MainLog);
            }

            return root;
        }
    }
}