#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// General.xaml.cs is part of LeagueSharp.Loader.
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

namespace LeagueSharp.Loader.Views.Settings
{
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Input;

    using LeagueSharp.Loader.Class;
    using LeagueSharp.Loader.Data;

    using MahApps.Metro;

    public partial class General
    {
        public General()
        {
            this.InitializeComponent();
        }

        private readonly string[] _accentColors =
            {
                "Red",
                "Green",
                "Blue",
                "Purple",
                "Orange",
                "Lime",
                "Emerald",
                "Teal",
                "Cyan",
                "Cobalt",
                "Indigo",
                "Violet",
                "Pink",
                "Magenta",
                "Crimson",
                "Amber",
                "Yellow",
                "Brown",
                "Olive",
                "Steel",
                "Mauve",
                "Taupe",
                "Sienna"
            };

        private void AppData_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(Directories.AppDataDirectory);
        }

        private void Color_Loaded(object sender, RoutedEventArgs e)
        {
            var colorBox = (ComboBox)sender;

            foreach (var asccent in this._accentColors)
            {
                colorBox.Items.Add(asccent);
            }

            if (Config.Instance.SelectedColor != null)
            {
                colorBox.SelectedItem = Config.Instance.SelectedColor;
            }

            if (colorBox.SelectedIndex == -1)
            {
                colorBox.SelectedIndex = colorBox.Items.IndexOf("Blue");
            }
        }

        private void Color_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var color = ((ComboBox)sender).SelectedValue.ToString();

            if (this._accentColors.Contains(color))
            {
                ThemeManager.ChangeAppStyle(
                    Application.Current,
                    ThemeManager.GetAccent(color),
                    ThemeManager.GetAppTheme("BaseLight"));
                Config.Instance.SelectedColor = color;
            }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            var senderBox = (ComboBox)sender;

            senderBox.Items.Clear();
            senderBox.Items.Add("Arabic");
            senderBox.Items.Add("Bulgarian");
            senderBox.Items.Add("Chinese");
            senderBox.Items.Add("Czech");
            senderBox.Items.Add("Dutch");
            senderBox.Items.Add("English");
            senderBox.Items.Add("French");
            senderBox.Items.Add("German");
            senderBox.Items.Add("Greek");
            senderBox.Items.Add("Hungarian");
            senderBox.Items.Add("Italian");
            senderBox.Items.Add("Korean");
            senderBox.Items.Add("Latvian");
            senderBox.Items.Add("Lithuanian");
            senderBox.Items.Add("Polish");
            senderBox.Items.Add("Portuguese");
            senderBox.Items.Add("Romanian");
            senderBox.Items.Add("Russian");
            senderBox.Items.Add("Spanish");
            senderBox.Items.Add("Swedish");
            senderBox.Items.Add("Thai");
            senderBox.Items.Add("Traditional-Chinese");
            senderBox.Items.Add("Turkish");
            senderBox.Items.Add("Vietnamese");

            if (Config.Instance.SelectedLanguage != null)
            {
                senderBox.SelectedItem =
                    senderBox.Items.Cast<string>().FirstOrDefault(item => item == Config.Instance.SelectedLanguage);
            }

            //English as default
            if (senderBox.SelectedIndex == -1)
            {
                senderBox.SelectedIndex = senderBox.Items.IndexOf("English");
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count <= 0 || e.RemovedItems.Count <= 0)
            {
                return;
            }

            var selected = (string)e.AddedItems[0];

            if (Config.Instance.SelectedLanguage == selected
                || (Config.Instance.SelectedLanguage == null && selected == "English"))
            {
                return;
            }

            Config.Instance.SelectedLanguage = selected;
            ((MainWindow)this.DataContext).MainWindow_OnClosing(null, null);
            Process.Start(Application.ResourceAssembly.Location);
            Environment.Exit(0);
        }

        private void GameSettingsDataGrid_OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = ((DataGrid)sender).SelectedItem;
            if (item != null)
            {
                ((GameSettings)item).SelectedValue = ((GameSettings)item).SelectedValue
                                                     == ((GameSettings)item).PosibleValues[0]
                                                         ? ((GameSettings)item).PosibleValues[1]
                                                         : ((GameSettings)item).PosibleValues[0];
            }
        }

        private void Logout_OnClick(object sender, RoutedEventArgs e)
        {
            Config.Instance.Username = "";
            Config.Instance.Password = "";
            ((MainWindow)this.DataContext).MainWindow_OnClosing(null, null);

            Process.Start(Application.ResourceAssembly.Location);
            Environment.Exit(0);
        }

        private void SaveConfig_Click(object sender, RoutedEventArgs e)
        {
            Utility.MapClassToXmlFile(typeof(Config), Config.Instance, Directories.ConfigFilePath);
        }
    }
}