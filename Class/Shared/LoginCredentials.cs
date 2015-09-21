#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// LoginCredentials.cs is part of LeagueSharp.Loader.
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

namespace LeagueSharp.Sandbox.Shared
{
    using System.Runtime.Serialization;

    [DataContract]
    public class LoginCredentials
    {
        [DataMember]
        public string Password { get; set; }

        [DataMember]
        public string User { get; set; }
    }
}