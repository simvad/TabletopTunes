using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TabletopTunes.App.Views.Popups
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