using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

public class AboutForm : Form
{
    public AboutForm()
    {
        this.Text = "Hakkında - Nvidia GPU Limiter";
        this.Size = new Size(350, 230);
        this.BackColor = Color.FromArgb(30, 30, 30);
        this.ForeColor = Color.White;
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.StartPosition = FormStartPosition.CenterScreen;

        Icon? appIcon = null;
        try
        {
            // .exe dosyasının içine gömülü olan ikonu otomatik çekip alır
            appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (appIcon != null)
            {
                this.Icon = appIcon;
            }
        }
        catch
        {
            // Herhangi bir aksilik olursa çökmek yerine kalkan ikonunu kullanır
            appIcon = SystemIcons.Shield;
        }

        // Çekilen gömülü ikonu formun içindeki resim kutusuna yansıt
        PictureBox picIcon = new PictureBox
        {
            Image = appIcon?.ToBitmap(), 
            Location = new Point(20, 20),
            Size = new Size(48, 48),
            SizeMode = PictureBoxSizeMode.Zoom
        };

        // Uygulama Adı
        Label lblTitle = new Label
        {
            Text = "Nvidia GPU Limiter",
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            Location = new Point(80, 20),
            AutoSize = true
        };

        // Versiyon
        Label lblVersion = new Label
        {
            Text = "v1.0",
            Font = new Font("Segoe UI", 10F),
            ForeColor = Color.MediumSpringGreen,
            Location = new Point(82, 48),
            AutoSize = true
        };

        // Geliştirici Bilgileri
        Label lblDev = new Label
        {
            Text = "Geliştirici: Çetin Çöne\nİletişim: djdeeles@gmail.com",
            Font = new Font("Segoe UI", 10F),
            Location = new Point(20, 90),
            AutoSize = true
        };

        // GitHub Linki
        LinkLabel lnkGithub = new LinkLabel
        {
            Text = "https://github.com/djdeeles/nvidiagpulimiter",
            Location = new Point(20, 140),
            AutoSize = true,
            Font = new Font("Segoe UI", 10F),
            LinkColor = Color.DeepSkyBlue,
            ActiveLinkColor = Color.DodgerBlue
        };
        
        lnkGithub.LinkClicked += (s, e) => 
        {
            Process.Start(new ProcessStartInfo 
            { 
                FileName = lnkGithub.Text, 
                UseShellExecute = true 
            });
        };

        this.Controls.Add(picIcon);
        this.Controls.Add(lblTitle);
        this.Controls.Add(lblVersion);
        this.Controls.Add(lblDev);
        this.Controls.Add(lnkGithub);
    }
}