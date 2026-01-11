using MusicLibrary.Commands;
using MusicLibrary.Services;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using MusicLibrary.Views.Dialogs;


namespace MusicLibrary.ViewModels;

public class EditDialogViewModel : BaseViewModel
{
    private readonly MusicService _service = new();
    private int? _defaultMediaTypeId;
    private readonly Window _owner;

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

    // ---- Selector (Update/Delete) ----
    public bool ShowSelector => Mode != CrudMode.Add;

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
    public bool ShowName => Mode != CrudMode.Delete;

    public string NameLabel => Entity switch
    {
        EntityType.Playlist => "Edit playlist name",
        EntityType.Artist => "Edit artist name",
        EntityType.Album => "Edit album title",
        EntityType.Track => "Edit track name",
        _ => "Edit name"
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

    public EditDialogViewModel(CrudMode mode, EntityType entity, Window owner)
    {
        Mode = mode;
        Entity = entity;
        _owner = owner;

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

        if (Entity == EntityType.Playlist && SelectedSelectorItem is Playlist p)
        {
            Name = p.Name ?? "";


            if (Mode == CrudMode.Update)
            {
                _ = LoadLibraryAlbumsOnceAsync();
                _ = ReloadPlaylistTracksAsync();
            }
        }
        else if (Entity == EntityType.Artist && SelectedSelectorItem is Artist a)
        {
            Name = a.Name ?? "";
        }
        else if (Entity == EntityType.Track && SelectedSelectorItem is Track selectedTrack)
        {
            Name = selectedTrack.Name ?? "";
            MillisecondsText = FormatMsToMmSs(selectedTrack.Milliseconds);

            SelectedAlbum = Albums.FirstOrDefault(album => album.AlbumId == selectedTrack.AlbumId);
            SelectedMediaType = MediaTypes.FirstOrDefault(mediaType => mediaType.MediaTypeId == selectedTrack.MediaTypeId);
        }
        else if (Entity == EntityType.Album && SelectedSelectorItem is Album al)
        {
            Name = al.Title ?? "";
            SelectedArtistForAlbum = Artists.FirstOrDefault(x => x.ArtistId == al.ArtistId);
        }
    }

    private async Task ConfirmAsync()
    {
        // stoppa dubbelkörning
        if (IsBusy)
            return;

        IsBusy = true;

        try
        {
            ErrorText = "";

            string itemDescription = GetPendingItemDescription();

            string title = GetConfirmTitle();
            string okText = GetOkButtonText();
            string verb = GetActionVerb();

            string message = $"Are you sure you want to {verb}:\n\n{itemDescription}?";

            bool userConfirmed = UserConfirmedAction(title, message, okText);
            if (!userConfirmed)
                return;


            if (Entity == EntityType.Playlist)
                await DoPlaylistAsync();
            else if (Entity == EntityType.Artist)
                await DoArtistAsync();
            else if (Entity == EntityType.Album)
                await DoAlbumAsync();
            else
                await DoTrackAsync();


            ShowActionSuccess(Mode, itemDescription);

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

    private void ShowActionSuccess(CrudMode mode, string itemDescription)
    {
        string title = mode switch
        {
            CrudMode.Add => "Added",
            CrudMode.Update => "Updated",
            CrudMode.Delete => "Deleted",
            _ => "Done"
        };

        var infoDialog = new InfoDialog(
            dialogTitle: title,
            messageText: $"{title}:\n\n{itemDescription}")
        {
            Owner = _owner
        };

        infoDialog.ShowDialog();
    }
    private bool UserConfirmedAction(string dialogTitle, string messageText, string okButtonText)
    {
        var confirmDialog = new ConfirmDialog(
            dialogTitle: dialogTitle,
            messageText: messageText,
            okButtonText: okButtonText,
            cancelButtonText: "Cancel")
        {
            Owner = _owner
        };

        return confirmDialog.ShowDialog() == true;
    }

    private string GetConfirmTitle() => Mode switch
    {
        CrudMode.Add => "Confirm add",
        CrudMode.Update => "Confirm update",
        CrudMode.Delete => "Confirm delete",
        _ => "Confirm"
    };

    private string GetOkButtonText() => Mode switch
    {
        CrudMode.Add => "Add",
        CrudMode.Update => "Update",
        CrudMode.Delete => "Delete",
        _ => "OK"
    };

    private string GetActionVerb() => Mode switch
    {
        CrudMode.Add => "add",
        CrudMode.Update => "update",
        CrudMode.Delete => "delete",
        _ => "apply changes to"
    };


    private static string Fallback(string? value, string fallback)
    => string.IsNullOrWhiteSpace(value) ? fallback : value;


    private string GetPendingItemDescription()
    {

        if (Mode == CrudMode.Delete)
            return GetSelectedItemDescriptionForDialog();


        if (Mode == CrudMode.Add)
        {
            return Entity switch
            {
                EntityType.Playlist =>
                    $"Playlist:\nName: {Fallback(Name, "(empty)")}",

            EntityType.Artist =>
                string.IsNullOrWhiteSpace(NewAlbumTitle)
                    ? $"Artist:\nName: {Fallback(Name, "(empty)")}"
                    : $"Artist:\nName: {Fallback(Name, "(empty)")}\nCreate album: {NewAlbumTitle}",

            EntityType.Album =>
                $"Album:\nTitle: {Fallback(Name, "(empty)")}\nArtist: {SelectedArtistForAlbum?.Name ?? "(none)"}",

            EntityType.Track =>
                $"Track:\nName: {Fallback(Name, "(empty)")}\nLength (ms): {Fallback(MillisecondsText, "(empty)")}\nAlbum: {SelectedAlbum?.Title ?? "(No album)"}",

            _ => Fallback(Name, "(empty)")
            };
        }

        if (Mode == CrudMode.Update)
        {
            if (SelectedSelectorItem == null)
                return "(No item selected)";

            return Entity switch
            {
                EntityType.Playlist when SelectedSelectorItem is Playlist p =>
                    "Playlist:\n" +
                    $"Name: {Fallback(p.Name, "(No name)")} → {Fallback(Name, "(empty)")}",

                EntityType.Artist when SelectedSelectorItem is Artist a =>
                    "Artist:\n" +
                    $"Name: {Fallback(a.Name, "(No name)")} → {Fallback(Name, "(empty)")}",

                EntityType.Album when SelectedSelectorItem is Album al =>
                    "Album:\n" +
                    $"Title: {Fallback(al.Title, "(No title)")} → {Fallback(Name, "(empty)")}\n" +
                    $"Artist: {al.Artist?.Name ?? "(Unknown artist)"} → {SelectedArtistForAlbum?.Name ?? "(none)"}",

                EntityType.Track when SelectedSelectorItem is Track t =>
                    "Track:\n" +
                    $"Name: {Fallback(t.Name, "(No name)")} → {Fallback(Name, "(empty)")}\n" +
                    $"Length (ms): {t.Milliseconds} → {Fallback(MillisecondsText, "(empty)")}\n" +
                    $"Album: {t.Album?.Title ?? "(No album)"} → {SelectedAlbum?.Title ?? "(No album)"}",

                _ => SelectedSelectorItem.ToString() ?? ""
            };
        }

        return "";
    }


    // ---- Playlist CRUD ----
    private async Task DoPlaylistAsync()
    {
        if (Mode == CrudMode.Add)
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException("Name is required.");

            await _service.CreatePlaylistAsync(Name);
        }
        else if (Mode == CrudMode.Update)
        {
            if (SelectedSelectorItem is not Playlist p)
                throw new InvalidOperationException("Select a playlist.");

            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException("Name is required.");

            await _service.UpdatePlaylistNameAsync(p.PlaylistId, Name);
        }
        else 
        {
            if (SelectedSelectorItem is not Playlist p)
                throw new InvalidOperationException("Select a playlist.");

            await _service.DeletePlaylistAsync(p.PlaylistId);
        }
    }

    // ---- Artist CRUD ----
    private async Task DoArtistAsync()
    {
        if (Mode == CrudMode.Add)
        {
            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException("Name is required.");

            var createdArtist = await _service.CreateArtistAsync(Name.Trim());

            if (!string.IsNullOrWhiteSpace(NewAlbumTitle))
                await _service.CreateAlbumAsync(NewAlbumTitle.Trim(), createdArtist.ArtistId);

            return;
        }
        else if (Mode == CrudMode.Update)
        {
            if (SelectedSelectorItem is not Artist a)
                throw new InvalidOperationException("Select an artist.");

            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException("Name is required.");

            await _service.UpdateArtistAsync(a.ArtistId, Name);
        }
        else 
        {
            if (SelectedSelectorItem is not Artist a)
                throw new InvalidOperationException("Select an artist.");

            await _service.DeleteArtistAsync(a.ArtistId);
        }
    }

    // ---- Album CRUD ----

    private async Task DoAlbumAsync()
    {
        if (Mode == CrudMode.Delete)
        {
            if (SelectedSelectorItem is not Album al)
                throw new InvalidOperationException("Select an album.");

            await _service.DeleteAlbumAsync(al.AlbumId);
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Album title is required.");

        if (SelectedArtistForAlbum == null)
            throw new InvalidOperationException("Select an artist.");

        if (Mode == CrudMode.Add)
            await _service.CreateAlbumAsync(Name.Trim(), SelectedArtistForAlbum.ArtistId);
        else
        {
            if (SelectedSelectorItem is not Album al)
                throw new InvalidOperationException("Select an album.");

            await _service.UpdateAlbumAsync(al.AlbumId, Name.Trim(), SelectedArtistForAlbum.ArtistId);
        }
    }

    // ---- Track CRUD ----
    private async Task DoTrackAsync()
    {
        if (Mode == CrudMode.Delete)
        {
            if (SelectedSelectorItem is not Track track)
                throw new InvalidOperationException("Select a track.");

            await _service.DeleteTrackAsync(track.TrackId);
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Track name is required.");

        if (!TryParseMmSsToMs(MillisecondsText, out var ms))
            throw new InvalidOperationException("Length must be in format mm:ss (e.g. 3:45).");

        var albumId = SelectedAlbum?.AlbumId;
        var genreId = (int?)null;

        if (Mode == CrudMode.Add)
        {
            if (_defaultMediaTypeId == null)
                throw new InvalidOperationException("No MediaType available in DB (cannot add track).");

            await _service.CreateTrackAsync(
                Name,
                ms,
                _defaultMediaTypeId.Value,
                albumId,
                genreId,
                composer: null
            );
        }
        else 
        {
            if (SelectedSelectorItem is not Track track)
                throw new InvalidOperationException("Select a track.");

            await _service.UpdateTrackAsync(
                track.TrackId,
                Name,
                ms,
                track.MediaTypeId,
                albumId,
                genreId,
                composer: null
            );
        }
    }




    // ---- Bibliotek: ladda album->tracks + filter (album + track) ----
    private async Task LoadLibraryAlbumsOnceAsync()
    {
        if (LibraryAlbums.Count > 0) return;

        var tracks = await _service.GetTracksAsync(); 

        LibraryAlbums.Clear();

        foreach (var g in tracks.GroupBy(t => t.AlbumId))
        {
            var first = g.FirstOrDefault();

            var node = new AlbumNodeViewModel
            {
                AlbumId = first?.AlbumId ?? -1,
                Title = first?.Album?.Title ?? "(No album)"
            };

            foreach (var t in g.OrderBy(x => x.Name))
                node.Tracks.Add(t);

            LibraryAlbums.Add(node);
        }

        // Sortera album
        var sorted = LibraryAlbums.OrderBy(a => a.Title).ToList();
        LibraryAlbums.Clear();
        foreach (var a in sorted) LibraryAlbums.Add(a);

        LibraryAlbumsView = CollectionViewSource.GetDefaultView(LibraryAlbums);
        LibraryAlbumsView.Filter = obj =>
        {
            if (obj is not AlbumNodeViewModel a) return false;
            if (string.IsNullOrWhiteSpace(TrackSearchText)) return true;

            var q = TrackSearchText.Trim();

            // albumtitel
            if (a.Title.Contains(q, StringComparison.OrdinalIgnoreCase))
                return true;

            // låttitel
            return a.Tracks.Any(t =>
                t.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) == true);
        };

        RaisePropertyChanged(nameof(LibraryAlbumsView));
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

    private string GetSelectedItemDescriptionForDialog()
    {
        if (SelectedSelectorItem == null)
            return "";

        if (Entity == EntityType.Playlist && SelectedSelectorItem is Playlist playlist)
            return $"Playlist: {playlist.Name ?? "(No name)"}";

        if (Entity == EntityType.Artist && SelectedSelectorItem is Artist artist)
            return $"Artist: {artist.Name ?? "(No name)"}";

        if (Entity == EntityType.Album && SelectedSelectorItem is Album album)
            return $"Album: {album.Title ?? "(No title)"}\nArtist: {album.Artist?.Name ?? "(Unknown artist)"}";

        if (Entity == EntityType.Track && SelectedSelectorItem is Track track)
        {
            string albumTitle = track.Album?.Title ?? "(No album)";
            return $"Track: {track.Name ?? "(No name)"}\nAlbum: {albumTitle}";
        }

        return "";
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

}
