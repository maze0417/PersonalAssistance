using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Microsoft.Extensions.Logging.Debug;
using Microsoft.Win32;
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace PunchCardApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly NotifyIcon _notifyIcon = new NotifyIcon
        {
            BalloonTipText = @"已經最小化，點擊查看選項",
            BalloonTipTitle = @"自動打卡系統.."
        };

        private readonly Assembly _curAssembly = Assembly.GetExecutingAssembly();

        private readonly RegistryKey _registryKey = Registry.CurrentUser.OpenSubKey
            ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        private readonly IHrResourceService _hrResourceService;

        public MainWindow()
        {
            var lp = new DebugLoggerProvider();
            var logger = lp.CreateLogger("HrResourceService");

            _hrResourceService = new HrResourceService(logger);
            _hrResourceService.Init();
            InitIcon();
            MinizeIcon();

            InitializeComponent();
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            SystemEvents.PowerModeChanged += OnPowerChange;
        }

        private void OnPowerChange(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    break;

                case PowerModes.Suspend:
                    {
                        AsyncHelper.RunSync(() => _hrResourceService.PunchCardAsync());
                        break;
                    }
            }
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            AsyncHelper.RunSync(() => _hrResourceService.PunchCardAsync());
        }

        private void MinizeIcon()
        {
            WindowState = WindowState.Minimized;
            Hide();
            _notifyIcon.Visible = !IsVisible;
            _notifyIcon?.ShowBalloonTip(2000);
        }

        private void InitIcon()
        {
            Stream LoadIcon()
            {
                var streamResourceInfo = Application.GetResourceStream(
                    new Uri(
                        "pack://application:,,,/PunchCardApp;component/timer.ico"));
                if (streamResourceInfo == null)
                    throw new Exception("could not load icon");
                return streamResourceInfo.Stream;
            }

            _notifyIcon.Icon = new Icon(LoadIcon());

            var contextMenu = new ContextMenu();
            AddAutoStartMenu(contextMenu);
            AddPunchCardMenu(contextMenu);
            AddPunchCardQueryMenu(contextMenu);
            AddCloseMenu(contextMenu);
            _notifyIcon.ContextMenu = contextMenu;
        }

        private void ShowContextMenu(object sender, EventArgs e)
        {
            var mi = typeof(NotifyIcon).GetMethod("ShowContextMenu", BindingFlags.Instance | BindingFlags.NonPublic);
            mi?.Invoke(_notifyIcon, null);
        }

        private void AddAutoStartMenu(Menu contextMenu)
        {
            var menuItem = new MenuItem();

            contextMenu.MenuItems.Add(menuItem);
            menuItem.Index = 0;
            menuItem.Checked = _registryKey?.GetValue(_curAssembly.GetName().Name) != null;
            menuItem.Text = @"自動開啟";
            menuItem.Click += (sender, args) =>
            {
                if (menuItem.Checked)
                {
                    _registryKey?.DeleteValue(_curAssembly.GetName().Name);
                    menuItem.Checked = false;
                    return;
                }
                _registryKey?.SetValue(_curAssembly.GetName().Name, _curAssembly.Location);
                menuItem.Checked = true;
            };
        }

        private void AddPunchCardQueryMenu(Menu contextMenu)
        {
            var menuItem = new MenuItem();
            contextMenu.MenuItems.Add(menuItem);
            menuItem.Index = 0;
            menuItem.Text = @"今日打卡紀錄";
            menuItem.Click += (sender, args) =>
            {
                var res = AsyncHelper.RunSync(() => _hrResourceService.GetDayCardDetailAsync());
                AutoClosingMessageBox.Show($@"
今日時間:{DateTime.Now:yyyy/MM/dd} {Environment.NewLine}
工時:{_hrResourceService.WorkerTime:hh\:mm\:ss} {Environment.NewLine}
Last Monitor:{_hrResourceService.LastMonitTime} {Environment.NewLine}
Last Interval:{_hrResourceService.CacheInterval} {Environment.NewLine}
打卡歷程:{Environment.NewLine}
{string.Join(Environment.NewLine, res)}",
                    "提醒..30 sec後關閉", MessageBoxButton.OK, MessageBoxImage.Warning);
            };
        }

        private void AddPunchCardMenu(Menu contextMenu)
        {
            var menuItem = new MenuItem();
            contextMenu.MenuItems.Add(menuItem);
            menuItem.Index = 0;
            menuItem.Text = @"想打就打卡";
            menuItem.Click += (sender, args) =>
            {
                var res = AsyncHelper.RunSync(() => _hrResourceService.PunchCardAsync());
                AutoClosingMessageBox.Show($"{res.message}", "提醒..30 sec後關閉", MessageBoxButton.OK, MessageBoxImage.Warning);
            };
        }

        private void AddCloseMenu(Menu contextMenu)
        {
            var menuItem = new MenuItem();
            contextMenu.MenuItems.Add(menuItem);
            menuItem.Index = 0;
            menuItem.Text = @"下班離開";
            menuItem.Click += (sender, args) =>
            {
                Close();
            };
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var result = MessageBox.Show("離開前要打卡嗎?", "Close", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                AsyncHelper.RunSync(() => _hrResourceService.PunchCardAsync());
            }
            _notifyIcon.Click -= ShowContextMenu;
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenu.Dispose();
            _notifyIcon.Dispose();
        }
    }
}