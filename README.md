# W3NTB Power Bridge

W3NTB Power Bridge is a Windows 11 WPF application that brings up and monitors a remote amateur radio station, bridging N3FJP Amateur Contact Log to wfview through local TCP services.

## How it fits into a station

W3NTB Power Bridge is useful in both local and remote operating setups.

For local desktop operating, you can run ACLog, wfview, and W3NTB Power Bridge on the same Windows computer. In that setup, wfview can provide radio control and spectrum/scope display while ACLog remains the logging program. W3NTB Power Bridge keeps frequency and mode information moving between them so changes made from ACLog, wfview, or the radio stay in sync.

For remote operating with wfview network radio access, the usual pattern is to run wfview on the shack computer as the radio server connected to the radio CI-V/USB path. On the remote computer, run wfview as the client, ACLog as the logger, and W3NTB Power Bridge as the bridge between them. In that setup, W3NTB Power Bridge talks to ACLog and the remote wfview client locally on the remote computer using `127.0.0.1`, while wfview and Tailscale handle the network path back to the shack computer.

This app does not replace Tailscale, wfview, or ACLog. It is the station bridge/control panel that helps those pieces work together.

More remote-access detail is available in [Remote Access and Tailscale Notes](docs/REMOTE_ACCESS.md).

## What it does

- Connects to the ACLog TCP API at `127.0.0.1:1100`.
- Reads ACLog XML messages and reacts to `CHANGEFREQ`.
- Converts ACLog MHz values such as `14.250` or `7.074` into integer Hz.
- Sends `F frequency_in_hz` to wfview's Hamlib rigctld server at `127.0.0.1:4533`.
- Queries `f` after each tune command and marks success only when the returned frequency is within 10 Hz.
- Polls wfview for frequency, mode, RF power, and read-only PTT/transmit state.
- Shows a persistent **ON AIR** indicator that lights red only when wfview positively reports transmit is active.
- Updates ACLog's band, mode, and frequency fields from wfview so ACLog tracks radio changes made in wfview.
- Optionally updates ACLog's log-entry Power field from wfview's read-only RF power level.
- Optionally monitors and controls a Shelly Gen 4 plug for station power.
- Provides a compact operator-friendly main window with configurable status panels and optional event log.
- Reconnects both TCP clients automatically after disconnects.
- Suppresses duplicate frequency commands received within 500 ms.
- Does not send transmit, PTT, or radio power commands.
- Stores settings locally under `%APPDATA%\W3NTBPowerBridge\settings.json`.
- Writes rolling logs under `%APPDATA%\W3NTBPowerBridge\Logs\app.log`.

## Build

Requirements:

- Windows 11
- Visual Studio 2022
- .NET 8 SDK with the Windows Desktop workload

Build from Visual Studio:

1. Open `W3NTBPowerBridge.sln`.
2. Restore NuGet packages.
3. Build the solution.
4. Run the `W3NTBPowerBridge` project.

Build from a terminal:

```powershell
dotnet restore
dotnet build
dotnet test
```

## Install from release package

For normal testing, use the installer ZIP from `artifacts/installer/W3NTBPowerBridge-0.1.0-beta-Installer.zip`.

1. Extract the ZIP.
2. Run `install.cmd`.
3. W3NTB Power Bridge installs for the current Windows user under `%LOCALAPPDATA%\Programs\W3NTB Power Bridge`.
4. The installer creates Start Menu and Desktop shortcuts.
5. Uninstall from Windows **Installed apps** or the Start Menu uninstall shortcut.

The installer does not require administrator rights and includes the .NET runtime inside the app build.

## Setup

1. Start N3FJP Amateur Contact Log and enable its TCP API on `127.0.0.1` port `1100`.
2. Start wfview with its Hamlib rigctld server listening on `127.0.0.1` port `4533`.
3. Launch W3NTB Power Bridge.
4. Use **Settings** to confirm host, port, and optional executable paths.
5. Click **Connect All**.

The app can also launch wfview and ACLog if you configure the executable paths. A sample configuration is available at `samples/settings.sample.json`.

## Tailscale and remote access notes

Tailscale is optional. You do not need it when ACLog, wfview, W3NTB Power Bridge, and the radio are all being used from the same desktop.

