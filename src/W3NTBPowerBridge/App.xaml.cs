using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace W3NTBPowerBridge;

/// <summary>
/// Application entry point for W3NTB Power Bridge.
/// </summary>
public partial class App : Application
{
    private Mutex? _singleInstanceMutex;

    /// <inheritdoc />
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            _singleInstanceMutex = new Mutex(true, "W3NTBPowerBridge.SingleInstance", out var createdNew);
            if (!createdNew)
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            MainWindow = mainWindow;

            if (mainWindow.DataContext is ViewModels.MainViewModel viewModel)
            {
                await viewModel.LoadSettingsAsync().ConfigureAwait(true);
                viewModel.RestoreWindowPlacement(mainWindow);
            }

            mainWindow.Show();
        }
        catch (Exception exception)
        {
            WriteStartupError(exception);
            MessageBox.Show(exception.ToString(), "W3NTB Power Bridge startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        WriteStartupError(e.Exception);
        MessageBox.Show(e.Exception.ToString(), "W3NTB Power Bridge error", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }

    private static void WriteStartupError(Exception exception)
    {
        try
        {
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "W3NTBPowerBridge", "Logs");
            Directory.CreateDirectory(directory);
            File.AppendAllText(Path.Combine(directory, "startup-error.log"), $"{DateTimeOffset.Now:O}{Environment.NewLine}{exception}{Environment.NewLine}{Environment.NewLine}");
        }
        catch
        {
            // Last-resort logger; avoid throwing while handling a startup failure.
        }
    }
}
