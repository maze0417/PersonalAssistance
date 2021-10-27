using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Core;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.Logging;
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
        private readonly ILogger _logger;

        public MainWindow(IHrResourceService hrResourceService, ILoggerReader loggerReader, ILogger logger)
        {
            _hrResourceService = hrResourceService;
            _loggerReader = loggerReader;
            _logger = logger;
            _hrResourceService.Init();


            _notifyIcon = new TaskbarIcon();
            InitIcon();
            MinimizeIcon();
            InitializeComponent();
        }

        private void MinimizeIcon()
        {
            WindowState = WindowState.Minimized;
            //Hide();
            _notifyIcon.Visibility = Visibility.Visible;
            var icon = new Icon("timer.ico");
            _notifyIcon.Icon = icon;
            //_notifyIcon.ToolTipText = @"已經最小化，點擊查看選項";
            //_notifyIcon.ToolTip = $@"自動打卡系統 v{_curAssembly.GetName().Version}";
            _notifyIcon.ShowBalloonTip($@"自動打卡系統 v{_curAssembly.GetName().Version}",
                @"已經最小化，點擊查看選項", icon);
        }

        private void InitIcon()
        {
            var contextMenu = new ContextMenu();
            AddAutoStartMenu(contextMenu);
            AddPunchCardWorkMenu(contextMenu);
            AddCloseMenu(contextMenu);
            AddLogMenu(contextMenu);
            _notifyIcon.ContextMenu = contextMenu;
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
                MessageBox.Show(_loggerReader.GetLoggedMessage(), "Log", MessageBoxButton.OK,
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
            menuItem.Header = @"自動開啟";
            menuItem.Click += (sender, args) =>
            {
                if (menuItem.IsChecked)
                {
                    _registryKey?.DeleteValue(_curAssembly.GetName().Name);
                    menuItem.IsChecked = false;
                    return;
                }

                _registryKey?.SetValue(_curAssembly.GetName().Name, _curAssembly.Location);
                menuItem.IsChecked = true;
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


            _notifyIcon.Visibility = Visibility.Collapsed;
            _notifyIcon.Dispose();
        }
    }
}