#region LICENSE

// Copyright 2014 LeagueSharp.Loader
// App.xaml.cs is part of LeagueSharp.Loader.
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

using System.Diagnostics;

namespace LeagueSharp.Loader
{
    #region

    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Windows;
    using LeagueSharp.Loader.Class;
    using LeagueSharp.Loader.Data;
    using MahApps.Metro;

    #endregion

    public partial class App
    {
        private Mutex _mutex;

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        protected override void OnStartup(StartupEventArgs e)
        {
            if (File.Exists(Updater.SetupFile))
            {
                Thread.Sleep(1000);
            }

            bool createdNew;
            _mutex = new Mutex(true, Utility.Md5Hash(Environment.UserName), out createdNew);

            Utility.CreateFileFromResource(Directories.ConfigFilePath, "LeagueSharp.Loader.Resources.config.xml");

            var configCorrupted = false;
            try
            {
                Config.Instance = ((Config) Utility.MapXmlFileToClass(typeof(Config), Directories.ConfigFilePath));
            }
            catch (Exception)
            {
                configCorrupted = true;
            }

            if (!configCorrupted)
            {
                try
                {
                    if (File.Exists(Directories.ConfigFilePath + ".bak"))
                    {
                        File.Delete(Directories.ConfigFilePath + ".bak");
                    }
                    File.Copy(Directories.ConfigFilePath, Directories.ConfigFilePath + ".bak");
                    File.SetAttributes(Directories.ConfigFilePath + ".bak", FileAttributes.Hidden);
                }
                catch (Exception)
                {
                    //ignore
                }
            }
            else
            {
                try
                {
                    Config.Instance = ((Config)Utility.MapXmlFileToClass(typeof(Config), Directories.ConfigFilePath + ".bak"));
                    File.Delete(Directories.ConfigFilePath);
                    File.Copy(Directories.ConfigFilePath + ".bak", Directories.ConfigFilePath);
                    File.SetAttributes(Directories.ConfigFilePath, FileAttributes.Normal);
                }
                catch (Exception)
                {
                    File.Delete(Directories.ConfigFilePath + ".bak");
                    File.Delete(Directories.ConfigFilePath);
                    MessageBox.Show("Couldn't load config.xml.");
                    Environment.Exit(0);
                }
            }

            #region Add GameSetting DisableDrawings

            if (Config.Instance.Settings.GameSettings.All(x => x.Name != "Show Drawings"))
            {
                Config.Instance.Settings.GameSettings.Add(new GameSettings
                {
                    Name = "Show Drawings",
                    PosibleValues = new List<string> { "True", "False" },
                    SelectedValue = "True"
                });
            }

            if (Config.Instance.Settings.GameSettings.All(x => x.Name != "Show Ping"))
            {
                Config.Instance.Settings.GameSettings.Add(new GameSettings
                {
                    Name = "Show Ping",
                    PosibleValues = new List<string> { "True", "False" },
                    SelectedValue = "True"
                });
            }

            if (Config.Instance.Settings.GameSettings.All(x => x.Name != "Send Anonymous Assembly Statistics"))
            {
                Config.Instance.Settings.GameSettings.Add(new GameSettings
                {
                    Name = "Send Anonymous Assembly Statistics",
                    PosibleValues = new List<string> { "True", "False" },
                    SelectedValue = "True"
                });
            }

            if (Config.Instance.Settings.GameSettings.All(x => x.Name != "Always Inject Default Profile"))
            {
                Config.Instance.Settings.GameSettings.Add(new GameSettings
                {
                    Name = "Always Inject Default Profile",
                    PosibleValues = new List<string> { "True", "False" },
                    SelectedValue = "False"
                });
            }

            try
            {
                if (Config.Instance.Profiles.First().InstalledAssemblies.All(a => a.Name != "LeagueSharp.SDK"))
                {
                    var sdk = new LeagueSharpAssembly
                    {
                        Name = "LeagueSharp.SDK",
                        DisplayName = "LeagueSharp.SDK",
                        SvnUrl = "https://github.com/LeagueSharp/LeagueSharp.SDK",
                        InjectChecked = true,
                        InstallChecked = true,
                        PathToProjectFile = Path.Combine(Directories.RepositoryDir, "8443D874", "trunk", "LeagueSharp.SDK.csproj")
                    };

                    sdk.Update();
                    sdk.Compile();

                    Config.Instance.Profiles.First().InstalledAssemblies.Add(sdk);
                }
            }
            catch
            {
                // wtf
            }

            #endregion

            #region Remove the old loader

            try
            {
                if (String.Compare(
                   Process.GetCurrentProcess().ProcessName, "LeagueSharp.Loader.exe",
                   StringComparison.InvariantCultureIgnoreCase) != 0 && File.Exists(Path.Combine(Directories.CurrentDirectory, "LeagueSharp.Loader.exe")))
                {
                    File.Delete(Path.Combine(Directories.CurrentDirectory, "LeagueSharp.Loader.exe"));
                    File.Delete(Path.Combine(Directories.CurrentDirectory, "LeagueSharp.Loader.exe.config"));
                }
            }
            catch (Exception ex)
            {
                //ignore
            }

            #endregion

            #region AppData randomization

            try
            {
                if (!Directory.Exists(Directories.AppDataDirectory))
                {
                    Directory.CreateDirectory(Directories.AppDataDirectory);

                    var oldPath = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData), "LeagueSharp" + Environment.UserName.GetHashCode().ToString("X"));

                    var oldPath2 = Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.ApplicationData), "LeagueSharp");

