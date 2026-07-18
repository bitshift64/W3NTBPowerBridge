namespace W3NTBPowerBridge.Models;

/// <summary>
/// User-configurable application settings saved under the user's AppData folder.
/// </summary>
public sealed class AppSettings
{
    /// <summary>
    /// Gets or sets the ACLog TCP API host name or IP address.
    /// </summary>
    public string AcLogHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the ACLog TCP API port.
    /// </summary>
    public int AcLogPort { get; set; } = 1100;

    /// <summary>
    /// Gets or sets the wfview rigctld host name or IP address.
    /// </summary>
    public string WfviewHost { get; set; } = "127.0.0.1";

    /// <summary>
    /// Gets or sets the wfview rigctld port.
    /// </summary>
    public int WfviewPort { get; set; } = 4533;

    /// <summary>
    /// Gets or sets the optional path to wfview.exe.
    /// </summary>
    public string WfviewPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional path to ACLog.exe.
    /// </summary>
    public string AcLogPath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the shack-side computer IP address or host name used for restarting wfview server.
    /// </summary>
    public string WfviewServerHost { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the expected wfview.exe path on the shack-side computer.
    /// </summary>
    public string WfviewServerPath { get; set; } = @"C:\Program Files\wfview\wfview.exe";

    /// <summary>
    /// Gets or sets a value indicating whether both TCP services should connect on startup.
    /// </summary>
    public bool AutoConnectOnStartup { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the full station startup sequence should run when the application starts.
    /// </summary>
    public bool AutoStartStationOnStartup { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether wfview should launch when the application starts.
    /// </summary>
    public bool AutoLaunchWfview { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether ACLog should launch when the application starts.
    /// </summary>
    public bool AutoLaunchAcLog { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the full station startup sequence should launch the shack-side wfview server.
    /// </summary>
    public bool LaunchWfviewServerDuringStartStation { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether read-only RF power from wfview should update ACLog's power field.
    /// </summary>
    public bool SyncRfPowerToAcLog { get; set; }

    /// <summary>
    /// Gets or sets the radio's maximum RF power in watts for converting wfview RF power percent to ACLog watts.
    /// </summary>
    public double RadioMaxPowerWatts { get; set; } = 100.0;

    /// <summary>
    /// Gets or sets a value indicating whether Shelly station power monitoring is enabled.
    /// </summary>
    public bool ShellyEnabled { get; set; }

    /// <summary>
    /// Gets or sets the Shelly device host name or IP address.
    /// </summary>
    public string ShellyHost { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Shelly HTTP port.
    /// </summary>
    public int ShellyPort { get; set; } = 80;

    /// <summary>
    /// Gets or sets the Shelly switch component ID.
    /// </summary>
    public int ShellySwitchId { get; set; }

    /// <summary>
    /// Gets or sets the wattage below which the station is considered powered off.
    /// </summary>
    public double StationOffPowerThresholdWatts { get; set; } = 5.0;

    /// <summary>
    /// Gets or sets the delay after station power-on before launching apps and connecting services.
    /// </summary>
    public int StationPowerOnDelaySeconds { get; set; } = 8;

    /// <summary>
    /// Gets or sets the maximum time the shack computer waits for USB audio devices before launching wfview.
    /// </summary>
    public int WfviewServerAudioWaitSeconds { get; set; } = 45;

    /// <summary>
    /// Gets or sets the delay after the shack-side wfview server launch confirms before local apps are opened.
    /// </summary>
    public int WfviewServerSettleDelaySeconds { get; set; } = 8;

    /// <summary>
    /// Gets or sets the delay after launching local wfview before launching ACLog.
    /// </summary>
    public int AcLogLaunchDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Gets or sets a value indicating whether the event log is shown on the main window.
    /// </summary>
    public bool ShowEventLog { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether ACLog status is shown.
    /// </summary>
    public bool ShowAcLogStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether wfview status is shown.
    /// </summary>
    public bool ShowWfviewStatus { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether radio frequency is shown.
    /// </summary>
    public bool ShowRadioFrequency { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether radio mode and RF power are shown.
    /// </summary>
    public bool ShowModeAndRfPower { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether last requested frequency is shown.
    /// </summary>
    public bool ShowLastRequestedFrequency { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether last confirmed frequency is shown.
    /// </summary>
    public bool ShowLastConfirmedFrequency { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether tune result is shown.
    /// </summary>
    public bool ShowTuneResult { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the Shelly power panel starts expanded.
    /// </summary>
    public bool ExpandPowerPanel { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether dark mode is enabled.
    /// </summary>
    public bool DarkModeEnabled { get; set; }

    /// <summary>
    /// Gets or sets the saved main window left coordinate.
    /// </summary>
    public double? MainWindowLeft { get; set; }

    /// <summary>
    /// Gets or sets the saved main window top coordinate.
    /// </summary>
    public double? MainWindowTop { get; set; }

    /// <summary>
    /// Gets or sets the saved main window width.
    /// </summary>
    public double? MainWindowWidth { get; set; }

    /// <summary>
    /// Gets or sets the saved main window height.
    /// </summary>
    public double? MainWindowHeight { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the main window was maximized.
    /// </summary>
    public bool MainWindowMaximized { get; set; }
}
