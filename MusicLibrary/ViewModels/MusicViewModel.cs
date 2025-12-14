using MusicLibrary.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading.Tasks;
using MusicLibrary.Commands;

namespace MusicLibrary.ViewModels
{
    public class MusicViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName) 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly MusicService _service = new();

        public ObservableCollection<Playlist> Playlists { get; } = new();
        public ObservableCollection<Track> Tracks { get; } = new();
        public ObservableCollection<Track> LibraryTracks { get; } = new();
        public RelayCommand CreatePlaylistCommand { get; }
        public RelayCommand AddTrackToPlaylistCommand { get; }
        public RelayCommand UpdatePlaylistCommand { get; }
        public RelayCommand RemoveTrackFromPlaylistCommand { get; }


        public MusicViewModel()
        {
            CreatePlaylistCommand = new RelayCommand(
                _ => CreatePlaylistAsync(),
                _ => !string.IsNullOrWhiteSpace(NewPlaylistName)
            );

            AddTrackToPlaylistCommand = new RelayCommand(
                _ => AddTrackToPlaylistAsync(),
                _ => SelectedPlaylist != null && SelectedLibraryTrack != null
            );

            UpdatePlaylistCommand = new RelayCommand(
                _ => UpdatePlaylistAsync(),
                _ => SelectedPlaylist != null &&
                     !string.IsNullOrWhiteSpace(EditPlaylistName) &&
                     EditPlaylistName != SelectedPlaylist.Name
            );

            RemoveTrackFromPlaylistCommand = new RelayCommand(
                _ => RemoveTrackFromPlaylistAsync(),
                _ => SelectedPlaylist != null && SelectedPlaylistTrack != null
            );
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged(nameof(IsLoading));
                }
            }
        }

        private const int PageSize = 15;
        private int _currentOffset = 0;
        private bool _hasMoreTracks = true;

        private Playlist? _selectedPlaylist;
        public Playlist? SelectedPlaylist
        {
            get => _selectedPlaylist;
            set
            {
                _selectedPlaylist = value;
                OnPropertyChanged(nameof(SelectedPlaylist));

                if (_selectedPlaylist != null)
                {
                    EditPlaylistName = _selectedPlaylist.Name;

                    _currentOffset = 0;
                    _hasMoreTracks = true;
                    Tracks.Clear();

                    _ = LoadMoreTracksAsync();
                }
            }

        }

        private string _newPlaylistName = string.Empty;
        public string NewPlaylistName
        {
            get => _newPlaylistName;
            set
            {
                _newPlaylistName = value;
                OnPropertyChanged(nameof(NewPlaylistName));

                CreatePlaylistCommand?.RaiseCanExecuteChanged();
            }
        }

        private string _editPlaylistName = string.Empty;
        public string EditPlaylistName
        {
            get => _editPlaylistName;
            set
            {
                _editPlaylistName = value;
                OnPropertyChanged(nameof(EditPlaylistName));
                UpdatePlaylistCommand?.RaiseCanExecuteChanged();
            }
        }

        private Track? _selectedPlaylistTrack;
        public Track? SelectedPlaylistTrack
        {
            get => _selectedPlaylistTrack;
            set
            {
                _selectedPlaylistTrack = value;
                OnPropertyChanged(nameof(SelectedPlaylistTrack));
                RemoveTrackFromPlaylistCommand?.RaiseCanExecuteChanged();
            }
        }

        public async Task LoadDataAsync()
        {
            Playlists.Clear();
            var playlists = await _service.GetPlaylistsAsync();
            foreach (var p in playlists)
                Playlists.Add(p);
        }

        public async Task LoadLibraryAsync()
        {
            LibraryTracks.Clear();
            var tracks = await _service.GetTracksAsync();
            foreach (var t in tracks)
                LibraryTracks.Add(t);
        }


        private async Task LoadTracksForSelectedPlaylistAsync()
        {
            Tracks.Clear();

            var tracks = await _service.GetTracksForPlaylistAsync(
                _selectedPlaylist!.PlaylistId
            );

            Debug.WriteLine($"Tracks loaded: {tracks.Count}");

            foreach (var t in tracks)
                Tracks.Add(t);

            OnPropertyChanged(nameof(Tracks));
        }

        private async void CreatePlaylistAsync()
        {
            var playlist = await _service.CreatePlaylistAsync(NewPlaylistName);

            Playlists.Add(playlist);
            SelectedPlaylist = playlist;

            NewPlaylistName = string.Empty;
        }

        private Track? _selectedLibraryTrack;
        public Track? SelectedLibraryTrack
        {
            get => _selectedLibraryTrack;
            set
            {
                _selectedLibraryTrack = value;
                OnPropertyChanged(nameof(SelectedLibraryTrack));
                AddTrackToPlaylistCommand?.RaiseCanExecuteChanged();
            }
        }

        private async void AddTrackToPlaylistAsync()
        {
            await _service.AddTrackToPlaylistAsync(
                SelectedPlaylist!.PlaylistId,
                SelectedLibraryTrack!.TrackId
            );

            await LoadTracksForSelectedPlaylistAsync();
        }

        private async void UpdatePlaylistAsync()
        {
            await _service.UpdatePlaylistNameAsync(
                SelectedPlaylist!.PlaylistId,
                EditPlaylistName
            );

            SelectedPlaylist.Name = EditPlaylistName;
            OnPropertyChanged(nameof(Playlists));
        }

        public async Task LoadMoreTracksAsync()
        {
            if (!_hasMoreTracks || IsLoading || SelectedPlaylist == null)
                return;

            try
            {
                IsLoading = true;

                var newTracks = await _service.GetTracksForPlaylistPagedAsync(
                    SelectedPlaylist.PlaylistId,
                    _currentOffset,
                    PageSize
                );

                foreach (var t in newTracks)
                    Tracks.Add(t);

                _currentOffset += newTracks.Count;

                if (newTracks.Count < PageSize)
                    _hasMoreTracks = false;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async void RemoveTrackFromPlaylistAsync()
        {
            if (SelectedPlaylist == null || SelectedPlaylistTrack == null)
                return;

            await _service.RemoveTrackFromPlaylistAsync(
             SelectedPlaylist.PlaylistId,
             SelectedPlaylistTrack.TrackId
         );

            Tracks.Remove(SelectedPlaylistTrack);
            SelectedPlaylistTrack = null;
        }
    }
}