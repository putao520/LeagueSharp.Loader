#region LICENSE

// Copyright 2016-2016 LeagueSharp.Loader
// InstallerWindow.xaml.cs is part of LeagueSharp.Loader.
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

namespace LeagueSharp.Loader.Views
{
    #region

    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;

    using LeagueSharp.Loader.Class;
    using LeagueSharp.Loader.Class.Installer;
    using LeagueSharp.Loader.Data;

    using MahApps.Metro.Controls.Dialogs;

    using PlaySharp.Service.Model;

    using RestSharp.Extensions.MonoHttp;

    #endregion

    public partial class InstallerWindow : INotifyPropertyChanged
    {
        private bool _ableToList = true;

        private List<LeagueSharpAssembly> _foundAssemblies = new List<LeagueSharpAssembly>();

        public InstallerWindow()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private ProgressDialogController controller { get; set; }

        public bool AbleToList
        {
            get
            {
                return this._ableToList;
            }
            set
            {
                this._ableToList = value;
                this.OnPropertyChanged("AbleToList");
            }
        }

        public List<LeagueSharpAssembly> FoundAssemblies
        {
            get
            {
                return this._foundAssemblies;
            }
            set
            {
                this._foundAssemblies = value;
                this.OnPropertyChanged("FoundAssemblies");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public static async Task InstallAssembly(AssemblyEntry assembly, bool silent)
        {
            if (Config.Instance.SelectedProfile.InstalledAssemblies.Any(a => a.Name == assembly.Name))
            {
                return;
            }

            try
            {
                var projectName = Path.GetFileNameWithoutExtension(new Uri(assembly.GithubUrl).AbsolutePath);
                var repositoryMatch = Regex.Match(assembly.GithubUrl, @"^(http[s]?)://(?<host>.*?)/(?<author>.*?)/(?<repo>.*?)(/{1}|$)");
                var repositoryUrl = $"https://{repositoryMatch.Groups["host"]}/{repositoryMatch.Groups["author"]}/{repositoryMatch.Groups["repo"]}";

                var installer = new InstallerWindow { Owner = MainWindow.Instance };

                if (silent)
                {
                    await installer.ListAssemblies(repositoryUrl, true, true, HttpUtility.UrlDecode(projectName));
                    installer.Close();
                    return;
                }

                installer.ShowProgress(repositoryUrl, true, HttpUtility.UrlDecode(projectName));
                installer.ShowDialog();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        public static void InstallAssembly(Match m)
        {
            var gitHubUser = m.Groups[2].ToString();
            var repositoryName = m.Groups[3].ToString();
            var assemblyName = m.Groups[4].ToString();

            var w = new InstallerWindow { Owner = MainWindow.Instance };
            w.ShowProgress($"https://github.com/{gitHubUser}/{repositoryName}", true, assemblyName != "" ? m.Groups[4].ToString() : null);
            w.ShowDialog();
        }

        public async Task InstallSelected(bool silent)
        {
            var list = this.FoundAssemblies.Where(a => a.InstallChecked).ToList();
            var amount = this.FoundAssemblies.Count(a => a.InstallChecked);

            try
            {
                var di = new DependencyInstaller(list.Select(a => a.PathToProjectFile).ToList());
                await di.SatisfyAsync();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            await MainWindow.Instance.PrepareAssemblies(list, true, true);

            foreach (var assembly in list)
            {
                if (File.Exists(assembly.PathToBinary))
                {
                    Config.Instance.SelectedProfile.InstalledAssemblies.Add(assembly);
                    amount--;
                }
            }

            if (silent)
            {
                return;
            }

            if (amount == 0)
            {
                this.AfterInstallMessage(Utility.GetMultiLanguageText("SuccessfullyInstalled"), true);
            }
            else
            {
                this.AfterInstallMessage(Utility.GetMultiLanguageText("ErrorInstalling"), true);
            }
        }

        public async Task ListAssemblies(string location, bool isSvn, bool silent, string autoInstallName = null)
        {
            this.AbleToList = false;

            if (!isSvn)
            {
                this.FoundAssemblies = LeagueSharpAssemblies.GetAssemblies(location);
            }
            else
            {
                await Task.Factory.StartNew(
                    () =>
                        {
                            var updatedDir = GitUpdater.Update(location);

                            if (Config.Instance.BlockedRepositories.Any(location.StartsWith))
                            {
                                this.FoundAssemblies = new List<LeagueSharpAssembly>();
                            }
                            else
                            {
                                this.FoundAssemblies = LeagueSharpAssemblies.GetAssemblies(updatedDir, location);
                            }

                            foreach (var assembly in this.FoundAssemblies.ToArray())
                            {
                                var assemblies =
                                    Config.Instance.SelectedProfile.InstalledAssemblies.Where(
                                        y => y.Name == assembly.Name && y.SvnUrl == assembly.SvnUrl).ToList();
                                assemblies.ForEach(a => this.FoundAssemblies.Remove(a));
                            }

                            if (autoInstallName != null)
                            {
                                foreach (var assembly in this.FoundAssemblies)
                                {
                                    if (assembly.Name.ToLower() == autoInstallName.ToLower())
                                    {
                                        assembly.InstallChecked = true;

                                        Application.Current.Dispatcher.Invoke(() => { this.search.Text = autoInstallName; });
                                    }
                                }
                            }
                        });
            }

            this.AbleToList = true;
            this.installTabControl.SelectedIndex++;

            if (autoInstallName != null)
            {
                await this.InstallSelected(silent);
            }
        }

        public async void ShowProgress(string location, bool isSvn, string autoInstallName = null)
        {
            while (!this.IsInitialized || !this.IsVisible)
            {
                await Task.Delay(100);
            }

            try
            {
                this.controller =
                    await this.ShowProgressAsync(Utility.GetMultiLanguageText("Updating"), Utility.GetMultiLanguageText("DownloadingData"));
                this.controller.SetIndeterminate();
                this.controller.SetCancelable(true);
            }
            catch
            {
            }

            await this.ListAssemblies(location, isSvn, false, autoInstallName);

            try
            {
                await this.controller.CloseAsync();
            }
            catch
            {
            }
        }

        private async void AfterInstallMessage(string msg, bool close = false)
        {
            if (close)
            {
                Config.Save(true);

                if (this.IsVisible)
                {
                    this.Close();
                }

                return;
            }

            await this.ShowMessageAsync(Utility.GetMultiLanguageText("Installer"), msg);
        }

        private void InstallerWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.SvnComboBox.ItemsSource = Config.Instance.KnownRepositories;
        }

        private void OnPropertyChanged(string propertyName)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void PathTextBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            this.SvnRadioButton.IsChecked = false;
            this.LocalRadioButton.IsChecked = !this.SvnRadioButton.IsChecked;
        }

        private void PathTextBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var textBox = (TextBox)sender;
            if (textBox != null && string.IsNullOrWhiteSpace(textBox.SelectedText))
            {
                var folderDialog = new FolderSelectDialog();

                folderDialog.Title = "Select project folder";

                if (folderDialog.ShowDialog())
                {
                    textBox.Text = folderDialog.FileName;
                    this.LocalRadioButton.IsChecked = true;
                }
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var assembly in this.FoundAssemblies)
            {
                assembly.InstallChecked = true;
            }
            this.OnPropertyChanged("FoundAssemblies");
        }

        private void Step1_Click(object sender, RoutedEventArgs e)
        {
            if (this.InstalledRadioButton.IsChecked == true)
            {
                this.FoundAssemblies.Clear();
                foreach (var profile in Config.Instance.Profiles)
                {
                    foreach (var assembly in profile.InstalledAssemblies)
                    {
                        this.FoundAssemblies.Add(assembly.Copy());
                    }
                }
                this.FoundAssemblies = this.FoundAssemblies.Distinct().ToList();

                this.installTabControl.SelectedIndex++;
            }
            else
            {
                this.ShowProgress(
                    this.SvnRadioButton.IsChecked == true ? this.SvnComboBox.Text : this.PathTextBox.Text,
                    this.SvnRadioButton.IsChecked == true);
            }
        }

        private async void Step2_Click(object sender, RoutedEventArgs e)
        {
            await this.InstallSelected(false);
        }

        private void Step2P_Click(object sender, RoutedEventArgs e)
        {
            this.installTabControl.SelectedIndex--;
        }

        private void SvnComboBox_OnGotFocus(object sender, RoutedEventArgs e)
        {
            this.SvnRadioButton.IsChecked = true;
            this.LocalRadioButton.IsChecked = !this.SvnRadioButton.IsChecked;
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = this.search.Text;
            var view = CollectionViewSource.GetDefaultView(this.FoundAssemblies);
            searchText = searchText.Replace("*", "(.*)");
            view.Filter = obj =>
                {
                    try
                    {
                        var assembly = obj as LeagueSharpAssembly;
                        var nameMatch = Regex.Match(assembly.Name, searchText, RegexOptions.IgnoreCase);

                        return nameMatch.Success;
                    }
                    catch (Exception)
                    {
                        return true;
                    }
                };
        }

        private void UnselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var assembly in this.FoundAssemblies)
            {
                assembly.InstallChecked = false;
            }
            this.OnPropertyChanged("FoundAssemblies");
        }
    }
}