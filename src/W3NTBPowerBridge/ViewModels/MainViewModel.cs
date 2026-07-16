using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using W3NTBPowerBridge.Models;
using W3NTBPowerBridge.Services;
using W3NTBPowerBridge.Views;

namespace W3NTBPowerBridge.ViewModels;

/// <summary>
/// Main window view model that coordinates station services and status display.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly IAppLogger _logger;
    private readonly ISettingsService _settingsService;
    private readonly IProcessLauncher _processLauncher;
    private readonly AcLogClientService _acLogClient;
    private readonly RigctldClientService _rigClient;
    private readonly ShellyPowerService _shellyPowerService;
    private readonly DuplicateCommandSuppressor _duplicateSuppressor = new(TimeSpan.FromMilliseconds(500));
    private AppSettings _settings = new();
    private ServiceConnectionState _acLogStatus = ServiceConnectionState.Disconnected;
    private ServiceConnectionState _rigStatus = ServiceConnectionState.Disconnected;
    private long? _radioFrequencyHz;
    private string? _radioMode;
    private double? _txPowerPercent;
    private bool? _isTransmitting;
    private long? _lastRequestedFrequencyHz;
    private long? _lastConfirmedFrequencyHz;
    private long? _lastAcLogSyncedFrequencyHz;
    private string? _lastAcLogSyncedMode;
    private string? _pendingAcLogMode;
    private DateTimeOffset _suppressAcLogSyncUntil = DateTimeOffset.MinValue;
    private string _lastTuneResult = "No tune request yet.";
    private ShellyPowerStatus _shellyStatus = ShellyPowerStatus.Disabled;
    private string _stationStartupStatus = "Station startup has not run.";

    /// <summary>
    /// Creates the main window view model.
    /// </summary>
    public MainViewModel()
    {
        _logger = new RollingFileLogger();
        _settingsService = new JsonSettingsService();
        _processLauncher = new ProcessLauncher(_logger);
        _acLogClient = new AcLogClientService(_logger);
        _rigClient = new RigctldClientService(_logger);
        _shellyPowerService = new ShellyPowerService(_logger);

        _logger.LineWritten += (_, line) => Application.Current.Dispatcher.Invoke(() => AddLogLine(line));
        _acLogClient.StatusChanged += (_, state) => Application.Current.Dispatcher.Invoke(() => AcLogStatus = state);
        _rigClient.StatusChanged += (_, state) => Application.Current.Dispatcher.Invoke(() => RigStatus = state);
        _rigClient.RadioStateChanged += OnRadioStateChanged;
        _acLogClient.FrequencyRequested += OnFrequencyRequested;
        _acLogClient.ModeRequested += OnModeRequested;

        StartStationCommand = new RelayCommand(_ => StartStationAsync());
        ConnectAllCommand = new RelayCommand(_ => ConnectAllAsync());
        DisconnectAllCommand = new RelayCommand(_ => DisconnectAllAsync());
        RefreshShellyCommand = new RelayCommand(_ => RefreshShellyAsync());
        PowerOnCommand = new RelayCommand(_ => SetStationPowerAsync(true));
        PowerOffCommand = new RelayCommand(_ => SetStationPowerAsync(false));
        OpenWfviewCommand = new RelayCommand(_ => OpenWfviewAsync());
        OpenAcLogCommand = new RelayCommand(_ => OpenAcLogAsync());
        ClearLogCommand = new RelayCommand(_ => ClearLogAsync());
        OpenSettingsCommand = new RelayCommand(_ => OpenSettingsAsync());
    }

    /// <summary>
    /// Gets the ACLog connection status.
    /// </summary>
    public ServiceConnectionState AcLogStatus
    {
        get => _acLogStatus;
        private set => SetProperty(ref _acLogStatus, value);
    }

    /// <summary>
    /// Gets the wfview rigctld connection status.
    /// </summary>
    public ServiceConnectionState RigStatus
    {
        get => _rigStatus;
        private set => SetProperty(ref _rigStatus, value);
    }

    /// <summary>
    /// Gets the radio frequency display text.
    /// </summary>
    public string RadioFrequencyDisplay => FrequencyConverter.FormatHzAsMhz(_radioFrequencyHz);

    /// <summary>
    /// Gets the current mode and RF power display text.
    /// </summary>
    public string ModeAndPowerDisplay
    {
        get
        {
            var mode = string.IsNullOrWhiteSpace(_radioMode) ? "Mode unknown" : _radioMode;
            var power = _txPowerPercent.HasValue ? $"{_txPowerPercent:0.#}% RF" : "Power unknown";
            return $"{mode} / {power}";
        }
    }

    /// <summary>
    /// Gets whether wfview positively reports PTT/transmit is active.
    /// </summary>
    public bool IsOnAir => _isTransmitting == true;

    /// <summary>
    /// Gets the last requested frequency display text.
    /// </summary>
    public string LastRequestedFrequencyDisplay => FrequencyConverter.FormatHzAsMhz(_lastRequestedFrequencyHz);

    /// <summary>
    /// Gets the last confirmed frequency display text.
    /// </summary>
    public string LastConfirmedFrequencyDisplay => FrequencyConverter.FormatHzAsMhz(_lastConfirmedFrequencyHz);

    /// <summary>
    /// Gets the last tune result display text.
    /// </summary>
    public string LastTuneResult
    {
        get => _lastTuneResult;
        private set => SetProperty(ref _lastTuneResult, value);
    }

    /// <summary>
    /// Gets the station startup workflow status.
    /// </summary>
    public string StationStartupStatus
    {
        get => _stationStartupStatus;
        private set => SetProperty(ref _stationStartupStatus, value);
    }

    /// <summary>
    /// Gets the Shelly status indicator state.
    /// </summary>
    public ServiceConnectionState ShellyStatusIndicator
    {
        get
        {
            if (!_shellyStatus.IsEnabled)
            {
                return ServiceConnectionState.Disconnected;
            }

            return _shellyStatus.IsReachable ? ServiceConnectionState.Connected : ServiceConnectionState.Waiting;
        }
    }

    /// <summary>
    /// Gets the station power state display.
    /// </summary>
    public string ShellyPowerStateDisplay
    {
        get
        {
            if (!_shellyStatus.IsEnabled)
            {
                return "Disabled";
            }

            if (!_shellyStatus.IsReachable)
            {
                return "Not reachable";
            }

            return _shellyStatus.OutputOn == true ? "On" : "Off";
        }
    }

    /// <summary>
    /// Gets the Shelly power usage display.
    /// </summary>
    public string ShellyPowerUsageDisplay => _shellyStatus.PowerWatts.HasValue ? $"{_shellyStatus.PowerWatts:0.0} W" : "Not available";

    /// <summary>
    /// Gets the Shelly voltage/current display.
    /// </summary>
    public string ShellyElectricalDisplay
    {
        get
        {
            var voltage = _shellyStatus.Voltage.HasValue ? $"{_shellyStatus.Voltage:0.0} V" : "No voltage";
            var current = _shellyStatus.CurrentAmps.HasValue ? $"{_shellyStatus.CurrentAmps:0.00} A" : "No current";
            return $"{voltage} / {current}";
        }
    }

    /// <summary>
    /// Gets the Shelly station-off confirmation display.
    /// </summary>
    public string ShellyConfirmationDisplay => _shellyStatus.Message;

    /// <summary>
    /// Gets visibility for Shelly station power controls.
    /// </summary>
    public Visibility ShellyControlsVisibility => _settings.ShellyEnabled ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets visibility for the event log.
    /// </summary>
    public Visibility EventLogVisibility => _settings.ShowEventLog ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets visibility for ACLog status.
    /// </summary>
    public Visibility AcLogStatusVisibility => _settings.ShowAcLogStatus ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets visibility for wfview status.
    /// </summary>
    public Visibility WfviewStatusVisibility => _settings.ShowWfviewStatus ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets visibility for radio frequency.
    /// </summary>
    public Visibility RadioFrequencyVisibility => _settings.ShowRadioFrequency ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets visibility for mode and RF power.
    /// </summary>
    public Visibility ModeAndPowerVisibility => _settings.ShowModeAndRfPower ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets visibility for last requested frequency.
    /// </summary>
    public Visibility LastRequestedVisibility => _settings.ShowLastRequestedFrequency ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets visibility for last confirmed frequency.
    /// </summary>
    public Visibility LastConfirmedVisibility => _settings.ShowLastConfirmedFrequency ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets visibility for the tune result.
    /// </summary>
    public Visibility TuneResultVisibility => _settings.ShowTuneResult ? Visibility.Visible : Visibility.Collapsed;

    /// <summary>
    /// Gets whether the power panel starts expanded.
    /// </summary>
    public bool IsPowerPanelExpanded => _settings.ExpandPowerPanel;

    /// <summary>
    /// Gets the scrolling event log lines.
    /// </summary>
    public ObservableCollection<string> EventLog { get; } = new();

    /// <summary>
    /// Gets the command that powers on the station, launches apps, and connects services.
    /// </summary>
    public ICommand StartStationCommand { get; }

    /// <summary>
    /// Gets the command that starts both TCP connections.
    /// </summary>
    public ICommand ConnectAllCommand { get; }

    /// <summary>
    /// Gets the command that stops both TCP connections.
    /// </summary>
    public ICommand DisconnectAllCommand { get; }

    /// <summary>
    /// Gets the command that refreshes Shelly station power status.
    /// </summary>
    public ICommand RefreshShellyCommand { get; }

    /// <summary>
    /// Gets the command that turns station power on.
    /// </summary>
    public ICommand PowerOnCommand { get; }

    /// <summary>
    /// Gets the command that turns station power off.
    /// </summary>
    public ICommand PowerOffCommand { get; }

    /// <summary>
    /// Gets the command that launches wfview.
    /// </summary>
    public ICommand OpenWfviewCommand { get; }

    /// <summary>
    /// Gets the command that launches ACLog.
    /// </summary>
    public ICommand OpenAcLogCommand { get; }

    /// <summary>
    /// Gets the command that clears the visible event log.
    /// </summary>
    public ICommand ClearLogCommand { get; }

    /// <summary>
    /// Gets the command that opens settings.
    /// </summary>
    public ICommand OpenSettingsCommand { get; }

    /// <summary>
    /// Initializes settings and optional startup actions.
    /// </summary>
    /// <returns>A task representing initialization.</returns>
    public async Task InitializeAsync()
    {
        _settings = await _settingsService.LoadAsync().ConfigureAwait(true);
        ThemeService.Apply(_settings.DarkModeEnabled);
        _logger.Info("Application started.");

        if (_settings.AutoStartStationOnStartup)
        {
            await StartStationAsync().ConfigureAwait(true);
        }
        else
        {
            if (_settings.AutoLaunchWfview)
            {
                _processLauncher.LaunchWfview(_settings);
            }

            if (_settings.AutoLaunchAcLog)
            {
                _processLauncher.LaunchAcLog(_settings);
            }

            if (_settings.AutoConnectOnStartup)
            {
                await ConnectAllAsync().ConfigureAwait(true);
            }
        }

        await RefreshShellyAsync().ConfigureAwait(true);
    }

    /// <summary>
    /// Stops background services before the application exits.
    /// </summary>
    /// <returns>A completed task.</returns>
    public Task ShutdownAsync()
    {
        _acLogClient.Stop();
        _rigClient.Stop();
        _logger.Info("Application stopped.");
        return Task.CompletedTask;
    }

    private Task ConnectAllAsync()
    {
        _logger.Info("Connecting to ACLog and wfview.");
        _acLogClient.Start(_settings);
        _rigClient.Start(_settings);
        return Task.CompletedTask;
    }

    private async Task StartStationAsync()
    {
        StationStartupStatus = "Starting station...";
        _logger.Info("Starting full station sequence.");

        if (_settings.ShellyEnabled)
        {
            StationStartupStatus = "Turning station power on.";
            var status = await _shellyPowerService.SetPowerAsync(_settings, true).ConfigureAwait(false);
            await Application.Current.Dispatcher.InvokeAsync(() => ApplyShellyStatus(status));

            var delaySeconds = Math.Clamp(_settings.StationPowerOnDelaySeconds, 0, 60);
            if (delaySeconds > 0)
            {
                StationStartupStatus = $"Waiting {delaySeconds} seconds for station power to settle.";
                _logger.Info($"Waiting {delaySeconds} seconds after station power on.");
                await Task.Delay(TimeSpan.FromSeconds(delaySeconds)).ConfigureAwait(false);
            }
        }
        else
        {
            _logger.Info("Shelly station power integration is disabled; skipping power-on step.");
        }

        StationStartupStatus = "Launching station apps.";
        _processLauncher.LaunchWfview(_settings);
        _processLauncher.LaunchAcLog(_settings);

        StationStartupStatus = "Connecting station services.";
        await ConnectAllAsync().ConfigureAwait(false);
        await RefreshShellyAsync().ConfigureAwait(false);

        StationStartupStatus = "Station startup sequence complete.";
        _logger.Info("Full station sequence complete.");
    }

    private Task DisconnectAllAsync()
    {
        _logger.Info("Disconnecting from ACLog and wfview.");
        _acLogClient.Stop();
        _rigClient.Stop();
        return Task.CompletedTask;
    }

    private Task OpenWfviewAsync()
    {
        _processLauncher.LaunchWfview(_settings);
        return Task.CompletedTask;
    }

    private async Task RefreshShellyAsync()
    {
        var status = await _shellyPowerService.GetStatusAsync(_settings).ConfigureAwait(false);
        await Application.Current.Dispatcher.InvokeAsync(() => ApplyShellyStatus(status));
    }

    private async Task SetStationPowerAsync(bool turnOn)
    {
        if (!_settings.ShellyEnabled)
        {
            _logger.Info("Shelly station power integration is disabled.");
            return;
        }

        if (!turnOn)
        {
            var result = MessageBox.Show(
                "Turn off the station power supply? This controls the 120 VAC feed to the radio power supply.",
                "Confirm Station Power Off",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }
        }

        var status = await _shellyPowerService.SetPowerAsync(_settings, turnOn).ConfigureAwait(false);
        await Application.Current.Dispatcher.InvokeAsync(() => ApplyShellyStatus(status));

        if (!turnOn)
        {
            await ConfirmStationOffAsync().ConfigureAwait(false);
        }
    }

    private async Task ConfirmStationOffAsync()
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            await Task.Delay(TimeSpan.FromSeconds(1)).ConfigureAwait(false);
            var status = await _shellyPowerService.GetStatusAsync(_settings).ConfigureAwait(false);
            await Application.Current.Dispatcher.InvokeAsync(() => ApplyShellyStatus(status));
            if (status.StationOffConfirmed)
            {
                _logger.Info($"Station off confirmed by Shelly: {status.PowerWatts?.ToString("0.0") ?? "unknown"} W.");
                return;
            }
        }

        _logger.Error("Station off was not confirmed by Shelly within 10 seconds.");
    }

    private Task OpenAcLogAsync()
    {
        _processLauncher.LaunchAcLog(_settings);
        return Task.CompletedTask;
    }

    private Task ClearLogAsync()
    {
        EventLog.Clear();
        return Task.CompletedTask;
    }

    private async Task OpenSettingsAsync()
    {
        var dialog = new SettingsWindow(_settings) { Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() == true)
        {
            _settings = dialog.Settings;
            await _settingsService.SaveAsync(_settings).ConfigureAwait(true);
            ThemeService.Apply(_settings.DarkModeEnabled);
            _logger.Info("Settings saved.");
            OnDisplaySettingsChanged();
        }
    }

    private async void OnFrequencyRequested(object? sender, FrequencyRequest request)
    {
        _suppressAcLogSyncUntil = DateTimeOffset.Now.AddSeconds(3);
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _lastRequestedFrequencyHz = request.FrequencyHz;
            OnPropertyChanged(nameof(LastRequestedFrequencyDisplay));
        });

        if (_duplicateSuppressor.ShouldSuppress(request.FrequencyHz, DateTimeOffset.Now))
        {
            _logger.Info($"Suppressed duplicate CHANGEFREQ request for {request.FrequencyHz} Hz.");
            return;
        }

        var requestedMode = request.Mode ?? _pendingAcLogMode;
        var rigMode = MapAcLogModeToRigMode(requestedMode, request.FrequencyHz);
        if (!string.IsNullOrWhiteSpace(rigMode))
        {
            await _rigClient.SetModeAsync(rigMode).ConfigureAwait(false);
        }

        var result = await _rigClient.TuneAsync(request.FrequencyHz).ConfigureAwait(false);
        await Application.Current.Dispatcher.InvokeAsync(() => ApplyTuneResult(result));
    }

    private void OnModeRequested(object? sender, string mode)
    {
        _pendingAcLogMode = mode;
        _logger.Info($"Remembered ACLog mode request for next frequency command: {mode}.");
    }

    private async void OnRadioStateChanged(object? sender, RadioState state)
    {
        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _radioFrequencyHz = state.FrequencyHz;
            _radioMode = state.Mode;
            _txPowerPercent = state.TxPowerPercent;
            _isTransmitting = state.IsTransmitting;
            OnPropertyChanged(nameof(RadioFrequencyDisplay));
            OnPropertyChanged(nameof(ModeAndPowerDisplay));
            OnPropertyChanged(nameof(IsOnAir));
        });

        if (state.FrequencyHz is not null
            && DateTimeOffset.Now > _suppressAcLogSyncUntil
            && ShouldSyncRadioStateToAcLog(state))
        {
            await _acLogClient.UpdateRadioStateAsync(state).ConfigureAwait(false);
            _lastAcLogSyncedFrequencyHz = state.FrequencyHz;
            _lastAcLogSyncedMode = state.Mode;
        }
    }

    private void ApplyTuneResult(TuneResult result)
    {
        _lastConfirmedFrequencyHz = result.ConfirmedHz;
        _radioFrequencyHz = result.ConfirmedHz;
        LastTuneResult = result.Succeeded ? "Successful" : $"Failed - {result.Message}";
        OnPropertyChanged(nameof(LastConfirmedFrequencyDisplay));
        OnPropertyChanged(nameof(RadioFrequencyDisplay));
        _logger.Info($"Tune result: requested {result.RequestedHz} Hz, confirmed {result.ConfirmedHz?.ToString() ?? "none"}, success={result.Succeeded}.");
    }

    private void ApplyShellyStatus(ShellyPowerStatus status)
    {
        _shellyStatus = status;
        OnPropertyChanged(nameof(ShellyStatusIndicator));
        OnPropertyChanged(nameof(ShellyPowerStateDisplay));
        OnPropertyChanged(nameof(ShellyPowerUsageDisplay));
        OnPropertyChanged(nameof(ShellyElectricalDisplay));
        OnPropertyChanged(nameof(ShellyConfirmationDisplay));
        OnPropertyChanged(nameof(ShellyControlsVisibility));
    }

    private void OnDisplaySettingsChanged()
    {
        OnPropertyChanged(nameof(ShellyControlsVisibility));
        OnPropertyChanged(nameof(EventLogVisibility));
        OnPropertyChanged(nameof(AcLogStatusVisibility));
        OnPropertyChanged(nameof(WfviewStatusVisibility));
        OnPropertyChanged(nameof(RadioFrequencyVisibility));
        OnPropertyChanged(nameof(ModeAndPowerVisibility));
        OnPropertyChanged(nameof(LastRequestedVisibility));
        OnPropertyChanged(nameof(LastConfirmedVisibility));
        OnPropertyChanged(nameof(TuneResultVisibility));
        OnPropertyChanged(nameof(IsPowerPanelExpanded));
    }

    private bool ShouldSyncRadioStateToAcLog(RadioState state)
    {
        return state.FrequencyHz != _lastAcLogSyncedFrequencyHz
            || !string.Equals(state.Mode, _lastAcLogSyncedMode, StringComparison.OrdinalIgnoreCase);
    }

    private static string MapAcLogModeToRigMode(string? mode, long frequencyHz)
    {
        return mode?.ToUpperInvariant() switch
        {
            "PH" or "SSB" => frequencyHz < 10_000_000 ? "LSB" : "USB",
            "PHONE" => frequencyHz < 10_000_000 ? "LSB" : "USB",
            "DIG" => frequencyHz < 10_000_000 ? "LSB" : "USB",
            "FT8" => frequencyHz < 10_000_000 ? "LSB" : "USB",
            "CW" => "CW",
            "RTTY" => "RTTY",
            "AM" => "AM",
            "FM" => "FM",
            "USB" => "USB",
            "LSB" => "LSB",
            _ => string.Empty
        };
    }

    private void AddLogLine(string line)
    {
        EventLog.Add(line);
        while (EventLog.Count > 500)
        {
            EventLog.RemoveAt(0);
        }
    }
}
