using Gameloop.Vdf;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UniversalGameLauncher
{
    public class SteamGameCollector : IGameCollector
    {
        const string STEAM_LIB_INFO_PATH = @"C:\Program Files (x86)\Steam\steamapps\libraryfolders.vdf";

        static readonly List<string> IgnoreSteamIds = new()
        {
            "228980"
        };

        public GameInfo[] GetGames()
        {
            if (!File.Exists(STEAM_LIB_INFO_PATH))
            {
                Debug.WriteLine($"[WARN] File {STEAM_LIB_INFO_PATH} does not exist!");
                return new GameInfo[0];
            }

            dynamic volvo = VdfConvert.Deserialize(File.ReadAllText(STEAM_LIB_INFO_PATH));

            var libFoldersList = (IEnumerable<dynamic>)volvo.Value;
            //var libFolders = libFoldersList.Where(v => v.Value.totalsize.Value != "0" && Enumerable.ToArray<dynamic>(v.Value.apps).Length > 0).Select(v => v.Value.path.ToString()).ToArray();
            var libFolders = libFoldersList.Where(v => Enumerable.ToArray<dynamic>(v.Value.apps).Length > 0).Select(v => v.Value.path.ToString()).ToArray();

            var infos = libFolders.Select(f =>
            {
                Debug.WriteLine($"[LOG] {f}");

                var folder = Path.Combine(f, "steamapps");
                if (!Directory.Exists(folder))
                {
                    Debug.WriteLine($"[WARN] Folder {folder} does not exist!");
                    return null;
                }

                string[] appmanifestFiles = Directory.GetFiles(folder, "appmanifest_*.acf");

                if (appmanifestFiles.Length < 1)
                {
                    Debug.WriteLine($"[WARN] No appmanifest_*.acf files in {folder}!");
                    return null;
                }

                var appInfos = (IEnumerable<GameInfo?>)(appmanifestFiles
                    .Select(f => File.ReadAllText(f))
                    .Select(c => (VdfConvert.Deserialize(c) as dynamic).Value)
                    .Where(v => !IgnoreSteamIds.Contains(v.appid.Value.ToString()))
                    .Select(v => new SteamGameInfo()
                    {
                        SteamGameId = v.appid.Value.ToString(),
                        Name = v.name.Value,
                        ExecutableLocation = v.LauncherPath.Value,
                        ExecutableArguments = new string[] {
                            "-applaunch",
                            v.appid.Value.ToString()
                        }
                    }));

                return appInfos.ToArray();
            }).SelectMany(i => i).ToArray();
            return infos;
        }
    }
}
