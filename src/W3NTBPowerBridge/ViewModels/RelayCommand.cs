using System.Windows.Input;

namespace W3NTBPowerBridge.ViewModels;

/// <summary>
/// Command implementation for binding user interface actions to view model methods.
/// </summary>
public sealed class RelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Predicate<object?>? _canExecute;
    private bool _isExecuting;

    /// <summary>
    /// Creates a relay command.
    /// </summary>
    /// <param name="execute">Action to execute.</param>
    /// <param name="canExecute">Optional can-execute predicate.</param>
    public RelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    /// <inheritdoc />
    public event EventHandler? CanExecuteChanged;

    /// <inheritdoc />
    public bool CanExecute(object? parameter)
    {
        return !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);
    }

    /// <inheritdoc />
    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
        {
            return;
        }

        try
        {
            _isExecuting = true;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
            await _execute(parameter).ConfigureAwait(true);
        }
        finally
        {
            _isExecuting = false;
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
