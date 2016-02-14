#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// Remoting.cs is part of LeagueSharp.Loader.
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
    using System;
    using System.IO;
    using System.ServiceModel;
    using System.Windows;

    using LeagueSharp.Sandbox.Shared;

    internal class Remoting
    {
        private static ServiceHost _loaderServiceHost;

        public static void Init()
        {
            _loaderServiceHost = ServiceFactory.CreateService<ILoaderService, LoaderService>();
            _loaderServiceHost.Faulted += OnLoaderServiceFaulted;
        }

        private static void OnLoaderServiceFaulted(object sender, EventArgs eventArgs)
        {
            Console.WriteLine("ILoaderService faulted, trying restart");
            _loaderServiceHost.Faulted -= OnLoaderServiceFaulted;
            _loaderServiceHost.Abort();

            try
            {
                _loaderServiceHost = ServiceFactory.CreateService<ILoaderService, LoaderService>();
                _loaderServiceHost.Faulted += OnLoaderServiceFaulted;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }
}