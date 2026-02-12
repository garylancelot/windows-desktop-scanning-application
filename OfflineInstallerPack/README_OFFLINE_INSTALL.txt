Scan Center Offline Installer Pack
=================================

Required files to place before running Install_Offline.ps1:

1) Prereqs\dotNetFx48Offline.exe
   - Download from Microsoft official .NET Framework 4.8 offline installer page.

2) Drivers\HP_Scanjet_4850_BasicDriver.exe
   - Use HP signed Scanjet 4850 driver package.
   - If silent switches are unsupported, the script will open interactive mode.

3) App\ScanCenterInstaller.exe (or App\ScanCenterInstaller.msi)
   - Built from this repository.

Installation:
- Run PowerShell as Administrator.
- Execute: .\Install_Offline.ps1
- Review install log in this folder after completion.
