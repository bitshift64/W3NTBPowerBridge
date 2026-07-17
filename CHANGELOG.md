# Changelog

## 0.1.1-beta

- Adds optional RF power sync from wfview into ACLog's Power field using a configurable max-watts scale.
- Adds Launch Server support for restarting shack-side wfview through SSH and a logged-in-user Scheduled Task.
- Simplifies server restart setup to a server IP/host and standard wfview path instead of free-form commands.
- Improves logging for remote wfview server restart results.
- Adds explicit ACLog `READBMFRESPONSE` startup sync parsing.

## 0.1.0-beta

- Renamed the app and solution to W3NTB Power Bridge.
- Added W3NTB Power Bridge application icon.
- Bridges N3FJP Amateur Contact Log and wfview rigctld.
- Supports ACLog DX cluster frequency changes through ACLog field updates.
- Syncs frequency and mode changes from wfview back to ACLog.
- Shows connected status for ACLog and wfview.
- Displays radio frequency, requested/confirmed frequency, mode, RF power, tune result, and read-only ON AIR status.
- Adds compact display options and dark mode with red text.
- Adds optional Shelly Gen 4 station power control as beta.
- Adds a self-contained Windows x64 installer ZIP.
