using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

public class SettingsForm : Form
{
    private GPUController gpuController;
    private TrayApplicationContext trayContext;
    
    private Label lblStatusText = null!;
    private Label lblClocks = null!;
    private TextBox txtCoreMin = null!;
    private TextBox txtCoreMax = null!;
    private TextBox txtMemLock = null!;
    private Button btnApply = null!;
    private Button btnToggle = null!;
    private CheckBox chkStartup = null!;
    
    private CheckBox chkForceSave = null!;
    private ComboBox cmbListMode = null!;
    private ListBox lstApps = null!;
    private Button btnAddApp = null!;
    private Button btnRemoveApp = null!;

    private System.Windows.Forms.Timer uiTimer = null!;

    public SettingsForm(GPUController controller, TrayApplicationContext context)
    {
        this.gpuController = controller;
        this.trayContext = context;
        SetupUI();
        UpdateToggleText();

        uiTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        uiTimer.Tick += UiTimer_Tick;
        
        // AYARLAR PENCERESİ GİZLENDİĞİNDE ARKA PLAN SAYACINI DURDUR (PERFORMANS İYİLEŞTİRMESİ)
        this.VisibleChanged += (s, e) => {
            if (this.Visible) uiTimer.Start();
            else uiTimer.Stop();
        };
    }

    private void UiTimer_Tick(object? sender, EventArgs e)
    {
        if (chkForceSave.Checked != gpuController.ForcePowerSave)
        {
            chkForceSave.CheckedChanged -= ChkForceSave_CheckedChanged;
            chkForceSave.Checked = gpuController.ForcePowerSave;
            chkForceSave.CheckedChanged += ChkForceSave_CheckedChanged;
        }

        if (!trayContext.IsAppEnabled)
        {
            lblStatusText.Text = "MOD: DEVRE DIŞI";
            lblStatusText.ForeColor = Color.Gray;
        }
        else if (gpuController.ForcePowerSave)
        {
            lblStatusText.Text = "MOD: ZORUNLU TASARRUF";
            lblStatusText.ForeColor = Color.MediumPurple;
        }
        else
        {
            lblStatusText.Text = gpuController.CurrentState == "LockedClocks" ? "MOD: OTOMATİK (Kilitli)" : "MOD: OTOMATİK (Serbest)";
            lblStatusText.ForeColor = gpuController.CurrentState == "LockedClocks" ? Color.MediumSpringGreen : Color.Tomato;
        }

        lblClocks.Text = $"Anlık Çekirdek, Bellek:\n{gpuController.GetActualClocks()}";
    }

