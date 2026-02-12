#define MyAppName "Scan Center"
#define MyAppVersion "1.0.0"
#define MyAppPublisher "Scan Center Project"
#define MyAppExeName "ScanCenter.exe"

[Setup]
AppId={{B2452A29-D848-4A43-8A22-BCCFC4B045E5}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
DefaultDirName={autopf}\Scan Center
DefaultGroupName=Scan Center
OutputBaseFilename=ScanCenterInstaller
Compression=lzma
SolidCompression=yes
ArchitecturesInstallIn64BitMode=x64

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Files]
Source: "..\ScanCenter\bin\Release\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
Name: "{group}\Scan Center"; Filename: "{app}\{#MyAppExeName}"
Name: "{commondesktop}\Scan Center"; Filename: "{app}\{#MyAppExeName}"

[Run]
Filename: "{app}\{#MyAppExeName}"; Description: "Launch Scan Center"; Flags: nowait postinstall skipifsilent
