﻿using Microsoft.Win32;
using SixLabors.ImageSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace UniversalGameLauncher
{
    public class GogGamesCollector : IGameCollector
    {
        public GameInfo[] GetGames()
        {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\GOG.com\Games");
            if (key == null)
                return new GameInfo[0];

            var subkeys = key.GetSubKeyNames();

            return subkeys.Select(subkeyName =>
            {
                using var subkey = key.OpenSubKey(subkeyName);
                if (subkey == null)
                    return null;

                var gameName = subkey.GetValue("gameName", null) as string;
                var gameID = subkey.GetValue("gameID", null) as string;
                var exe = subkey.GetValue("exe", null) as string;

                if (gameName == null || gameID == null || exe == null)
                    return null;

                return new GameInfo()
                {
                    FetchImageSourceAction = (game, dispatcher, cacheManager) =>
                    {
                        string? coverImagePath = null;

                        var possibleCachedImagePath = @"C:\ProgramData\GOG.com\Galaxy\webcache\52727450581289995\gog\" + gameID + @"\";
                        if (Directory.Exists(possibleCachedImagePath))
                        {
                            var possibleImageFile = Directory.GetFiles(possibleCachedImagePath).Where(f => f.EndsWith("_glx_vertical_cover.webp")).FirstOrDefault();
                            if (possibleImageFile != null && File.Exists(possibleImageFile))
                            {
                                try
                                {
                                    var img = Image.Load(possibleImageFile);
                                    var temp = TempFileManager.GetTempFilename("tmp.jpg");
                                    img.Save(temp);
                                    coverImagePath = temp;
                                }
                                catch
                                {
                                    return;
                                }
                            }
                        }

                        if (coverImagePath != null)
                            dispatcher.Invoke(() =>
                            {
                                game.CoverImage = new BitmapImage(new Uri(coverImagePath));
                            });
                    },
                    ExecutableArguments = new string[0],
                    ExecutableLocation = exe,
                    Name = gameName
                };
            }).Where(i => i != null).Select(i => i!).ToArray();
        }
    }
}
