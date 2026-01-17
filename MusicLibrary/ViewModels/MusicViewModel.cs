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

        private readonly Func<CrudMode, EntityType, object?, Task> _openDialogAndRefresh;


        //---- COMMANDS ----
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
        public RelayCommand AddTrackToPlaylistCommand { get; }
        public RelayCommand RemoveTrackFromPlaylistCommand { get; }
        public RelayCommand DeleteSelectedPlaylistCommand { get; }

        public RelayCommand AddAlbumUnderArtistCommand { get; }
        public RelayCommand EditArtistFromNodeCommand { get; }
        public RelayCommand DeleteArtistFromNodeCommand { get; }

        public RelayCommand AddTrackUnderAlbumCommand { get; }
        public RelayCommand EditAlbumFromNodeCommand { get; }
        public RelayCommand DeleteAlbumFromNodeCommand { get; }

        public RelayCommand AddTrackToSelectedPlaylistFromNodeCommand { get; }
        public RelayCommand EditTrackFromNodeCommand { get; }
        public RelayCommand DeleteTrackFromNodeCommand { get; }


        //---- COLLECTIONS ----
        public ObservableCollection<Playlist> Playlists { get; } = new();
        public ObservableCollection<Track> Tracks { get; } = new();
        public ObservableCollection<Track> LibraryTracks { get; } = new();


        // ---- CONSTRUCTOR ----
        public MusicViewModel(Func<CrudMode, EntityType, object?, Task> openDialogAndRefresh)
        {
            _openDialogAndRefresh = openDialogAndRefresh;

            // Add
            AddPlaylistDialogCommand = new RelayCommand(parameter => _openDialogAndRefresh(CrudMode.Add, EntityType.Playlist, null));
            AddArtistDialogCommand = new RelayCommand(parameter => _openDialogAndRefresh(CrudMode.Add, EntityType.Artist, null));
            AddAlbumDialogCommand = new RelayCommand(parameter => _openDialogAndRefresh(CrudMode.Add, EntityType.Album, null));
            AddTrackDialogCommand = new RelayCommand(parameter => _openDialogAndRefresh(CrudMode.Add, EntityType.Track, null));

            // Update
            UpdatePlaylistDialogCommand = new RelayCommand(
                parameter => _openDialogAndRefresh(CrudMode.Update, EntityType.Playlist, null),
                parameter => SelectedPlaylist != null
            );
            UpdateArtistDialogCommand = new RelayCommand(parameter => _openDialogAndRefresh(CrudMode.Update, EntityType.Artist, null));
            UpdateAlbumDialogCommand = new RelayCommand(parameter => _openDialogAndRefresh(CrudMode.Update, EntityType.Album, null));
            UpdateTrackDialogCommand = new RelayCommand(parameter => _openDialogAndRefresh(CrudMode.Update, EntityType.Track, null));

            // Delete
            DeleteSelectedPlaylistCommand = new RelayCommand(async _ => await DeleteSelectedPlaylistAsync(),_ => SelectedPlaylist != null);

            DeletePlaylistDialogCommand = new RelayCommand(parameter => _openDialogAndRefresh(CrudMode.Delete, EntityType.Playlist, null));
            DeleteArtistDialogCommand = new RelayCommand(parameter => _openDialogAndRefresh(CrudMode.Delete, EntityType.Artist, null));
            DeleteAlbumDialogCommand = new RelayCommand(parameter => _openDialogAndRefresh(CrudMode.Delete, EntityType.Album, null));
            DeleteTrackDialogCommand = new RelayCommand(parameter => _openDialogAndRefresh(CrudMode.Delete, EntityType.Track, null));


            AddTrackToPlaylistCommand = new RelayCommand(
                parameter => AddTrackToPlaylistAsync(),
                parameter => SelectedPlaylist != null && SelectedLibraryTrack != null
            );

            RemoveTrackFromPlaylistCommand = new RelayCommand(
                parameter => RemoveTrackFromPlaylistAsync(),
                parameter => SelectedPlaylist != null && SelectedPlaylistTrack != null
            );

            AddAlbumUnderArtistCommand = new RelayCommand(
                p => _openDialogAndRefresh(CrudMode.Add, EntityType.Album, p),
                p => p is Artist
);

            EditArtistFromNodeCommand = new RelayCommand(
                p => _openDialogAndRefresh(CrudMode.Update, EntityType.Artist, p),
                p => p is Artist
            );

            DeleteArtistFromNodeCommand = new RelayCommand(
                p => _openDialogAndRefresh(CrudMode.Delete, EntityType.Artist, p),
                p => p is Artist
            );

            AddTrackUnderAlbumCommand = new RelayCommand(
                p => _openDialogAndRefresh(CrudMode.Add, EntityType.Track, p),
                p => p is Album
            );

            EditAlbumFromNodeCommand = new RelayCommand(
                p => _openDialogAndRefresh(CrudMode.Update, EntityType.Album, p),
                p => p is Album
            );

            DeleteAlbumFromNodeCommand = new RelayCommand(
                p => _openDialogAndRefresh(CrudMode.Delete, EntityType.Album, p),
                p => p is Album
            );

            // Track: add to selected playlist (disabled om ingen playlist)
            AddTrackToSelectedPlaylistFromNodeCommand = new RelayCommand(
                async p => await AddTrackToSelectedPlaylistFromNodeAsync(p as Track),
                p => SelectedPlaylist != null && p is Track
            );

            EditTrackFromNodeCommand = new RelayCommand(
                p => _openDialogAndRefresh(CrudMode.Update, EntityType.Track, p),
                p => p is Track
            );

            DeleteTrackFromNodeCommand = new RelayCommand(
                p => _openDialogAndRefresh(CrudMode.Delete, EntityType.Track, p),
                p => p is Track
            );

        }

        //---- FIELDS & PROPERTIES ----
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

        private const int _pageSize = 15;
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

                UpdatePlaylistDialogCommand?.RaiseCanExecuteChanged();
                DeleteSelectedPlaylistCommand?.RaiseCanExecuteChanged();
                AddTrackToSelectedPlaylistFromNodeCommand?.RaiseCanExecuteChanged();


                if (_selectedPlaylist != null)
                {

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

        //---- METHODS ----

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
                    _pageSize
                );

                foreach (var t in newTracks)
                    Tracks.Add(t);

                _currentOffset += newTracks.Count;

                if (newTracks.Count < _pageSize)
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

        private async Task DeleteSelectedPlaylistAsync()
        {
            if (SelectedPlaylist == null)
                return;

            var idToDelete = SelectedPlaylist.PlaylistId;

            // Nollställ i UI
            SelectedPlaylist = null;
            Tracks.Clear();
            SelectedPlaylistTrack = null;

            // Tar bort playlist + alla kopplingar oavsett innehåll
            await _service.DeletePlaylistAsync(idToDelete);

            await LoadDataAsync();
        }

        private async Task AddTrackToSelectedPlaylistFromNodeAsync(Track? track)
        {
            if (track == null || SelectedPlaylist == null)
                return;

            SelectedLibraryTrack = track;

            await _service.AddTrackToPlaylistAsync(SelectedPlaylist.PlaylistId, track.TrackId);

          
            _currentOffset = 0;
            _hasMoreTracks = true;
            Tracks.Clear();
            await LoadMoreTracksAsync();
        }


    }
}