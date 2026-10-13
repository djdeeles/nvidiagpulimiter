using System;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        // Ana form yerine Tray Context ile başlat
        Application.Run(new TrayApplicationContext());
    }
}