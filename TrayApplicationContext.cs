using System;
using System.Drawing;
using System.Windows.Forms;

public class TrayApplicationContext : ApplicationContext
{
    private NotifyIcon trayIcon;
    private System.Windows.Forms.Timer monitorTimer;
    private GPUController gpuController;
    private SettingsForm? settingsForm;
    
    private ToolStripMenuItem toggleMenuItem;
    private ToolStripMenuItem forceSaveMenuItem;
    
    public bool IsAppEnabled { get; private set; } = true;

    private Icon iconDisabled;
    private Icon iconForceSave;
    private Icon iconLocked;
    private Icon iconAuto;

    public TrayApplicationContext()
    {
        gpuController = new GPUController();

        iconDisabled = GenerateDotIcon(Color.Gray);
        iconForceSave = GenerateDotIcon(Color.MediumPurple);
        iconLocked = GenerateDotIcon(Color.MediumSpringGreen);
        iconAuto = GenerateDotIcon(Color.Tomato);

        toggleMenuItem = new ToolStripMenuItem("Devre Dışı Bırak", null, ToggleMenu_Click);
        forceSaveMenuItem = new ToolStripMenuItem("Zorunlu Tasarruf Modu", null, ForceSave_Click);
        forceSaveMenuItem.Checked = gpuController.ForcePowerSave;

        trayIcon = new NotifyIcon()
        {
            ContextMenuStrip = new ContextMenuStrip(),
            Visible = true,
            Text = "Nvidia GPU Limiter" // İSİM GÜNCELLENDİ
        };

        trayIcon.ContextMenuStrip.Items.Add("Ayarlar", null, ShowSettings);
        trayIcon.ContextMenuStrip.Items.Add("Hakkında", null, (s, e) => new AboutForm().ShowDialog()); // HAKKINDA EKLENDİ
        trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        trayIcon.ContextMenuStrip.Items.Add(forceSaveMenuItem);
        trayIcon.ContextMenuStrip.Items.Add(toggleMenuItem);
        trayIcon.ContextMenuStrip.Items.Add(new ToolStripSeparator());
        trayIcon.ContextMenuStrip.Items.Add("Çıkış", null, Exit);
        
        trayIcon.DoubleClick += ShowSettings;

        UpdateIconAndText();

        monitorTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        monitorTimer.Tick += MonitorTimer_Tick;
        monitorTimer.Start();
    }

    private void MonitorTimer_Tick(object? sender, EventArgs e)
    {
        if (!IsAppEnabled) return;

        if (gpuController.ForcePowerSave)
        {
            gpuController.SetLockedMode();
        }
        else
        {
            bool isFullScreen = NativeMethods.IsForegroundFullScreen();

            if (isFullScreen)
            {
                string processName = NativeMethods.GetForegroundProcessName();
                bool isInList = gpuController.AppList.Contains(processName);
                
                bool shouldUnlock = false;
                if (gpuController.ListMode == "Blacklist") shouldUnlock = !isInList; 
                else shouldUnlock = isInList; 
                
                if (shouldUnlock) gpuController.SetAutoMode();
                else gpuController.SetLockedMode();
            }
            else
            {
                gpuController.SetLockedMode(); 
            }
        }
        UpdateIconAndText();
    }

    private void ForceSave_Click(object? sender, EventArgs e)
    {
        gpuController.ForcePowerSave = !gpuController.ForcePowerSave;
        forceSaveMenuItem.Checked = gpuController.ForcePowerSave;
        gpuController.SaveConfig();
        UpdateIconAndText();
    }

    public void ToggleEnableDisable()
    {
        IsAppEnabled = !IsAppEnabled;
        toggleMenuItem.Text = IsAppEnabled ? "Devre Dışı Bırak" : "Aktifleştir";
        
        if (!IsAppEnabled) gpuController.SetAutoMode();
        else gpuController.CurrentState = "Unknown"; 
        
        UpdateIconAndText();
    }

    private void ToggleMenu_Click(object? sender, EventArgs e) => ToggleEnableDisable();

    private void UpdateIconAndText()
    {
        if (!IsAppEnabled)
        {
            trayIcon.Icon = iconDisabled;
            trayIcon.Text = "Nvidia GPU Limiter (Devre Dışı)";
        }
        else if (gpuController.ForcePowerSave)
        {
            trayIcon.Icon = iconForceSave;
            trayIcon.Text = "Nvidia GPU Limiter (ZORUNLU TASARRUF)";
        }
        else if (gpuController.CurrentState == "LockedClocks")
        {
            trayIcon.Icon = iconLocked;
            trayIcon.Text = "Nvidia GPU Limiter (Tasarruf/Kilitli)";
        }
        else
        {
            trayIcon.Icon = iconAuto;
            trayIcon.Text = "Nvidia GPU Limiter (Oyun/Serbest)";
        }
    }

    private Icon GenerateDotIcon(Color color)
    {
        using (Bitmap bmp = new Bitmap(16, 16))
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            using (Brush b = new SolidBrush(color))
            { g.FillEllipse(b, 2, 2, 12, 12); }
            return Icon.FromHandle(bmp.GetHicon());
        }
    }

    private void ShowSettings(object? sender, EventArgs e)
    {
        if (settingsForm == null || settingsForm.IsDisposed) settingsForm = new SettingsForm(gpuController, this);
        settingsForm.Show();
        settingsForm.BringToFront();
    }

    private void Exit(object? sender, EventArgs e)
    {
        monitorTimer.Stop();
        gpuController.SetAutoMode();
        
        if (trayIcon != null) { trayIcon.Visible = false; trayIcon.Dispose(); }
        
        iconDisabled?.Dispose();
        iconForceSave?.Dispose();
        iconLocked?.Dispose();
        iconAuto?.Dispose();
        
        Environment.Exit(0);
    }
}