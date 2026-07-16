using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace W3NTBPowerBridge;

/// <summary>
/// Application entry point for W3NTB Power Bridge.
/// </summary>
public partial class App : Application
{
    /// <inheritdoc />
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;
            mainWindow.Show();
        }
        catch (Exception exception)
        {
            WriteStartupError(exception);
            MessageBox.Show(exception.ToString(), "W3NTB Power Bridge startup error", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
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
