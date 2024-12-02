using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ModernMusicPlayer.Views.Popups
{
    public partial class SettingsPopup : UserControl
    {
        public SettingsPopup()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
