#region LICENSE

// Copyright 2016-2016 LeagueSharp.Loader
// AssemblyDatabase.cs is part of LeagueSharp.Loader.
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
    using System.Collections.Generic;
    using System.Linq;

    using PlaySharp.Service.Model;

    internal class AssemblyDatabase
    {
        public static List<AssemblyEntry> GetAssemblies()
        {
            try
            {
                var assemblies = WebService.Client.Assemblies();

                if (assemblies != null)
                {
                    return assemblies.ToList();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }
    }
}