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
    public class XBOXGamesCollector : IGameCollector
    {
        public GameInfo[] GetGames()
        {
            var games = new List<GameInfo>();

            try
            {
                var FOLDERID_AppsFolder = new Guid("{1e87508d-89c2-42f0-8a7e-645a0f50ca58}");
                var appsFolder = (ShellObject)KnownFolderHelper.FromKnownFolderId(FOLDERID_AppsFolder);

                var key = new PropertyKey("9f4c2855-9f79-4b39-a8d0-e1d42de1d5f3", 15); // System.AppUserModel.PackageInstallPath

                foreach (var app in (IKnownFolder)appsFolder)
                {
                    var prop = app.Properties.GetProperty(key);
                    if (prop == null || prop.ValueAsObject == null) continue;

                    var appDir = prop.ValueAsObject.ToString();

                    if (appDir == null) continue;

                    var xboxPath = Path.Combine(appDir, "xboxservices.config");

                    if (!File.Exists(xboxPath)) continue;

                    games.Add(new GameInfo()
                    {
                        Name = app.Name,
                        CoverImage = app.Thumbnail.ExtraLargeBitmapSource,
                        GameSource = GameSourceUtils.GetOverlayIcon(GameSource.XBOX),
                        ExecutableLocation = "explorer",
                        ExecutableArguments = new[]
                            {
                            @"shell:appsFolder\" + app.ParsingName
                        },
                    });
                }
            }
            catch (ShellException) { }

            return games.ToArray();
        }
    }
}
