using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace TabletopTunes.App.Views.Popups
{
    public partial class EditTagsPopup : UserControl
    {
        public EditTagsPopup()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}