using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace W3NTBPowerBridge.ViewModels;

/// <summary>
/// Base class for view models that notify the user interface when properties change.
/// </summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// Sets a property and raises change notification when the value changes.
    /// </summary>
    /// <typeparam name="T">Property type.</typeparam>
    /// <param name="field">Backing field.</param>
    /// <param name="value">New value.</param>
    /// <param name="propertyName">Caller-provided property name.</param>
    /// <returns>True when the value changed.</returns>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    /// <summary>
    /// Raises property change notification.
    /// </summary>
    /// <param name="propertyName">Name of the changed property.</param>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
