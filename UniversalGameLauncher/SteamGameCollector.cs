using Gameloop.Vdf;
using Gameloop.Vdf.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

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
            var libFolders = libFoldersList
                .Where(v => DoesPropertyExist(v.Value, "apps") && Enumerable.ToArray<dynamic>(v.Value.apps).Length > 0)
                .Select(v => v.Value.path.ToString())
                .ToArray();

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
                    .Select(v => new GameInfo()
                    {
                        GameSource = GameSourceUtils.GetOverlayIcon(GameSource.Steam),
                        FetchImageSourceAction = (game, innerDispatcher, cacheManager) =>
                        {
                            var appId = v.appid.Value.ToString();
                            var coverUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/library_600x900.jpg";
                            Debug.WriteLine($"Cover url: {coverUrl}");
                            var coverFile = cacheManager.GetCacheFileName(coverUrl);
                            if (coverFile == null)
                            {
                                coverUrl = $"https://steamcdn-a.akamaihd.net/steam/apps/{appId}/header.jpg";
                                coverFile = cacheManager.GetCacheFileName(coverUrl);
                                if (coverFile == null)
                                    return;
                            }
                            //cacheManager.Save();
                            innerDispatcher.Invoke(() =>
                            {
                                try
                                {
                                    game.CoverImage = new BitmapImage(new Uri(coverFile));
                                }
                                catch (FileNotFoundException) { }
                            });
                        },
                        Name = v.name.Value,
                        ExecutableLocation = v.LauncherPath.Value,
                        ExecutableArguments = new string[] {
                            "-applaunch",
                            v.appid.Value.ToString()
                        }
                    }));

                return appInfos.Where(i => i is not null).Select(i => i!).ToArray();
            }).SelectMany(i => i!).ToArray();
            return infos;
        }

        public static bool DoesPropertyExist(dynamic settings, string name)
        {
            string fn = settings.GetType().FullName;
            Debug.WriteLine("[DEBUG] {DoesPropertyExist} " + fn);

            if (settings is VObject)
                return ((IDictionary<string, VToken>)settings).ContainsKey(name);
            else if (settings is ExpandoObject)
                return ((IDictionary<string, object>)settings).ContainsKey(name);

            return settings.GetType().GetProperty(name) != null;
        }
    }
}
