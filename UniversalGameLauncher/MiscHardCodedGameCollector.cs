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
