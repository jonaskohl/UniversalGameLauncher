using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UniversalGameLauncher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public static readonly DependencyProperty GamesProperty = DependencyProperty.Register("Games", typeof(GameInfo[]), typeof(MainWindow));
        public static readonly DependencyProperty GamesAreLoadingProperty = DependencyProperty.Register("GamesAreLoading", typeof(bool), typeof(MainWindow));

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

        private const int WM_SYSCOMMAND = 0x112;
        uint TPM_LEFTALIGN = 0x0000;
        uint TPM_RETURNCMD = 0x0100;
        const UInt32 MF_ENABLED = 0x00000000;
        const UInt32 MF_GRAYED = 0x00000001;
        internal const UInt32 SC_MAXIMIZE = 0xF030;
        internal const UInt32 SC_RESTORE = 0xF120;

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("user32.dll")]
        static extern int TrackPopupMenuEx(IntPtr hmenu, uint fuFlags,
          int x, int y, IntPtr hwnd, IntPtr lptpm);

        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        CacheManager cacheManager;

        public MainWindow()
        {
            InitializeComponent();

            var curDir = System.IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var cacheFilePath = System.IO.Path.Combine(curDir, "cacheinfo");
            var cachePath = System.IO.Path.Combine(curDir, "cache");

            cacheManager = new()
            {
                CacheInfoPath = cacheFilePath,
                CachePath = cachePath
            };
            cacheManager.TryLoad();

            //Games = Enumerable.Range(0, 256).Select(i => new GameInfo() { Name = $"Game {i + 1}" }).ToArray();

            GamesAreLoading = true;

            Task.Run(() =>
            {
                try
                {
                    Dispatcher.BeginInvoke(() =>
                    {
                        Games = new GameInfo[][]
                        {
                            new SteamGameCollector().GetGames(),
                            new MiscHardCodedGameCollector().GetGames()
                        }.SelectMany(i => i).Where(i => i is not null).ToArray();
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

        private async Task PopulateCoverArt(GameInfo[] games)
        {
            await Task.Run(() =>
            {
                Parallel.ForEach(games, new ParallelOptions()
                {
                    MaxDegreeOfParallelism = 2
                }, game =>
                {
                    if (game is not SteamGameInfo)
                        return;

                    var coverUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{(game as SteamGameInfo)?.SteamGameId}/library_600x900.jpg";
                    Debug.WriteLine($"Cover url: {coverUrl}");
                    var coverFile = cacheManager.GetCacheFileName(coverUrl);
                    if (coverFile == null)
                    {
                        coverUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{(game as SteamGameInfo)?.SteamGameId}/header.jpg";
                        coverFile = cacheManager.GetCacheFileName(coverUrl);
                        if (coverFile == null)
                            return;
                    }
                    Dispatcher.Invoke(() =>
                    {
                        game.CoverImage = new BitmapImage(new Uri(coverFile));
                    });
                });
            });
        }

        private bool inSysMenu = false;

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

            var pt = new Point(8, 24);
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
            if (WindowState == WindowState.Maximized)
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

            var psi = new ProcessStartInfo()
            {
                FileName = dataObject.ExecutableLocation,
                Arguments = string.Join(" ", dataObject.ExecutableArguments.Select(a => EscapeArg(a)))
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
    }
}
