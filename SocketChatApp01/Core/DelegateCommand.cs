using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace SocketChatApp01.Core
{
    class DelegateCommand : ICommand
    {
        private Action _execute;
        private Func<bool> _canExecute;

        public event EventHandler? CanExecuteChanged;

        public DelegateCommand(Action execute) : this(execute, () => true) { }

        public DelegateCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute();

        public void Execute(object? parameter) => _execute();

        public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    class DelegateCommand<T> : ICommand
    {
        private Action<T> _execute;
        private Func<T, bool> _canExecute;

        public event EventHandler? CanExecuteChanged;

        public DelegateCommand(Action<T> execute) : this(execute, (T) => true) { }

        public DelegateCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is null)
            {
                return false;
            }

            return _canExecute((T)parameter);
        }

        public void Execute(object? parameter)
        {
            if (parameter is null)
            {
                return;
            }

            _execute((T)parameter);
        }

        public void OnCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
