﻿#region LICENSE

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
    using System.Collections.Generic;

    using PlaySharp.Service.Model;

    internal class AssemblyDatabase
    {
        private static IReadOnlyList<AssemblyEntry> assemblies;

        public static IReadOnlyList<AssemblyEntry> Assemblies
        {
            get
            {
                if (assemblies == null)
                {
                    assemblies = WebService.Client.Assemblies();
                }

                return assemblies;
            }
        }
    }
}