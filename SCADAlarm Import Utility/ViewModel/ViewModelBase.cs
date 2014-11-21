using System;
using System.ComponentModel;
using System.Windows.Input;

namespace SCADAlarm_Import_Utility.ViewModel
{
    /// <summary>
    /// A base class with generic view model support.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        public class RelayCommand : ICommand
        {
            private readonly Action _execute;
            private readonly Func<bool> _canExecute;

            public RelayCommand(Action execute, Func<bool> canExecute)
            {
                if (execute == null) throw new ArgumentNullException("execute");
                _execute = new Action(execute);
                if (canExecute != null) _canExecute = new Func<bool>(canExecute);
            }

            /// <summary>
            /// Initializes a new instance of the RelayCommand class that
            /// can always execute.
            /// </summary>
            /// <param name="execute">The execution logic.</param>
            /// <exception cref="ArgumentNullException">If the execute argument is null.</exception>
            public RelayCommand(Action execute) : this(execute, null) { }

            /// <summary>
            /// Occurs when changes occur that affect whether the command should execute.
            /// </summary>
            public event EventHandler CanExecuteChanged;

            public void RaiseCanExecuteChanged()
            {
                var handler = CanExecuteChanged;

                if (handler != null) handler(this, EventArgs.Empty);
            }

            public bool CanExecute(object parameter)
            {
                if (_canExecute == null) return true;
                return _canExecute.Invoke();
            }

            public virtual void Execute(object parameter)
            {
                if (CanExecute(parameter) && _execute != null) _execute.Invoke();
            }
        }

        public class RelayCommand<T> : ICommand
        {
            private readonly Action<T> _execute;
            private readonly Predicate<T> _canExecute;

            public RelayCommand(Action<T> execute) : this(execute, null) { }

            public RelayCommand(Action<T> execute, Predicate<T> canExecute)
            {
                if (execute == null) throw new ArgumentNullException("execute");
                _execute = execute;
                if (canExecute != null) _canExecute = canExecute;
            }

            /// <summary>
            /// Occurs when changes occur that affect whether the command should execute.
            /// </summary>
            public event EventHandler CanExecuteChanged;

            public void RaiseCanExecuteChanged()
            {
                var handler = CanExecuteChanged;
                if (handler != null) handler(this, EventArgs.Empty);
            }

            public bool CanExecute(object parameter)
            {
                if (_canExecute == null) return true;
                return _canExecute.Invoke((T)parameter);
            }

            public virtual void Execute(object parameter)
            {
                if (CanExecute(parameter) && _execute != null)
                {
                    _execute.Invoke((T)parameter);
                }
            }
        }

        /// <summary>
        /// Generic PropertyChanged method to inform the view when data has changed.
        /// </summary>
        /// <param name="name"></param>
        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;

            if (handler != null) handler(this, new PropertyChangedEventArgs(name));
        }
    }
}
