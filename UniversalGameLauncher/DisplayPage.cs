using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace UniversalGameLauncher
{
    public class DisplayPage : UserControl
    {
        public event EventHandler<DisplayPageEventArgs> EventOccurred;

        protected void OnEventOccurred(DisplayPageEventArgs e)
        {
            EventOccurred?.Invoke(this, e);
        }
    }
}
