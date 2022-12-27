using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace UniversalGameLauncher
{
    public enum GameSource
    {
        None,
        Steam,
        GOG,
        EpicGames,
        XBOX,
    }

    public class GameSourceUtils
    {
        private ImageSource SteamIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/steam_16.png"));
        private ImageSource GogIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/gog_16.png"));
        private ImageSource EgsIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/egs_16.png"));
        private ImageSource XboxIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/xbox_16.png"));

        private GameSourceUtils() {}
        public static ImageSource? GetOverlayIcon(GameSource gameSource)
        {
            GameSourceUtils? instance = new();
            return gameSource switch
            {
                GameSource.Steam => instance!.SteamIcon,
                GameSource.GOG => instance!.GogIcon,
                GameSource.EpicGames => instance!.EgsIcon,
                GameSource.XBOX => instance!.XboxIcon,
                _ => null
            };
        }

        public static ImageSource? GetOverlayIcon(Dispatcher dispatcher, GameSource gameSource)
        {
            GameSourceUtils? instance = null;
            dispatcher.Invoke(() =>
            {
                instance = new();
            });
            return gameSource switch
            { 
                GameSource.Steam => instance!.SteamIcon,
                GameSource.GOG=> instance!.GogIcon,
                GameSource.EpicGames => instance!.EgsIcon,
                GameSource.XBOX => instance!.XboxIcon,
                _ => null
            };
        }
    }
}
