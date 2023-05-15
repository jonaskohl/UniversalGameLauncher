using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Threading;

namespace UniversalGameLauncher
{
    public class GamesExplorerGamesCollector : IGameCollector
    {
        public GameInfo[] GetGames()
        {
            var games = new List<GameInfo>();

            try
            {
                var FOLDERID_GamesFolder = new Guid("{CAC52C1A-B53D-4edc-92D7-6B2E8AC19434}");
                var gamesFolder = KnownFolderHelper.FromKnownFolderId(FOLDERID_GamesFolder);

                foreach (var item in gamesFolder)
                {
                    var installLocation = item.Properties.GetProperty(new PropertyKey(new Guid("{841e4f90-ff59-4d16-8947-e81bbffab36d}"), 9))?.ValueAsObject?.ToString();
                    var displayName = item.Properties.GetProperty(new PropertyKey(new Guid("{b725f130-47ef-101a-a5f1-02608c9eebac}"), 10))?.ValueAsObject?.ToString();
                    var parsingPath = item.Properties.GetProperty(new PropertyKey(new Guid("{28636aa6-953d-11d2-b5d6-00c04fd918d0}"), 30))?.ValueAsObject?.ToString();

                    if (installLocation == @"C:\Program Files\Microsoft Games\More Games") continue;

                    games.Add(new GameInfo()
                    {
                        Name = displayName,
                        CoverImage = item.Thumbnail.ExtraLargeBitmapSource,
                        //GameSource = GameSourceUtils.GetOverlayIcon(GameSource.),
                        ExecutableLocation = "explorer",
                        ExecutableArguments = new[]
                            {
                            @"shell:" + parsingPath
                        },
                    });
                }
            }
            catch (ShellException) { }

            return games.ToArray();
        }
    }
}
