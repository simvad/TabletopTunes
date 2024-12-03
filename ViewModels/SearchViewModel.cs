using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ModernMusicPlayer.Entities;
using ModernMusicPlayer.Query;
using ReactiveUI;

namespace ModernMusicPlayer.ViewModels
{
    public class SearchViewModel : ReactiveObject
    {
        private string _searchQuery = "";
        public string SearchQuery
        {
            get => _searchQuery;
            set
            {
                this.RaiseAndSetIfChanged(ref _searchQuery, value);
                UpdateFilteredTracks();
            }
        }

        private ObservableCollection<TrackEntity> _displayedTracks;
        public ObservableCollection<TrackEntity> DisplayedTracks
        {
            get => _displayedTracks;
            private set => this.RaiseAndSetIfChanged(ref _displayedTracks, value);
        }

        private readonly ObservableCollection<TrackEntity> _allTracks;

        public event EventHandler<IQueryable<TrackEntity>>? FilteredTracksChanged;

        public SearchViewModel(ObservableCollection<TrackEntity> allTracks)
        {
            _allTracks = allTracks;
            _displayedTracks = new ObservableCollection<TrackEntity>();
            
            // Initialize displayed tracks with all tracks
            foreach (var track in _allTracks)
            {
                _displayedTracks.Add(track);
            }

            // Subscribe to collection changes
            _allTracks.CollectionChanged += (s, e) =>
            {
                UpdateFilteredTracks();
            };
        }

        private void UpdateFilteredTracks()
        {
            IEnumerable<TrackEntity> filtered = _allTracks;

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                filtered = filtered.AsQueryable().ApplyQuery(SearchQuery);
            }

            // Clear and repopulate the existing collection instead of creating a new one
            _displayedTracks.Clear();
            foreach (var track in filtered)
            {
                _displayedTracks.Add(track);
            }

            FilteredTracksChanged?.Invoke(this, filtered.AsQueryable());
        }

        public void UpdateDisplay()
        {
            UpdateFilteredTracks();
        }
    }
}
