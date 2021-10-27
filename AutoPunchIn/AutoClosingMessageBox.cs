using System;
using System.Threading;
using System.Windows;
using System.Windows.Interop;

namespace AutoPunchIn
{
    public class AutoClosingMessageBox
    {
        private readonly Timer _timeoutTimer;
        private readonly string _caption;
        private static AutoClosingMessageBox _instance;
        private readonly MessageBoxResult _messageBoxResult;

        private AutoClosingMessageBox(string text, string caption, MessageBoxButton mbb, MessageBoxImage mbi,
            int timeout)
        {
            _caption = caption;
            _timeoutTimer = new Timer(OnTimerElapsed,
                null, timeout, Timeout.Infinite);

            var tempWindow = new Window();
            var helper = new WindowInteropHelper(tempWindow);
            helper.EnsureHandle();
            _messageBoxResult = MessageBox.Show(tempWindow, text, caption, mbb, mbi);
            tempWindow.Close();
        }

        public static MessageBoxResult Show(string text, string caption, MessageBoxButton mbb, MessageBoxImage mbi,
            int timeout = 30000)
        {
            _instance = new AutoClosingMessageBox(text, caption, mbb, mbi, timeout);

            return _instance._messageBoxResult;
        }

        private void OnTimerElapsed(object state)
        {
            var mbWnd = FindWindow(null, _caption);
            if (mbWnd != IntPtr.Zero)
                SendMessage(mbWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
            _timeoutTimer.Dispose();
        }

        private const int WM_CLOSE = 0x0010;

        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, IntPtr wParam, IntPtr lParam);
    }
}