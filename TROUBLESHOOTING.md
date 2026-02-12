# TROUBLESHOOTING.md

## Scanner not detected
1. Reconnect USB cable directly to PC (avoid hubs).
2. Power cycle scanner.
3. In app, click **Refresh Devices** then **Test Connection**.

## WIA service disabled
1. Open elevated command prompt.
2. Run:
   - `sc config stisvc start= auto`
   - `net start stisvc`
3. Retry scan.

## Driver missing / wrong driver
1. Uninstall existing scanner device from Device Manager.
2. Re-run HP Scanjet 4850 basic driver installer from `OfflineInstallerPack\Drivers`.
3. Reboot and retest.

## Preview/scan fails
- Use app **Fix It** button.
- Use fallback acquisition wizard when prompted.
- Check logs in `Documents\ScanCenterOutput\logs`.

## PDF issues
- Ensure app has write permission to output folder.
- Verify disk space and filename template does not include invalid characters.
