using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using TabletopTunes.Services;
using TabletopTunes.Repositories;
using TabletopTunes.ViewModels;

namespace TabletopTunes
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is MainViewModel viewModel)
            {
                viewModel.SearchViewModel.UpdateDisplay();
            }
        }

        private void ProgressSlider_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is Slider slider && 
                DataContext is MainViewModel viewModel && 
                viewModel.PlaybackViewModel.SeekCommand != null)
            {
                viewModel.PlaybackViewModel.SeekCommand.Execute(slider.Value);
            }
        }

        private void VolumeSlider_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is Slider slider && 
                DataContext is MainViewModel viewModel &&
                viewModel.PlaybackViewModel.VolumeCommand != null)
            {
                viewModel.PlaybackViewModel.VolumeCommand.Execute(slider.Value);
            }
        }
    }
}
