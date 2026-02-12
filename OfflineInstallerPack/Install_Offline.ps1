param()

$ErrorActionPreference = "Stop"
$PackRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$LogFile = Join-Path $PackRoot ("OfflineInstall_{0:yyyyMMdd_HHmmss}.log" -f (Get-Date))

function Write-Log {
    param([string]$Message)
    $line = "{0:u} {1}" -f (Get-Date), $Message
    $line | Tee-Object -FilePath $LogFile -Append
}

function Test-IsAdmin {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Get-DotNet48Installed {
    $key = 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full'
    if (!(Test-Path $key)) { return $false }
    $release = (Get-ItemProperty -Path $key -Name Release -ErrorAction SilentlyContinue).Release
    return ($release -ge 528040)
}

function Ensure-WiaService {
    Write-Log "Ensuring WIA service (stisvc) is enabled and running."
    sc.exe config stisvc start= auto | Out-Null
    try {
        Start-Service stisvc -ErrorAction Stop
    } catch {
        Write-Log "Start-Service stisvc returned: $($_.Exception.Message)"
    }
}

if (-not (Test-IsAdmin)) {
    throw "Please run this script as Administrator."
}

Write-Log "=== Starting offline install ==="
Write-Log "OS: $([Environment]::OSVersion.VersionString)"

$dotnetExe = Join-Path $PackRoot 'Prereqs\dotNetFx48Offline.exe'
if (Get-DotNet48Installed) {
    Write-Log ".NET Framework 4.8 already installed."
} else {
    if (!(Test-Path $dotnetExe)) {
        throw "Missing prerequisite: $dotnetExe"
    }
    Write-Log "Installing .NET Framework 4.8 from $dotnetExe"
    $p = Start-Process -FilePath $dotnetExe -ArgumentList '/q /norestart' -Wait -PassThru
    Write-Log ".NET installer exit code: $($p.ExitCode)"
}

$driverExe = Join-Path $PackRoot 'Drivers\HP_Scanjet_4850_BasicDriver.exe'
if (Test-Path $driverExe) {
    Write-Log "Installing HP Scanjet 4850 driver package."
    $silentArgs = @('/S', '/silent', '/qn', '/quiet')
    $installed = $false
    foreach ($args in $silentArgs) {
        try {
            Write-Log "Trying driver installer with switch: $args"
            $p = Start-Process -FilePath $driverExe -ArgumentList $args -Wait -PassThru -ErrorAction Stop
            Write-Log "Driver install attempt exit code: $($p.ExitCode)"
            if ($p.ExitCode -eq 0 -or $p.ExitCode -eq 3010) { $installed = $true; break }
        } catch {
            Write-Log "Switch $args failed: $($_.Exception.Message)"
        }
    }

    if (-not $installed) {
        Write-Log "Silent install unsupported or failed. Starting interactive installer."
        Start-Process -FilePath $driverExe -Wait
    }
} else {
    Write-Log "WARNING: driver package not found at $driverExe"
}

Ensure-WiaService

$appMsi = Join-Path $PackRoot 'App\ScanCenterInstaller.msi'
$appExe = Join-Path $PackRoot 'App\ScanCenterInstaller.exe'
if (Test-Path $appMsi) {
    Write-Log "Installing app from MSI: $appMsi"
    Start-Process msiexec.exe -ArgumentList "/i `"$appMsi`" /qn /norestart" -Wait
} elseif (Test-Path $appExe) {
    Write-Log "Installing app from EXE: $appExe"
    Start-Process -FilePath $appExe -ArgumentList '/VERYSILENT /NORESTART' -Wait
} else {
    throw "No app installer found in App folder."
}

Write-Log "Offline install complete."
Write-Host "Installation completed. Log file: $LogFile"
