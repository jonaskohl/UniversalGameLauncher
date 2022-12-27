using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalGameLauncher
{
    public class IniFile
    {
        Dictionary<string, Dictionary<string, string>> file = new();

        public static IniFile Load(string file)
        {
            var instance = new IniFile();
            instance.ParseInternal(File.ReadAllText(file));
            return instance;
        }

        public static IniFile Parse(string content)
        {
            var instance = new IniFile();
            instance.ParseInternal(content);
            return instance;
        }

        private IniFile() { }

        private void ParseInternal(string content)
        {
            content = content
                .Replace("\r\n", "\n")
                .Replace("\n\r", "\n")
                .Replace("\r", "\n");

            var lines = content.Split("\n");
            file.Clear();
            string? currentSection = null;

            foreach (var _line in lines)
            {
                var line = _line.Trim();

                if (line.Length < 1) continue;
                if (line.StartsWith(";")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    currentSection = line.Substring(1, line.Length - 2);
                } else if (line.Contains("="))
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length != 2)
                        throw new FormatException("Invalid INI file");

                    var key = parts[0];
                    var value = parts[1];

                    if (!file.ContainsKey(currentSection))
                        file[currentSection] = new();

                    file[currentSection][key] = value;
                } else
                {
                    throw new FormatException("Invalid INI file");
                }
            }
        }

        public string? GetValue(string? section, string key)
        {
            if (!file.ContainsKey(section)) return null;
            if (!file[section].ContainsKey(key)) return null;
            return file[section][key];
        }
    }
}
