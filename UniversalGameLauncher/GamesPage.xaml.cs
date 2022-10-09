using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace UniversalGameLauncher
{
    /// <summary>
    /// Interaktionslogik für GamesPage.xaml
    /// </summary>
    public partial class GamesPage : DisplayPage
    {
        //public static readonly DependencyProperty GamesProperty = DependencyProperty.Register("Games", typeof(GameInfo[]), typeof(MainWindow));

        //public GameInfo[] Games
        //{
        //    get => (GameInfo[])GetValue(GamesProperty);
        //    set => SetValue(GamesProperty, value);
        //}

        public GamesPage()
        {
            InitializeComponent();
        }

        private void GameClicked(object sender, RoutedEventArgs e)
        {
            OnEventOccurred(new DisplayPageEventArgs()
            {
                EventArgs = e,
                EventName = nameof(GameClicked),
                EventSource = sender
            });
        }
    }
}
