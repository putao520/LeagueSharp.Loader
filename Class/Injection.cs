#region LICENSE

// Copyright 2015-2015 LeagueSharp.Loader
// Injection.cs is part of LeagueSharp.Loader.
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
    using System.Diagnostics;
    using System.IO.MemoryMappedFiles;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;

    using LeagueSharp.Loader.Data;

    public static class Injection
    {
        public static MemoryMappedFile mmf = null;

        private static IntPtr bootstrapper;

        private static GetFilePathDelegate getFilePath;

        private static HasModuleDelegate hasModule;

        private static InjectDLLDelegate injectDLL;

        public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        public delegate void OnInjectDelegate(IntPtr hwnd);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate bool GetFilePathDelegate(int processId, [Out] StringBuilder path, int size);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate bool HasModuleDelegate(int processId, string path);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate bool InjectDLLDelegate(int processId, string path);

        public static event OnInjectDelegate OnInject;

        public static bool InjectedAssembliesChanged { get; set; }

        public static bool IsInjected
        {
            get
            {
                return LeagueProcess.Any(IsProcessInjected);
            }
        }

        public static List<IntPtr> LeagueInstances
        {
            get
            {
                return FindWindows("League of Legends (TM) Client");
            }
        }

        public static bool PrepareDone { get; set; }

        internal static bool IsLeagueOfLegendsFocused
        {
            get
            {
                return GetWindowText(Win32Imports.GetForegroundWindow()).Contains("League of Legends (TM) Client");
            }
        }

        private static List<Process> LeagueProcess
        {
            get
            {
                return Process.GetProcessesByName("League of Legends").ToList();
            }
        }

        public static void Pulse()
        {
            if (injectDLL == null || hasModule == null)
            {
                ResolveInjectDLL();
            }

            if (LeagueProcess == null)
            {
                return;
            }

            //Don't inject untill we checked that there are not updates for the loader.
            if (Updater.Updating || !Updater.CheckedForUpdates || !PrepareDone)
            {
                return;
            }

            foreach (var instance in LeagueProcess)
            {
                try
                {
                    Config.Instance.LeagueOfLegendsExePath = GetFilePath(instance);

                    if (!IsProcessInjected(instance))
                    {
                        if (Config.Instance.UpdateCoreOnInject)
                        {
                            try
                            {
                                Updater.UpdateCore(Config.Instance.LeagueOfLegendsExePath, true).Wait();
                            }
                            catch (Exception e)
                            {
                                Utility.Log(LogStatus.Error, "UpdateCoreOnInject", e.Message, Logs.MainLog);
                            }
                        }

                        var supported = true;

                        try
                        {
                            supported = Updater.IsSupported(Config.Instance.LeagueOfLegendsExePath).Result;
                        }
                        catch (Exception e)
                        {
                            Utility.Log(LogStatus.Error, "IsSupported", e.Message, Logs.MainLog);
                        }

                        if (injectDLL != null && supported)
                        {
                            injectDLL(instance.Id, PathRandomizer.LeagueSharpCoreDllPath);

                            OnInject?.Invoke(IntPtr.Zero);
                        }
                    }
                }
                catch (Exception e)
                {
                    Utility.Log(LogStatus.Error, "Pulse", e.Message, Logs.MainLog);
                    // ignored
                }
            }
        }

        public static void Unload()
        {
            if (bootstrapper != IntPtr.Zero)
            {
                try
                {
                    Win32Imports.FreeLibrary(bootstrapper);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private static List<IntPtr> FindWindows(string title)
        {
            var windows = new List<IntPtr>();

            Win32Imports.EnumWindows(
                delegate(IntPtr wnd, IntPtr param)
                    {
                        if (GetWindowText(wnd).Contains(title))
                        {
                            windows.Add(wnd);
                        }
                        return true;
                    },
                IntPtr.Zero);

            return windows;
        }

        private static string GetFilePath(Process process)
        {
            var sb = new StringBuilder(255);
            getFilePath(process.Id, sb, sb.Capacity);
            return sb.ToString();
        }

        private static string GetWindowText(IntPtr hWnd)
        {
            var size = Win32Imports.GetWindowTextLength(hWnd);
            if (size++ > 0)
            {
                var builder = new StringBuilder(size);
                Win32Imports.GetWindowText(hWnd, builder, builder.Capacity);
                return builder.ToString();
            }

            return string.Empty;
        }

        private static bool IsProcessInjected(Process leagueProcess)
        {
            if (leagueProcess != null)
            {
                try
                {
                    return hasModule(leagueProcess.Id, PathRandomizer.LeagueSharpCoreDllName);
                }
                catch (Exception e)
                {
                    Utility.Log(LogStatus.Error, "Injector", string.Format("Error - {0}", e), Logs.MainLog);
                }
            }
            return false;
        }

        private static void ResolveInjectDLL()
        {
            try
            {
                mmf = MemoryMappedFile.CreateOrOpen(
                    "Local\\LeagueSharpBootstrap",
                    260 * 2,
                    MemoryMappedFileAccess.ReadWrite);

                var sharedMem = new SharedMemoryLayout(
                    PathRandomizer.LeagueSharpSandBoxDllPath,
                    PathRandomizer.LeagueSharpBootstrapDllPath,
                    Config.Instance.Username,
                    Config.Instance.Password);

                using (var writer = mmf.CreateViewAccessor())
                {
                    var len = Marshal.SizeOf(typeof(SharedMemoryLayout));
                    var arr = new byte[len];
                    var ptr = Marshal.AllocHGlobal(len);
                    Marshal.StructureToPtr(sharedMem, ptr, true);
                    Marshal.Copy(ptr, arr, 0, len);
                    Marshal.FreeHGlobal(ptr);
                    writer.WriteArray(0, arr, 0, arr.Length);
                }

                bootstrapper = Win32Imports.LoadLibrary(Directories.BootstrapFilePath);
                if (!(bootstrapper != IntPtr.Zero))
                {
                    return;
                }

                var procAddress = Win32Imports.GetProcAddress(bootstrapper, "InjectModule");
                if (!(procAddress != IntPtr.Zero))
                {
                    return;
                }

                injectDLL =
                    Marshal.GetDelegateForFunctionPointer(procAddress, typeof(InjectDLLDelegate)) as InjectDLLDelegate;

                procAddress = Win32Imports.GetProcAddress(bootstrapper, "HasModule");
                if (!(procAddress != IntPtr.Zero))
                {
                    return;
                }

                hasModule =
                    Marshal.GetDelegateForFunctionPointer(procAddress, typeof(HasModuleDelegate)) as HasModuleDelegate;

                procAddress = Win32Imports.GetProcAddress(bootstrapper, "GetFilePath");
                if (!(procAddress != IntPtr.Zero))
                {
                    return;
                }

                getFilePath =
                    Marshal.GetDelegateForFunctionPointer(procAddress, typeof(GetFilePathDelegate)) as
                    GetFilePathDelegate;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Unicode)]
        private struct SharedMemoryLayout
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            private readonly string SandboxPath;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            private readonly string BootstrapPath;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            private readonly string User;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            private readonly string Password;

            public SharedMemoryLayout(string sandboxPath, string bootstrapPath, string user, string password)
            {
                this.SandboxPath = sandboxPath;
                this.BootstrapPath = bootstrapPath;
                this.User = user;
                this.Password = password;
            }
        }
    }
}