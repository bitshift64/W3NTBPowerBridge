using System.Windows;
using W3NTBPowerBridge.ViewModels;

namespace W3NTBPowerBridge;

/// <summary>
/// Main window for station status, controls, and the live event log.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Creates the main application window.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        SourceInitialized += OnSourceInitialized;
        Loaded += OnLoaded;
        Closing += OnClosing;
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ApplyWindowFrame(this);
        }
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.LoadSettingsAsync();
            viewModel.RestoreWindowPlacement(this);
            await viewModel.InitializeAsync();
        }
    }

    private async void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            await viewModel.SaveWindowPlacementAsync(this);
            await viewModel.ShutdownAsync();
        }
    }
}
