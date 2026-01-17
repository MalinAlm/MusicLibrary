using MusicLibrary.Commands;
using MusicLibrary.Services;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;



namespace MusicLibrary.ViewModels;

public class EditDialogViewModel : BaseViewModel
{
    private readonly MusicService _service = new();
    private int? _defaultMediaTypeId;
    private readonly Window _owner;
    private readonly object? _context;

    public CrudMode Mode { get; }
    public EntityType Entity { get; }

    private string ModeText => Mode switch
    {
        CrudMode.Add => "Add",
        CrudMode.Update => "Update",
        CrudMode.Delete => "Delete",
        _ => Mode.ToString()
    };

    private string EntityText => Entity switch
    {
        EntityType.Playlist => "Playlist",
        EntityType.Artist => "Artist",
        EntityType.Album => "Album",
        EntityType.Track => "Track",
        _ => Entity.ToString()
    };

    public string DialogTitle => $"{ModeText} {EntityText}";
    public string ConfirmLabel => Mode switch
    {
        CrudMode.Add => "Add",
        CrudMode.Update => "Update",
        _ => "Delete"
    };

    public bool IsTrack => Entity == EntityType.Track;
    public bool ShowMediaTypeSelector => Entity == EntityType.Track && Mode == CrudMode.Add;
    public bool IsUpdatePlaylist => Mode == CrudMode.Update && Entity == EntityType.Playlist;
    public bool IsArtistSelectorVisible => Entity == EntityType.Artist;
    public bool IsAlbum => Entity == EntityType.Album;
    public bool ShowAlbumOnAddArtist => Entity == EntityType.Artist && Mode == CrudMode.Add;
    public bool ShowAlbumArtistSelector => Entity == EntityType.Album && Mode != CrudMode.Delete;
    public bool ShowName => Mode is CrudMode.Add or CrudMode.Update;
    public bool ShowTrackLength => Entity == EntityType.Track && Mode is CrudMode.Add or CrudMode.Update;
    public bool ShowSelector => !_forceHideSelector && Mode != CrudMode.Add;
    public bool ShowAlbumSelectorForTrack => Entity == EntityType.Track && Mode == CrudMode.Add && _context is null;
    public bool ShowArtistSelectorForAlbum => Entity == EntityType.Album && Mode == CrudMode.Add && _context is null;

    // ---- Selector (Update/Delete) ----
    private readonly bool _forceHideSelector;
   

    public string SelectorLabel => Entity switch
    {
        EntityType.Playlist => "Select playlist to edit",
        EntityType.Artist => "Select artist to edit",
        EntityType.Album => "Select album to edit",
        _ => "Select track to edit"
    };

    private IList _selectorItems = new ArrayList();
    public IList SelectorItems
    {
        get => _selectorItems;
        set { _selectorItems = value; RaisePropertyChanged(); }
    }

    // ---------- Paging för Track-selector (Update/Delete) ----------
    private const int NumberOfTracksToLoadPerPage = 15;

    private int numberOfTracksAlreadyLoaded = 0;
    private bool moreTracksExistInDatabase = true;

    private bool isCurrentlyLoadingMoreTracks;
    public bool IsCurrentlyLoadingMoreTracks
    {
        get => isCurrentlyLoadingMoreTracks;
        set { isCurrentlyLoadingMoreTracks = value; RaisePropertyChanged(); }
    }

    // ---------- Paging för Artist-selector (Delete/Update) ----------
    private const int NumberOfArtistsToLoadPerPage = 15;

    private int numberOfArtistsAlreadyLoaded = 0;
    private bool moreArtistsExistInDatabase = true;

    private bool isCurrentlyLoadingMoreArtists;
    public bool IsCurrentlyLoadingMoreArtists
    {
        get => isCurrentlyLoadingMoreArtists;
        set { isCurrentlyLoadingMoreArtists = value; RaisePropertyChanged(); }
    }

    public ObservableCollection<Artist> ArtistsAvailableToSelect { get; } = new();


