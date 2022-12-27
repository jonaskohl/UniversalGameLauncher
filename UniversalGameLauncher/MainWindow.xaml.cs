using SharpDX.XInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace UniversalGameLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {

        public static readonly DependencyProperty GamesProperty = DependencyProperty.Register("Games", typeof(GameInfo[]), typeof(MainWindow));
        public static readonly DependencyProperty GamesAreLoadingProperty = DependencyProperty.Register("GamesAreLoading", typeof(bool), typeof(MainWindow));
        public static readonly DependencyProperty IsOnHomePageProperty = DependencyProperty.Register("IsOnHomePage", typeof(bool), typeof(MainWindow));
        public static readonly DependencyProperty IsFullscreenProperty = DependencyProperty.Register("IsFullscreen", typeof(bool), typeof(MainWindow));

        const int CAPTIONHEIGHT = 30;
        float windowScale = 1.0f;

        DispatcherTimer _timer = new DispatcherTimer();
        private string _leftAxis;
        private string _rightAxis;
        private string _buttons;
        private Controller _controller;

        private CacheManager cacheManager;

        [Flags]
        enum GamepadNavigationChangeKind
        {
            Nothing,
            XDirection,
            YDirection,
            ActivateButton
        }

        private GamepadNavigationState lastState = GamepadNavigationState.Empty;

        struct GamepadNavigationState
        {
            public byte XDirection = 0;
            public byte YDirection = 0;
            public bool ActivateButton = false;

            public static GamepadNavigationState Empty = new GamepadNavigationState();

            public GamepadNavigationState() { }
        }


        public GameInfo[] Games
        {
            get => (GameInfo[])GetValue(GamesProperty);
            set => SetValue(GamesProperty, value);
        }

        public bool GamesAreLoading
        {
            get => (bool)GetValue(GamesAreLoadingProperty);
            set => SetValue(GamesAreLoadingProperty, value);
        }

        public bool IsOnHomePage
        {
            get => (bool)GetValue(IsOnHomePageProperty);
            set => SetValue(IsOnHomePageProperty, value);
        }

        public bool IsFullscreen
        {
            get => (bool)GetValue(IsFullscreenProperty);
            set
            {
                if (ChangeFullscreen(value))
                    SetValue(IsFullscreenProperty, value);
            }
        }

        private const int WM_SYSCOMMAND = 0x112;
        uint TPM_LEFTALIGN = 0x0000;
        uint TPM_RETURNCMD = 0x0100;
        const UInt32 MF_ENABLED = 0x00000000;
        const UInt32 MF_GRAYED = 0x00000001;
        internal const UInt32 SC_MAXIMIZE = 0xF030;
        internal const UInt32 SC_RESTORE = 0xF120;
        const uint DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        const uint DWMWCP_DEFAULT = 0;
        const uint DWMWCP_DONOTROUND = 1;
        const uint DWMWCP_ROUND = 2;
        const uint DWMWCP_ROUNDSMALL = 3;

        IntPtr Handle => new WindowInteropHelper(this).Handle;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags,
          int x, int y, IntPtr hwnd, IntPtr lptpm);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("dwmapi.dll")]
        static extern IntPtr DwmGetWindowAttribute(IntPtr hWnd, uint dwAttribute, out uint pvAttribute, ref uint cbAttribute);

        [DllImport("dwmapi.dll")]
        static extern IntPtr DwmSetWindowAttribute(IntPtr hWnd, uint dwAttribute, ref uint pvAttribute, uint cbAttribute);

        public MainWindow()
        {
            InitializeComponent();

            Loaded += MainWindow_Loaded;
            Closing += MainWindow_Closing;
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            _timer.Tick += _timer_Tick;
            _timer.Start();

            DataContext = this;

            Navigate<GamesPage>();

            var curDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var cacheFilePath = System.IO.Path.Combine(curDir, "cacheinfo");
            var cachePath = System.IO.Path.Combine(curDir, "cache");

            if (Environment.OSVersion.Version.Major < 10 || (Environment.OSVersion.Version < new Version(10, 0, 22000)))
            {
                ToolButtonsWrapper.CornerRadius = new CornerRadius(0);
                CaptionButtonsWrapper.CornerRadius = new CornerRadius(0);
            }

            var args = Environment.GetCommandLineArgs();

            cacheManager = new()
            {
                CacheInfoPath = cacheFilePath,
                CachePath = cachePath
            };
            cacheManager.TryLoad();

            int dummyGameCount = -1;

            {
                const string prefix = "/debug:dummyGames=";
                var _c = args.Where(a => a.StartsWith(prefix));
                if (_c.Count() > 0)
                {
                    var param = _c.First().Substring(prefix.Length);
                    if (int.TryParse(param, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                    {
                        dummyGameCount = value;
                    }
                }
            }

            if (dummyGameCount >= 0)
                Games = Enumerable.Range(0, dummyGameCount).Select(i => new GameInfo() { Name = $"Game {i + 1}" }).ToArray();
            else
            {
                GamesAreLoading = true;

                Task.Run(() =>
                {
                    try
                    {
                        Dispatcher.Invoke(() =>
                        {
                            Games = new GameInfo[][]
                            {
                            new SteamGameCollector().GetGames(),
                            new XBOXGamesCollector().GetGames(),
                            new GogGamesCollector().GetGames(),
                            new OriginGamesCollector().GetGames(),
                            new EpicGamesCollector().GetGames(),
                            }.SelectMany(i => i).Where(i => i is not null).OrderBy(i => i.Name?.ToLower()).ToArray();
                        });
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }).ContinueWith(t1 =>
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        GamesAreLoading = false;
                        PopulateCoverArt(Games).ContinueWith(t2 =>
                        {
                            cacheManager.Save();
                        });
                    });
                });
            }
        }

        private bool ChangeFullscreen(bool value)
        {
            if (value)
            {
                Visibility = Visibility.Collapsed;
                WindowStyle = WindowStyle.None;
                RootChrome.CaptionHeight = 0;
                ResizeMode = ResizeMode.NoResize;
                Topmost = true;
                WindowState = WindowState.Maximized;
                BorderThickness = new Thickness(0);
                Visibility = Visibility.Visible;
            }
            else
            {
                WindowState = WindowState.Normal;
                ResizeMode = ResizeMode.CanResize;
                WindowStyle = WindowStyle.SingleBorderWindow;
                RootChrome.CaptionHeight = CAPTIONHEIGHT * windowScale;
                Topmost = false;
            }

            return true;
        }

        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            _controller = null;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _controller = new Controller(UserIndex.One);
        }

        void _timer_Tick(object sender, EventArgs e)
        {
            DisplayControllerInformation();
        }

        void DisplayControllerInformation()
        {
            if (!_controller.IsConnected)
                return;

            var state = _controller.GetState();
            var rawAxisX = state.Gamepad.LeftThumbX;
            var rawAxisY = state.Gamepad.LeftThumbY;

            var btns = state.Gamepad.Buttons;

            var x = rawAxisX / (float)short.MaxValue;
            if (Math.Abs(x) < 0.1) x = 0;

            var y = rawAxisY / (float)short.MaxValue;
            if (Math.Abs(y) < 0.1) y = 0;

            var directionX = (Math.Abs(x) > 0.5) ? Math.Sign(x) : 0;
            var directionY = (Math.Abs(y) > 0.5) ? Math.Sign(y) : 0;

            directionX = btns.HasFlag(GamepadButtonFlags.DPadLeft) ? -1 : (btns.HasFlag(GamepadButtonFlags.DPadRight) ? 1 : directionX);
            directionY = btns.HasFlag(GamepadButtonFlags.DPadDown) ? -1 : (btns.HasFlag(GamepadButtonFlags.DPadUp) ? 1 : directionY);

            var fire = btns.HasFlag(GamepadButtonFlags.A);

            var newState = new GamepadNavigationState()
            {
                ActivateButton = fire,
                XDirection = (byte)(directionX & 0xFF),
                YDirection = (byte)(directionY & 0xFF)
            };

            var whatChanged = GamepadNavigationChangeKind.Nothing;

            if (lastState.ActivateButton != newState.ActivateButton)
                whatChanged |= GamepadNavigationChangeKind.ActivateButton;

            if (lastState.XDirection != newState.XDirection)
                whatChanged |= GamepadNavigationChangeKind.XDirection;

            if (lastState.YDirection != newState.YDirection)
                whatChanged |= GamepadNavigationChangeKind.YDirection;

            if (whatChanged != GamepadNavigationChangeKind.Nothing)
                OnGamepadInputChanged(whatChanged, newState);

            lastState = newState;

            Title = string.Format("X: {0} Y: {1}", directionX, directionY);

        }

        void OnGamepadInputChanged(GamepadNavigationChangeKind changed, GamepadNavigationState newState)
        {
            //MessageBox.Show(changed.ToString());
            if (changed.HasFlag(GamepadNavigationChangeKind.XDirection) && newState.XDirection != 0)
            {
                GameMoveFocus(new TraversalRequest(newState.XDirection > 0 ? FocusNavigationDirection.Right : FocusNavigationDirection.Left));
            }
            if (changed.HasFlag(GamepadNavigationChangeKind.YDirection) && newState.YDirection != 0)
            {
                GameMoveFocus(new TraversalRequest(newState.YDirection > 0 ? FocusNavigationDirection.Up : FocusNavigationDirection.Down));
            }
            if (changed.HasFlag(GamepadNavigationChangeKind.ActivateButton) && newState.ActivateButton)
            {
                var focusedControl = FocusManager.GetFocusedElement(this);

                if (focusedControl is ItemsControl)
                {
                    var ic = focusedControl as ItemsControl;
                    for (var i = 0; i < ic.Items.Count; ++i)
                    {
                        var elem = (UIElement)ic.ItemContainerGenerator.ContainerFromIndex(i);
                        if (elem is Button && (elem as Button).IsFocused)
                        {
                            focusedControl = elem;
                            break;
                        }
                        else
                        {
                            MessageBox.Show(elem.GetType().FullName);
                        }
                    }

                    if (focusedControl is Button)
                    {
                        var peer = new ButtonAutomationPeer(focusedControl as Button);
                        var invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                        invokeProv?.Invoke();
                    }
                    else
                    {
                        MessageBox.Show("FOCUSED CONTROL IS NOT A BUTTON, IT IS " + (focusedControl == null ? "NULL" : focusedControl.GetType().FullName));
                    }
                }
            }
        }

        private void GameMoveFocus(TraversalRequest traversalRequest)
        {
            UIElement elementWithFocus = Keyboard.FocusedElement as UIElement;

            // Change keyboard focus. 
            if (elementWithFocus != null)
            {
                elementWithFocus.MoveFocus(traversalRequest);
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            var args = Environment.GetCommandLineArgs();

            if (args.Contains("/fullscreen"))
                IsFullscreen = true;

            {
                const string prefix = "/overridescale=";
                var _c = args.Where(a => a.StartsWith(prefix));
                if (_c.Count() > 0)
                {
                    var param = _c.First().Substring(prefix.Length);
                    if (float.TryParse(param, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                    {
                        RootScale.ScaleX = RootScale.ScaleY = value;
                        windowScale = value;
                        RootChrome.CaptionHeight = CAPTIONHEIGHT * windowScale;
                    }
                }
            }

            {
                const string prefix = "/experimentalFeature:showFullscreenButton=";
                var _c = args.Where(a => a.StartsWith(prefix));
                if (_c.Count() > 0)
                {
                    var param = _c.First().Substring(prefix.Length).ToLower();
                    if (param == "1" || param == "yes" || param == "true" || param == "on")
                    {
                        titleButton_ToggleFullScreen.Visibility = Visibility.Visible;
                    }
                }
            }
        }

        private async Task PopulateCoverArt(GameInfo[] games)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(games, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 4
                }, game =>
                {
                    game.FetchImageSourceAction?.Invoke(game, Dispatcher, cacheManager);
                });
            });
        }

        private bool inSysMenu = false;

        public event PropertyChangedEventHandler? PropertyChanged;

        void SystemMenuIcon_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1 && !inSysMenu)
            {
                inSysMenu = true;
                ShowSystemMenu(); //replace with your code
            }
            else if (e.ClickCount == 2 && e.ChangedButton == MouseButton.Left)
            {
                Close();
            }
        }

        private void ShowSystemMenu()
        {
            var helper = new WindowInteropHelper(this);
            var callingWindow = helper.Handle;
            var wMenu = GetSystemMenu(callingWindow, false);
            EnableMenuItem(wMenu, SC_MAXIMIZE, (WindowState == WindowState.Maximized) ? MF_GRAYED : MF_ENABLED);

            var pt = new Point((int)(96 * windowScale), (int)(24 * windowScale));
            pt.Offset(BorderThickness.Left, BorderThickness.Top);
            pt = PointToScreen(pt);

            var command = TrackPopupMenuEx(wMenu, TPM_LEFTALIGN | TPM_RETURNCMD, (int)pt.X, (int)pt.Y, callingWindow, IntPtr.Zero);
            if (command == 0)
                return;

            PostMessage(callingWindow, WM_SYSCOMMAND, new IntPtr(command), IntPtr.Zero);
        }

        void SystemMenuIcon_MouseLeave(object sender, MouseEventArgs e)
        {
            inSysMenu = false;
        }

        void SystemMenuIcon_MouseUp(object sender, MouseButtonEventArgs e)
        {
            inSysMenu = false;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (WindowState == WindowState.Maximized && !IsFullscreen)
            {
                BorderThickness = new Thickness(8);
            }
            else
            {
                BorderThickness = new Thickness(0);
            }
        }

        private void CommandBinding_CanExecute_1(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBinding_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void CommandBinding_Executed_2(object sender, ExecutedRoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
                SystemCommands.RestoreWindow(this);
            else
                SystemCommands.MaximizeWindow(this);
        }

        private void CommandBinding_Executed_3(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void GameClicked(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var dataObject = btn.DataContext as GameInfo;

            if (dataObject is null)
            {
                MessageBox.Show("Could not retrieve GameInfo from item", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var message = $"Launch {dataObject?.Name ?? "<NO_NAME>"}?";
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                message += Environment.NewLine + Environment.NewLine + "Command:"
                        + Environment.NewLine + (dataObject?.ExecutableLocation ?? "<NO_NAME>")
                        + Environment.NewLine + string.Join(" ", (dataObject?.ExecutableArguments ?? new string[0]).Select(a => EscapeArg(a)));
            }

            if (MessageBox.Show(message, "", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            var psi = new ProcessStartInfo()
            {
                FileName = dataObject?.ExecutableLocation,
                Arguments = string.Join(" ", (dataObject?.ExecutableArguments ?? new string[0]).Select(a => EscapeArg(a)))
            };
            Process.Start(psi);
        }

        private string EscapeArg(string a)
        {
            return "\"" + Regex.Replace(a, @"(\\+)$", @"$1$1") + "\"";
        }

        private string PsiToString(ProcessStartInfo psi)
        {
            return $"{psi.FileName} {psi.Arguments}";
        }

        private void Navigate<T>() where T : DisplayPage
        {
            if (MainViewContainer.Content is not null && MainViewContainer.Content is DisplayPage)
            {
                (MainViewContainer.Content as DisplayPage)!.EventOccurred -= DisplayPage_EventOccurred;
            }

            IsOnHomePage = typeof(T) == typeof(GamesPage);

            var instance = (T?)Activator.CreateInstance(typeof(T));
            instance.DataContext = this;
            instance.EventOccurred += DisplayPage_EventOccurred;
            MainViewContainer.Content = instance;
        }

        private void DisplayPage_EventOccurred(object? sender, DisplayPageEventArgs e)
        {
            if (e.EventName == "GameClicked")
            {
                GameClicked(e.EventSource!, e.EventArgs);
            }
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            Navigate<SettingsPage>();
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            Navigate<GamesPage>();
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void ToggleFullscreen_Click(object sender, RoutedEventArgs e)
        {
            IsFullscreen = !IsFullscreen;
        }
    }
}
