# Shack-Side wfview Server Restart Setup

W3NTB Power Bridge can restart wfview on the shack computer after station power is restored. This helps when the radio loses its COM port while powered off and wfview does not recover by itself.

This feature is optional. It is intended for remote setups where:

- wfview server runs on the shack computer connected to the radio.
- W3NTB Power Bridge runs on the remote operating computer.
- The computers can reach each other over Tailscale or another private network.

## What The App Does

When you click **Launch Server**, W3NTB Power Bridge:

1. Connects to the shack computer using SSH.
2. Creates or updates a Windows Scheduled Task named `W3NTB Launch wfview`.
3. Starts that task as the logged-in shack desktop user.
4. The scheduled task opens `wfview.exe` in the visible shack desktop session.

This avoids the common Windows problem where starting a GUI program directly through SSH launches it in a hidden service session.

## Shack Computer Setup

Run these commands on the shack computer in **PowerShell as Administrator**.

Install and enable OpenSSH Server:

```powershell
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0
Start-Service sshd
Set-Service -Name sshd -StartupType Automatic
New-NetFirewallRule -Name sshd -DisplayName "OpenSSH Server (sshd)" -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22
```

Confirm SSH is listening locally:

```powershell
Test-NetConnection 127.0.0.1 -Port 22
```

## Operator Computer Setup

From the remote/operator computer, confirm the shack computer is reachable:

```powershell
tailscale ping SHACK_TAILSCALE_IP
ssh.exe SHACK_TAILSCALE_IP hostname
```

If SSH asks for a password, create an SSH key on the operator computer and add the public key to the shack computer's `authorized_keys` file. W3NTB Power Bridge needs passwordless SSH because it runs the launch command in the background.

## W3NTB Power Bridge Settings

In **Settings**, configure:

- **Server IP / host**: the shack computer's Tailscale IP address or host name.
- **Server wfview path**: normally `C:\Program Files\wfview\wfview.exe`.
- **Launch server during Start Station**: enable this if you want Start Station to restart shack-side wfview automatically.

## Testing

1. Open an RDP session to the shack computer so you can see its desktop.
2. Close wfview on the shack computer.
3. On the operator computer, click **Launch Server** in W3NTB Power Bridge.
4. Confirm wfview opens on the shack desktop.

If wfview does not appear, check the W3NTB Power Bridge event log. SSH failures and scheduled-task output are written there.
