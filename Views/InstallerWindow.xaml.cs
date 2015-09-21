#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
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
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Input;

    using LeagueSharp.Loader.Class;
    using LeagueSharp.Loader.Data;

    using MahApps.Metro.Controls.Dialogs;

    #endregion

    public partial class InstallerWindow : INotifyPropertyChanged
    {
        public InstallerWindow()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }

        private bool _ableToList = true;

        private List<LeagueSharpAssembly> _foundAssemblies = new List<LeagueSharpAssembly>();

        private ProgressDialogController controller;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public void InstallSelected()
        {
            var amount = this.FoundAssemblies.Count(a => a.InstallChecked);

            foreach (var assembly in this.FoundAssemblies.ToArray())
            {
                if (assembly.InstallChecked)
                {
                    if (assembly.Compile())
                    {
                        if (
                            Config.Instance.SelectedProfile.InstalledAssemblies.All(
                                a => a.Name != assembly.Name || a.SvnUrl != assembly.SvnUrl))
                        {
                            Config.Instance.SelectedProfile.InstalledAssemblies.Add(assembly);
                            this.FoundAssemblies.Remove(assembly);
                        }
                        amount--;
                    }
                }
            }

            this.FoundAssemblies.Where(a => !string.IsNullOrEmpty(a.SvnUrl))
                .ToList()
                .ForEach(x => GitUpdater.ClearUnusedRepoFolder(x.PathToProjectFile, Logs.MainLog));

            if (amount == 0)
            {
                this.AfterInstallMessage(Utility.GetMultiLanguageText("SuccessfullyInstalled"), true);
            }
            else
            {
                this.AfterInstallMessage(Utility.GetMultiLanguageText("ErrorInstalling"));
            }
        }

        public void ListAssemblies(string location, bool isSvn, string autoInstallName = null)
        {
            this.AbleToList = false;
            var bgWorker = new BackgroundWorker();

            if (!isSvn)
            {
                bgWorker.DoWork += delegate { FoundAssemblies = LeagueSharpAssemblies.GetAssemblies(location); };
            }
            else
            {
                bgWorker.DoWork += delegate
                    {
                        var updatedDir = GitUpdater.Update(location, Logs.MainLog, Directories.RepositoryDir);
                        FoundAssemblies = LeagueSharpAssemblies.GetAssemblies(updatedDir, location);

                        foreach (var assembly in FoundAssemblies.ToArray())
                        {
                            var assemblies =
                                Config.Instance.SelectedProfile.InstalledAssemblies.Where(
                                    y => y.Name == assembly.Name && y.SvnUrl == assembly.SvnUrl).ToList();
                            assemblies.ForEach(a => FoundAssemblies.Remove(a));
                        }

                        foreach (var assembly in FoundAssemblies)
                        {
                            if (autoInstallName != null && assembly.Name.ToLower() == autoInstallName.ToLower())
                            {
                                assembly.InstallChecked = true;
                            }
                        }
                    };
            }

            bgWorker.RunWorkerCompleted += delegate
                {
                    if (controller != null)
                    {
                        controller.CloseAsync();
                        controller = null;
                    }

                    AbleToList = true;
                    Application.Current.Dispatcher.Invoke(() => installTabControl.SelectedIndex++);
                    if (autoInstallName != null)
                    {
                        InstallSelected();
                    }
                };

            bgWorker.RunWorkerAsync();
        }

        public async void ShowProgress(string location, bool isSvn, string autoInstallName = null)
        {
            this.controller =
                await
                this.ShowProgressAsync(
                    Utility.GetMultiLanguageText("Updating"),
                    Utility.GetMultiLanguageText("DownloadingData"));
            this.controller.SetIndeterminate();
            this.controller.SetCancelable(true);
            this.ListAssemblies(location, isSvn, autoInstallName);
        }

        private async void AfterInstallMessage(string msg, bool close = false)
        {
            await this.ShowMessageAsync(Utility.GetMultiLanguageText("Installer"), msg);
            if (close)
            {
                this.Close();
            }
        }

        private void InstallerWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.SvnComboBox.ItemsSource = Config.Instance.KnownRepositories;
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
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
                    (this.SvnRadioButton.IsChecked == true) ? this.SvnComboBox.Text : this.PathTextBox.Text,
                    (this.SvnRadioButton.IsChecked == true));
            }
        }

        private void Step2_Click(object sender, RoutedEventArgs e)
        {
            this.InstallSelected();
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
            var searchText = ((TextBox)sender).Text;
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