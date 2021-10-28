using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Core;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using PunchCardApp;

namespace AutoPunchIn
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TaskbarIcon _notifyIcon;
        private readonly Assembly _curAssembly = Assembly.GetExecutingAssembly();

        private readonly RegistryKey _registryKey = Registry.CurrentUser.OpenSubKey
            ("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        private readonly IHrResourceService _hrResourceService;
        private readonly ILoggerReader _loggerReader;
        private readonly IAppConfiguration _appConfiguration;


        public MainWindow(IHrResourceService hrResourceService, ILoggerReader loggerReader,
            IAppConfiguration appConfiguration)
        {
            _hrResourceService = hrResourceService;
            _loggerReader = loggerReader;
            _appConfiguration = appConfiguration;
            _hrResourceService.Init();


            _notifyIcon = new TaskbarIcon();
            InitIcon();
            InitializeComponent();
        }


        private void InitIcon()
        {
            var contextMenu = new ContextMenu();
            var icon = new Icon("timer.ico");

            var title = $@"{_curAssembly.GetName().Name} v{_curAssembly.GetName().Version}";
            _notifyIcon.Icon = icon;
            _notifyIcon.ToolTipText = title;
            _notifyIcon.ShowBalloonTip(title, "已經最小化", BalloonIcon.Info);
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
            contextMenu.Items.Add(menuItem);
            menuItem.Header = @"個人資訊";

            menuItem.Click += (sender, args) =>
            {
                var msg = $@"帳號: {_appConfiguration.NueIpId}
經度:{_appConfiguration.Lat}
緯度:{_appConfiguration.Lng}

上班打卡: {_hrResourceService.PunchedInTime}
下次上班打卡: {_hrResourceService.NextPunchedInTime}

下班打卡: {_hrResourceService.PunchedOutTime}
下次下班打卡: {_hrResourceService.NextPunchedOutTime}
";

                AutoClosingMessageBox.Show(msg, "打卡資訊", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            };
        }

        private void AddCloseMenu(ContextMenu contextMenu)
        {
            var menuItem = new MenuItem();
            contextMenu.Items.Add(menuItem);
            menuItem.Header = @"下班離開";
            menuItem.Click += (sender, args) => { Close(); };
        }

        private void AddLogMenu(ContextMenu contextMenu)
        {
            var menuItem = new MenuItem();
            contextMenu.Items.Add(menuItem);
            menuItem.Header = @"Log";

            menuItem.Click += (sender, args) =>
            {
                var msg = string.IsNullOrEmpty(_loggerReader.GetLoggedMessage())
                    ? "無資訊"
                    : _loggerReader.GetLoggedMessage();
                AutoClosingMessageBox.Show(msg, "Log", MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            };
        }

        private void AddPunchCardWorkMenu(ContextMenu contextMenu)
        {
            var menuItem = new MenuItem();
            contextMenu.Items.Add(menuItem);
            menuItem.Header = @"上班打卡";
            menuItem.Click += (sender, args) =>
            {
                var res = AsyncHelper.RunSync(() => _hrResourceService.PunchCardAsync(false));
                AutoClosingMessageBox.Show($"{res.message ?? res.errorCode ?? res.code}", "提醒..30 sec後關閉",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            };
        }

        private void AddAutoStartMenu(ContextMenu contextMenu)
        {
            var menuItem = new MenuItem();

            contextMenu.Items.Add(menuItem);
            menuItem.IsChecked = _registryKey?.GetValue(_curAssembly.GetName().Name) != null;
            menuItem.Header = @"開機時啟動";
            menuItem.Click += (sender, args) =>
            {
                if (menuItem.IsChecked)
                {
                    _registryKey?.DeleteValue(_curAssembly.GetName().Name);
                    menuItem.IsChecked = false;
                    return;
                }

                var exe = _curAssembly.Location.Replace(".dll", ".exe");
                _registryKey?.SetValue(_curAssembly.GetName().Name, exe);
                menuItem.IsChecked = true;
            };
        }

        private void OnClose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var result =
                AutoClosingMessageBox.Show("離開前要打卡嗎?", "Close", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                var res = AsyncHelper.RunSync(() => _hrResourceService.PunchCardAsync(true));
                AutoClosingMessageBox.Show($"{res.message ?? res.errorCode ?? res.code}", "提醒..30 sec後關閉",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }


            _notifyIcon.Visibility = Visibility.Collapsed;
            _notifyIcon.Dispose();
        }
    }
}