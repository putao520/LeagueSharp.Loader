#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// Profile.cs is part of LeagueSharp.Loader.
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

using LeagueSharp.Loader.Data.Assemblies;

namespace LeagueSharp.Loader.Class
{
    #region

    using System.Collections.ObjectModel;
    using System.ComponentModel;

    #endregion

    public class Profile : INotifyPropertyChanged
    {
        private ObservableCollection<LeagueSharpAssembly> _installedAssemblies;

        private string _name;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<LeagueSharpAssembly> InstalledAssemblies
        {
            get
            {
                return this._installedAssemblies;
            }
            set
            {
                this._installedAssemblies = value;
                this.OnPropertyChanged("InstalledAssemblies");
            }
        }

        public string Name
        {
            get
            {
                return this._name;
            }
            set
            {
                this._name = value;
                this.OnPropertyChanged("Name");
            }
        }

        public void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}