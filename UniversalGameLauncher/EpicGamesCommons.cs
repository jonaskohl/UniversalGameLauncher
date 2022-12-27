using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;
using Flurl.Util;
using Newtonsoft.Json.Linq;

namespace UniversalGameLauncher
{
    public static class EpicGamesCommons
    {
        private static Dictionary<string, string>? _lookup = null;

        private static async Task DownloadLookup(CacheManager cmgr)
        {
            var data = await cmgr.GetAsync("https://store-content.ak.epicgames.com/api/content/productmapping");

            if (data == null)
            {
                _lookup = new();
                return;
            }

            cmgr.Save();

            var lookupStr = Encoding.UTF8.GetString(data);

            _lookup = JObject.Parse(lookupStr).ToObject<Dictionary<string, string>>();
            
        }

        public static async Task<string?> GetSlugFromNamespace(CacheManager cmgr, string cns)
        {
            if (_lookup == null)
                await DownloadLookup(cmgr);

            if (_lookup?.ContainsKey(cns) != true)
                return null;

            return _lookup[cns];
        }
    }
}
