using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using TabletopTunes.App.ViewModels;

namespace TabletopTunes.App.Views.Controls
{
    public partial class SearchBarControl : UserControl
    {
        public SearchBarControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is SearchViewModel viewModel)
            {
                viewModel.UpdateDisplay();
            }
        }
    }
}