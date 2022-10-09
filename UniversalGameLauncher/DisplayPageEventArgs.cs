using System;
using System.Windows;

namespace UniversalGameLauncher
{
    public class DisplayPageEventArgs : EventArgs
    {
        public string EventName;
        public RoutedEventArgs EventArgs;
        public object? EventSource;
    }
}