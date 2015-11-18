#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// MainWindow.xaml.cs is part of LeagueSharp.Loader.
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

using System.Net;
using LeagueSharp.Loader.Data.Assemblies;

namespace LeagueSharp.Loader.Views
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Controls.Primitives;
    using System.Windows.Data;
    using System.Windows.Input;
    using System.Windows.Media;

    using LeagueSharp.Loader.Class;
    using LeagueSharp.Loader.Data;

    using MahApps.Metro.Controls;
    using MahApps.Metro.Controls.Dialogs;

    using Microsoft.Build.Evaluation;
    using Newtonsoft.Json;

    public partial class MainWindow : INotifyPropertyChanged
    {
        public BackgroundWorker AssembliesWorker = new BackgroundWorker();

        public bool AssembliesWorkerCancelled;

        public bool FirstTimeActivated = true;

        public BackgroundWorker UpdaterWorker = new BackgroundWorker();

        private bool checkingForUpdates;

        private bool columnWidthChanging;

        private Tuple<bool, string> loaderVersionCheckResult;

        private int rowIndex = -1;

        private string statusString = "?";

        private string updateMessage;

        private bool working;

        public delegate Point GetPosition(IInputElement element);

        public event PropertyChangedEventHandler PropertyChanged;

        public bool CheckingForUpdates
        {
            get
            {
                return this.checkingForUpdates;
            }
            set
            {
                this.checkingForUpdates = value;
                this.OnPropertyChanged("CheckingForUpdates");
            }
        }

        public Config Config
        {
            get
            {
                return Config.Instance;
            }
        }

        public Thread InjectThread { get; set; }

        public string StatusString
        {
            get
            {
                return this.statusString;
            }
            set
            {
                this.statusString = value;
                this.OnPropertyChanged("StatusString");
            }
        }

        public bool Working
        {
            get
            {
                return this.working;
            }
            set
            {
                this.working = value;
                this.OnPropertyChanged("Working");
            }
        }

        public void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            if (this.AssembliesWorker.IsBusy && e != null)
            {
                this.AssembliesWorker.CancelAsync();
                e.Cancel = true;
                this.Hide();
                return;
            }

            try
            {
                Utility.MapClassToXmlFile(typeof(Config), Config.Instance, Directories.ConfigFilePath);
            }
            catch
            {
                MessageBox.Show(Utility.GetMultiLanguageText("ConfigWriteError"));
            }

            try
            {
                if (this.InjectThread != null)
                {
                    this.InjectThread.Abort();
                }
            }
            catch
            {
                // ignored
            }

            var allAssemblies = new List<LeagueSharpAssembly>();
            foreach (var profile in Config.Instance.Profiles)
            {
                allAssemblies.AddRange(profile.InstalledAssemblies.ToList());
            }

            GitUpdater.ClearUnusedRepos(allAssemblies);
        }

        public async Task PrepareAssemblies(
            IEnumerable<LeagueSharpAssembly> assemblies,
            bool update,
            bool compile,
            bool updateCommonLibOnly = false)
        {
            if (this.Working)
            {
                return;
            }

            this.Working = true;
            var leagueSharpAssemblies = assemblies as IList<LeagueSharpAssembly> ?? assemblies.ToList();

            await Task.Factory.StartNew(
                () =>
                    {
                        if (update || updateCommonLibOnly)
                        {
                            var updateList = leagueSharpAssemblies.GroupBy(a => a.SvnUrl).Select(g => g.First());

                            Parallel.ForEach(
                                updateList.Where(
                                    a =>
                                    !updateCommonLibOnly
                                    || a.SvnUrl == "https://github.com/LeagueSharp/LeagueSharpCommon"),
                                new ParallelOptions { MaxDegreeOfParallelism = 5 },
                                (assembly, state) =>
                                    {
                                        assembly.Update();
                                        if (this.AssembliesWorker.CancellationPending)
                                        {
                                            this.AssembliesWorkerCancelled = true;
                                            state.Break();
                                        }
                                    });
                        }

                        if (compile)
                        {
                            foreach (var assembly in leagueSharpAssemblies.OrderBy(a => a.Type))
                            {
                                assembly.Compile();

                                if (this.AssembliesWorker.CancellationPending)
                                {
                                    this.AssembliesWorkerCancelled = true;
                                    break;
                                }
                            }
                        }

                        Injection.PrepareDone = true;
                    });

            await Task.Factory.StartNew(
                () =>
                    {
                        ProjectCollection.GlobalProjectCollection.UnloadAllProjects();
                        this.Working = false;
                        if (this.AssembliesWorkerCancelled)
                        {
                            this.Close();
                        }
                    });
        }

        public async void ShowTextMessage(string title, string message)
        {
            var visibility = this.Browser.Visibility;
            this.Browser.Visibility = Visibility.Hidden;
            await this.ShowMessageAsync(title, message);
            this.Browser.Visibility = (visibility == Visibility.Hidden) ? Visibility.Hidden : Visibility.Visible;
        }

        private void AssemblyButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.MainTabControl.SelectedIndex = 2;
        }

        private void AssemblyDBButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (Config.Instance.allDbAssemblies.Count == 0)
            {
                var assemblies = Class.AssemblyDB.getAssembliesFromDB();

                if (assemblies != null)
                {
                    Config.Instance.allDbAssemblies = assemblies;
                }
                else
                {
                    Config.Instance.allDbAssemblies.Add(new LeagueSharp.Loader.Data.Assemblies.Assembly() { Name = "ERROR", Description = "Please try again later, we seem to have trouble!" });
                }
            }

          this.MainTabControl.SelectedIndex = 4;
        }

        private void BaseDataGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (this.columnWidthChanging)
            {
                this.columnWidthChanging = false;

                Config.Instance.ColumnCheckWidth = this.ColumnCheck.Width.DesiredValue;
                Config.Instance.ColumnNameWidth = this.ColumnName.Width.DesiredValue;
                Config.Instance.ColumnTypeWidth = this.ColumnType.Width.DesiredValue;
                Config.Instance.ColumnVersionWidth = this.ColumnVersion.Width.DesiredValue;
                Config.Instance.ColumnLocationWidth = this.ColumnLocation.Width.DesiredValue;
            }
        }

        private async Task Bootstrap()
        {
            #region UI

            this.Header.Text = "LEAGUESHARP " + Assembly.GetExecutingAssembly().GetName().Version;

            this.Browser.Visibility = Visibility.Hidden;
            this.TosBrowser.Visibility = Visibility.Hidden;
            this.GeneralSettingsItem.IsSelected = true;

            foreach (var gameSetting in Config.Instance.Settings.GameSettings)
            {
                gameSetting.PropertyChanged += this.GameSettingOnPropertyChanged;
            }

            #region ColumnWidth

            PropertyDescriptor pd = DependencyPropertyDescriptor.FromProperty(
                DataGridColumn.ActualWidthProperty,
                typeof(DataGridColumn));

            foreach (var column in this.InstalledAssembliesDataGrid.Columns)
            {
                pd.AddValueChanged(column, this.ColumnWidthPropertyChanged);
            }

            this.ColumnCheck.Width = Config.Instance.ColumnCheckWidth;
            this.ColumnName.Width = Config.Instance.ColumnNameWidth;
            this.ColumnType.Width = Config.Instance.ColumnTypeWidth;
            this.ColumnVersion.Width = Config.Instance.ColumnVersionWidth;
            this.ColumnLocation.Width = Config.Instance.ColumnLocationWidth;

            #endregion

            this.NewsTabItem.Visibility = Visibility.Hidden;
            this.AssembliesTabItem.Visibility = Visibility.Hidden;
            this.SettingsTabItem.Visibility = Visibility.Hidden;
            this.AssemblyDB.Visibility = Visibility.Hidden;
            this.DataContext = this;

            #region ContextMenu.DevMenu

            this.DevMenu.Visibility = Config.Instance.ShowDevOptions ? Visibility.Visible : Visibility.Collapsed;
            this.Config.PropertyChanged += (o, args) =>
                {
                    if (args.PropertyName == "ShowDevOptions")
                    {
                        this.DevMenu.Visibility = Config.Instance.ShowDevOptions
                                                      ? Visibility.Visible
                                                      : Visibility.Collapsed;
                    }
                };

            #endregion

            #endregion

            Updater.MainWindow = this;

            await this.CheckForUpdates(true, true, false);

            Updater.GetRepositories(
                delegate(List<string> list)
                    {
                        if (list.Count > 0)
                        {
                            Config.Instance.KnownRepositories.Clear();
                            foreach (var repo in list)
                            {
                                Config.Instance.KnownRepositories.Add(repo);
                            }
                        }
                    });

            Config.Instance.FirstRun = false;

            //Try to login with the saved credentials.
            if (!Auth.Login(Config.Instance.Username, Config.Instance.Password).Item1)
            {
                await this.ShowLoginDialog();
            }
            else
            {
                this.OnLogin(Config.Instance.Username);
            }

            #region ToS

            if (!Config.Instance.TosAccepted)
            {
                this.RightWindowCommands.Visibility = Visibility.Collapsed;
            }
            else
            {
                this.MainTabControl.SelectedIndex = 1;
            }

            #endregion

            // wait for tos accept
            await Task.Factory.StartNew(
                () =>
                    {
                        while (Config.Instance.TosAccepted == false)
                        {
                            Thread.Sleep(100);
                        }
                    });

            Console.WriteLine("Tos Accepted");

            var allAssemblies = new List<LeagueSharpAssembly>();
            foreach (var profile in Config.Instance.Profiles)
            {
                allAssemblies.AddRange(profile.InstalledAssemblies);
            }

            allAssemblies = allAssemblies.Distinct().ToList();

            GitUpdater.ClearUnusedRepos(allAssemblies);
            await this.PrepareAssemblies(allAssemblies, Config.Instance.FirstRun || Config.Instance.UpdateOnLoad, true);

            this.MainTabControl.SelectedIndex = 2;
        }

        private async Task CheckForUpdates(bool loader, bool core, bool showDialogOnFinish)
        {
            try
            {
                if (this.CheckingForUpdates)
                {
                    return;
                }

                Console.WriteLine("Checking");

                this.StatusString = Utility.GetMultiLanguageText("Checking");
                this.updateMessage = "";
                this.CheckingForUpdates = true;

                if (loader)
                {
                    this.loaderVersionCheckResult = Updater.CheckLoaderVersion();

                    try
                    {
                        if (File.Exists(Updater.SetupFile))
                        {
                            Thread.Sleep(1000);
                            File.Delete(Updater.SetupFile);
                        }
                    }
                    catch
                    {
                        MessageBox.Show(Utility.GetMultiLanguageText("FailedToDelete"));
                        Environment.Exit(0);
                    }

                    if (this.loaderVersionCheckResult != null && this.loaderVersionCheckResult.Item1)
                    {
                        //Update the loader only when we are not injected to be able to replace the core files.
                        if (!Injection.IsInjected)
                        {
                            Console.WriteLine("Update Loader");
                            Updater.Updating = true;
                            await Updater.UpdateLoader(this.loaderVersionCheckResult);
                        }
                    }
                }

                if (core)
                {
                    if (Config.Instance.LeagueOfLegendsExePath != null)
                    {
                        var exe = Utility.GetLatestLeagueOfLegendsExePath(Config.Instance.LeagueOfLegendsExePath);
                        if (exe != null)
                        {
                            Console.WriteLine("Update Core");
                            var updateResult = await Updater.UpdateCore(exe, !showDialogOnFinish);
                            this.updateMessage = updateResult.Message;

                            switch (updateResult.State)
                            {
                                case Updater.CoreUpdateState.Operational:
                                    this.StatusString = Utility.GetMultiLanguageText("Updated");
                                    break;
                                case Updater.CoreUpdateState.Maintenance:
                                    this.StatusString = Utility.GetMultiLanguageText("OUTDATED");
                                    break;

                                default:
                                    this.StatusString = Utility.GetMultiLanguageText("Unknown");
                                    break;
                            }

                            return;
                        }
                    }

                    this.StatusString = Utility.GetMultiLanguageText("Unknown");
                    this.updateMessage = Utility.GetMultiLanguageText("LeagueNotFound");
                }
            }
            finally
            {
                this.CheckingForUpdates = false;
                Updater.CheckedForUpdates = true;

                if (showDialogOnFinish)
                {
                    this.ShowTextMessage(Utility.GetMultiLanguageText("UpdateStatus"), this.updateMessage);
                }
            }
        }

        private void CloneItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.InstalledAssembliesDataGrid.SelectedItems.Count <= 0)
            {
                return;
            }
            var selectedAssembly = (LeagueSharpAssembly)this.InstalledAssembliesDataGrid.SelectedItems[0];
            try
            {
                var source = Path.GetDirectoryName(selectedAssembly.PathToProjectFile);
                var destination = Path.Combine(Directories.LocalRepoDir, selectedAssembly.Name) + "_clone_"
                                  + Environment.TickCount.GetHashCode().ToString("X");
                Utility.CopyDirectory(source, destination);
                var leagueSharpAssembly = new LeagueSharpAssembly(
                    selectedAssembly.Name + "_clone",
                    Path.Combine(destination, Path.GetFileName(selectedAssembly.PathToProjectFile)),
                    "");
                leagueSharpAssembly.Compile();
                this.Config.SelectedProfile.InstalledAssemblies.Add(leagueSharpAssembly);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ColumnWidthPropertyChanged(object sender, EventArgs e)
        {
            // listen for when the mouse is released
            this.columnWidthChanging = true;
            if (sender != null)
            {
                Mouse.AddPreviewMouseUpHandler(this, this.BaseDataGrid_MouseLeftButtonUp);
            }
        }

        private async void CompileAll_OnClick(object sender, RoutedEventArgs e)
        {
            await this.PrepareAssemblies(Config.Instance.SelectedProfile.InstalledAssemblies, false, true);
        }

        private async void DeleteWithConfirmation(IEnumerable<LeagueSharpAssembly> asemblies)
        {
            var result =
                await
                this.ShowMessageAsync(
                    Utility.GetMultiLanguageText("Uninstall"),
                    Utility.GetMultiLanguageText("UninstallConfirm"),
                    MessageDialogStyle.AffirmativeAndNegative);

            if (result == MessageDialogResult.Negative)
            {
                return;
            }

            foreach (var ee in asemblies)
            {
                Config.Instance.SelectedProfile.InstalledAssemblies.Remove(ee);
            }
        }

        private void EditItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.InstalledAssembliesDataGrid.SelectedItems.Count <= 0)
            {
                return;
            }

            var selectedAssembly = (LeagueSharpAssembly)this.InstalledAssembliesDataGrid.SelectedItems[0];
            if (File.Exists(selectedAssembly.PathToProjectFile))
            {
                Process.Start(selectedAssembly.PathToProjectFile);
            }
        }

        private void EditProfileMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            this.ShowProfileNameChangeDialog();
        }

        private TChildItem FindVisualChild<TChildItem>(DependencyObject obj) where TChildItem : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is TChildItem)
                {
                    return (TChildItem)child;
                }
                else
                {
                    var childOfChild = this.FindVisualChild<TChildItem>(child);
                    if (childOfChild != null)
                    {
                        return childOfChild;
                    }
                }
            }
            return null;
        }

        private void GameSettingOnPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        {
            foreach (var instance in Injection.LeagueInstances)
            {
                Injection.SendConfig(instance);
            }
        }

        private DataGridCell GetCell(DataGridRow row, int columnIndex = 0)
        {
            if (row == null)
            {
                return null;
            }

            var presenterE = row.FindChildren<DataGridCellsPresenter>(true);
            if (presenterE == null)
            {
                return null;
            }

            var presenter = presenterE.ToList()[0];
            var cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);
            if (cell != null)
            {
                return cell;
            }

            // alternative way - now try to bring into view and retreive the cell
            this.InstalledAssembliesDataGrid.ScrollIntoView(row, this.InstalledAssembliesDataGrid.Columns[columnIndex]);
            cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex);

            return cell;
        }

        private int GetCurrentRowIndex(GetPosition pos)
        {
            var curIndex = -1;
            for (var i = 0; i < this.InstalledAssembliesDataGrid.Items.Count; i++)
            {
                var row = this.GetRowItem(i);
                if (row != null)
                {
                    var cell = this.GetCell(row);
                    if (cell != null && this.GetMouseTargetRow(row, pos) && !this.GetMouseTargetRow(cell, pos))
                    {
                        curIndex = i;
                        break;
                    }
                }
            }
            return curIndex;
        }

        private bool GetMouseTargetRow(Visual theTarget, GetPosition position)
        {
            var rect = VisualTreeHelper.GetDescendantBounds(theTarget);
            var point = position((IInputElement)theTarget);
            return rect.Contains(point);
        }

        private DataGridRow GetRowItem(int index)
        {
            if (this.InstalledAssembliesDataGrid.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
            {
                return null;
            }
            return this.InstalledAssembliesDataGrid.ItemContainerGenerator.ContainerFromIndex(index) as DataGridRow;
        }

        private void GithubAssembliesItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.InstalledAssembliesDataGrid.SelectedItems.Count <= 0)
            {
                return;
            }
            var selectedAssembly = (LeagueSharpAssembly)this.InstalledAssembliesDataGrid.SelectedItems[0];
            if (selectedAssembly.SvnUrl != "")
            {
                var window = new InstallerWindow { Owner = this };
                window.ListAssemblies(selectedAssembly.SvnUrl, true);
                window.ShowDialog();
            }
        }

        private void GithubItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.InstalledAssembliesDataGrid.SelectedItems.Count <= 0)
            {
                return;
            }
            var selectedAssembly = (LeagueSharpAssembly)this.InstalledAssembliesDataGrid.SelectedItems[0];
            if (selectedAssembly.SvnUrl != "")
            {
                Process.Start(selectedAssembly.SvnUrl);
            }
            else if (Directory.Exists(Path.GetDirectoryName(selectedAssembly.PathToProjectFile)))
            {
                Process.Start(Path.GetDirectoryName(selectedAssembly.PathToProjectFile));
            }
        }

        private void InstallButton_Click(object sender, RoutedEventArgs e)
        {
            var window = new InstallerWindow { Owner = this };
            window.ShowDialog();
        }

        private void InstalledAssembliesDataGrid_Drop(object sender, DragEventArgs e)
        {
            if (this.rowIndex < 0)
            {
                return;
            }
            var index = this.GetCurrentRowIndex(e.GetPosition);
            if (index < 0)
            {
                return;
            }
            if (index == this.rowIndex)
            {
                return;
            }
            var changedAssembly = this.Config.SelectedProfile.InstalledAssemblies[this.rowIndex];
            this.Config.SelectedProfile.InstalledAssemblies.RemoveAt(this.rowIndex);
            this.Config.SelectedProfile.InstalledAssemblies.Insert(index, changedAssembly);
        }

        private void InstalledAssembliesDataGrid_OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            var dataGrid = (DataGrid)sender;
            if (dataGrid != null)
            {
                if (dataGrid.SelectedItems.Count == 0)
                {
                    e.Handled = true;
                }
            }
            else
            {
                e.Handled = true;
            }
        }

        private void InstalledAssembliesDataGrid_OnPreviewDragOver(object sender, DragEventArgs e)
        {
            var container = sender as FrameworkElement;

            if (container == null)
            {
                return;
            }

            var scrollViewer = this.FindVisualChild<ScrollViewer>(container);

            if (scrollViewer == null)
            {
                return;
            }

            double tolerance = 15;
            var verticalPos = e.GetPosition(container).Y;
            double offset = 1;

            if (verticalPos < tolerance) // Top visible? 
            {
                //Scroll up
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - offset);
            }
            else if (verticalPos > container.ActualHeight - tolerance) //Bot visible? 
            {
                //Scroll down
                scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + offset);
            }
        }

        private void InstalledAssembliesDataGrid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.rowIndex = this.GetCurrentRowIndex(e.GetPosition);
            if (this.rowIndex < 0)
            {
                return;
            }
            if (this.IsColumnSelected(e))
            {
                return;
            }
            this.InstalledAssembliesDataGrid.SelectedIndex = this.rowIndex;
            var selectedAssembly = this.InstalledAssembliesDataGrid.Items[this.rowIndex] as LeagueSharpAssembly;
            if (selectedAssembly == null)
            {
                return;
            }

            if (DragDrop.DoDragDrop(this.InstalledAssembliesDataGrid, selectedAssembly, DragDropEffects.Move)
                != DragDropEffects.None)
            {
            }
        }

        private bool IsColumnSelected(MouseEventArgs e)
        {
            var dep = (DependencyObject)e.OriginalSource;
            while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }
            if (dep is DataGridColumnHeader)
            {
                return true;
            }
            return false;
        }

        private void LogItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.InstalledAssembliesDataGrid.SelectedItems.Count <= 0)
            {
                return;
            }

            var selectedAssembly = (LeagueSharpAssembly)this.InstalledAssembliesDataGrid.SelectedItems[0];
            var logFile = Path.Combine(
                Directories.LogsDir,
                "Error - " + Path.GetFileName(selectedAssembly.Name + ".txt"));
            if (File.Exists(logFile))
            {
                Process.Start(logFile);
            }
            else
            {
                this.ShowTextMessage("Error", Utility.GetMultiLanguageText("LogNotFound"));
            }
        }

        private async void MainWindow_OnActivated(object sender, EventArgs e)
        {
            var text = Clipboard.GetText();
            if (text.StartsWith(LSUriScheme.FullName))
            {
                Clipboard.SetText("");
                await LSUriScheme.HandleUrl(text, this);
            }
        }

        private async void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            await this.Bootstrap();

            this.SetForeground();
        }

        private void NewItem_OnClick(object sender, RoutedEventArgs e)
        {
            this.ShowNewAssemblyDialog();
        }

        private void NewProfileMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            Config.Instance.Profiles.Add(
                new Profile
                    {
                        InstalledAssemblies = new ObservableCollection<LeagueSharpAssembly>(),
                        Name = Utility.GetMultiLanguageText("NewProfile2")
                    });

            Config.Instance.SelectedProfile = Config.Instance.Profiles.Last();
        }

        private void NewsButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.MainTabControl.SelectedIndex = 1;
        }

        private void OnLogin(string username)
        {
            Utility.Log(LogStatus.Ok, "Login", string.Format("Succesfully signed in as {0}", username), Logs.MainLog);
            this.Browser.Visibility = Visibility.Visible;
            this.TosBrowser.Visibility = Visibility.Visible;

            try
            {
                Utility.MapClassToXmlFile(typeof(Config), Config.Instance, Directories.ConfigFilePath);
            }
            catch
            {
                MessageBox.Show(Utility.GetMultiLanguageText("ConfigWriteError"));
            }

            if (!PathRandomizer.CopyFiles())
            {
            }

            Remoting.Init();

            this.InjectThread = new Thread(
                () =>
                    {
                        while (true)
                        {
                            if (Config.Instance.Install)
                            {
                                Injection.Pulse();
                            }

                            Application.Current.Dispatcher.Invoke(
                                () =>
                                    {
                                        if (Injection.IsInjected)
                                        {
                                            this.icon_connected.Visibility = Visibility.Visible;
                                            this.icon_disconnected.Visibility = Visibility.Collapsed;
                                        }
                                        else
                                        {
                                            this.icon_connected.Visibility = Visibility.Collapsed;
                                            this.icon_disconnected.Visibility = Visibility.Visible;
                                        }
                                    });

                            Thread.Sleep(3000);
                        }
                    });

            this.InjectThread.SetApartmentState(ApartmentState.STA);
            this.InjectThread.Start();
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void ProfilesButton_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            this.ShowProfileNameChangeDialog();
        }

        private void ProfilesButton_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0 || e.RemovedItems.Count <= 0)
            {
                return;
            }

            this.TextBoxBase_OnTextChanged(null, null);
        }

        private void RemoveMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.InstalledAssembliesDataGrid.SelectedItems.Count <= 0)
            {
                return;
            }

            var remove = this.InstalledAssembliesDataGrid.SelectedItems.Cast<LeagueSharpAssembly>().ToList();
            this.DeleteWithConfirmation(remove);
        }

        private void RemoveProfileMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (Config.Instance.Profiles.Count > 1)
            {
                Config.Instance.Profiles.Remove(Config.SelectedProfile);
                Config.Instance.SelectedProfile = Config.Instance.Profiles.First();
            }
            else
            {
                Config.Instance.SelectedProfile.InstalledAssemblies = new ObservableCollection<LeagueSharpAssembly>();
                Config.Instance.SelectedProfile.Name = Utility.GetMultiLanguageText("DefaultProfile");
            }
        }

        private void SetForeground()
        {
            this.Show();

            if (this.WindowState == WindowState.Minimized)
            {
                this.WindowState = WindowState.Normal;
            }

            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
            this.Focus();
        }

        private void SettingsButton_OnClick(object sender, RoutedEventArgs e)
        {
            this.MainTabControl.SelectedIndex = 3;
        }

        private void ShareItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.InstalledAssembliesDataGrid.SelectedItems.Count <= 0)
            {
                return;
            }

            var stringToAppend = "";
            var count = 0;
            foreach (var selectedAssembly in this.InstalledAssembliesDataGrid.SelectedItems.Cast<LeagueSharpAssembly>())
            {
                if (selectedAssembly.SvnUrl.StartsWith(
                    "https://github.com",
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    var user = selectedAssembly.SvnUrl.Remove(0, 19);
                    stringToAppend += string.Format("{0}/{1}/", user, selectedAssembly.Name);
                    count++;
                }
            }

            if (count > 0)
            {
                var uri = LSUriScheme.FullName + (count == 1 ? "project" : "projectGroup") + "/" + stringToAppend;
                Clipboard.SetText(uri);
                this.ShowTextMessage(
                    Utility.GetMultiLanguageText("MenuShare"),
                    Utility.GetMultiLanguageText("ShareText"));
            }
        }

        private async void ShowAfterLoginDialog(string message, bool showLoginDialog)
        {
            await this.ShowMessageAsync("Login", message);
            if (showLoginDialog)
            {
                await this.ShowLoginDialog();
            }
        }

        private async Task ShowLoginDialog()
        {
            this.MetroDialogOptions.ColorScheme = MetroDialogColorScheme.Theme;
            var result =
                await
                this.ShowLoginAsync(
                    "LeagueSharp",
                    "Sign in",
                    new LoginDialogSettings
                        {
                            ColorScheme = this.MetroDialogOptions.ColorScheme,
                            NegativeButtonVisibility = Visibility.Visible
                        });

            var loginResult = new Tuple<bool, string>(false, "Cancel button pressed");
            if (result != null)
            {
                var hash = Auth.Hash(result.Password);

                loginResult = Auth.Login(result.Username, hash);
            }

            if (result != null && loginResult.Item1)
            {
                //Save the login credentials
                Config.Instance.Username = result.Username;
                Config.Instance.Password = Auth.Hash(result.Password);

                this.OnLogin(result.Username);
            }
            else
            {
                if (result == null)
                {
                    this.MainWindow_OnClosing(null, null);
                    Environment.Exit(0);
                }

                this.ShowAfterLoginDialog(
                    string.Format(Utility.GetMultiLanguageText("FailedToLogin"), loginResult.Item2),
                    true);

                Utility.Log(
                    LogStatus.Error,
                    Utility.GetMultiLanguageText("Login"),
                    string.Format(
                        Utility.GetMultiLanguageText("LoginError"),
                        (result != null ? result.Username : "null"),
                        loginResult.Item2),
                    Logs.MainLog);
            }
        }

        private async void ShowNewAssemblyDialog()
        {
            var assemblyName = await this.ShowInputAsync("New Project", "Enter the new project name");

            if (assemblyName != null)
            {
                assemblyName = Regex.Replace(assemblyName, @"[^A-Za-z0-9]+", "");
            }

            if (!string.IsNullOrEmpty(assemblyName))
            {
                var leagueSharpAssembly = Utility.CreateEmptyAssembly(assemblyName);
                if (leagueSharpAssembly != null)
                {
                    leagueSharpAssembly.Compile();
                    this.Config.SelectedProfile.InstalledAssemblies.Add(leagueSharpAssembly);
                }
            }
        }

        private async void ShowProfileNameChangeDialog()
        {
            var result =
                await
                this.ShowInputAsync(
                    Utility.GetMultiLanguageText("Rename"),
                    Utility.GetMultiLanguageText("RenameText"),
                    new MetroDialogSettings { DefaultText = Config.Instance.SelectedProfile.Name, });

            if (!string.IsNullOrEmpty(result))
            {
                Config.Instance.SelectedProfile.Name = result;
            }
        }

        private async void StatusButton_OnClick(object sender, RoutedEventArgs e)
        {
            await this.CheckForUpdates(true, true, true);
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = this.SearchTextBox.Text;
            var view = CollectionViewSource.GetDefaultView(Config.Instance.SelectedProfile.InstalledAssemblies);
            searchText = searchText.Replace("*", "(.*)");
            view.Filter = obj =>
                {
                    try
                    {
                        var assembly = obj as LeagueSharpAssembly;

                        if (searchText == "checked")
                        {
                            return assembly.InjectChecked;
                        }

                        var nameMatch = Regex.Match(assembly.Name, searchText, RegexOptions.IgnoreCase);
                        var displayNameMatch = Regex.Match(assembly.DisplayName, searchText, RegexOptions.IgnoreCase);
                        var svnNameMatch = Regex.Match(assembly.SvnUrl, searchText, RegexOptions.IgnoreCase);
                        var descNameMatch = Regex.Match(assembly.Description, searchText, RegexOptions.IgnoreCase);

                        return displayNameMatch.Success || nameMatch.Success || svnNameMatch.Success
                               || descNameMatch.Success;
                    }
                    catch (Exception)
                    {
                        return true;
                    }
                };
        }

        private void TosAccept_Click(object sender, RoutedEventArgs e)
        {
            Config.Instance.TosAccepted = true;
            this.MainTabControl.SelectedIndex = 1;
            this.RightWindowCommands.Visibility = Visibility.Visible;
            this.TosBrowser.Visibility = Visibility.Collapsed;
        }

        private void TosDecline_Click(object sender, RoutedEventArgs e)
        {
            this.MainWindow_OnClosing(null, null);
            Environment.Exit(0);
        }

        private void TrayIcon_OnTrayLeftMouseUp(object sender, RoutedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                this.Hide();
                this.MenuItemLabelHide.Header = "Show";
            }
        }

        private void TrayIcon_OnTrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            if (this.Visibility == Visibility.Hidden)
            {
                this.SetForeground();
                this.MenuItemLabelHide.Header = "Hide";
            }
        }

        private void TrayMenuClose_OnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TrayMenuHide_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.Visibility == Visibility.Visible)
            {
                this.Hide();
                this.MenuItemLabelHide.Header = "Show";
            }
            else
            {
                this.SetForeground();
                this.MenuItemLabelHide.Header = "Hide";
            }
        }

        private void TreeView_OnSelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var name = ((TreeViewItem)((TreeView)sender).SelectedItem).Uid;
            this.SettingsFrame.Content =
                Activator.CreateInstance(null, "LeagueSharp.Loader.Views.Settings." + name).Unwrap();
        }

        private async void UpdateAll_OnClick(object sender, RoutedEventArgs e)
        {
            await this.PrepareAssemblies(Config.Instance.SelectedProfile.InstalledAssemblies, true, true);
        }

        private async void UpdateAndCompileMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.InstalledAssembliesDataGrid.SelectedItems.Count == 0)
            {
                return;
            }

            await
                this.PrepareAssemblies(
                    this.InstalledAssembliesDataGrid.SelectedItems.Cast<LeagueSharpAssembly>(),
                    true,
                    true);
        }

        static string GetParentUriString(Uri uri)
        {
            return uri.AbsoluteUri.Remove(uri.AbsoluteUri.Length - uri.Segments.Last().Length);
        }

        private async void InstallFromDbItem_OnClick(object sender, RoutedEventArgs e)
        {
            if (this.AssembliesDBDataGrid.SelectedItems.Count == 0)
            {
                return;
            }

            var assemblies = this.AssembliesDBDataGrid.SelectedItems.Cast<Data.Assemblies.Assembly>().ToList();

            foreach (var assembly in assemblies)
            {
                await LSUriScheme.HandleUrl(assembly.GithubUrl, this);
            }
        }

        private void DBTextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = this.DBSearchTextBox.Text;
            var view = CollectionViewSource.GetDefaultView(Config.Instance.allDbAssemblies);
            searchText = searchText.Replace("*", "(.*)");
            view.Filter = obj =>
            {
                try
                {
                    var assembly = obj as Data.Assemblies.Assembly;
                    if (assembly == null)
                        return false;

                    var nameMatch = Regex.Match(assembly.Name, searchText, RegexOptions.IgnoreCase);
                    var champeMatch = assembly.Type ==AssemblyType.Executable && Regex.Match(string.Join(", ", assembly.Champions), searchText, RegexOptions.IgnoreCase).Success;
                    var authorMatch = Regex.Match(assembly.AuthorName, searchText, RegexOptions.IgnoreCase);
                    var svnNameMatch = Regex.Match(assembly.GithubUrl, searchText, RegexOptions.IgnoreCase);
                    var descNameMatch = Regex.Match(assembly.Description, searchText, RegexOptions.IgnoreCase);
                    

                    return authorMatch.Success || champeMatch || nameMatch.Success || svnNameMatch.Success
                           || descNameMatch.Success;
                }
                catch (Exception)
                {
                    return true;
                }
            };
        }
    }
}