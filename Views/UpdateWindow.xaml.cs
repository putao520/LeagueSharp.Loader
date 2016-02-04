#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// UpdateWindow.xaml.cs is part of LeagueSharp.Loader.
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
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.IO.Compression;
    using System.Net;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;

    using LeagueSharp.Loader.Class;
    using LeagueSharp.Loader.Data;

    #endregion

    public enum UpdateAction
    {
        Core,

        Loader
    }

    /// <summary>
    ///     Interaction logic for UpdateWindow.xaml
    /// </summary>
    public partial class UpdateWindow : INotifyPropertyChanged
    {
        public UpdateWindow(UpdateAction action, string url)
        {
            this.InitializeComponent();
            this.DataContext = this;
            this.Action = action;
            this.UpdateUrl = url;
        }

        public UpdateWindow()
        {
            this.InitializeComponent();
            this.DataContext = this;

            if (DesignerProperties.GetIsInDesignMode(this))
            {
                this.UpdateMessage = this.FindResource("Updating").ToString();
                this.ProgressText = this.FindResource("UpdateText").ToString();
            }
        }

        private string progressText;

        private string updateMessage;

        public event PropertyChangedEventHandler PropertyChanged;

        public string ProgressText
        {
            get
            {
                return this.progressText;
            }
            set
            {
                if (Equals(value, this.progressText))
                {
                    return;
                }
                this.progressText = value;
                this.OnPropertyChanged();
            }
        }

        public string UpdateMessage
        {
            get
            {
                return this.updateMessage;
            }
            set
            {
                if (Equals(value, this.updateMessage))
                {
                    return;
                }
                this.updateMessage = value;
                this.OnPropertyChanged();
            }
        }

        private UpdateAction Action { get; set; }

        private string UpdateUrl { get; set; }

        public async Task<bool> Update()
        {
            this.Focus();
            var result = false;
            this.UpdateProgressBar.Value = 0;
            this.UpdateProgressBar.Maximum = 100;

            switch (this.Action)
            {
                case UpdateAction.Loader:
                    result = await this.UpdateLoader();
                    break;
                case UpdateAction.Core:
                    result = await this.UpdateCore();
                    break;
            }

            Application.Current.Dispatcher.InvokeAsync(
                async () =>
                    {
                        await Task.Delay(250);
                        this.Close();
                    });

            return result;
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async Task<bool> UpdateCore()
        {
            this.UpdateMessage = "Core " + this.FindResource("Updating");

            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += this.WebClientOnDownloadProgressChanged;

                try
                {
                    if (File.Exists(Updater.UpdateZip))
                    {
                        File.Delete(Updater.UpdateZip);
                        Thread.Sleep(500);
                    }

                    await client.DownloadFileTaskAsync(this.UpdateUrl, Updater.UpdateZip);

                    using (var archive = ZipFile.OpenRead(Updater.UpdateZip))
                    {
                        foreach (var entry in archive.Entries)
                        {
                            try
                            {
                                File.Delete(Path.Combine(Directories.CoreDirectory, entry.FullName));
                                entry.ExtractToFile(Path.Combine(Directories.CoreDirectory, entry.FullName), true);
                            }
                            catch
                            {
                                File.WriteAllText(Directories.CoreFilePath, "-"); // force an update
                                return false;
                            }
                        }
                    }

                    PathRandomizer.CopyFiles();
                    Config.Instance.TosAccepted = false;

                    return true;
                }
                catch (Exception e)
                {
                    MessageBox.Show(Utility.GetMultiLanguageText("FailedToDownload") + e);
                }
            }

            return false;
        }

        private async Task<bool> UpdateLoader()
        {
            this.UpdateMessage = "Loader " + this.FindResource("Updating");

            using (var client = new WebClient())
            {
                client.DownloadProgressChanged += this.WebClientOnDownloadProgressChanged;

                try
                {
                    await client.DownloadFileTaskAsync(this.UpdateUrl, Updater.SetupFile);
                }
                catch (Exception e)
                {
                    MessageBox.Show(Utility.GetMultiLanguageText("LoaderUpdateFailed") + e);
                    Environment.Exit(0);
                }
            }

            Config.Instance.TosAccepted = false;
            Config.Save(true);

            new Process
            {
                StartInfo =
                        {
                            FileName = Updater.SetupFile,
                            Arguments = "/VERYSILENT /DIR=\"" + Directories.CurrentDirectory + "\""
                        }
            }.Start();

            Environment.Exit(0);

            return true;
        }

        private void WebClientOnDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs args)
        {
            Application.Current.Dispatcher.InvokeAsync(
                () =>
                    {
                        this.UpdateProgressBar.Value = args.ProgressPercentage;

                        this.ProgressText = string.Format(
                            this.FindResource("UpdateText").ToString(),
                            args.BytesReceived / 1024,
                            args.TotalBytesToReceive / 1024);
                    });
        }
    }
}