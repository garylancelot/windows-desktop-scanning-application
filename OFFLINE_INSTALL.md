# OFFLINE_INSTALL.md

## Goal
Prepare an **Offline Installer Pack** on an online machine, then transfer to offline Windows 8.1/10/11 and install everything without internet.

## 1) Prepare package on online machine
1. Build app + installer (`ScanCenterInstaller.exe` or MSI).
2. Download .NET Framework 4.8 offline installer from Microsoft.
3. Download HP Scanjet 4850 signed driver package from HP support archive or trusted OEM mirror.
4. Place files into:

```text
OfflineInstallerPack/
  Prereqs/dotNetFx48Offline.exe
  Drivers/HP_Scanjet_4850_BasicDriver.exe
  App/ScanCenterInstaller.exe
```

## 2) Transfer
- Copy `OfflineInstallerPack` to USB storage.
- Move to offline target PC.

## 3) Install on offline target
1. Right-click PowerShell -> **Run as Administrator**.
2. `Set-ExecutionPolicy -Scope Process Bypass`
3. `cd <path-to-OfflineInstallerPack>`
4. `./Install_Offline.ps1`
5. Follow prompts if driver installer cannot run silently.

## 4) Validate
- Launch **Scan Center** from Start menu.
- Click **Refresh Devices**.
- Run **Test Connection**.
- Perform a preview and save a test scan.