    private string artistSearchTextUserIsTyping = "";
    public string ArtistSearchTextUserIsTyping
    {
        get => artistSearchTextUserIsTyping;
        set { artistSearchTextUserIsTyping = value; RaisePropertyChanged(); }
    }


    private string? activeArtistSearchText = null;
    private string? ActiveArtistSearchText
    {
        get => activeArtistSearchText;
        set { activeArtistSearchText = value; }
    }


    public ObservableCollection<Track> TracksAvailableToSelect { get; } = new();

    private string trackSearchTextInSelector = "";
    public string TrackSearchTextInSelector
    {
        get => trackSearchTextInSelector;
        set
        {
            trackSearchTextInSelector = value;
            RaisePropertyChanged();
            _ = ResetSelectorAndLoadFirstPageAsync();
        }
    }

    private string trackSearchTextUserIsTyping = "";
    public string TrackSearchTextUserIsTyping
    {
        get => trackSearchTextUserIsTyping;
        set { trackSearchTextUserIsTyping = value; RaisePropertyChanged(); }
    }

    private string? activeTrackSearchText = null;
    private string? ActiveTrackSearchText
    {
        get => activeTrackSearchText;
        set { activeTrackSearchText = value; }
    }


    private object? _selectedSelectorItem;
    public object? SelectedSelectorItem
    {
        get => _selectedSelectorItem;
        set
        {
            _selectedSelectorItem = value;
            RaisePropertyChanged();
            PrefillFromSelected();
        }
    }

    public ObservableCollection<Artist> Artists { get; } = new();

    private Artist? _selectedArtistForAlbum;
    public Artist? SelectedArtistForAlbum
    {
        get => _selectedArtistForAlbum;
        set { _selectedArtistForAlbum = value; RaisePropertyChanged(); }
    }

    private string _newAlbumTitle = "";
    public string NewAlbumTitle
    {
        get => _newAlbumTitle;
        set { _newAlbumTitle = value; RaisePropertyChanged(); }
    }

