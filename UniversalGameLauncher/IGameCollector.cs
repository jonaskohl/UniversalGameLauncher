using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace UniversalGameLauncher
{
    public interface IGameCollector
    {
        public GameInfo[] GetGames();
    }
}
