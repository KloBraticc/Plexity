using Microsoft.VisualBasic;
using Plexity.Enums;
using Plexity.Integrations;
using System;
using System.Threading.Tasks;
using static System.Windows.Forms;
using System.Windows.Input;

namespace Plexity.UI
{
    public class NotifyIconWrapper : IDisposable
    {
        // lol who needs properly structured mvvm and xaml when you have the absolute catastrophe that this is

        private bool _disposing = false;

        private readonly NotifyIcon? _notifyIcon;

        private readonly Watcher? _watcher;

        private readonly MenuContainer? _menuContainer;

        private EventHandler? _alertClickHandler;

        private ActivityWatcher? _activityWatcher => _watcher.ActivityWatcher;

        public object? MouseButtons { get; private set; }

        #region Activity handlers
        public async void OnGameJoin(object? sender, EventArgs e)
        {
            if (_activityWatcher is null)
                return;

            string? serverLocation = await _activityWatcher.Data.QueryServerLocation();

            if (string.IsNullOrEmpty(serverLocation))
                return;

            string title = _activityWatcher.Data.ServerType switch
            {
                ServerType.Public => "Public",
                ServerType.Private => "Private",
                ServerType.Reserved => "Reserved",
                _ => string.Empty
            };
        }
        #endregion

        /// <summary>
        /// Show a balloon tip notification on the notify icon.
        /// </summary>
        /// <param name="caption">Title of the notification.</param>
        /// <param name="message">Message body.</param>
        /// <param name="duration">Duration in seconds.</param>
        /// <param name="clickHandler">Optional click handler for balloon tip click.</param>
        public void ShowAlert(string caption, string message, int duration, EventHandler? clickHandler)
        {
            string id = Guid.NewGuid().ToString()[..8];
            string LOG_IDENT = $"NotifyIconWrapper::ShowAlert.{id}";

            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"Showing alert for {duration} seconds (clickHandler={clickHandler != null})");
            App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, $"{caption}: {message.Replace("\n", "\\n")}");

            _notifyIcon.BalloonTipTitle = caption;
            _notifyIcon.BalloonTipText = message;

            // Unsubscribe previous handler if any
            if (_alertClickHandler is not null)
            {
                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Previous alert still present, erasing click handler");
                _notifyIcon.BalloonTipClicked -= _alertClickHandler;
            }

            _alertClickHandler = clickHandler;

            if (clickHandler is not null)
                _notifyIcon.BalloonTipClicked += clickHandler;

            _notifyIcon.ShowBalloonTip(duration);

            // Remove the handler after the duration asynchronously
            Task.Run(async () =>
            {
                await Task.Delay(duration * 1000);

                if (clickHandler is not null)
                    _notifyIcon.BalloonTipClicked -= clickHandler;

                App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Duration over, erasing current click handler");

                if (_alertClickHandler == clickHandler)
                    _alertClickHandler = null;
                else
                    App.Logger.WriteLine(LogLevel.Info, LOG_IDENT, "Click handler has been overridden by another alert");
            });
        }

        #region IDisposable Support
        // Finalizer
        ~NotifyIconWrapper()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposing)
                return;

            _disposing = true;

            if (disposing)
            {


                if (_alertClickHandler is not null)
                    _notifyIcon.BalloonTipClicked -= _alertClickHandler;

                _notifyIcon.Dispose();
            }
        }
        #endregion
    }
}
