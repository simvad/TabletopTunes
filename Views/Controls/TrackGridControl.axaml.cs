using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModernMusicPlayer.Views.Controls
{
    public partial class TrackGridControl : UserControl
    {
        public TrackGridControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
