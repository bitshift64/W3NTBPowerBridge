using System.Windows;
using W3NTBPowerBridge.Models;

namespace W3NTBPowerBridge.Views;

/// <summary>
/// Dialog window for editing local application settings.
/// </summary>
public partial class SettingsWindow : Window
{
    /// <summary>
    /// Creates the settings window with a copy of the current settings.
    /// </summary>
    /// <param name="settings">Current settings.</param>
    public SettingsWindow(AppSettings settings)
    {
        Settings = new AppSettings
        {
            AcLogHost = settings.AcLogHost,
            AcLogPort = settings.AcLogPort,
            WfviewHost = settings.WfviewHost,
            WfviewPort = settings.WfviewPort,
            WfviewPath = settings.WfviewPath,
            AcLogPath = settings.AcLogPath,
            AutoConnectOnStartup = settings.AutoConnectOnStartup,
            AutoStartStationOnStartup = settings.AutoStartStationOnStartup,
            AutoLaunchWfview = settings.AutoLaunchWfview,
            AutoLaunchAcLog = settings.AutoLaunchAcLog,
            SyncRfPowerToAcLog = settings.SyncRfPowerToAcLog,
            RadioMaxPowerWatts = settings.RadioMaxPowerWatts,
            ShellyEnabled = settings.ShellyEnabled,
            ShellyHost = settings.ShellyHost,
            ShellyPort = settings.ShellyPort,
            ShellySwitchId = settings.ShellySwitchId,
            StationOffPowerThresholdWatts = settings.StationOffPowerThresholdWatts,
            StationPowerOnDelaySeconds = settings.StationPowerOnDelaySeconds,
            ShowEventLog = settings.ShowEventLog,
            ShowAcLogStatus = settings.ShowAcLogStatus,
            ShowWfviewStatus = settings.ShowWfviewStatus,
            ShowRadioFrequency = settings.ShowRadioFrequency,
            ShowModeAndRfPower = settings.ShowModeAndRfPower,
            ShowLastRequestedFrequency = settings.ShowLastRequestedFrequency,
            ShowLastConfirmedFrequency = settings.ShowLastConfirmedFrequency,
            ShowTuneResult = settings.ShowTuneResult,
            ExpandPowerPanel = settings.ExpandPowerPanel,
            DarkModeEnabled = settings.DarkModeEnabled
        };

        InitializeComponent();
        DataContext = Settings;
    }

    /// <summary>
    /// Gets the settings edited by the user.
    /// </summary>
    public AppSettings Settings { get; }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
