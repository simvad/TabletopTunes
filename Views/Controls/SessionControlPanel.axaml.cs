using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Concurrency;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.ReactiveUI;
using Avalonia.VisualTree;
using ModernMusicPlayer.Services;
using ModernMusicPlayer.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ModernMusicPlayer.Views.Controls
{
    public partial class SessionControlPanel : UserControl
    {
        private readonly ISessionService _sessionService;
        private TextBlock? _statusText;
        private TextBox? _sessionCodeText;
        private StackPanel? _sessionCodePanel;
        private StackPanel? _connectionControls;
        private StackPanel? _clientsList;
        private ItemsControl? _clientsItemsControl;
        private Button? _hostButton;
        private Button? _joinButton;
        private Button? _closeButton;
        private TextBox? _joinCodeInput;
        private ObservableCollection<string> _connectedClients;
        private IDisposable? _clientJoinedSubscription;
        private IDisposable? _clientLeftSubscription;
        private IDisposable? _sessionEndedSubscription;

        public SessionControlPanel()
        {
            InitializeComponent();
            _sessionService = Program.ServiceProvider?.GetService<ISessionService>() 
                ?? throw new InvalidOperationException("SessionService not found in DI container");

            _connectedClients = new ObservableCollection<string>();
            SetupControls();
            SetupEventHandlers();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            _statusText = this.FindControl<TextBlock>("StatusText");
            _sessionCodeText = this.FindControl<TextBox>("SessionCodeText");
            _sessionCodePanel = this.FindControl<StackPanel>("SessionCodePanel");
            _connectionControls = this.FindControl<StackPanel>("ConnectionControls");
            _clientsList = this.FindControl<StackPanel>("ClientsList");
            _clientsItemsControl = this.FindControl<ItemsControl>("ClientsItemsControl");
            _hostButton = this.FindControl<Button>("HostButton");
            _joinButton = this.FindControl<Button>("JoinButton");
            _closeButton = this.FindControl<Button>("CloseButton");
            _joinCodeInput = this.FindControl<TextBox>("JoinCodeInput");

            if (_statusText == null || _sessionCodeText == null || _sessionCodePanel == null || 
                _connectionControls == null || _clientsList == null || _clientsItemsControl == null || 
                _hostButton == null || _joinButton == null || _closeButton == null || _joinCodeInput == null)
            {
                throw new InvalidOperationException("Failed to initialize required controls");
            }
        }

        private void SetupControls()
        {
            _clientsItemsControl!.ItemsSource = _connectedClients;
            UpdateConnectionStatus(false);
        }

        private void SetupEventHandlers()
        {
            _hostButton!.Click += async (s, e) => await HostSession();
            _joinButton!.Click += async (s, e) => await JoinSession();
            _closeButton!.Click += (s, e) => ClosePanel();

            var uiScheduler = AvaloniaScheduler.Instance;

            _clientJoinedSubscription = _sessionService.ClientJoined
                .ObserveOn(uiScheduler)
                .Subscribe(clientId =>
                {
                    _connectedClients.Add(clientId);
                });

            _clientLeftSubscription = _sessionService.ClientLeft
                .ObserveOn(uiScheduler)
                .Subscribe(clientId =>
                {
                    _connectedClients.Remove(clientId);
                });

            _sessionEndedSubscription = _sessionService.SessionEnded
                .ObserveOn(uiScheduler)
                .Subscribe(_ =>
                {
                    ResetSessionState();
                });
        }

        private void ClosePanel()
        {
            if (this.Parent is Grid grid)
            {
                grid.IsVisible = false;
            }
            
            var mainViewModel = this.DataContext as MainViewModel;
            if (mainViewModel != null)
            {
                mainViewModel.IsSessionPanelOpen = false;
            }
        }

        private async Task HostSession()
        {
            try
            {
                _hostButton!.IsEnabled = false;
                var success = await _sessionService.StartHosting();
                
                if (success)
                {
                    _connectionControls!.IsVisible = false;
                    _clientsList!.IsVisible = true;
                    UpdateConnectionStatus(true, true);
                    
                    if (_sessionService.SessionCode != null)
                    {
                        _sessionCodeText!.Text = _sessionService.SessionCode;
                        _sessionCodePanel!.IsVisible = true;
                    }
                }
                else
                {
                    await ShowError("Failed to start hosting session");
                    _hostButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await ShowError($"Failed to host session: {ex.Message}");
                _hostButton!.IsEnabled = true;
            }
        }

        private async Task JoinSession()
        {
            try
            {
                string sessionCode = _joinCodeInput!.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(sessionCode))
                {
                    await ShowError("Please enter a session code");
                    return;
                }

                _joinButton!.IsEnabled = false;
                var success = await _sessionService.JoinSession(sessionCode);
                
                if (success)
                {
                    _connectionControls!.IsVisible = false;
                    _clientsList!.IsVisible = true;
                    UpdateConnectionStatus(true, false);
                }
                else
                {
                    await ShowError("Failed to join session. Invalid code or session not found.");
                    _joinButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await ShowError($"Failed to join session: {ex.Message}");
                _joinButton!.IsEnabled = true;
            }
        }

        private void UpdateConnectionStatus(bool connected, bool isHost = false)
        {
            _statusText!.Text = connected
                ? isHost ? "Hosting Session" : "Connected to Session"
                : "Not Connected";
        }

        private void ResetSessionState()
        {
            _connectedClients.Clear();
            _connectionControls!.IsVisible = true;
            _clientsList!.IsVisible = false;
            _sessionCodePanel!.IsVisible = false;
            _hostButton!.IsEnabled = true;
            _joinButton!.IsEnabled = true;
            UpdateConnectionStatus(false);
        }

        private async Task ShowError(string message)
        {
            var stackPanel = new StackPanel
            {
                Spacing = 10,
                Margin = new Thickness(20)
            };

            var textBox = new TextBox
            {
                Text = message,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                IsReadOnly = true,
                AcceptsReturn = true,
                BorderThickness = new Thickness(0)
            };

            var closeButton = new Button
            {
                Content = "Close",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                Width = 100
            };

            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(closeButton);

            var window = new Window
            {
                Title = "Error",
                Width = 300,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = stackPanel,
                CanResize = false,
                ShowInTaskbar = false
            };

            closeButton.Click += (s, e) => window.Close();
            window.KeyDown += (s, e) => 
            {
                if (e.Key == Avalonia.Input.Key.Escape)
                    window.Close();
            };

            var topLevel = TopLevel.GetTopLevel(this) as Window;
            if (topLevel != null)
            {
                await window.ShowDialog(topLevel);
            }
        }

        public void Dispose()
        {
            _clientJoinedSubscription?.Dispose();
            _clientLeftSubscription?.Dispose();
            _sessionEndedSubscription?.Dispose();
        }
    }
}
