using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UniversalGameLauncher
{
    public static class TempFileManager
    {
        private static List<string> tempFiles = new();

        public static string GetTempFilename(string extension = "tmp")
        {
            var path = Path.Combine(Environment.GetEnvironmentVariable("temp") ?? "\\", "UGL-" + Guid.NewGuid().ToString("N") + "." + extension);
            tempFiles.Add(path);
            return path;
        }

        public static void DeleteTempFiles()
        {
            foreach (var file in tempFiles.ToArray())
            {
                try
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                        tempFiles.Remove(file);
                    }
                }
                catch {  }
            }
        }
    }
}
