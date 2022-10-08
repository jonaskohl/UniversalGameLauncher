using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace UniversalGameLauncher
{
    public class MiscHardCodedGameCollector : IGameCollector
    {
        struct AppInfo
        {
            public string name;
            public string parsingName;
            public ImageSource icon;
        }

        //private GameInfo? TryFindMinecraftJavaEdition()
        //{

        //}

        private AppInfo[] GetApps()
        {
            var FODLERID_AppsFolder = new Guid("{1e87508d-89c2-42f0-8a7e-645a0f50ca58}");
            ShellObject appsFolder = (ShellObject)KnownFolderHelper.FromKnownFolderId(FODLERID_AppsFolder);
            var folder = (IKnownFolder)appsFolder;
            var count = folder.Count();
            var infos = new AppInfo[count];

            for (var i = 0; i < count; ++i)
            {
                var app = folder.ElementAt(i);
                infos[i] = new AppInfo()
                {
                    name = app.Name,
                    parsingName = app.ParsingName,
                    icon = app.Thumbnail.MediumBitmapSource
                };
            }

            return infos;
        }
        private AppInfo? TryGetApp(string name)
        {
            try
            {
                var shellFile = ShellObject.FromParsingName(@"shell:appsFolder\" + name);
                return new AppInfo()
                {
                    name = shellFile.Name,
                    parsingName = shellFile.ParsingName,
                    icon = shellFile.Thumbnail.ExtraLargeBitmapSource
                };
            }
            catch (ShellException)
            {
                return null;
            }
        }

        private GameInfo? TryFindUWPGame(string appId)
        {
            var info = TryGetApp(appId);
            if (info is null)
                return null;

            return new GameInfo()
            {
                CoverImage = info.Value.icon,
                ExecutableLocation = "explorer",
                ExecutableArguments = new[]
                {
                    @"shell:appsFolder\" + appId
                },
                Name = info.Value.name
            };
        }

        public GameInfo[] GetGames()
        {
            return new GameInfo?[]
            {
                TryFindUWPGame(@"Microsoft.MinecraftUWP_8wekyb3d8bbwe!App"),
                TryFindUWPGame(@"GAMELOFTSA.DespicableMeMinionRush_0pp20fcewvvtj!App"),
                TryFindUWPGame(@"{7C5A40EF-A0FB-4BFC-874A-C0F2E0B9FA8E}\Minecraft Launcher\MinecraftLauncher.exe"),
            }.Where(i => i is not null).Select(i => i!).ToArray();
        }
    }
}
