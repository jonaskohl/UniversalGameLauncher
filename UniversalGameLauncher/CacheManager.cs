using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace UniversalGameLauncher
{
    public class CacheManager
    {
        public string CachePath { get; init; }
        public string CacheInfoPath { get; init; }

        //private static CacheManager? _instance = null;
        //private static object syncRoot = new();

        //public static CacheManager Instance
        //{
        //    get
        //    {
        //        if (_instance == null)
        //        {
        //            lock (syncRoot)
        //            {
        //                if (_instance == null)
        //                    _instance = new();
        //            }
        //        }

        //        return _instance;
        //    }
        //}

        struct CacheInfo
        {
            public DateTime LastChanged;
            public TimeSpan MaxCacheDuration;
            public string CacheFilename;
            public string OriginalUrl;

            public XElement ToXML()
            {
                return new XElement(
                    "CacheInfo",
                    new XAttribute("LastChanged", LastChanged.ToBinary().ToString("X16")),
                    new XAttribute("MaxCacheDuration", MaxCacheDuration.TotalSeconds.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("CacheFilename", CacheFilename),
                    new XAttribute("OriginalUrl", OriginalUrl)
                );
            }

            public override string ToString()
            {
                return ToXML().ToString();
            }

            public static CacheInfo Parse(string cacheInfoString)
            {
                return Parse(XDocument.Parse($"<A>{cacheInfoString}</A>").Root!.Element("CacheInfo")!);
            }

            public static CacheInfo Parse(XElement xml)
            {
                var instance = new CacheInfo();
                instance.LastChanged = DateTime.FromBinary(Convert.ToInt64(xml.Attribute("LastChanged")!.Value, 16));
                instance.MaxCacheDuration = TimeSpan.FromSeconds(Convert.ToDouble(xml.Attribute("MaxCacheDuration")!.Value));
                instance.CacheFilename = xml.Attribute("CacheFilename")!.Value;
                instance.OriginalUrl = xml.Attribute("OriginalUrl")!.Value;
                return instance;
            }
        }

        Dictionary<string, CacheInfo> cacheDb = new();

        public void TryLoad()
        {
            if (!File.Exists(CacheInfoPath))
                return;

            var doc = XDocument.Load(CacheInfoPath);
            if (doc.Root?.Name != "CacheState")
                return;

            var elements = doc.Root?.Elements("CacheEntry").Select(xel => new KeyValuePair<string, CacheInfo>(
                xel.Attribute("Key")!.Value,
                CacheInfo.Parse(xel.Element("CacheInfo")!)
            ));
            cacheDb = elements!.ToDictionary(i => i.Key, i => i.Value)!;
        }

        public void Save()
        {
            new XDocument(
                new XElement(
                    "CacheState",
                    new XAttribute("LastSaved", DateTime.Now.ToBinary().ToString("X16")),
                    cacheDb.Select(kvp => new XElement("CacheEntry",
                        new XAttribute("Key", kvp.Key),
                        kvp.Value.ToXML()
                    ))
                )
            ).Save(CacheInfoPath);
        }

        public byte[]? Get(string urlOrPath)
        {
            var key = GetCacheKey(urlOrPath);
            if (IsCachedAndNotExpired(key))
            {
                var data = GetCachedVersion(key);
                if (data != null)
                    return data;
            }
            return RetrieveNewAndCache(urlOrPath, key);
        }

        public async Task<byte[]?> GetAsync(string urlOrPath)
        {
            var key = GetCacheKey(urlOrPath);
            if (IsCachedAndNotExpired(key))
            {
                var data = GetCachedVersion(key);
                if (data != null)
                    return data;
            }
            return await RetrieveNewAndCacheAsync(urlOrPath, key);
        }

        public string? GetCacheFileName(string urlOrPath)
        {
            var key = GetCacheKey(urlOrPath);
            if (!IsCachedAndNotExpired(key))
                if (RetrieveNewAndCache(urlOrPath, key) == null)
                    return null;
            if (!cacheDb.ContainsKey(key))
                return null;
            return cacheDb[key].CacheFilename;
        }
        public async Task<string?> GetCacheFileNameAsync(string urlOrPath)
        {
            var key = GetCacheKey(urlOrPath);
            if (!IsCachedAndNotExpired(key))
                if (await RetrieveNewAndCacheAsync(urlOrPath, key) == null)
                    return null;
            if (!cacheDb.ContainsKey(key))
                return null;
            return cacheDb[key].CacheFilename;
        }

        private string GetCacheKey(string urlOrPath)
        {
            return KnuthHash(urlOrPath).ToString("X16");
        }

        private bool IsCachedAndNotExpired(string key)
        {
            if (!cacheDb.ContainsKey(key))
                return false;

            var info = cacheDb[key];

            return (info.LastChanged + info.MaxCacheDuration) > DateTime.Now;
        }

        private byte[]? GetCachedVersion(string key)
        {
            var info = cacheDb[key];
            try
            {
                return File.ReadAllBytes(info.CacheFilename);
            }
            catch (FileNotFoundException)
            {
                return null;
            }
        }

        private byte[]? RetrieveNewAndCache(string urlOrPath, string cacheKey)
        {
            var data = GetOriginal(urlOrPath);

            if (data == null)
                return null;

            var cacheFile = GetCacheFile(cacheKey);

            File.WriteAllBytes(cacheFile, data);

            cacheDb[cacheKey] = new CacheInfo()
            {
                LastChanged = DateTime.Now,
                MaxCacheDuration = TimeSpan.FromHours(2),
                CacheFilename = cacheFile,
                OriginalUrl = urlOrPath
            };

            return data;
        }

        private async Task<byte[]?> RetrieveNewAndCacheAsync(string urlOrPath, string cacheKey)
        {
            var data = await GetOriginalAsync(urlOrPath);

            if (data == null)
                return null;

            var cacheFile = GetCacheFile(cacheKey);

            await File.WriteAllBytesAsync(cacheFile, data);

            cacheDb[cacheKey] = new CacheInfo()
            {
                LastChanged = DateTime.Now,
                MaxCacheDuration = TimeSpan.FromHours(2),
                CacheFilename = cacheFile,
                OriginalUrl = urlOrPath
            };

            return data;
        }

        private string GetCacheFile(string cacheKey)
        {
            if (!Directory.Exists(CachePath))
                Directory.CreateDirectory(CachePath);
            return Path.Combine(CachePath, cacheKey);
        }

        private async Task<byte[]?> DownloadBytes(string urlOrPath)
        {
            using var hc = new HttpClient();
            try
            {
                return await hc.GetByteArrayAsync(urlOrPath);
            }
            catch (HttpRequestException)
            {
                return null;
            }
        }

        private byte[]? GetOriginal(string originalUrlOrPath)
        {
            byte[]? data;
            if (new Uri(originalUrlOrPath).IsFile)
                data = File.ReadAllBytes(originalUrlOrPath);
            else
                data = DownloadBytes(originalUrlOrPath).Result;
            return data;
        }

        private async Task<byte[]?> GetOriginalAsync(string originalUrlOrPath)
        {
            byte[]? data;
            if (new Uri(originalUrlOrPath).IsFile)
                data = File.ReadAllBytes(originalUrlOrPath);
            else
                data = await DownloadBytes(originalUrlOrPath);
            return data;
        }

        private static ulong KnuthHash(string read)
        {
            var hashedValue = 3074457345618258791ul;
            for (int i = 0; i < read.Length; i++)
            {
                hashedValue += read[i];
                hashedValue *= 3074457345618258799ul;
            }
            return hashedValue;
        }

        //[EditorBrowsable(EditorBrowsableState.Never)]
        //internal static void __setInstance(CacheManager instance)
        //{
        //    _instance = instance;
        //}
    }
}
