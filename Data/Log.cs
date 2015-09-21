#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// Log.cs is part of LeagueSharp.Loader.
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

namespace LeagueSharp.Loader.Data
{
    #region

    using System.Collections.ObjectModel;
    using System.ComponentModel;

    #endregion

    public static class Logs
    {
        public static Log MainLog = new Log();
    }

    public class Log : INotifyPropertyChanged
    {
        private ObservableCollection<LogItem> _items = new ObservableCollection<LogItem>();

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<LogItem> Items
        {
            get
            {
                return this._items;
            }
            set
            {
                this._items = value;
                this.OnPropertyChanged("Items");
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public class LogItem : INotifyPropertyChanged
    {
        private string _message;

        private string _source;

        private string _status;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Message
        {
            get
            {
                return this._message;
            }
            set
            {
                this._message = value;
                this.OnPropertyChanged("Message");
            }
        }

        public string Source
        {
            get
            {
                return this._source;
            }
            set
            {
                this._source = value;
                this.OnPropertyChanged("Source");
            }
        }

        public string Status
        {
            get
            {
                return this._status;
            }
            set
            {
                this._status = value;
                this.OnPropertyChanged("Status");
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }

    public static class LogStatus
    {
        public static string Error
        {
            get
            {
                return "Error";
            }
        }

        public static string Info
        {
            get
            {
                return "Info";
            }
        }

        public static string Ok
        {
            get
            {
                return "Ok";
            }
        }

        public static string Skipped
        {
            get
            {
                return "Skipped";
            }
        }
    }
}