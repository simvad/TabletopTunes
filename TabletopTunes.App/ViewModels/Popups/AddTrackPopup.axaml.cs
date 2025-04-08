using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TabletopTunes.App.Views.Popups
{
    public partial class AddTrackPopup : UserControl
    {
        public AddTrackPopup()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}