    private bool _isBusy;
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            _isBusy = value;
            RaisePropertyChanged();
            ConfirmCommand.RaiseCanExecuteChanged();
        }
    }


    // ---- Name field ----

    public string NameLabel => Entity switch
    {
        EntityType.Playlist => "Playlist name",
        EntityType.Artist => "Artist name",
        EntityType.Album => "Album title",
        EntityType.Track => "Track name",
        _ => "Name"
    };

    private string _name = "";
    public string Name
    {
        get => _name;
        set { _name = value; RaisePropertyChanged(); }
    }

    // ---- Track fields ----
    private string _millisecondsText = "";
    public string MillisecondsText
    {
        get => _millisecondsText;
        set { _millisecondsText = value; RaisePropertyChanged(); }
    }

    private static string FormatMsToMmSs(int ms)
    {
        if (ms < 0) return "";
        var ts = TimeSpan.FromMilliseconds(ms);
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
    }

    private static bool TryParseMmSsToMs(string? input, out int milliseconds)
    {
        milliseconds = 0;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        input = input.Trim();

        // Tillåt även om någon råkar skriva t.ex. "03:45"
        var parts = input.Split(':');
        if (parts.Length != 2)
            return false;

        if (!int.TryParse(parts[0], out var minutes))
            return false;

        if (!int.TryParse(parts[1], out var seconds))
            return false;

        if (minutes < 0 || seconds < 0 || seconds > 59)
            return false;

        var ts = TimeSpan.FromMinutes(minutes) + TimeSpan.FromSeconds(seconds);

        var totalMs = ts.TotalMilliseconds;
        if (totalMs > int.MaxValue)
            return false;

        milliseconds = (int)totalMs;
        return true;
    }


    public ObservableCollection<Album> Albums { get; } = new();

    private Album? _selectedAlbum;
    public Album? SelectedAlbum
    {
        get => _selectedAlbum;
        set { _selectedAlbum = value; RaisePropertyChanged(); }
    }

    public ObservableCollection<MediaType> MediaTypes { get; } = new();

    private MediaType? _selectedMediaType;
    public MediaType? SelectedMediaType
    {
        get => _selectedMediaType;
        set { _selectedMediaType = value; RaisePropertyChanged(); }
    }


    private string _errorText = "";
    public string ErrorText
    {
        get => _errorText;
        set { _errorText = value; RaisePropertyChanged(); }
    }

    // ---- Commands ----
    public RelayCommand ConfirmCommand { get; }
    public RelayCommand AddTrackCommand { get; }
    public RelayCommand RemoveTrackCommand { get; }
    public RelayCommand SearchArtistsCommand { get; }
    public RelayCommand ClearArtistSearchCommand { get; }
    public RelayCommand SearchTracksCommand { get; }
    public RelayCommand ClearSearchCommand { get; }


    // ---- Playlist track management ----
    public ObservableCollection<Track> PlaylistTracks { get; } = new();

    private Track? _selectedPlaylistTrack;
    public Track? SelectedPlaylistTrack
    {
        get => _selectedPlaylistTrack;
        set
        {
            _selectedPlaylistTrack = value;
            RaisePropertyChanged();
            RemoveTrackCommand.RaiseCanExecuteChanged();
        }
    }

    // Bibliotek: Album -> Tracks
    public ObservableCollection<AlbumNodeViewModel> LibraryAlbums { get; } = new();

    public ICollectionView? LibraryAlbumsView { get; private set; }

    private Track? _selectedLibraryTrack;
    public Track? SelectedLibraryTrack
    {
        get => _selectedLibraryTrack;
        set
        {
            _selectedLibraryTrack = value;
            RaisePropertyChanged();
            AddTrackCommand.RaiseCanExecuteChanged();
        }
    }

    private string _trackSearchText = "";
    public string TrackSearchText
    {
        get => _trackSearchText;
        set
        {
            _trackSearchText = value;
            RaisePropertyChanged();
            LibraryAlbumsView?.Refresh();
        }
    }

    public EditDialogViewModel(CrudMode mode, EntityType entity, Window owner, object? context = null)
    {
        Mode = mode;
        Entity = entity;
        _owner = owner;
        _context = context;
        _forceHideSelector = mode == CrudMode.Update && context != null; 

        ConfirmCommand = new RelayCommand(parameter => ConfirmAsync(), _ => !IsBusy);
        SearchTracksCommand = new RelayCommand(parameter => ApplySearchAndReloadAsync());
        ClearSearchCommand = new RelayCommand(parameter => ClearSearchAndReloadAsync());

        SearchArtistsCommand = new RelayCommand(parameter => ApplyArtistSearchAndReloadAsync());
        ClearArtistSearchCommand = new RelayCommand(parameter => ClearArtistSearchAndReloadAsync());

        AddTrackCommand = new RelayCommand(parameter => AddTrackToPlaylistAsync(),
            parameter => IsUpdatePlaylist &&
                 SelectedSelectorItem is Playlist &&
                 SelectedLibraryTrack != null);

        RemoveTrackCommand = new RelayCommand(parameter => RemoveTrackFromPlaylistAsync(),
            parameter => IsUpdatePlaylist &&
                 SelectedSelectorItem is Playlist &&
                 SelectedPlaylistTrack != null);

        _ = LoadAsync();

    }

    private async Task LoadAsync()
    {
        ErrorText = "";

        if (_context != null)
        {
         
            if (Mode == CrudMode.Update && Entity == EntityType.Playlist && _context is Playlist contextPlaylistToEdit)
            {
                SelectedSelectorItem = contextPlaylistToEdit;
                Name = contextPlaylistToEdit.Name ?? "";
                return;
            }

           
            if (Mode == CrudMode.Add && Entity == EntityType.Album && _context is Artist contextArtist)
            {
                SelectedArtistForAlbum = contextArtist;
                return; 
            }

           
            if (Mode == CrudMode.Add && Entity == EntityType.Track && _context is Album contextAlbum)
            {
                SelectedAlbum = contextAlbum;
                return;
            }

           
            if (Mode == CrudMode.Update && Entity == EntityType.Artist && _context is Artist contextArtistToEdit)
            {
                SelectedSelectorItem = contextArtistToEdit;
                Name = contextArtistToEdit.Name ?? "";
                return;
            }

            if (Mode == CrudMode.Update && Entity == EntityType.Album && _context is Album contextAlbumToEdit)
            {
                SelectedSelectorItem = contextAlbumToEdit;
                Name = contextAlbumToEdit.Title ?? "";
                return;
            }

            if (Mode == CrudMode.Update && Entity == EntityType.Track && _context is Track contextTrackToEdit)
            {
                SelectedSelectorItem = contextTrackToEdit;
                Name = contextTrackToEdit.Name ?? "";
                MillisecondsText = FormatMsToMmSs(contextTrackToEdit.Milliseconds);
                return;
            }
        }

        if (ShowSelector)
        {
            if (Entity == EntityType.Playlist)
            {
                SelectorItems = new ArrayList(await _service.GetPlaylistsAsync());
            }
            else if (Entity == EntityType.Artist)
            {
                SelectorItems = ArtistsAvailableToSelect;
                await ResetArtistSelectorAndLoadFirstPageAsync();
            }
            else if (Entity == EntityType.Album)
            {
                SelectorItems = new ArrayList(await _service.GetAlbumsAsync());
            }
            else 
            {
                SelectorItems = TracksAvailableToSelect;
                await ResetSelectorAndLoadFirstPageAsync();
            }

        }

        // ----- PRESELECT (UPDATE/DELETE) -----
        if (Mode != CrudMode.Add && _context != null)
        {
            if (Entity == EntityType.Album && _context is Album ctxAl)
            {
                
                if (SelectorItems is IList list)
                    SelectedSelectorItem = list.Cast<object>()
                                               .OfType<Album>()
                                               .FirstOrDefault(a => a.AlbumId == ctxAl.AlbumId);
            }

           
            if (Entity == EntityType.Artist && _context is Artist ctxA)
                SelectedSelectorItem = ArtistsAvailableToSelect.FirstOrDefault(a => a.ArtistId == ctxA.ArtistId);

            if (Entity == EntityType.Track && _context is Track ctxT)
                SelectedSelectorItem = TracksAvailableToSelect.FirstOrDefault(t => t.TrackId == ctxT.TrackId);
        }


        if (Entity == EntityType.Album || (Entity == EntityType.Artist && Mode == CrudMode.Add))
        {
            Artists.Clear();
            foreach (var a in await _service.GetArtistsAsync())
                Artists.Add(a);
        }

        if (IsTrack)
        {
            Albums.Clear();
            foreach (var a in await _service.GetAlbumsAsync())
                Albums.Add(a);

            MediaTypes.Clear();
            foreach (var mt in await _service.GetMediaTypesAsync())
                MediaTypes.Add(mt);

            if (MediaTypes.Count == 0)
                ErrorText = "No MediaTypes found in database.";
        }

  

        var mediaTypes = await _service.GetMediaTypesAsync();
        _defaultMediaTypeId = mediaTypes.FirstOrDefault()?.MediaTypeId;

        if (_defaultMediaTypeId == null)
            ErrorText = "No MediaTypes found in database.";

    }

    private void PrefillFromSelected()
    {
        ErrorText = "";
        if (SelectedSelectorItem == null) return;

        if (Entity == EntityType.Artist && SelectedSelectorItem is Artist a)
        {
            Name = a.Name ?? "";
            return;
        }

        if (Entity == EntityType.Album && SelectedSelectorItem is Album al)
        {
            Name = al.Title ?? "";
            return;
        }

        if (Entity == EntityType.Track && SelectedSelectorItem is Track t)
        {
            Name = t.Name ?? "";
            MillisecondsText = FormatMsToMmSs(t.Milliseconds);
        }
    }

    private async Task ConfirmAsync()
    {

        //prevent double execution
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            ErrorText = "";

            if (Entity == EntityType.Playlist)
                await AddOrUpdatePlaylistAsync();
            else if (Entity == EntityType.Artist)
                await AddOrUpdateArtistAsync();
            else if (Entity == EntityType.Album)
                await AddOrUpdateAlbumAsync();
            else if (Entity == EntityType.Track)
                await AddOrUpdateTrackAsync();
            else
                throw new InvalidOperationException("Unsupported entity.");

            _owner.DialogResult = true;
            _owner.Close();
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ReloadPlaylistTracksAsync()
    {
        PlaylistTracks.Clear();

        if (SelectedSelectorItem is not Playlist p) return;

        var tracks = await _service.GetTracksForPlaylistAsync(p.PlaylistId);
        foreach (var t in tracks)
            PlaylistTracks.Add(t);
    }

    private async Task AddTrackToPlaylistAsync()
    {
        ErrorText = "";
        if (SelectedSelectorItem is not Playlist p || SelectedLibraryTrack == null) return;

        await _service.AddTrackToPlaylistAsync(p.PlaylistId, SelectedLibraryTrack.TrackId);
        await ReloadPlaylistTracksAsync();
    }

    private async Task RemoveTrackFromPlaylistAsync()
    {
        ErrorText = "";
        if (SelectedSelectorItem is not Playlist p || SelectedPlaylistTrack == null) return;

        await _service.RemoveTrackFromPlaylistAsync(p.PlaylistId, SelectedPlaylistTrack.TrackId);
        await ReloadPlaylistTracksAsync();
    }

    private async Task ResetSelectorAndLoadFirstPageAsync()
    {
        numberOfTracksAlreadyLoaded = 0;
        moreTracksExistInDatabase = true;

        TracksAvailableToSelect.Clear();

        await LoadNextTracksPageForSelectorAsync();
    }

    public async Task LoadNextTracksPageForSelectorAsync()
    {
        if (IsCurrentlyLoadingMoreTracks)
            return;

        if (!moreTracksExistInDatabase)
            return;

        try
        {
            IsCurrentlyLoadingMoreTracks = true;

            List<Track> nextTracksPage = await _service.GetTracksPageAsync(
                numberOfTracksToSkip: numberOfTracksAlreadyLoaded,
                numberOfTracksToTake: NumberOfTracksToLoadPerPage,
                searchText: ActiveTrackSearchText
            );

            foreach (Track track in nextTracksPage)
                TracksAvailableToSelect.Add(track);

            numberOfTracksAlreadyLoaded += nextTracksPage.Count;

            if (nextTracksPage.Count < NumberOfTracksToLoadPerPage)
                moreTracksExistInDatabase = false;

            if (nextTracksPage.Count == 0)
            {
                moreTracksExistInDatabase = false;
                return;
            }
        }
        finally
        {
            IsCurrentlyLoadingMoreTracks = false;
        }
    }

    public async Task ApplySearchAndReloadAsync()
    {
        ActiveTrackSearchText = string.IsNullOrWhiteSpace(TrackSearchTextUserIsTyping)
            ? null
            : TrackSearchTextUserIsTyping.Trim();

        await ResetSelectorAndLoadFirstPageAsync();
    }

    public async Task ClearSearchAndReloadAsync()
    {
        TrackSearchTextUserIsTyping = "";
        ActiveTrackSearchText = null;
        RaisePropertyChanged(nameof(TrackSearchTextUserIsTyping));

        await ResetSelectorAndLoadFirstPageAsync();
    }

    private async Task ResetArtistSelectorAndLoadFirstPageAsync()
    {
        numberOfArtistsAlreadyLoaded = 0;
        moreArtistsExistInDatabase = true;

        ArtistsAvailableToSelect.Clear();

        await LoadNextArtistsPageForSelectorAsync();
    }

    public async Task LoadNextArtistsPageForSelectorAsync()
    {
        if (IsCurrentlyLoadingMoreArtists)
            return;

        if (!moreArtistsExistInDatabase)
            return;

        try
        {
            IsCurrentlyLoadingMoreArtists = true;

            List<Artist> nextArtistsPage = await _service.GetArtistsPageAsync(
                numberOfArtistsToSkip: numberOfArtistsAlreadyLoaded,
                numberOfArtistsToTake: NumberOfArtistsToLoadPerPage,
                searchText: ActiveArtistSearchText
            );

            if (nextArtistsPage.Count == 0)
            {
                moreArtistsExistInDatabase = false;
                return;
            }

            foreach (Artist artist in nextArtistsPage)
                ArtistsAvailableToSelect.Add(artist);

            numberOfArtistsAlreadyLoaded += nextArtistsPage.Count;

            if (nextArtistsPage.Count < NumberOfArtistsToLoadPerPage)
                moreArtistsExistInDatabase = false;
        }
        finally
        {
            IsCurrentlyLoadingMoreArtists = false;
        }
    }

    public async Task ApplyArtistSearchAndReloadAsync()
    {
        ActiveArtistSearchText = string.IsNullOrWhiteSpace(ArtistSearchTextUserIsTyping)
            ? null
            : ArtistSearchTextUserIsTyping.Trim();

        await ResetArtistSelectorAndLoadFirstPageAsync();
    }

    public async Task ClearArtistSearchAndReloadAsync()
    {
        ArtistSearchTextUserIsTyping = "";
        ActiveArtistSearchText = null;
        RaisePropertyChanged(nameof(ArtistSearchTextUserIsTyping));

        await ResetArtistSelectorAndLoadFirstPageAsync();
    }

    private async Task AddOrUpdateArtistAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Name is required.");

        if (Mode == CrudMode.Add)
        {
            await _service.CreateArtistAsync(Name.Trim());
            return;
        }

        if (SelectedSelectorItem is not Artist artist)
            throw new InvalidOperationException("Select an artist.");

        await _service.UpdateArtistAsync(artist.ArtistId, Name.Trim());
    }

    private async Task AddOrUpdateAlbumAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Album title is required.");

        if (Mode == CrudMode.Add)
        {
           
            if (SelectedArtistForAlbum == null)
                throw new InvalidOperationException("Select an artist.");

            await _service.CreateAlbumAsync(Name.Trim(), SelectedArtistForAlbum.ArtistId);
            return;
        }

        if (SelectedSelectorItem is not Album album)
            throw new InvalidOperationException("Select an album.");

      
        await _service.UpdateAlbumAsync(album.AlbumId, Name.Trim(), album.ArtistId);
    }

    private async Task AddOrUpdatePlaylistAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Playlist name is required.");

        if (Mode == CrudMode.Add)
        {
            await _service.CreatePlaylistAsync(Name.Trim());
            return;
        }

        if (SelectedSelectorItem is not Playlist playlist)
            throw new InvalidOperationException("Select a playlist.");

        await _service.UpdatePlaylistNameAsync(playlist.PlaylistId, Name.Trim());
    }

    private async Task AddOrUpdateTrackAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Track name is required.");

        if (!TryParseMmSsToMs(MillisecondsText, out var milliseconds))
            throw new InvalidOperationException("Length must be in format mm:ss (e.g. 3:45).");

        if (Mode == CrudMode.Add)
        {
            int mediaTypeId = _defaultMediaTypeId ?? (await _service.GetMediaTypesAsync())
                .FirstOrDefault()?.MediaTypeId
                ?? throw new InvalidOperationException("No MediaType available in DB (cannot add track).");

            var albumId = SelectedAlbum?.AlbumId;

            await _service.CreateTrackAsync(
                name: Name.Trim(),
                milliseconds: milliseconds,
                mediaTypeId: mediaTypeId,
                albumId: albumId,
                genreId: null,
                composer: null);

            return;
        }

        if (SelectedSelectorItem is not Track track)
            throw new InvalidOperationException("Select a track.");

     
        await _service.UpdateTrackAsync(
            trackId: track.TrackId,
            name: Name.Trim(),
            milliseconds: milliseconds,
            mediaTypeId: track.MediaTypeId,
            albumId: track.AlbumId,
            genreId: track.GenreId,
            composer: track.Composer);
    }
}
