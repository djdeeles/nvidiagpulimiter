using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;

public class GPUController
{
    public string CurrentState { get; set; } = "Unknown";
    
    public int CoreMin { get; set; } = 210;
    public int CoreMax { get; set; } = 420;
    public int MemLock { get; set; } = 405;
    
    public bool ForcePowerSave { get; set; } = false;
    public string ListMode { get; set; } = "Blacklist";
    public List<string> AppList { get; set; } = new List<string>();
    
    // Başlangıç durumu için yeni ayar
    public bool RunAtStartup { get; set; } = false;

    private readonly string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.ini");

    public GPUController()
    {
        LoadConfig(); 
    }

    public bool IsAdministrator()
    {
        var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public void LoadConfig()
    {
        try
        {
            if (File.Exists(configPath))
            {
                string[] lines = File.ReadAllLines(configPath);
                foreach (string line in lines)
                {
                    if (line.StartsWith("CoreMin=")) CoreMin = int.Parse(line.Split('=')[1].Trim());
                    else if (line.StartsWith("CoreMax=")) CoreMax = int.Parse(line.Split('=')[1].Trim());
                    else if (line.StartsWith("MemLock=")) MemLock = int.Parse(line.Split('=')[1].Trim());
                    else if (line.StartsWith("ForcePowerSave=")) ForcePowerSave = bool.Parse(line.Split('=')[1].Trim());
                    else if (line.StartsWith("ListMode=")) ListMode = line.Split('=')[1].Trim();
                    else if (line.StartsWith("RunAtStartup=")) RunAtStartup = bool.Parse(line.Split('=')[1].Trim());
                    else if (line.StartsWith("AppList=")) 
                    {
                        string apps = line.Substring("AppList=".Length).Trim();
                        if (!string.IsNullOrEmpty(apps))
                            AppList = new List<string>(apps.Split(','));
                    }
                }
            }
        }
        catch { }
    }

    public void SaveConfig()
    {
        try
        {
            File.WriteAllLines(configPath, new string[]
            {
                "[Settings]",
                $"CoreMin={CoreMin}",
                $"CoreMax={CoreMax}",
                $"MemLock={MemLock}",
                $"ForcePowerSave={ForcePowerSave}",
                $"ListMode={ListMode}",
                $"RunAtStartup={RunAtStartup}",
                $"AppList={string.Join(",", AppList)}"
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ayarlar kaydedilirken hata: {ex.Message}");
        }
    }

    public void SetLockedMode()
    {
        if (CurrentState == "LockedClocks") return;
        bool coreSuccess = ExecuteSmiCommand($"-lgc {CoreMin},{CoreMax}");
        bool memSuccess = ExecuteSmiCommand($"-lmc {MemLock}");
        CurrentState = (coreSuccess && memSuccess) ? "LockedClocks" : "Unknown";
    }

    public void SetAutoMode()
    {
        if (CurrentState == "Auto") return;
        bool coreSuccess = ExecuteSmiCommand("-rgc");
        bool memSuccess = ExecuteSmiCommand("-rmc");
        CurrentState = (coreSuccess && memSuccess) ? "Auto" : "Unknown";
    }

    private bool ExecuteSmiCommand(string args)
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo { FileName = "nvidia-smi", Arguments = args, UseShellExecute = false, RedirectStandardError = true, RedirectStandardOutput = true, CreateNoWindow = true };
            using (Process? process = Process.Start(psi))
            {
                if (process != null)
                {
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit(); 
                    if (!string.IsNullOrWhiteSpace(error))
                    {
                        return false; // Hata durumunu yoksayıp loglamak yerine arka planda boolean dönüyoruz
                    }
                    return true;
                }
            }
        }
        catch { return false; }
        return false;
    }
    
    public string GetActualClocks()
    {
        try
        {
            ProcessStartInfo psi = new ProcessStartInfo { FileName = "nvidia-smi", Arguments = "--query-gpu=clocks.gr,clocks.mem --format=csv,noheader", RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true };
            using (Process? process = Process.Start(psi))
            {
                if (process != null)
                {
                    string? actualState = process.StandardOutput.ReadToEnd()?.Trim();
                    process.WaitForExit();
                    return string.IsNullOrEmpty(actualState) ? "Bilinmiyor" : actualState;
                }
            }
        }
        catch { }
        return "Hata";
    }
}