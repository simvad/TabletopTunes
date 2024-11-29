using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace ModernMusicPlayer
{
    public partial class MainWindow : ReactiveWindow<MainViewModel>
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private void ProgressSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "Value" && DataContext is MainViewModel vm && sender is Slider slider)
            {
                var value = slider.Value;
                if (!double.IsNaN(value))
                {
                    vm.SeekCommand.Execute(value);
                }
            }
        }

        private void VolumeSlider_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "Value" && DataContext is MainViewModel vm && sender is Slider slider)
            {
                var value = slider.Value;
                if (!double.IsNaN(value))
                {
                    vm.VolumeCommand.Execute(value);
                }
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}