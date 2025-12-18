using MusicLibrary.Commands;
using MusicLibrary.Services;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;

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

    // ---- Selector (Update/Delete) ----
    public bool ShowSelector => Mode != CrudMode.Add;
    public string SelectorLabel => Entity switch
    {
        EntityType.Playlist => "Select playlist",
        EntityType.Artist => "Select artist",
        _ => "Select track"
    };

    public string SelectorDisplayMember => Entity switch
    {
        EntityType.Playlist => "Name",
        EntityType.Artist => "Name",
        _ => "Name"
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
    public ObservableCollection<Genre> Genres { get; } = new();
    public ObservableCollection<MediaType> MediaTypes { get; } = new();

    private Album? _selectedAlbum;
    public Album? SelectedAlbum
    {
        get => _selectedAlbum;
        set { _selectedAlbum = value; RaisePropertyChanged(); }
    }

    private Genre? _selectedGenre;
    public Genre? SelectedGenre
    {
        get => _selectedGenre;
        set { _selectedGenre = value; RaisePropertyChanged(); }
    }

    private MediaType? _selectedMediaType;
    public MediaType? SelectedMediaType
    {
        get => _selectedMediaType;
        set { _selectedMediaType = value; RaisePropertyChanged(); }
    }

    private string? _composer;
    public string? Composer
    {
        get => _composer;
        set { _composer = value; RaisePropertyChanged(); }
    }

    private string _errorText = "";
    public string ErrorText
    {
        get => _errorText;
        set { _errorText = value; RaisePropertyChanged(); }
    }

    public RelayCommand ConfirmCommand { get; }

    public EditDialogViewModel(CrudMode mode, EntityType entity, Window owner)
    {
        Mode = mode;
        Entity = entity;
        _owner = owner;

        ConfirmCommand = new RelayCommand(_ => ConfirmAsync());

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
                SelectorItems = new ArrayList(await _service.GetArtistsAsync()); // ni har redan denna i service – annars byt till er load
            else
                SelectorItems = new ArrayList(await _service.GetTracksAsync());
        }

        if (IsTrack)
        {
            Albums.Clear();
            foreach (var a in await _service.GetAlbumsAsync()) Albums.Add(a);

        }
    }

    private void PrefillFromSelected()
    {
        ErrorText = "";

        if (SelectedSelectorItem == null) return;

        if (Entity == EntityType.Playlist && SelectedSelectorItem is Playlist p)
        {
            Name = p.Name ?? "";
        }
        else if (Entity == EntityType.Artist && SelectedSelectorItem is Artist a)
        {
            Name = a.Name ?? "";
        }
        else if (Entity == EntityType.Track && SelectedSelectorItem is Track t)
        {
            Name = t.Name ?? "";
            MillisecondsText = t.Milliseconds.ToString() ?? "";
            Composer = t.Composer;

            // dessa kan vara null
            SelectedAlbum = Albums.FirstOrDefault(x => x.AlbumId == t.AlbumId);
            SelectedGenre = Genres.FirstOrDefault(x => x.GenreId == t.GenreId);
            SelectedMediaType = MediaTypes.FirstOrDefault(x => x.MediaTypeId == t.MediaTypeId) ?? SelectedMediaType;
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

        if (SelectedMediaType == null)
            throw new InvalidOperationException("Media Type is required.");

        var albumId = SelectedAlbum?.AlbumId;
        var genreId = SelectedGenre?.GenreId;

        if (Mode == CrudMode.Add)
        {
            await _service.CreateTrackAsync(
                Name, ms, SelectedMediaType.MediaTypeId, albumId, genreId, Composer
            );
        }
        else // Update
        {
            if (SelectedSelectorItem is not Track t)
                throw new InvalidOperationException("Select a track.");

            await _service.UpdateTrackAsync(
                t.TrackId, Name, ms, SelectedMediaType.MediaTypeId, albumId, genreId, Composer
            );
        }
    }
}
