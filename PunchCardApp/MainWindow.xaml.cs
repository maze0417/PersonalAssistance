using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;
using Core;
using Microsoft.Extensions.Logging;
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
        private readonly NotifyIcon _notifyIcon;
        private readonly Assembly _curAssembly = Assembly.GetExecutingAssembly();

        private readonly RegistryKey _registryKey = Registry.CurrentUser.OpenSubKey
            ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        private readonly IHrResourceService _hrResourceService;
        private readonly ILoggerReader _loggerReader;
        private readonly ILogger _logger;
        private readonly IAppConfiguration _appConfiguration;

        public MainWindow()
        {
            _notifyIcon = new NotifyIcon
            {
                BalloonTipText = @"已經最小化，點擊查看選項",
                BalloonTipTitle = $@"自動打卡系統 v{_curAssembly.GetName().Version}"
            };
            _notifyIcon.Text = _curAssembly.GetName().Version.ToString();
            _loggerReader = new Logger();
            _logger = (ILogger) _loggerReader;
            _appConfiguration = new AppConfiguration();
            var service = new NueIpService(_logger, _appConfiguration);

            _hrResourceService = new HrResourceService(_logger, service);
            _hrResourceService.Init();
            InitIcon();
            MinizeIcon();
            InitializeComponent();
            SystemEvents.SessionEnding += SystemEvents_SessionEnding;
            SystemEvents.PowerModeChanged += OnPowerChange;
        }

        private void OnPowerChange(object sender, PowerModeChangedEventArgs e)
        {
            var status = _hrResourceService.TaskStatus;
            _logger.LogInformation($"task status :{status} when power change to {e.Mode}");
            switch (e.Mode)
            {
                case PowerModes.Resume:
                {
                    break;
                }

                case PowerModes.Suspend:
                {
                    //AsyncHelper.RunSync(() => _hrResourceService.PunchCardAsync(false));
                    break;
                }
            }
        }

        private void SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
        {
            AsyncHelper.RunSync(() => _hrResourceService.PunchCardAsync(false));
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
            AddInfoMenu(contextMenu);
            AddAutoStartMenu(contextMenu);
            AddPunchCardWorkMenu(contextMenu);


            AddCloseMenu(contextMenu);
            AddLogMenu(contextMenu);
            _notifyIcon.ContextMenu = contextMenu;
        }

        private void AddInfoMenu(ContextMenu contextMenu)
        {
            var menuItem = new MenuItem();
            contextMenu.MenuItems.Add(menuItem);
            menuItem.Text = @"個人資訊";

            menuItem.Click += (b, c) =>
            {
                var pwd = _appConfiguration.NueIpPwd.Length > 0 ? "*****" : "尚未填寫";
                var msg = $@"帳號: {_appConfiguration.NueIpId}
密碼: {pwd}
經度:{_appConfiguration.Lat}
緯度:{_appConfiguration.Lng}

上班打卡: {_hrResourceService.PunchedInTime}
下次上班打卡: {_hrResourceService.NextPunchedInTime}

下班打卡: {_hrResourceService.PunchedOutTime}
下次下班打卡: {_hrResourceService.NextPunchedOutTime}

最後偵測時間: {_hrResourceService.LastMonitTime}
";

                AutoClosingMessageBox.Show(msg, "打卡資訊", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            };
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


        private void AddPunchCardWorkMenu(Menu contextMenu)
        {
            var menuItem = new MenuItem();
            contextMenu.MenuItems.Add(menuItem);

            menuItem.Text = @"上班打卡";
            menuItem.Click += (sender, args) =>
            {
                var res = AsyncHelper.RunSync(() => _hrResourceService.PunchCardAsync(false));
                AutoClosingMessageBox.Show($"{res.message ?? res.errorCode ?? res.code}", "提醒..30 sec後關閉",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            };
        }


        private void AddCloseMenu(Menu contextMenu)
        {
            var menuItem = new MenuItem();
            contextMenu.MenuItems.Add(menuItem);

            menuItem.Text = @"下班離開";
            menuItem.Click += (sender, args) => { Close(); };
        }

        private void AddLogMenu(Menu contextMenu)
        {
            var menuItem = new MenuItem();
            contextMenu.MenuItems.Add(menuItem);

            menuItem.Text = @"Log";
            menuItem.Click += (sender, args) =>
            {
                var msg = string.IsNullOrEmpty(_loggerReader.GetLoggedMessage())
                    ? "無資訊"
                    : _loggerReader.GetLoggedMessage();
                MessageBox.Show(msg, "Log", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            };
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var result = MessageBox.Show("離開前要打卡嗎?", "Close", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                var res = AsyncHelper.RunSync(() => _hrResourceService.PunchCardAsync(true));
                AutoClosingMessageBox.Show($"{res.message ?? res.errorCode ?? res.code}", "提醒..30 sec後關閉",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            _notifyIcon.Click -= ShowContextMenu;
            _notifyIcon.Visible = false;
            _notifyIcon.ContextMenu.Dispose();
            _notifyIcon.Dispose();
        }
    }
}