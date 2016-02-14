#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// Updater.cs is part of LeagueSharp.Loader.
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
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;

    using LeagueSharp.Loader.Data;
    using LeagueSharp.Loader.Views;

    using PlaySharp.Service.Model;

    #endregion

    internal class Updater
    {
        public delegate void RepositoriesUpdateDelegate(List<string> list);

        public enum CoreUpdateState
        {
            Operational,

            Maintenance,

            Unknown
        }

        public const string VersionCheckURL = "http://api.joduska.me/public/deploy/loader/version";

        public static bool CheckedForUpdates = false;

        public static MainWindow MainWindow;

        public static string SetupFile = Path.Combine(Directories.CurrentDirectory, "LeagueSharp-update.exe");

        public static string UpdateZip = Path.Combine(Directories.CoreDirectory, "update.zip");

        public static bool Updating = false;

        public static Tuple<bool, string> CheckLoaderVersion()
        {
            try
            {
                using (var client = new WebClient())
                {
                    var data = client.DownloadData(VersionCheckURL);
                    var ser = new DataContractJsonSerializer(typeof(UpdateInfo));
                    var updateInfo = (UpdateInfo)ser.ReadObject(new MemoryStream(data));
                    var v = Version.Parse(updateInfo.version);
                    if (Utility.VersionToInt(Assembly.GetEntryAssembly().GetName().Version) < Utility.VersionToInt(v))
                    {
                        return new Tuple<bool, string>(true, updateInfo.url);
                    }
                }
            }
            catch
            {
                return new Tuple<bool, string>(false, "");
            }

            return new Tuple<bool, string>(false, "");
        }

        public static async Task UpdateBlockedRepos()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response =
                        await
                        client.GetAsync("https://raw.githubusercontent.com/LeagueSharp/LeagueSharp.Loader/master/Updates/BlockedRepositories.txt");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                        Config.Instance.BlockedRepositories = new List<string>(lines);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async Task UpdateRepositories()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.Timeout = TimeSpan.FromSeconds(5);
                    var response = await client.GetAsync("https://loader.joduska.me/repositories.txt");

                    if (response.IsSuccessStatusCode)
                    {
                        var content = await response.Content.ReadAsStringAsync();
                        var result = new List<string>();

                        try
                        {
                            var matches = Regex.Matches(content, "<repo>(.*)</repo>");
                            result.AddRange(from Match match in matches select match.Groups[1].ToString());
                        }
                        catch (Exception)
                        {
                            // ignored
                        }

                        Config.Instance.KnownRepositories = new ObservableCollection<string>(result);
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static async Task<bool> IsSupported(string path)
        {
            if (Directory.Exists(Path.Combine(Directories.CurrentDirectory, "iwanttogetbanned")))
            {
                return true;
            }

            try
            {
                if (!WebService.Client.IsAuthenticated)
                {
                    Utility.Log(LogStatus.Error, "IsSupported", "WebService authentication failed", Logs.MainLog);
                    return false;
                }

                var leagueChecksum = Utility.Md5Checksum(path);
                var coreChecksum = Utility.Md5Checksum(Directories.CoreFilePath);
                var core = WebService.Client.Core(leagueChecksum);

                if (core == null)
                {
                    Utility.Log(LogStatus.Error, "IsSupported", "Failed to receive Core version from WebService", Logs.MainLog);
                    return false;
                }

                return core.HashCore == coreChecksum;
            }
            catch (Exception e)
            {
                Utility.Log(LogStatus.Error, "IsSupported", e.Message, Logs.MainLog);
            }

            return false;
        }

        public static async Task<UpdateResponse> UpdateCore(string path, bool showMessages)
        {
            if (Directory.Exists(Path.Combine(Directories.CurrentDirectory, "iwanttogetbanned")))
            {
                return new UpdateResponse(CoreUpdateState.Operational, Utility.GetMultiLanguageText("NotUpdateNeeded"));
            }

            try
            {
                if (!WebService.Client.IsAuthenticated)
                {
                    Utility.Log(LogStatus.Error, "UpdateCore", "WebService authentication failed", Logs.MainLog);
                    return new UpdateResponse(CoreUpdateState.Unknown, "WebService authentication failed");
                }

                var leagueChecksum = Utility.Md5Checksum(path);
                var coreChecksum = Utility.Md5Checksum(Directories.CoreFilePath);
                var core = WebService.Client.Core(leagueChecksum);

                if (core == null)
                {
                    return new UpdateResponse(
                        CoreUpdateState.Maintenance,
                        Utility.GetMultiLanguageText("WrongVersion") + Environment.NewLine + leagueChecksum);
                }

                if (core.HashCore != coreChecksum && (core.Url.StartsWith("https://github.com/joduskame/") || core.Url.StartsWith("https://github.com/LeagueSharp/")))
                {
                    try
                    {
                        var result = CoreUpdateState.Unknown;

                        await Application.Current.Dispatcher.Invoke(
                            async () =>
                                {
                                    var window = new UpdateWindow(UpdateAction.Core, core.Url);
                                    window.Show();

                                    if (await window.Update())
                                    {
                                        result = CoreUpdateState.Operational;
                                    }
                                });

                        return new UpdateResponse(result, Utility.GetMultiLanguageText("UpdateSuccess"));
                    }
                    catch (Exception e)
                    {
                        var message = Utility.GetMultiLanguageText("FailedToDownload") + e;

                        if (showMessages)
                        {
                            MessageBox.Show(message);
                        }

                        return new UpdateResponse(CoreUpdateState.Unknown, message);
                    }
                    finally
                    {
                        if (File.Exists(UpdateZip))
                        {
                            File.Delete(UpdateZip);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Utility.Log(LogStatus.Error, "UpdateCore", e.Message, Logs.MainLog);
            }

            return new UpdateResponse(CoreUpdateState.Operational, Utility.GetMultiLanguageText("NotUpdateNeeded"));
        }

        public static async Task UpdateLoader(Tuple<bool, string> versionCheckResult)
        {
            if (versionCheckResult.Item1
                && (versionCheckResult.Item2.StartsWith("https://github.com/LeagueSharp/")
                    || versionCheckResult.Item2.StartsWith("https://github.com/joduskame/")
                    || versionCheckResult.Item2.StartsWith("https://github.com/Esk0r/")))
            {
                var window = new UpdateWindow(UpdateAction.Loader, versionCheckResult.Item2);
                window.Show();
                await window.Update();
            }
        }

        public class UpdateResponse
        {
            public UpdateResponse(CoreUpdateState state, string message = "")
            {
                this.State = state;
                this.Message = message;
            }

            public string Message { get; set; }

            public CoreUpdateState State { get; set; }
        }

        [DataContract]
        internal class UpdateInfo
        {
            [DataMember]
            internal string url;

            [DataMember]
            internal bool valid;

            [DataMember]
            internal string version;
        }

        public static async Task UpdateWebService()
        {
            try
            {
                var assemblies = new ObservableCollection<AssemblyEntry>();

                await Task.Factory.StartNew(
                    () =>
                    {
                        try
                        {
                            var entries = AssemblyDatabase.GetAssemblies();
                            foreach (var entry in entries)
                            {
                                entry.Name = entry.Name.WebDecode();
                                entry.Description = entry.Description.WebDecode();
                            }
                            entries.ShuffleRandom();

                            assemblies = new ObservableCollection<AssemblyEntry>(entries);
                        }
                        catch(Exception e)
                        {
                            Utility.Log(LogStatus.Error, "UpdateWebService", e.Message, Logs.MainLog);
                        }
                    });

                Config.Instance.DatabaseAssemblies = assemblies;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}