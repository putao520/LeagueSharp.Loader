#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// ServiceFactory.cs is part of LeagueSharp.Loader.
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

using System.IO;
using System.Windows;

namespace LeagueSharp.Sandbox.Shared
{
    using System;
    using System.ServiceModel;

    public static class ServiceFactory
    {
        public static TInterfaceType CreateProxy<TInterfaceType>() where TInterfaceType : class
        {
            try
            {
                return
                    new ChannelFactory<TInterfaceType>(
                        new NetNamedPipeBinding(),
                        new EndpointAddress("net.pipe://localhost/" + typeof(TInterfaceType).Name)).CreateChannel();
            }
            catch (Exception e)
            {
                throw new Exception(
                    "Failed to connect to pipe for communication. The targetted pipe may not be loaded yet. Desired interface: "
                    + typeof(TInterfaceType).Name,
                    e);
            }
        }

        public static ServiceHost CreateService<TInterfaceType, TImplementationType>(bool open = true)
            where TImplementationType : class
        {
            try
            {
                if (!typeof (TInterfaceType).IsAssignableFrom(typeof (TImplementationType)))
                {
                    throw new NotImplementedException(
                        typeof (TImplementationType).FullName + " does not implement " + typeof (TInterfaceType).FullName);
                }

                var endpoint = new Uri("net.pipe://localhost/" + typeof (TInterfaceType).Name);
                var host = new ServiceHost(typeof (TImplementationType));

                host.AddServiceEndpoint(typeof (TInterfaceType), new NetNamedPipeBinding(), endpoint);
                host.Opened += (sender, args) => { Console.WriteLine("Opened: " + endpoint); };
                host.Faulted += (sender, args) => { Console.WriteLine("Faulted: " + endpoint); };
                host.UnknownMessageReceived += (sender, args) => { Console.WriteLine("UnknownMessage: " + endpoint); };

                if (open)
                {
                    host.Open();
                }

                return host;
            }
            catch (Exception e)
            {
                MessageBox.Show($"Make sure only one LeagueSharp Instance is running on your Computer.\n\n{e.Message}", "Failed to initialize Remoting");
                Environment.Exit(0);
            }

            return null;
        }
    }
}