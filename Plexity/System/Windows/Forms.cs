
using System.Windows.Input;

namespace System.Windows
{
    internal class Forms
    {
        internal class NotifyIcon
        {
            public string BalloonTipTitle { get; internal set; }
            public string BalloonTipText { get; internal set; }
            public EventHandler BalloonTipClicked { get; internal set; }
            public Action<object?, MouseEventArgs> MouseClick { get; internal set; }

            internal void Dispose()
            {
                throw new NotImplementedException();
            }

            internal void ShowBalloonTip(int duration)
            {
                throw new NotImplementedException();
            }
        }

        internal class FolderBrowserDialog
        {
            public FolderBrowserDialog()
            {
            }
        }
    }
}