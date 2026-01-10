using MusicLibrary.Commands;
using MusicLibrary.Services;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;

namespace MusicLibrary.ViewModels
{
    public class MusicViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName) 
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly MusicService _service = new();

        private readonly Func<CrudMode, EntityType, Task> _openDialogAndRefresh;

        public RelayCommand AddPlaylistDialogCommand { get; }
        public RelayCommand AddArtistDialogCommand { get; }
        public RelayCommand AddAlbumDialogCommand { get; }
        public RelayCommand AddTrackDialogCommand { get; }

        public RelayCommand UpdatePlaylistDialogCommand { get; }
        public RelayCommand UpdateArtistDialogCommand { get; }
        public RelayCommand UpdateAlbumDialogCommand { get; }
        public RelayCommand UpdateTrackDialogCommand { get; }

        public RelayCommand DeletePlaylistDialogCommand { get; }
        public RelayCommand DeleteArtistDialogCommand { get; }
        public RelayCommand DeleteAlbumDialogCommand { get; }
        public RelayCommand DeleteTrackDialogCommand { get; }

        public ObservableCollection<Playlist> Playlists { get; } = new();
        public ObservableCollection<Track> Tracks { get; } = new();
        public ObservableCollection<Track> LibraryTracks { get; } = new();
        public RelayCommand AddTrackToPlaylistCommand { get; }
        public RelayCommand RemoveTrackFromPlaylistCommand { get; }


        public MusicViewModel(Func<CrudMode, EntityType, Task> openDialogAndRefresh)
        {
            _openDialogAndRefresh = openDialogAndRefresh;

            // Add
            AddPlaylistDialogCommand = new RelayCommand(_ => _openDialogAndRefresh(CrudMode.Add, EntityType.Playlist));
            AddArtistDialogCommand = new RelayCommand(_ => _openDialogAndRefresh(CrudMode.Add, EntityType.Artist));
            AddAlbumDialogCommand = new RelayCommand(_ => _openDialogAndRefresh(CrudMode.Add, EntityType.Album));
            AddTrackDialogCommand = new RelayCommand(_ => _openDialogAndRefresh(CrudMode.Add, EntityType.Track));

            // Update
            UpdatePlaylistDialogCommand = new RelayCommand(_ => _openDialogAndRefresh(CrudMode.Update, EntityType.Playlist));
            UpdateArtistDialogCommand = new RelayCommand(_ => _openDialogAndRefresh(CrudMode.Update, EntityType.Artist));
            UpdateAlbumDialogCommand = new RelayCommand(_ => _openDialogAndRefresh(CrudMode.Update, EntityType.Album));
            UpdateTrackDialogCommand = new RelayCommand(_ => _openDialogAndRefresh(CrudMode.Update, EntityType.Track));

            // Delete
            DeletePlaylistDialogCommand = new RelayCommand(_ => _openDialogAndRefresh(CrudMode.Delete, EntityType.Playlist));
            DeleteArtistDialogCommand = new RelayCommand(_ => _openDialogAndRefresh(CrudMode.Delete, EntityType.Artist));
            DeleteAlbumDialogCommand = new RelayCommand(_ => _openDialogAndRefresh(CrudMode.Delete, EntityType.Album));
            DeleteTrackDialogCommand = new RelayCommand(_ => _openDialogAndRefresh(CrudMode.Delete, EntityType.Track));


            AddTrackToPlaylistCommand = new RelayCommand(
                _ => AddTrackToPlaylistAsync(),
                _ => SelectedPlaylist != null && SelectedLibraryTrack != null
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
                else
                {
                    Tracks.Clear();
                    SelectedPlaylistTrack = null;
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

        public Action? ArtistsChanged { get; set; }

        private Artist? _selectedArtist;
        public Artist? SelectedArtist
        {
            get => _selectedArtist;
            set
            {
                _selectedArtist = value;
                OnPropertyChanged(nameof(SelectedArtist));

                EditArtistName = _selectedArtist?.Name ?? "";

            }
        }

        private string _newArtistName = "";
        public string NewArtistName
        {
            get => _newArtistName;
            set
            {
                _newArtistName = value;
                OnPropertyChanged(nameof(NewArtistName));
            }
        }

        private string _editArtistName = "";
        public string EditArtistName
        {
            get => _editArtistName;
            set
            {
                _editArtistName = value;
                OnPropertyChanged(nameof(EditArtistName));
            }
        }


        public async Task LoadDataAsync()
        {
            var selectedId = SelectedPlaylist?.PlaylistId;

            Playlists.Clear();
            var playlists = await _service.GetPlaylistsAsync();
            foreach (var p in playlists)
                Playlists.Add(p);

            if (selectedId != null)
            {
                SelectedPlaylist = Playlists.FirstOrDefault(p => p.PlaylistId == selectedId);
                OnPropertyChanged(nameof(SelectedPlaylist));
            }
        }

        public async Task LoadLibraryAsync()
        {
            LibraryTracks.Clear();
            var tracks = await _service.GetTracksAsync();
            foreach (var t in tracks)
                LibraryTracks.Add(t);
        }

        public Task<List<Artist>> LoadArtistsTreeAsync()
              => _service.GetArtistsTreeAsync();



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


        private async Task AddTrackToPlaylistAsync()
        {
            await _service.AddTrackToPlaylistAsync(
                SelectedPlaylist!.PlaylistId,
                SelectedLibraryTrack!.TrackId
            );

            await LoadTracksForSelectedPlaylistAsync();
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

        private async Task RemoveTrackFromPlaylistAsync()
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