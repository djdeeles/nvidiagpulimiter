# Nvidia GPU Limiter

A lightweight, highly optimized Windows system-tray utility that dynamically manages Nvidia GPU power states. It locks your GPU to low-power frequencies (saving energy and reducing heat) during normal desktop usage, and automatically unlocks maximum performance when a full-screen game or application is launched.

Developed by **Çetin Çöne** ([djdeeles@gmail.com](mailto:djdeeles@gmail.com)).

## ✨ Features

* **Dynamic Clock Locking:** Automatically applies your custom minimum Core and Memory clock limits while on the desktop to simulate an ultra-low power state (P8).
* **Smart Full-Screen Detection:** Instantly removes clock limits when a full-screen application (like a game) is focused, allowing the GPU to boost normally (P0).
* **App Filtering (Whitelist / Blacklist):** Don't want your GPU to heat up just because you made a YouTube video full-screen? Add `chrome.exe` to the Blacklist, and the app will keep the GPU locked. Alternatively, use a Whitelist to *only* allow specific games to trigger performance mode.
* **Force Power Save Mode:** A quick toggle from the system tray to lock your GPU at minimum frequencies regardless of what is running.
* **Zero CPU Overhead:** Built using native Windows Kernel32 APIs (`QueryFullProcessImageName` and `GetForegroundWindow`) instead of standard .NET process diagnostics to ensure virtually 0% CPU usage in the background.
* **Seamless Windows Startup:** Integrates directly with Windows Task Scheduler to start automatically with elevated privileges, bypassing annoying UAC prompts on every boot.
* **Modern Dark UI:** A clean, dark-themed settings panel to easily adjust your clock limits and manage your app filters.

## ⚙️ Requirements

* **OS:** Windows 10 / 11
* **GPU:** Nvidia Graphics Card (RTX series recommended)
* **Runtime:** [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
* **Drivers:** Nvidia Display Drivers (which include `nvidia-smi` by default)

## 🚀 Installation & Usage

1. Download the latest `.exe` release or build the project from source using Visual Studio / the .NET CLI.
2. **Run the application as Administrator.** (This is strictly required for `nvidia-smi` to modify hardware clocks).
3. The app will appear in your System Tray. Right-click the icon and select **Settings** (Ayarlar).
4. Enter your desired minimum Core and Memory frequencies.
5. Check the "Run on Windows startup" option to create a seamless auto-start task.

## ⚠️ Important Notes & Troubleshooting

* **Administrator Rights are Mandatory:** The tool relies on `nvidia-smi` commands (`-lgc` and `-lmc`) to enforce hardware-level clock limits. If the app is not running as Administrator, Nvidia drivers will silently reject the commands.
* **Finding Your Supported Clocks:** Every GPU architecture has a different minimum hardware limit. For example, older GDDR6 cards might support dropping the memory clock to `405 MHz`, while newer GDDR7 cards (like the RTX 50 series) might require a higher minimum floor (e.g., `810 MHz` or higher). If your limits are not being applied, try increasing the values in the Settings menu slightly. You can check your GPU's supported clocks by running `nvidia-smi -q -d SUPPORTED_CLOCKS` in an elevated command prompt.
* **Multi-Monitor Setups:** Nvidia drivers sometimes force higher memory clocks to prevent screen flickering when multiple high-refresh-rate monitors are connected. If the memory clock refuses to drop, this hardware protection might be active.

## 🛠️ Built With
* C# / .NET 8.0 (Windows Forms)
* Windows API (P/Invoke)
* `nvidia-smi` (Nvidia System Management Interface)

## 📄 License
This project is open-source and available under the MIT License.
