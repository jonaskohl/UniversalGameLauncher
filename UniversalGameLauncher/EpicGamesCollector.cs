using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using System.Windows.Media.Imaging;
using Flurl;
using Flurl.Http;
using System.Web;
using System.Windows;
using System.Globalization;

namespace UniversalGameLauncher
{
    public class EpicGamesCollector : IGameCollector
    {
        public GameInfo[] GetGames()
        {
            var iniPath = Environment.ExpandEnvironmentVariables(@"%localappdata%\EpicGamesLauncher\Saved\Config\Windows\GameUserSettings.ini");

            if (!File.Exists(iniPath))
                return new GameInfo[0];

            IniFile iniFile;

            try
            {
                iniFile = IniFile.Load(iniPath);
            } catch (FormatException)
            {
                return new GameInfo[0];
            }

            var location = iniFile.GetValue("Launcher", "DefaultAppInstallLocation");

            if (location == null)
                return new GameInfo[0];

            if (!Directory.Exists(location))
                return new GameInfo[0];

            var subFolders = Directory.GetDirectories(location);

            return subFolders.Select(gameFolder =>
            {
                var exeFiles = Directory.GetFiles(gameFolder, "*.exe");
                if (exeFiles.Length < 1)
                    return null;

                var egstoreDir = Path.Combine(gameFolder, ".egstore");

                if (!Directory.Exists(egstoreDir))
                    return null;

                var metaFiles = Directory.GetFiles(egstoreDir, "*.mancpn");

                if (metaFiles.Length < 1)
                    return null;

                var metaFilePath = metaFiles[0];
                JObject? metaFile;

                try
                {
                    metaFile = JObject.Parse(File.ReadAllText(metaFilePath));
                } catch (JsonReaderException)
                {
                    return null;
                }

                var cns = metaFile["CatalogNamespace"]?.Value<string>();
                var cid = metaFile["CatalogItemId"]?.Value<string>();
                var an = metaFile["AppName"]?.Value<string>();

                if (cns == null || cid == null || an == null)
                    return null;

                var prodName = FileVersionInfo.GetVersionInfo(exeFiles[0]).ProductName;
                if (string.IsNullOrEmpty(prodName))
                    prodName = Path.GetFileNameWithoutExtension(exeFiles[0]);

                return new GameInfo()
                {
                    ExecutableLocation = "explorer",
                    ExecutableArguments = new[] {
                        "com.epicgames.launcher://apps/" + HttpUtility.UrlEncode(string.Join(":", new[]
                        {
                            cns, cid, an
                        })) + "?action=launch&silent=true"
                    },
                    Name = prodName,
                    GameSource = GameSourceUtils.GetOverlayIcon(GameSource.EpicGames),
                    FetchImageSourceAction = async (game, dispatcher, cacheManager) =>
                    {
                        var slug = await EpicGamesCommons.GetSlugFromNamespace(cacheManager, cns);

                        var url = "https://store-content.ak.epicgames.com/api/"
                            .AppendPathSegment(CultureInfo.CurrentCulture.LCID)
                            .AppendPathSegment("content")
                            .AppendPathSegment("products")
                            .AppendPathSegment(slug);

                        var dataBytes = await cacheManager.GetAsync(url);

                        if (dataBytes == null)
                            return;

                        var dataStr = Encoding.UTF8.GetString(dataBytes);
                        var data = JObject.Parse(dataStr);

                        var name = data["productName"].Value<string>();

                        var pages = data["pages"];
                        var imageListPage = pages.Where(p => p["_images_"] != null && p["_images_"].Where(i => i.Value<string>().Contains("1200x1600")).Count() > 0).First();
                        var imageUrl = imageListPage["_images_"].Where(i => i.Value<string>().Contains("1200x1600")).First().Value<string>();
                        
                        var coverFile = await cacheManager.GetCacheFileNameAsync(imageUrl);
                        if (coverFile == null)
                        {
                            return;
                        }
                        dispatcher.Invoke(() =>
                        {
                            try
                            {
                                game.CoverImage = new BitmapImage(new Uri(coverFile));
                            } catch (FileNotFoundException) { }
                            game.Name = name;
                        });

                    },
                };
            }).Where(i => i != null).Select(i => i!).ToArray();
        }
    }
}
