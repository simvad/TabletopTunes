using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TabletopTunes.Views.Controls
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
