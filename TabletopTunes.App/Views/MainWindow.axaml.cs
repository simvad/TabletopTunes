using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using TabletopTunes.App.ViewModels;
using TabletopTunes.App.Services.Audio;
using TabletopTunes.Core.Repositories;
using TabletopTunes.Core.Services.Session;
using Microsoft.Extensions.DependencyInjection;

namespace TabletopTunes.App.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = App.ServiceProvider!.GetRequiredService<MainViewModel>();
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

        private void ProgressSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (sender is Slider slider && 
                DataContext is MainViewModel viewModel && 
                viewModel.PlaybackViewModel.SeekCommand != null)
            {
                viewModel.PlaybackViewModel.SeekCommand.Execute(slider.Value);
            }
        }

        private void VolumeSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
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