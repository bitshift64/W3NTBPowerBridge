# W3NTB Power Bridge 0.1.0-beta

First shareable beta release.

## Highlights

- Bridges N3FJP Amateur Contact Log to wfview using local TCP APIs.
- Sends ACLog frequency requests to wfview rigctld and confirms the result.
- Tracks radio frequency and mode changes from wfview back into ACLog.
- Displays read-only ON AIR status from wfview/Hamlib PTT state.
- Includes compact UI options and dark mode for operating use.
- Includes optional Shelly Gen 4 station power control as beta.
- Ships as a self-contained Windows x64 installer ZIP.
- Supports both local desktop operation and remote-station workflows where wfview network radio access and Tailscale are used to reach the shack computer.

## Important Notes

- Shelly station power control is beta and should be tested carefully before relying on it.
- The app does not send PTT, transmit, or radio power commands.
- No administrator rights are required for the installer ZIP.
- Tailscale is optional and is configured separately; W3NTB Power Bridge normally talks to ACLog and the local wfview client on `127.0.0.1`.
- Remote access and Shelly-over-Tailscale setup notes are documented in `docs/REMOTE_ACCESS.md`.
