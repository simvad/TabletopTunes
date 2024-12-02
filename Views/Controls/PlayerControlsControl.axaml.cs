using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ModernMusicPlayer.ViewModels;

namespace ModernMusicPlayer.Views.Controls
{
    public partial class PlayerControlsControl : UserControl
    {
        public PlayerControlsControl()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void ProgressSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "Value" && DataContext is PlaybackViewModel viewModel)
            {
                if (sender is Slider slider)
                {
                    viewModel.SeekCommand?.Execute(slider.Value);
                }
            }
        }

        private void VolumeSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "Value" && DataContext is PlaybackViewModel viewModel)
            {
                if (sender is Slider slider)
                {
                    viewModel.VolumeCommand?.Execute(slider.Value);
                }
            }
        }
    }
}
