#region LICENSE

// Copyright 2016-2016 LeagueSharp.Loader
// LSUriScheme.cs is part of LeagueSharp.Loader.
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
    using System.Net;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    using LeagueSharp.Loader.Data;
    using LeagueSharp.Loader.Views;

    using MahApps.Metro.Controls;

    #endregion

    public static class LSUriScheme
    {
        public const string Name = "ls";

        public static string FullName
        {
            get
            {
                return Name + "://";
            }
        }

        public static async Task HandleUrl(string url, MetroWindow window)
        {
            url = url.Remove(0, FullName.Length).WebDecode();

            var r = Regex.Matches(url, "(project|projectGroup)/([^/]*)/([^/]*)/([^/]*)/?");
            foreach (Match m in r)
            {
                var linkType = m.Groups[1].ToString();

                switch (linkType)
                {
                    case "project":
                        InstallerWindow.InstallAssembly(m);
                        break;
                }
            }
        }
    }
}