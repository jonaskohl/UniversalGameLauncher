using System.ComponentModel;
using System.Windows.Media;

namespace UniversalGameLauncher
{
    public class GameInfo : INotifyPropertyChanged
    {
        private string? _name;
        private string? _executableLocation;
        private string[]? _executableArguments;
        private ImageSource? _coverImage;

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

        public event PropertyChangedEventHandler? PropertyChanged;
    }
}