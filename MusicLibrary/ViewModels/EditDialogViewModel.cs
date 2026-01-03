using MusicLibrary.Commands;
using MusicLibrary.Services;
using System.Collections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace MusicLibrary.ViewModels;

public class EditDialogViewModel : BaseViewModel
{
    private readonly MusicService _service = new();
    private readonly Window _owner;

    public CrudMode Mode { get; }
    public EntityType Entity { get; }

    public string DialogTitle => $"{Mode} {Entity}";
    public string ConfirmLabel => Mode switch
    {
        CrudMode.Add => "Add",
        CrudMode.Update => "Update",
        _ => "Delete"
    };

    public bool IsTrack => Entity == EntityType.Track;
    public bool IsUpdatePlaylist => Mode == CrudMode.Update && Entity == EntityType.Playlist;

    // ---- Selector (Update/Delete) ----
    public bool ShowSelector => Mode != CrudMode.Add;

    public string SelectorLabel => Entity switch
    {
        EntityType.Playlist => "Select playlist",
        EntityType.Artist => "Select artist",
        _ => "Select track"
    };

    private IList _selectorItems = new ArrayList();
    public IList SelectorItems
    {
        get => _selectorItems;
        set { _selectorItems = value; RaisePropertyChanged(); }
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

    // ---- Name field ----
    public bool ShowName => Mode != CrudMode.Delete;

    public string NameLabel => Entity switch
    {
        EntityType.Playlist => "Name",
        EntityType.Artist => "Name",
        _ => "Track name"
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

    public ObservableCollection<Album> Albums { get; } = new();

    private Album? _selectedAlbum;
    public Album? SelectedAlbum
    {
        get => _selectedAlbum;
        set { _selectedAlbum = value; RaisePropertyChanged(); }
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

        ConfirmCommand = new RelayCommand(_ => ConfirmAsync());

        AddTrackCommand = new RelayCommand(async _ => await AddTrackToPlaylistAsync(),
            _ => IsUpdatePlaylist &&
                 SelectedSelectorItem is Playlist &&
                 SelectedLibraryTrack != null);

        RemoveTrackCommand = new RelayCommand(async _ => await RemoveTrackFromPlaylistAsync(),
            _ => IsUpdatePlaylist &&
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
                SelectorItems = new ArrayList(await _service.GetPlaylistsAsync());
            else if (Entity == EntityType.Artist)
                SelectorItems = new ArrayList(await _service.GetArtistsAsync());
            else
                SelectorItems = new ArrayList(await _service.GetTracksAsync());
        }

        // Track-dialogen behöver album-lista
        if (IsTrack)
        {
            Albums.Clear();
            foreach (var a in await _service.GetAlbumsAsync())
                Albums.Add(a);
        }
    }

    private void PrefillFromSelected()
    {
        ErrorText = "";
        if (SelectedSelectorItem == null) return;

        if (Entity == EntityType.Playlist && SelectedSelectorItem is Playlist p)
        {
            Name = p.Name ?? "";

            // Vid Update Playlist: ladda bibliotek + playlist tracks
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
        else if (Entity == EntityType.Track && SelectedSelectorItem is Track t)
        {
            Name = t.Name ?? "";
            MillisecondsText = t.Milliseconds.ToString();

            SelectedAlbum = Albums.FirstOrDefault(x => x.AlbumId == t.AlbumId);
        }
    }

    private async void ConfirmAsync()
    {
        ErrorText = "";

        try
        {
            if (Entity == EntityType.Playlist)
                await DoPlaylistAsync();
            else if (Entity == EntityType.Artist)
                await DoArtistAsync();
            else
                await DoTrackAsync();

            _owner.DialogResult = true;
            _owner.Close();
        }
        catch (Exception ex)
        {
            ErrorText = ex.Message;
        }
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
        else // Delete
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

            await _service.CreateArtistAsync(Name);
        }
        else if (Mode == CrudMode.Update)
        {
            if (SelectedSelectorItem is not Artist a)
                throw new InvalidOperationException("Select an artist.");

            if (string.IsNullOrWhiteSpace(Name))
                throw new InvalidOperationException("Name is required.");

            await _service.UpdateArtistAsync(a.ArtistId, Name);
        }
        else // Delete
        {
            if (SelectedSelectorItem is not Artist a)
                throw new InvalidOperationException("Select an artist.");

            await _service.DeleteArtistAsync(a.ArtistId);
        }
    }

    // ---- Track CRUD (enklare: name + ms + album) ----
    // OBS: ni hade tidigare MediaType/Genre/Composer. Här behåller vi bara det som UI:t visar just nu.
    private async Task DoTrackAsync()
    {
        if (Mode == CrudMode.Delete)
        {
            if (SelectedSelectorItem is not Track t)
                throw new InvalidOperationException("Select a track.");

            await _service.DeleteTrackAsync(t.TrackId);
            return;
        }

        if (string.IsNullOrWhiteSpace(Name))
            throw new InvalidOperationException("Track name is required.");

        if (!int.TryParse(MillisecondsText, out var ms) || ms < 0)
            throw new InvalidOperationException("Length (ms) must be a non-negative number.");

        // Om ni vill kräva album: validera här.
        var albumId = SelectedAlbum?.AlbumId;

        if (Mode == CrudMode.Add)
        {
            // Ni behöver ett MediaTypeId i er service. Om ni vill återinföra MediaType i UI,
            // säg till så bygger vi det. För stunden stoppar vi med tydligt fel:
            throw new InvalidOperationException("Track Add kräver MediaTypeId. Lägg tillbaka MediaType i dialogen eller ändra service.");
        }
        else // Update
        {
            if (SelectedSelectorItem is not Track t)
                throw new InvalidOperationException("Select a track.");

            // Samma här: service kräver MediaTypeId. Vi stoppar tydligt.
            throw new InvalidOperationException("Track Update kräver MediaTypeId. Lägg tillbaka MediaType i dialogen eller ändra service.");
        }
    }

    // ---- Bibliotek: ladda album->tracks + filter (album + track) ----
    private async Task LoadLibraryAlbumsOnceAsync()
    {
        if (LibraryAlbums.Count > 0) return;

        var tracks = await _service.GetTracksAsync(); // inkluderar Album + Artist i er service

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
}
