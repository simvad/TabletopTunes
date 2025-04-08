using System;
using System.Collections.ObjectModel;
using System.Windows.Input;
using System.Threading.Tasks;
using TabletopTunes.App.Services.Audio;
using TabletopTunes.Core.Repositories;
using TabletopTunes.Core.Entities;
using TabletopTunes.Core.Services.Session;
using ReactiveUI;
using System.Reactive.Linq;
using System.Reactive.Disposables;

namespace TabletopTunes.App.ViewModels
{
    public class MainViewModel : ReactiveObject, IDisposable
    {
        private readonly CompositeDisposable _disposables = new();
        
        public PlaybackViewModel PlaybackViewModel { get; }
        public TrackManagementViewModel TrackManagementViewModel { get; }
        public TagManagementViewModel TagManagementViewModel { get; }
        public SearchViewModel SearchViewModel { get; }
        public ISessionService SessionService { get; }

        private bool _isSettingsOpen;
        public bool IsSettingsOpen
        {
            get => _isSettingsOpen;
            set => this.RaiseAndSetIfChanged(ref _isSettingsOpen, value);
        }

        private bool _isSessionPanelOpen;
        public bool IsSessionPanelOpen
        {
            get => _isSessionPanelOpen;
            set => this.RaiseAndSetIfChanged(ref _isSessionPanelOpen, value);
        }

        private bool _isAddTrackOpen;
        public bool IsAddTrackOpen
        {
            get => _isAddTrackOpen;
            set => this.RaiseAndSetIfChanged(ref _isAddTrackOpen, value);
        }

        private bool _isEditTagsOpen;
        public bool IsEditTagsOpen
        {
            get => _isEditTagsOpen;
            set => this.RaiseAndSetIfChanged(ref _isEditTagsOpen, value);
        }

        private string? _errorMessage;
        public string? ErrorMessage
        {
            get => _errorMessage;
            set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        public ICommand OpenAddTrackCommand { get; }
        public ICommand OpenSessionPanelCommand { get; }
        public ICommand ClearErrorCommand { get; }
        // Settings menu temporarily disabled
        private ICommand OpenSettingsCommand { get; }
        public ICommand PlayTrackCommand { get; }
        public ICommand EditTrackTagsCommand { get; }
        public ICommand? DeleteTrackCommand { get; private set; }
        public ICommand CloseAddTrackCommand { get; }
        public ICommand CloseEditTagsCommand { get; }
        public ICommand CloseSettingsCommand { get; }

        public MainViewModel(
            AudioPlayerService audioPlayer,
            ITrackRepository trackRepository,
            ITagRepository tagRepository,
            ISessionService sessionService)
        {
            SessionService = sessionService;

            // Initialize child view models
            TrackManagementViewModel = new TrackManagementViewModel(trackRepository, tagRepository, audioPlayer);
            TagManagementViewModel = new TagManagementViewModel(tagRepository, trackRepository);
            PlaybackViewModel = new PlaybackViewModel(audioPlayer, sessionService);
            SearchViewModel = new SearchViewModel(TrackManagementViewModel.AllTracks);

            // Subscribe to track management events
            TrackManagementViewModel.AddTrackCompleted += (s, e) => 
            {
                IsAddTrackOpen = false;
                SearchViewModel.UpdateDisplay(); // Explicitly update display after adding track
            };
            
            TrackManagementViewModel.TracksChanged += (s, e) => 
            {
                SearchViewModel.UpdateDisplay(); // Explicitly update display when tracks change
            };
            
            TagManagementViewModel.TagsChanged += (s, e) => 
            {
                SearchViewModel.UpdateDisplay(); // Explicitly update display when tags change
            };

            // Initialize commands
            OpenAddTrackCommand = ReactiveCommand.Create(() => IsAddTrackOpen = true);
            OpenSessionPanelCommand = ReactiveCommand.Create(() => IsSessionPanelOpen = true);
            ClearErrorCommand = ReactiveCommand.Create(() => ErrorMessage = null);
            OpenSettingsCommand = ReactiveCommand.Create(() => IsSettingsOpen = true);
            PlayTrackCommand = ReactiveCommand.CreateFromTask<TrackEntity>(async track => 
            {
                if (track != null)
                {
                    await PlaybackViewModel.PlayTrack(track);
                }
            });
            EditTrackTagsCommand = ReactiveCommand.Create<TrackEntity>(track => 
            {
                if (track != null)
                {
                    TagManagementViewModel.EditingTrack = track;
                    IsEditTagsOpen = true;
                }
            });
            DeleteTrackCommand = TrackManagementViewModel.DeleteTrackCommand;
            CloseAddTrackCommand = ReactiveCommand.Create(() => IsAddTrackOpen = false);
            CloseEditTagsCommand = ReactiveCommand.Create(() => IsEditTagsOpen = false);
            CloseSettingsCommand = ReactiveCommand.Create(() => IsSettingsOpen = false);

            SearchViewModel.FilteredTracksChanged += (s, filteredTracks) => 
            {
                PlaybackViewModel.UpdatePlayQueue(filteredTracks);
            };

            // Subscribe to session events
            SessionService.SessionEnded
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ =>
                {
                    if (IsSessionPanelOpen)
                    {
                        IsSessionPanelOpen = false;
                    }
                    ErrorMessage = "Session ended";
                })
                .DisposeWith(_disposables);

            // Monitor client join/leave for host
            SessionService.ClientJoined
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(clientId =>
                {
                    if (SessionService.IsHost)
                    {
                        ErrorMessage = $"Client {clientId} joined the session";
                    }
                })
                .DisposeWith(_disposables);

            SessionService.ClientLeft
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(clientId =>
                {
                    if (SessionService.IsHost)
                    {
                        ErrorMessage = $"Client {clientId} left the session";
                    }
                })
                .DisposeWith(_disposables);
        }

        public void Dispose()
        {
            // Cleanup session if active
            if (SessionService.IsConnected)
            {
                try
                {
                    Task.Run(async () => await SessionService.LeaveSession()).Wait();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error during session cleanup: {ex.Message}");
                }
            }

            PlaybackViewModel?.Dispose();
            _disposables.Dispose();
        }
    }
}