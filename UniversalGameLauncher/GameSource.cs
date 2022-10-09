using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UniversalGameLauncher
{
    public enum GameSource
    {
        None,
        Steam,
        GOG
    }

    public static class GameSourceUtils
    {
        private static ImageSource SteamIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/steam_16.png"));
        private static ImageSource GogIcon = new BitmapImage(new Uri("pack://application:,,,/Resources/gog_16.png"));

        public static ImageSource? GetOverlayIcon(GameSource gameSource)
        {
            return gameSource switch
            { 
                GameSource.Steam => SteamIcon,
                GameSource.GOG=> GogIcon,
                _ => null
            };
        }
    }
}
