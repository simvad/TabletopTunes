using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;  // Add this line
using Avalonia.Markup.Xaml;
using ModernMusicPlayer.Services;
using ModernMusicPlayer.Repositories;

namespace ModernMusicPlayer
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

        private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is MainViewModel viewModel)
            {
                await viewModel.StartRandomPlayback();
            }
        }

        private void ProgressSlider_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is Slider slider && 
                DataContext is MainViewModel viewModel && 
                viewModel.SeekCommand != null)
            {
                viewModel.SeekCommand.Execute(slider.Value);
            }
        }

        private void VolumeSlider_PropertyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is Slider slider && 
                DataContext is MainViewModel viewModel &&
                viewModel.VolumeCommand != null)
            {
                viewModel.VolumeCommand.Execute(slider.Value);
            }
        }
    }
}