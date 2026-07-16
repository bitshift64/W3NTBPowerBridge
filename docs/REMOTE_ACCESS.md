# Remote Access and Tailscale Notes

W3NTB Power Bridge can be useful whether you operate from the radio desk or from another room, laptop, or remote location.

## Common Local Setup

For a single Windows desktop operating position:

```text
IC-7300
  |
  | USB: CAT control and audio
  v
Windows desktop
  |
  +-- wfview for radio control and spectrum/scope
  +-- ACLog for logging and DX cluster
  +-- W3NTB Power Bridge to keep wfview and ACLog synchronized
```

In this setup, W3NTB Power Bridge normally connects to:

```text
ACLog API:        127.0.0.1:1100
wfview rigctld:   127.0.0.1:4533
```

This is useful even when you are not operating remotely. wfview can provide the radio controls and scope, while ACLog remains the logging program. W3NTB Power Bridge keeps frequency and mode changes moving between them.

## Common Remote Setup

For remote operation with wfview network radio access, the radio-facing software runs on the shack computer and the operating/logging software runs on the remote computer:

```text
IC-7300
  |
  | USB / CI-V
  v
Shack Windows desktop
  |
  +-- wfview radio server
      |
      | Tailscale / private network
      v
Remote Windows computer
  |
  +-- wfview client
  +-- ACLog
  +-- W3NTB Power Bridge
```

In this arrangement, W3NTB Power Bridge runs on the remote operating computer. It talks to ACLog and the remote wfview client locally using `127.0.0.1`. The remote wfview client is responsible for talking back to the shack computer's wfview radio server over Tailscale or another private network path.

You do not need to expose ACLog, wfview rigctld, or W3NTB Power Bridge directly to the public Internet. ACLog can stay on the remote operating computer, which is often where the operator wants the logging screen and DX cluster anyway.

## wfview and wfweb

wfview is the proven desktop option for radio control, audio, and spectrum/scope display. In the remote pattern above, the shack computer runs wfview's radio server and the remote computer runs the wfview client. The remote wfview client can also provide the local rigctld endpoint that W3NTB Power Bridge uses.

wfweb may be useful for browser-based operation from a laptop or phone, depending on your station setup. If you use wfweb or any browser-based radio interface, configure it separately and keep it behind Tailscale or another private access method.

W3NTB Power Bridge does not replace wfview, wfweb, ACLog, or Tailscale. It is the small bridge/control panel that keeps station status and logging/radio-control data synchronized.

## Shelly Over Tailscale

Shelly station power support is beta. Test it carefully before relying on it for unattended or remote station power control.

If W3NTB Power Bridge runs on the same LAN as the Shelly plug, you can usually point the Shelly host setting directly at the Shelly's LAN IP address or local hostname.

If W3NTB Power Bridge is running on the remote operating computer and needs to reach a Shelly plug on the shack LAN, one good approach is to make the always-on shack desktop a Tailscale subnet router for the shack LAN.

Example layout:

```text
Laptop
  |
  | Tailscale
  v
Shack Windows desktop
  |
  | Shack LAN: 192.168.1.0/24
  v
Shelly Plug: 192.168.1.75
```

## Shelly Subnet Router Checklist

1. Reserve a stable IP address for the Shelly plug in your router.

Example:

```text
192.168.1.75
```

2. Keep the Shelly local web UI and local RPC API enabled.

3. On the shack Windows desktop, open an Administrator PowerShell and advertise the LAN route:

```powershell
tailscale up --advertise-routes=192.168.1.0/24
```

Replace `192.168.1.0/24` with your actual shack LAN subnet.

4. In the Tailscale admin console, approve the advertised route for the shack desktop.

5. From the remote laptop, test access:

```powershell
ping 192.168.1.75
```

Then open:

```text
http://192.168.1.75
```

6. Test the Shelly RPC API:

```powershell
Invoke-RestMethod "http://192.168.1.75/rpc/Switch.GetStatus?id=0"
```

Turn on:

```powershell
Invoke-RestMethod "http://192.168.1.75/rpc/Switch.Set?id=0&on=true"
```

Turn off:

```powershell
Invoke-RestMethod "http://192.168.1.75/rpc/Switch.Set?id=0&on=false"
```

7. In W3NTB Power Bridge settings, set the Shelly host to the reachable Shelly address, such as:

```text
192.168.1.75
```

## Safety Notes

- Do not port-forward ACLog, wfview, wfweb, Shelly, or rigctld directly to the public Internet.
- Prefer Tailscale or another private VPN-style path.
- Confirm your radio, power supply, antenna, and station layout are safe for remote operation.
- Test transmit only into a dummy load or at low power until the full audio/control chain is understood.
- Shelly power control should be treated as beta until it has been tested with the actual station power supply.
- W3NTB Power Bridge does not send PTT or transmit commands. The ON AIR light is read-only status reported through wfview/Hamlib.
