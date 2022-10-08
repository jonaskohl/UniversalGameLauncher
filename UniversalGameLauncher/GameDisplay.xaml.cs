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
    /// Interaktionslogik für GameDisplay.xaml
    /// </summary>
    public partial class GameDisplay : UserControl
    {
        public event RoutedEventHandler Click
        {
            add { RootButton.Click += value; }
            remove { RootButton.Click -= value; }
        }

        public static readonly DependencyProperty GameNameProperty = DependencyProperty.Register("GameName", typeof(string), typeof(GameDisplay));
        public static readonly DependencyProperty GameCoverProperty = DependencyProperty.Register("GameCover", typeof(ImageSource), typeof(GameDisplay));

        public string GameName
        {
            get => (string)GetValue(GameNameProperty);
            set => SetValue(GameNameProperty, value);
        }
        public ImageSource GameCover
        {
            get => (ImageSource)GetValue(GameCoverProperty);
            set => SetValue(GameCoverProperty, value);
        }

        public GameDisplay()
        {
            InitializeComponent();
        }
    }
}
