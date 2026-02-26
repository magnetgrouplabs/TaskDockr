using System;
using System.Runtime.InteropServices;

namespace TaskDockr.Utils
{
    /// <summary>
    /// Sets a window's AppUserModelID so it appears as a separate taskbar button
    /// instead of being grouped with other windows from the same process.
    /// </summary>
    public static class TaskbarHelper
    {
        public static void SetWindowAppUserModelId(IntPtr hwnd, string appId)
        {
            try
            {
                var iid = new Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99");
                SHGetPropertyStoreForWindow(hwnd, ref iid, out var store);
                if (store == null) return;

                var key = new PropertyKey(new Guid("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), 5);
                var pv  = new PropVariant(appId);
                store.SetValue(ref key, ref pv);
                store.Commit();
                Marshal.ReleaseComObject(store);
            }
            catch { /* non-fatal */ }
        }

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SHGetPropertyStoreForWindow(
            IntPtr hwnd, ref Guid riid,
            [Out, MarshalAs(UnmanagedType.Interface)] out IPropertyStore ppv);

        [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"),
         InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        private interface IPropertyStore
        {
            int GetCount(out uint count);
            int GetAt(uint idx, out PropertyKey key);
            int GetValue(ref PropertyKey key, out PropVariant pv);
            int SetValue(ref PropertyKey key, ref PropVariant pv);
            int Commit();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct PropertyKey
        {
            public Guid fmtid;
            public uint pid;
            public PropertyKey(Guid fmtid, uint pid) { this.fmtid = fmtid; this.pid = pid; }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PropVariant
        {
            [FieldOffset(0)]  public ushort vt;
            [FieldOffset(8)]  public IntPtr pwszVal;

            public PropVariant(string val)
            {
                pwszVal = Marshal.StringToCoTaskMemUni(val);
                vt      = 31; // VT_LPWSTR
            }
        }
    }
}