For remote station use, Tailscale can provide a private network path back to the shack computer. In that arrangement:

- Install and configure Tailscale separately on the computers you use for remote access.
- Run wfview on the shack computer as the radio server connected to the radio.
- Run wfview, ACLog, and W3NTB Power Bridge on the remote operating computer.
- Leave the ACLog and wfview settings in W3NTB Power Bridge pointed at `127.0.0.1` when ACLog and the remote wfview client are running on that same remote computer.
- Use Tailscale to carry wfview's network radio connection back to the shack computer, not as a replacement for ACLog's TCP API or wfview's local rigctld service.
- If you use a Shelly plug over Tailscale, set the Shelly host to its Tailscale-reachable name or address in **Settings**.

Remote station operation has safety implications. Confirm that your station power, transmit control, and local radio setup are safe before relying on remote access.

See [Remote Access and Tailscale Notes](docs/REMOTE_ACCESS.md) for a more complete Tailscale/Shelly setup checklist.

## Shelly station power beta

Shelly station power control is optional and currently beta because it has not yet been verified against the actual plug in station use. Configure the Shelly host or Tailscale-reachable name in **Settings**, enable the Shelly integration, and leave the switch ID at `0` for a Shelly Plug US Gen4.

The app uses Shelly's local HTTP RPC API:

- `Switch.GetStatus?id=0` for relay state, watts, voltage, current, and line frequency.
- `Switch.Set?id=0&on=true` to turn station power on.
- `Switch.Set?id=0&on=false` to turn station power off.

Power-off requires confirmation. After sending the off command, the app checks the Shelly readings for up to 10 seconds and reports station-off confirmation when the relay is off or measured power is below the configured watt threshold.

The **Start Station** button can power on the Shelly-controlled supply, wait for the configured delay, launch wfview and ACLog, and connect the bridge services. This sequence can also run automatically when the app starts.

## Compact display

Use **Settings** to choose which main-screen panels are visible. The event log can be hidden, and the Shelly station power section only appears when Shelly integration is enabled. The selected icon concept is `assets/icons/power-ham-w3.svg`.

Dark mode is available for night operating and uses a black background with red text.

## ACLog power field sync

The app can optionally copy wfview's read-only RF power level into ACLog's log-entry **Power** field. wfview reports RF power as a percentage, so W3NTB Power Bridge converts it to watts using the **Radio max watts** setting. For an IC-7300, leave this at `100`.

This only updates the value ACLog will log with the QSO. It does not send radio power-control commands or change the transmitter power setting.

## Tests

The test project covers:

- `CHANGEFREQ` XML parsing
- `READBMFRESPONSE` startup sync parsing
- MHz-to-Hz conversion
- duplicate command suppression
- Hamlib frequency response parsing
- Hamlib PTT response parsing

## Reporting issues and requesting features

Use the GitHub **Issues** tab to report problems or suggest improvements. Choose **Bug report** for something that is not working correctly, or **Feature request** for a new idea.

For bug reports, please include:

- What you expected to happen.
- What actually happened.
- Steps to reproduce the problem.
- The app version, such as `0.1.0-beta`.
- Whether the issue involves ACLog, wfview, the ON AIR indicator, Shelly power control, startup, or the installer.
- Relevant event log lines from the app, if available.

Logs are written under:

```text
%APPDATA%\W3NTBPowerBridge\Logs\app.log
```

Before posting logs, remove anything you do not want public, such as private hostnames, Tailscale device names, IP addresses, or personal station details.

For station-control issues, it also helps to include:

- Windows version.
- N3FJP Amateur Contact Log version.
- wfview version.
- Radio model.
- Whether ACLog TCP API is enabled on `127.0.0.1:1100`.
- Whether wfview rigctld is enabled on `127.0.0.1:4533`.
- Whether Shelly power control is enabled. Shelly support is beta, so extra detail is especially useful there.

## Current proof-of-concept scope

This version focuses on the ACLog-to-wfview bridge, station status, launching companion apps, settings, and logging. Shelly-controlled station power is included as a beta optional integration.

## License

W3NTB Power Bridge is released under the MIT License. See [LICENSE](LICENSE).
