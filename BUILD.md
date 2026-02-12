# BUILD.md

## Requirements
- Visual Studio 2019/2022/2026 with **.NET desktop development** workload.
- .NET Framework 4.8 targeting pack.
- WiX Toolset v4 (optional, for MSI), or Inno Setup 6 (provided script).

## Build Steps
1. Open `ScanCenter.sln`.
2. Right-click `ScanCenter` project > **Restore NuGet Packages**.
3. Ensure COM reference is present:
   - `Microsoft Windows Image Acquisition Library v2.0` (`Interop.WIA`)
   - `Embed Interop Types = False`
4. Build `ScanCenter` in **Release | Any CPU**.
5. App output:
   - `ScanCenter\bin\Release\ScanCenter.exe`

## Build Installer
### Option A: Inno Setup (recommended for lightweight offline pack)
1. Open `ScanCenter.Setup\ScanCenterInstaller.iss` in Inno Setup.
2. Compile script.
3. Output: `ScanCenterInstaller.exe`.

### Option B: WiX v4 MSI
1. Install WiX Toolset v4.
2. Build `ScanCenter.Setup` project.
3. Output MSI in setup project `bin\Release`.

## Notes
- App is intended for offline use once installed.
- Scanner drivers must be installed separately (HP-provided signed package).
