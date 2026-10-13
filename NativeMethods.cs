using System;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Text;

public static class NativeMethods
{
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    public static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    // OPTİMİZASYON: Çok daha hafif ve hızlı API'ler
    [DllImport("kernel32.dll")]
    public static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool CloseHandle(IntPtr hObject);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    public static extern bool QueryFullProcessImageName(IntPtr hProcess, uint dwFlags, [Out] StringBuilder lpExeName, ref uint lpdwSize);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    public static bool IsForegroundFullScreen()
    {
        IntPtr foregroundHwnd = GetForegroundWindow();
        IntPtr desktopHwnd = GetDesktopWindow();

        if (foregroundHwnd == IntPtr.Zero || foregroundHwnd == desktopHwnd)
            return false;

        GetWindowRect(foregroundHwnd, out RECT appBounds);
        Rectangle screenBounds = System.Windows.Forms.Screen.FromHandle(foregroundHwnd).Bounds;

        return (appBounds.Bottom - appBounds.Top == screenBounds.Height &&
                appBounds.Right - appBounds.Left == screenBounds.Width);
    }

    // YENİ: Döngü içerisinde CPU'yu yormadan Exe adını bulan optimize kod
    public static string GetForegroundProcessName()
    {
        IntPtr hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero) return "";
        
        GetWindowThreadProcessId(hwnd, out uint pid);
        
        // 0x1000 = PROCESS_QUERY_LIMITED_INFORMATION (En az yetkiyle, en hızlı okuma)
        IntPtr hProcess = OpenProcess(0x1000, false, pid);
        if (hProcess == IntPtr.Zero) return "";
        
        try
        {
            StringBuilder buffer = new StringBuilder(1024);
            uint size = (uint)buffer.Capacity;
            if (QueryFullProcessImageName(hProcess, 0, buffer, ref size))
            {
                return System.IO.Path.GetFileName(buffer.ToString()).ToLower();
            }
        }
        finally
        {
            CloseHandle(hProcess);
        }
        return "";
    }
}