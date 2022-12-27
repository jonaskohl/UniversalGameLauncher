using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace UniversalGameLauncher
{
    public class OriginGamesCollector : IGameCollector
    {
        // FIXME EXPERIMENTAL, doesn't work quite right yet
        public GameInfo[] GetGames()
        {
#if ENABLE_EXPERIMENTAL__GAMECOLLECTOR__ORIGIN
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Origin Games");
            if (key == null)
                return new GameInfo[0];

            var subkeys = key.GetSubKeyNames();

            return subkeys.Select(gameID =>
            {
                using var subkey = key.OpenSubKey(gameID);
                if (subkey == null)
                    return null;

                var gameName = subkey.GetValue("DisplayName", null) as string;
                var exe = $"origin://launchgame/{gameID}";

                if (gameName == null || gameID == null || exe == null)
                    return null;

                return new GameInfo()
                {
                    ExecutableLocation = exe,
                    ExecutableArguments = new string[0],
                    Name = gameName
                };
            }).Where(i => i != null).Select(i => i!).ToArray();
#else
            return new GameInfo[0];
#endif
        }
    }
}
