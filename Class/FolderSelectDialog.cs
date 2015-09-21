#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// FolderSelectDialog.cs is part of LeagueSharp.Loader.
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

 // ------------------------------------------------------------------
// Wraps System.Windows.Forms.OpenFileDialog to make it present
// a vista-style dialog.
// ------------------------------------------------------------------

namespace LeagueSharp.Loader.Class
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    ///     Wraps System.Windows.Forms.OpenFileDialog to make it present
    ///     a vista-style dialog.
    /// </summary>
    public class FolderSelectDialog
    {
        /// <summary>
        ///     Default constructor
        /// </summary>
        public FolderSelectDialog()
        {
            this.ofd = new OpenFileDialog();

            this.ofd.Filter = "Folders|\n";
            this.ofd.AddExtension = false;
            this.ofd.CheckFileExists = false;
            this.ofd.DereferenceLinks = true;
            this.ofd.Multiselect = false;
        }

        // Wrapped dialog
        private OpenFileDialog ofd = null;

        /// <summary>
        ///     Gets the selected folder
        /// </summary>
        public string FileName
        {
            get
            {
                return this.ofd.FileName;
            }
        }

        /// <summary>
        ///     Gets/Sets the initial folder to be selected. A null value selects the current directory.
        /// </summary>
        public string InitialDirectory
        {
            get
            {
                return this.ofd.InitialDirectory;
            }
            set
            {
                this.ofd.InitialDirectory = value == null || value.Length == 0 ? Environment.CurrentDirectory : value;
            }
        }

        /// <summary>
        ///     Gets/Sets the title to show in the dialog
        /// </summary>
        public string Title
        {
            get
            {
                return this.ofd.Title;
            }
            set
            {
                this.ofd.Title = value == null ? "Select a folder" : value;
            }
        }

        /// <summary>
        ///     Shows the dialog
        /// </summary>
        /// <returns>True if the user presses OK else false</returns>
        public bool ShowDialog()
        {
            return this.ShowDialog(IntPtr.Zero);
        }

        /// <summary>
        ///     Shows the dialog
        /// </summary>
        /// <param name="hWndOwner">Handle of the control to be parent</param>
        /// <returns>True if the user presses OK else false</returns>
        public bool ShowDialog(IntPtr hWndOwner)
        {
            var flag = false;

            if (Environment.OSVersion.Version.Major >= 6)
            {
                var r = new Reflector("System.Windows.Forms");

                uint num = 0;
                var typeIFileDialog = r.GetType("FileDialogNative.IFileDialog");
                var dialog = r.Call(this.ofd, "CreateVistaDialog");
                r.Call(this.ofd, "OnBeforeVistaDialog", dialog);

                var options = (uint)r.CallAs(typeof(FileDialog), this.ofd, "GetOptions");
                options |= (uint)r.GetEnum("FileDialogNative.FOS", "FOS_PICKFOLDERS");
                r.CallAs(typeIFileDialog, dialog, "SetOptions", options);

                var pfde = r.New("FileDialog.VistaDialogEvents", this.ofd);
                var parameters = new object[] { pfde, num };
                r.CallAs2(typeIFileDialog, dialog, "Advise", parameters);
                num = (uint)parameters[1];
                try
                {
                    var num2 = (int)r.CallAs(typeIFileDialog, dialog, "Show", hWndOwner);
                    flag = 0 == num2;
                }
                finally
                {
                    r.CallAs(typeIFileDialog, dialog, "Unadvise", num);
                    GC.KeepAlive(pfde);
                }
            }
            else
            {
                var fbd = new FolderBrowserDialog();
                fbd.Description = this.Title;
                fbd.SelectedPath = this.InitialDirectory;
                fbd.ShowNewFolderButton = false;
                if (fbd.ShowDialog(new WindowWrapper(hWndOwner)) != DialogResult.OK)
                {
                    return false;
                }
                this.ofd.FileName = fbd.SelectedPath;
                flag = true;
            }

            return flag;
        }
    }

    /// <summary>
    ///     Creates IWin32Window around an IntPtr
    /// </summary>
    public class WindowWrapper : IWin32Window
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="handle">Handle to wrap</param>
        public WindowWrapper(IntPtr handle)
        {
            this._hwnd = handle;
        }

        private IntPtr _hwnd;

        /// <summary>
        ///     Original ptr
        /// </summary>
        public IntPtr Handle
        {
            get
            {
                return this._hwnd;
            }
        }
    }
}