                    if (Directory.Exists(oldPath))
                    {
                        Utility.CopyDirectory(oldPath, Directories.AppDataDirectory, true, true);
                        Utility.ClearDirectory(oldPath);
                        Directory.Delete(oldPath, true);
                    }

                    if (Directory.Exists(oldPath2))
                    {
                        Utility.CopyDirectory(oldPath2, Directories.AppDataDirectory, true, true);
                        Utility.ClearDirectory(oldPath2);
                        Directory.Delete(oldPath2, true);
                    }
                }
            }
            catch (Exception ex)
            {
                //ignore
            }

            #endregion

            //Load the language resources.
            var dict = new ResourceDictionary();

            if (Config.Instance.SelectedLanguage != null)
            {
                dict.Source = new Uri(
                    "..\\Resources\\Language\\" + Config.Instance.SelectedLanguage + ".xaml", UriKind.Relative);
            }
            else
            {
                var lid = Thread.CurrentThread.CurrentCulture.ToString().Contains("-")
                    ? Thread.CurrentThread.CurrentCulture.ToString().Split('-')[0].ToUpperInvariant()
                    : Thread.CurrentThread.CurrentCulture.ToString().ToUpperInvariant();
                switch (lid)
                {
                    case "DE":
                        dict.Source = new Uri("..\\Resources\\Language\\German.xaml", UriKind.Relative);
                        break;
                    case "AR":
                        dict.Source = new Uri("..\\Resources\\Language\\Arabic.xaml", UriKind.Relative);
                        break;
                    case "ES":
                        dict.Source = new Uri("..\\Resources\\Language\\Spanish.xaml", UriKind.Relative);
                        break;
                    case "FR":
                        dict.Source = new Uri("..\\Resources\\Language\\French.xaml", UriKind.Relative);
                        break;
                    case "IT":
                        dict.Source = new Uri("..\\Resources\\Language\\Italian.xaml", UriKind.Relative);
                        break;
                    case "KO":
                        dict.Source = new Uri("..\\Resources\\Language\\Korean.xaml", UriKind.Relative);
                        break;
                    case "NL":
                        dict.Source = new Uri("..\\Resources\\Language\\Dutch.xaml", UriKind.Relative);
                        break;
                    case "PL":
                        dict.Source = new Uri("..\\Resources\\Language\\Polish.xaml", UriKind.Relative);
                        break;
                    case "PT":
                        dict.Source = new Uri("..\\Resources\\Language\\Portuguese.xaml", UriKind.Relative);
                        break;
                    case "RO":
                        dict.Source = new Uri("..\\Resources\\Language\\Romanian.xaml", UriKind.Relative);
                        break;
                    case "RU":
                        dict.Source = new Uri("..\\Resources\\Language\\Russian.xaml", UriKind.Relative);
                        break;
                    case "SE":
                        dict.Source = new Uri("..\\Resources\\Language\\Swedish.xaml", UriKind.Relative);
                        break;
                    case "TR":
                        dict.Source = new Uri("..\\Resources\\Language\\Turkish.xaml", UriKind.Relative);
                        break;
                    case "VI":
                        dict.Source = new Uri("..\\Resources\\Language\\Vietnamese.xaml", UriKind.Relative);
                        break;
                    case "ZH":
                        dict.Source = new Uri("..\\Resources\\Language\\Chinese.xaml", UriKind.Relative);
                        break;
                    case "LT":
                        dict.Source = new Uri("..\\Resources\\Language\\Lithuanian.xaml", UriKind.Relative);
                        break;
                    case "CZ":
                        dict.Source = new Uri("..\\Resources\\Language\\Czech.xaml", UriKind.Relative);
                        break;
                    default:
                        dict.Source = new Uri("..\\Resources\\Language\\English.xaml", UriKind.Relative);
                        break;
                }
            }

            if (Config.Instance.SelectedColor != null)
            {
                ThemeManager.ChangeAppStyle(Application.Current, ThemeManager.GetAccent(Config.Instance.SelectedColor), ThemeManager.GetAppTheme("BaseLight"));
            }

            Resources.MergedDictionaries.Add(dict);

            #region Executable Randomization

            if (Assembly.GetExecutingAssembly().Location.EndsWith("loader.exe") || Assembly.GetExecutingAssembly().Location.EndsWith("Loader.exe"))
            {
                try
                {
                    if (Config.Instance.RandomName != null)
                    {
                        try
                        {
                            var oldFile = Path.Combine(Directories.CurrentDirectory, Config.Instance.RandomName);
                            var oldConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, Config.Instance.RandomName + ".config");

                            if (File.Exists(oldFile))
                            {
                                File.SetAttributes(oldFile, FileAttributes.Normal);
                                File.Delete(oldFile);
                            }

                            if (File.Exists(oldConfigFile))
                            {
                                File.SetAttributes(oldConfigFile, FileAttributes.Normal);
                                File.Delete(oldConfigFile);
                            }
                        }
                        catch
                        {
                        }

                        if (!createdNew)
                        {
                            if (e.Args.Length > 0)
                            {
                                var loader = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Config.Instance.RandomName)).FirstOrDefault();
                                if (loader != null && loader.MainWindowHandle != IntPtr.Zero)
                                {
                                    Clipboard.SetText(e.Args[0]);
                                    ShowWindow(loader.MainWindowHandle, 5);
                                    SetForegroundWindow(loader.MainWindowHandle);
                                }
                            }

                            _mutex = null;
                            Environment.Exit(0);
                        }
                    }

                    Config.Instance.RandomName = Utility.GetUniqueKey(6) + ".exe";
                    var filePath = Path.Combine(Directories.CurrentDirectory, "loader.exe");
                    var rndExePath = Path.Combine(Directories.CurrentDirectory, Config.Instance.RandomName);
                    Utility.MapClassToXmlFile(typeof(Config), Config.Instance, Directories.ConfigFilePath);

                    File.Copy(filePath, rndExePath);
                    File.Copy(filePath + ".config", rndExePath + ".config");
                    Process.Start(rndExePath);
                    Environment.Exit(0);
                }
                catch (Exception)
                {
                    // restart
                }
            }

            AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
                {
                    try
                    {
                        if (_mutex != null && createdNew)
                        {
                            _mutex.ReleaseMutex();
                        }
                    }
                    catch
                    {
                    }

                    if (!Assembly.GetExecutingAssembly().Location.EndsWith("loader.exe"))
                    {
                        var oldConfigFile = Path.Combine(
                            AppDomain.CurrentDomain.BaseDirectory,
                            Config.Instance.RandomName + ".config");

                        var info = new ProcessStartInfo
                            {
                                Arguments =
                                    "/C choice /C Y /N /D Y /T 1 & Del " + Assembly.GetExecutingAssembly().Location + " "
                                    + oldConfigFile,
                                WindowStyle = ProcessWindowStyle.Hidden,
                                CreateNoWindow = true,
                                FileName = "cmd.exe"
                            };
                        Process.Start(info);
                    }
                };

            #endregion

            base.OnStartup(e);
        }
    }
}
