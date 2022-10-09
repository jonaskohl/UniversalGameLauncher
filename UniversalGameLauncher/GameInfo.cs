using System;
using System.ComponentModel;
using System.Windows.Media;
using System.Windows.Threading;

namespace UniversalGameLauncher
{
    public class GameInfo : INotifyPropertyChanged
    {
        private string? _name;
        private string? _executableLocation;
        private string[]? _executableArguments;
        private ImageSource? _coverImage;
        private ImageSource? _gameSource;

        internal Action<GameInfo, Dispatcher, CacheManager>? FetchImageSourceAction;

        public string? Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Name)));
            }
        }

        public string? ExecutableLocation
        {
            get
            {
                return _executableLocation;
            }
            set
            {
                _executableLocation = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExecutableLocation)));
            }
        }

        public string[]? ExecutableArguments
        {
            get
            {
                return _executableArguments;
            }
            set
            {
                _executableArguments = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ExecutableArguments)));
            }
        }

        public ImageSource? CoverImage
        {
            get
            {
                return _coverImage;
            }
            set
            {
                _coverImage = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CoverImage)));
            }
        }

        public ImageSource? GameSource
        {
            get
            {
                return _gameSource;
            }
            set
            {
                _gameSource = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(GameSource)));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}