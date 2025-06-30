using System;
using System.Threading.Tasks;
using System.Windows;

namespace Plexity.Helpers
{
    public static class UiThreadHelper
    {
        public static void SafeExecute(Action action)
        {
            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                action();
            }
            else
            {
                Application.Current?.Dispatcher.Invoke(action);
            }
        }
        
        public static Task SafeExecuteAsync(Action action)
        {
            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                action();
                return Task.CompletedTask;
            }
            else
            {
                return Application.Current?.Dispatcher.InvokeAsync(action).Task ?? Task.CompletedTask;
            }
        }
        
        public static Task<T> SafeExecuteAsync<T>(Func<T> function)
        {
            if (Application.Current?.Dispatcher.CheckAccess() == true)
            {
                return Task.FromResult(function());
            }
            else
            {
                return Application.Current?.Dispatcher.InvokeAsync(function).Task ?? Task.FromResult(default(T)!);
            }
        }
    }
}