    private void btnApply_Click(object? sender, EventArgs e)
    {
        if (int.TryParse(txtCoreMin.Text, out int cMin) && 
            int.TryParse(txtCoreMax.Text, out int cMax) && 
            int.TryParse(txtMemLock.Text, out int mLock))
        {
            gpuController.CoreMin = cMin;
            gpuController.CoreMax = cMax;
            gpuController.MemLock = mLock;
            gpuController.SaveConfig();
            
            if (trayContext.IsAppEnabled)
            {
                gpuController.CurrentState = "Unknown";
                gpuController.SetLockedMode();
            }
            MessageBox.Show("Frekans limitleri kaydedildi!", "Başarılı", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    private void btnToggle_Click(object? sender, EventArgs e)
    {
        trayContext.ToggleEnableDisable();
        UpdateToggleText();
        UiTimer_Tick(null, EventArgs.Empty);
    }

    private void UpdateToggleText()
    {
        btnToggle.Text = trayContext.IsAppEnabled ? "Uygulamayı Devre Dışı Bırak" : "Uygulamayı Aktifleştir";
        btnToggle.BackColor = trayContext.IsAppEnabled ? Color.IndianRed : Color.MediumSeaGreen;
    }
    
    private void ChkForceSave_CheckedChanged(object? sender, EventArgs e)
    {
        gpuController.ForcePowerSave = chkForceSave.Checked;
        gpuController.SaveConfig();
    }

    private void CmbListMode_SelectedIndexChanged(object? sender, EventArgs e)
    {
        gpuController.ListMode = cmbListMode.SelectedIndex == 1 ? "Whitelist" : "Blacklist";
        gpuController.SaveConfig();
    }

    private void BtnAddApp_Click(object? sender, EventArgs e)
    {
        using (OpenFileDialog ofd = new OpenFileDialog { Filter = "Uygulamalar (*.exe)|*.exe", Title = "Uygulama Seç" })
        {
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string exeName = Path.GetFileName(ofd.FileName).ToLower();
                if (!gpuController.AppList.Contains(exeName))
                {
                    gpuController.AppList.Add(exeName);
                    lstApps.Items.Add(exeName);
                    gpuController.SaveConfig();
                }
            }
        }
    }

    private void BtnRemoveApp_Click(object? sender, EventArgs e)
    {
        if (lstApps.SelectedItem != null)
        {
            string? exeName = lstApps.SelectedItem.ToString();
            if (exeName != null)
            {
                gpuController.AppList.Remove(exeName);
                lstApps.Items.Remove(exeName);
                gpuController.SaveConfig();
            }
        }
    }

    private void ChkStartup_CheckedChanged(object? sender, EventArgs e)
    {
        if (chkStartup.Checked)
        {
            if (!gpuController.IsAdministrator())
            {
                MessageBox.Show("Başlangıca eklemek için uygulamayı Yönetici Olarak Çalıştırmalısınız!", "Yetki Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
                chkStartup.CheckedChanged -= ChkStartup_CheckedChanged;
                chkStartup.Checked = false;
                chkStartup.CheckedChanged += ChkStartup_CheckedChanged;
                return;
            }

            try
            {
                string exePath = Application.ExecutablePath;
                string tempXml = Path.Combine(Path.GetTempPath(), "GPUMonitorTask.xml");
                
                string xml = $@"<?xml version=""1.0"" encoding=""UTF-16""?>
                <Task version=""1.2"" xmlns=""http://schemas.microsoft.com/windows/2004/02/mit/task"">
                  <Triggers><LogonTrigger><Enabled>true</Enabled><Delay>PT15S</Delay></LogonTrigger></Triggers>
                  <Principals><Principal id=""Author""><LogonType>InteractiveToken</LogonType><RunLevel>HighestAvailable</RunLevel></Principal></Principals>
                  <Settings>
                    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
                    <DisallowStartIfOnBatteries>false</DisallowStartIfOnBatteries>
                    <StopIfGoingOnBatteries>false</StopIfGoingOnBatteries>
                    <AllowHardTerminate>true</AllowHardTerminate>
                    <StartWhenAvailable>true</StartWhenAvailable>
                    <RunOnlyIfNetworkAvailable>false</RunOnlyIfNetworkAvailable>
                    <AllowStartOnDemand>true</AllowStartOnDemand>
                    <Enabled>true</Enabled><Hidden>false</Hidden><RunOnlyIfIdle>false</RunOnlyIfIdle>
                    <WakeToRun>false</WakeToRun><ExecutionTimeLimit>PT0S</ExecutionTimeLimit><Priority>7</Priority>
                  </Settings>
                  <Actions Context=""Author""><Exec><Command>""{exePath}""</Command></Exec></Actions>
                </Task>";

                File.WriteAllText(tempXml, xml, Encoding.Unicode);

                ProcessStartInfo psi = new ProcessStartInfo("schtasks", $"/create /tn \"GPUMonitorTask\" /xml \"{tempXml}\" /f")
                { CreateNoWindow = true, UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true };

                using (Process? p = Process.Start(psi))
                {
                    p?.WaitForExit();
                    if (p?.ExitCode != 0)
                    {
                        chkStartup.CheckedChanged -= ChkStartup_CheckedChanged;
                        chkStartup.Checked = false;
                        chkStartup.CheckedChanged += ChkStartup_CheckedChanged;
                        return; 
                    }
                }
                File.Delete(tempXml);
            }
            catch { return; }
        }
        else
        {
            try
            {
                Process.Start(new ProcessStartInfo("schtasks", "/delete /tn \"GPUMonitorTask\" /f") { CreateNoWindow = true, UseShellExecute = false })?.WaitForExit();
            }
            catch { }
        }

        gpuController.RunAtStartup = chkStartup.Checked;
        gpuController.SaveConfig();
    }

    private void SetupUI()
    {
        this.Text = "Nvidia GPU Limiter";
        this.Size = new Size(380, 600); 
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        Font mainFont = new Font("Segoe UI", 10F);
        Font titleFont = new Font("Segoe UI", 12F, FontStyle.Bold);

        lblStatusText = new Label { Location = new Point(20, 15), Size = new Size(340, 25), Font = titleFont };
        lblClocks = new Label { Location = new Point(20, 45), Size = new Size(340, 45), Font = mainFont, ForeColor = Color.LightGray };

        int yPos = 95;
        this.Controls.Add(CreateLabel("Çekirdek Min Hız:", 20, yPos));
        txtCoreMin = CreateTextBox(gpuController.CoreMin.ToString(), 160, yPos, 180);
        this.Controls.Add(txtCoreMin);

        yPos += 35;
        this.Controls.Add(CreateLabel("Çekirdek Max Hız:", 20, yPos));
        txtCoreMax = CreateTextBox(gpuController.CoreMax.ToString(), 160, yPos, 180);
        this.Controls.Add(txtCoreMax);

        yPos += 35;
        this.Controls.Add(CreateLabel("Bellek Hızı:", 20, yPos));
        txtMemLock = CreateTextBox(gpuController.MemLock.ToString(), 160, yPos, 180);
        this.Controls.Add(txtMemLock);

        btnApply = new Button { Text = "Frekansları Uygula", Location = new Point(20, 205), Size = new Size(320, 35), Font = mainFont, BackColor = Color.FromArgb(0, 122, 204), FlatStyle = FlatStyle.Flat, ForeColor = Color.White };
        btnApply.FlatAppearance.BorderSize = 0;
        btnApply.Click += btnApply_Click;
        
        Label sep1 = new Label { Location = new Point(20, 250), Size = new Size(320, 1), BackColor = Color.Gray };
        this.Controls.Add(sep1);

        chkForceSave = new CheckBox { Text = "Zorunlu Tasarruf Modu (Sürekli Kilitli)", Location = new Point(20, 260), Size = new Size(320, 25), Font = mainFont };
        chkForceSave.Checked = gpuController.ForcePowerSave;
        chkForceSave.CheckedChanged += ChkForceSave_CheckedChanged;

        this.Controls.Add(CreateLabel("Filtre Tipi:", 20, 295));
        cmbListMode = new ComboBox { Location = new Point(160, 295), Size = new Size(180, 25), Font = mainFont, DropDownStyle = ComboBoxStyle.DropDownList, BackColor = Color.FromArgb(45,45,48), ForeColor = Color.White };
        cmbListMode.Items.AddRange(new object[] { "Kara Liste (Engelle)", "Beyaz Liste (İzin Ver)" });
        cmbListMode.SelectedIndex = gpuController.ListMode == "Whitelist" ? 1 : 0;
        cmbListMode.SelectedIndexChanged += CmbListMode_SelectedIndexChanged;

        lstApps = new ListBox { Location = new Point(20, 330), Size = new Size(220, 90), Font = mainFont, BackColor = Color.FromArgb(45,45,48), ForeColor = Color.White };
        foreach (var app in gpuController.AppList) lstApps.Items.Add(app);
        
        btnAddApp = new Button { Text = "Gözat...", Location = new Point(250, 330), Size = new Size(90, 35), Font = mainFont, BackColor = Color.FromArgb(60,60,60), FlatStyle = FlatStyle.Flat };
        btnAddApp.FlatAppearance.BorderSize = 0;
        btnAddApp.Click += BtnAddApp_Click;
        
        btnRemoveApp = new Button { Text = "Kaldır", Location = new Point(250, 370), Size = new Size(90, 35), Font = mainFont, BackColor = Color.IndianRed, FlatStyle = FlatStyle.Flat };
        btnRemoveApp.FlatAppearance.BorderSize = 0;
        btnRemoveApp.Click += BtnRemoveApp_Click;
        
        Label sep2 = new Label { Location = new Point(20, 435), Size = new Size(320, 1), BackColor = Color.Gray };
        this.Controls.Add(sep2);

        btnToggle = new Button { Location = new Point(20, 450), Size = new Size(320, 35), Font = mainFont, FlatStyle = FlatStyle.Flat };
        btnToggle.FlatAppearance.BorderSize = 0;
        btnToggle.Click += btnToggle_Click;

        chkStartup = new CheckBox { Text = "Windows başladığında otomatik çalıştır", Location = new Point(20, 495), Size = new Size(320, 25), Font = mainFont, ForeColor = Color.LightGray };
        
        chkStartup.Checked = gpuController.RunAtStartup;
        chkStartup.CheckedChanged += ChkStartup_CheckedChanged;
        
        this.Controls.Add(lblStatusText); this.Controls.Add(lblClocks);
        this.Controls.Add(btnApply); this.Controls.Add(chkForceSave);
        this.Controls.Add(cmbListMode); this.Controls.Add(lstApps);
        this.Controls.Add(btnAddApp); this.Controls.Add(btnRemoveApp);
        this.Controls.Add(btnToggle); this.Controls.Add(chkStartup);
        
        if (!gpuController.IsAdministrator())
        {
            Label lblAdminWarning = new Label { Text = "DİKKAT: Yönetici olarak çalıştırmadınız!", Location = new Point(20, 525), Size = new Size(320, 25), Font = new Font("Segoe UI", 9F, FontStyle.Bold), ForeColor = Color.Tomato };
            this.Controls.Add(lblAdminWarning);
        }
    }

    private Label CreateLabel(string text, int x, int y) => new Label { Text = text, Location = new Point(x, y), Size = new Size(130, 25), Font = new Font("Segoe UI", 10F) };
    private TextBox CreateTextBox(string text, int x, int y, int w) => new TextBox { Text = text, Location = new Point(x, y), Size = new Size(w, 25), Font = new Font("Segoe UI", 10F), BackColor = Color.FromArgb(45, 45, 48), ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        e.Cancel = true;
        this.Hide(); 
    }
}