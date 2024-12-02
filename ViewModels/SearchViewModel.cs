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

        private ReadOnlyObservableCollection<TrackEntity> _displayedTracks;
        public ReadOnlyObservableCollection<TrackEntity> DisplayedTracks => _displayedTracks;

        private readonly ObservableCollection<TrackEntity> _allTracks;

        public event EventHandler<IQueryable<TrackEntity>>? FilteredTracksChanged;

        public SearchViewModel(ObservableCollection<TrackEntity> allTracks)
        {
            _allTracks = allTracks;
            _displayedTracks = new ReadOnlyObservableCollection<TrackEntity>(
                new ObservableCollection<TrackEntity>(allTracks)
            );
        }

        private void UpdateFilteredTracks()
        {
            IEnumerable<TrackEntity> filtered = _allTracks;

            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                filtered = filtered.AsQueryable().ApplyQuery(SearchQuery);
            }

            _displayedTracks = new ReadOnlyObservableCollection<TrackEntity>(
                new ObservableCollection<TrackEntity>(filtered)
            );
            
            this.RaisePropertyChanged(nameof(DisplayedTracks));
            FilteredTracksChanged?.Invoke(this, filtered.AsQueryable());
        }

        public void RefreshDisplayedTracks()
        {
            UpdateFilteredTracks();
        }
    }
}
