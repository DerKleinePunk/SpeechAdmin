using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SpeechAdmin.ViewModels
{
    /// <summary>
    /// Simple ICommand implementation for synchronous actions
    /// </summary>
    public class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
    {
        private readonly Action _execute = execute ?? throw new ArgumentNullException(nameof(execute));

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }

        public bool CanExecute(object? parameter)
        {
            return canExecute?.Invoke() ?? true;
        }
    }

    /// <summary>
    /// ICommand implementation for asynchronous actions
    /// </summary>
    public class RelayAsyncCommand(Func<Task> execute, Func<bool>? canExecute = null) : ICommand
    {
        private readonly Func<Task> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public async void Execute(object? parameter)
        {
            try
            {
                if (!CanExecute(parameter))
                    return;

                _isExecuting = true;
                try
                {
                    await _execute();
                }
                finally
                {
                    _isExecuting = false;
                    CommandManager.InvalidateRequerySuggested();
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine($"Error executing command: {e.Message}");
            }
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (canExecute?.Invoke() ?? true);
        }
    }
}