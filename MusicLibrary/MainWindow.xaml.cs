using MusicLibrary.ViewModels;
using MusicLibrary.Views;
using MusicLibrary.Services;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MusicLibrary;

public partial class MainWindow : Window
{
    private MusicViewModel _vm;

    private IReadOnlyList<Artist> _allArtistsTree = Array.Empty<Artist>();
    private bool _isArtistSearchActive;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MusicViewModel(OpenDialogAndRefreshAsync);
        DataContext = _vm;
        Loaded += MainWindow_Loaded;
    }


    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAllAsync();
    }

    private readonly MusicLibrary.Services.MusicService _musicService = new();

    private async Task RefreshAllAsync()
    {
        var selectedPlaylistId = _vm.SelectedPlaylist?.PlaylistId;

        await _vm.LoadDataAsync();    // playlists
        await _vm.LoadLibraryAsync(); // library tracks
        await LoadArtistsAsync();     // treeview 

        if (selectedPlaylistId != null)
        {
            _vm.SelectedPlaylist = _vm.Playlists.FirstOrDefault(p => p.PlaylistId == selectedPlaylistId);

            if (_vm.SelectedPlaylist != null)
            {
                await _vm.LoadMoreTracksAsync(); 
            }
            else
            {
              
                _vm.Tracks.Clear();
                _vm.SelectedPlaylistTrack = null;
            }
        }
        else
        {
            _vm.Tracks.Clear();
            _vm.SelectedPlaylistTrack = null;
        }

    }


    private async Task LoadArtistsAsync()
    {
        var artists = await _vm.LoadArtistsTreeAsync();
        _allArtistsTree = artists;
        myTreeView.ItemsSource = new ObservableCollection<Artist>(artists);
    }

    private void ArtistsSearchButton_Click(object sender, RoutedEventArgs e)
    {
        ApplyArtistTreeSearch();
    }

    private void ArtistsClearSearchButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MusicViewModel vm)
            vm.ArtistTreeSearchText = "";

        _isArtistSearchActive = false;
        myTreeView.ItemsSource = new ObservableCollection<Artist>(_allArtistsTree);
    }

    private void InfoButton_Click(object sender, RoutedEventArgs e)
    {
        const string message =
            "Click the plus icon to add artist \n\n" +
            "Search artist/album/track. Press Enter or click Search.\n\n" +
            "Right click artist/album/track to access context menu for editing.";

        MessageBox.Show(message, "Information", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void ArtistsSearchTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        ApplyArtistTreeSearch();
    }

    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MusicViewModel vm && e.NewValue is Track track)
        {
            vm.SelectedLibraryTrack = track;
        }
    }

    private void TreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is TreeViewItem item)
        {
            item.IsSelected = true;
            item.Focus();
            e.Handled = false;
        }
    }

    private async void DataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 20)
        {
            if (DataContext is MusicViewModel vm)
                await vm.LoadMoreTracksAsync();
        }
    }

    private void RowHeader_DeleteClicked(object sender, MouseButtonEventArgs e)
    {
        if (sender is DataGridRowHeader header &&
            header.DataContext is Track track &&
            DataContext is MusicViewModel vm)
        {
            vm.SelectedPlaylistTrack = track;
            if (vm.RemoveTrackFromPlaylistCommand.CanExecute(null))
                vm.RemoveTrackFromPlaylistCommand.Execute(null);
        }
    }

    private void TreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not MusicViewModel vm)
            return;

        if (vm.SelectedPlaylist == null)
        {
            MessageBox.Show("Select a playlist first.", "No playlist selected");
            return;
        }

        if (myTreeView.SelectedItem is not Track track)
            return;

        vm.SelectedLibraryTrack = track;

        if (vm.AddTrackToPlaylistCommand.CanExecute(null))
            vm.AddTrackToPlaylistCommand.Execute(null);

    }

    private void ApplyArtistTreeSearch()
    {
        if (DataContext is not MusicViewModel vm)
            return;

        string query = (vm.ArtistTreeSearchText ?? "").Trim();

        if (string.IsNullOrWhiteSpace(query))
        {
            _isArtistSearchActive = false;
            myTreeView.ItemsSource = new ObservableCollection<Artist>(_allArtistsTree);
            return;
        }

        _isArtistSearchActive = true;

        var filtered = BuildFilteredArtistTree(_allArtistsTree, query);
        myTreeView.ItemsSource = new ObservableCollection<Artist>(filtered);

        // Expandera så matchande album/tracks syns direkt
        Dispatcher.BeginInvoke(new Action(() => ExpandAllArtistsAndAlbums(myTreeView)));
    }


    private static List<Artist> BuildFilteredArtistTree(IEnumerable<Artist> sourceArtists, string query)
    {
        var results = new List<Artist>();
        var q = query.Trim();

        foreach (var artist in sourceArtists)
        {
            bool artistMatches =
                !string.IsNullOrWhiteSpace(artist.Name) &&
                artist.Name.Contains(q, StringComparison.OrdinalIgnoreCase);

            if (artistMatches)
            {
                // Artist-match -> visa hela artistens träd
                results.Add(CloneArtistWithAllChildren(artist));
                continue;
            }

            var matchedAlbums = new List<Album>();

            foreach (var album in artist.Albums)
            {
                bool albumMatches =
                    !string.IsNullOrWhiteSpace(album.Title) &&
                    album.Title.Contains(q, StringComparison.OrdinalIgnoreCase);

                if (albumMatches)
                {
                    matchedAlbums.Add(CloneAlbumWithAllTracks(album));
                    continue;
                }

                var matchedTracks = album.Tracks
                    .Where(t => !string.IsNullOrWhiteSpace(t.Name) &&
                                t.Name.Contains(q, StringComparison.OrdinalIgnoreCase))
                    .Select(CloneTrackShallow)
                    .ToList();

                if (matchedTracks.Count > 0)
                {
                    matchedAlbums.Add(CloneAlbumWithSpecificTracks(album, matchedTracks));
                }
            }

            if (matchedAlbums.Count > 0)
            {
                var artistClone = new Artist
                {
                    ArtistId = artist.ArtistId,
                    Name = artist.Name,
                    Albums = matchedAlbums
                };

                // sätt back-references för trygg navigation i UI/dialoger
                foreach (var albumClone in artistClone.Albums)
                    albumClone.Artist = artistClone;

                results.Add(artistClone);
            }
        }

        return results;
    }

    private static Artist CloneArtistWithAllChildren(Artist artist)
    {
        var artistClone = new Artist
        {
            ArtistId = artist.ArtistId,
            Name = artist.Name,
            Albums = artist.Albums.Select(CloneAlbumWithAllTracks).ToList()
        };

        foreach (var albumClone in artistClone.Albums)
            albumClone.Artist = artistClone;

        return artistClone;
    }

    private static Album CloneAlbumWithAllTracks(Album album)
    {
        var albumClone = new Album
        {
            AlbumId = album.AlbumId,
            Title = album.Title,
            ArtistId = album.ArtistId,
            Tracks = album.Tracks.Select(CloneTrackShallow).ToList()
        };

        foreach (var trackClone in albumClone.Tracks)
            trackClone.Album = albumClone;

        return albumClone;
    }

    private static Album CloneAlbumWithSpecificTracks(Album album, List<Track> tracks)
    {
        var albumClone = new Album
        {
            AlbumId = album.AlbumId,
            Title = album.Title,
            ArtistId = album.ArtistId,
            Tracks = tracks
        };

        foreach (var trackClone in albumClone.Tracks)
            trackClone.Album = albumClone;

        return albumClone;
    }

    private static Track CloneTrackShallow(Track track)
    {
        return new Track
        {
            TrackId = track.TrackId,
            Name = track.Name,
            AlbumId = track.AlbumId,
            MediaTypeId = track.MediaTypeId,
            GenreId = track.GenreId,
            Composer = track.Composer,
            Milliseconds = track.Milliseconds,
            Bytes = track.Bytes,
            UnitPrice = track.UnitPrice
        };
    }


    private static void ExpandAllArtistsAndAlbums(TreeView treeView)
    {
        foreach (var item in treeView.Items)
        {
            if (treeView.ItemContainerGenerator.ContainerFromItem(item) is not TreeViewItem artistItem)
                continue;

            artistItem.IsExpanded = true;
            artistItem.UpdateLayout();

            foreach (var album in artistItem.Items)
            {
                if (artistItem.ItemContainerGenerator.ContainerFromItem(album) is TreeViewItem albumItem)
                {
                    albumItem.IsExpanded = true;
                }
            }
        }
    }

    private void PlaylistItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem item)
        {
            item.IsSelected = true;
            item.Focus();
            e.Handled = false;
        }
    }

    //Meny
    private async Task OpenDialogAndRefreshAsync(CrudMode mode, EntityType entity, object? context)
    {
       
        if (mode == CrudMode.Delete && context != null)
        {
            switch (entity)
            {
                case EntityType.Artist when context is Artist artist:
                    await _musicService.DeleteArtistAsync(artist.ArtistId);
                    await RefreshAllAsync();
                    return;

                case EntityType.Album when context is Album album:
                    await _musicService.DeleteAlbumAsync(album.AlbumId);
                    await RefreshAllAsync();
                    return;

                case EntityType.Track when context is Track track:
                    await _musicService.DeleteTrackAsync(track.TrackId);
                    await RefreshAllAsync();
                    return;
            }
        }

       
        var dlg = new EditDialog(mode, entity, context) { Owner = this };
        var result = dlg.ShowDialog();

        if (result == true)
            await RefreshAllAsync();
    }

    public static class EditCommands
    {
        public static readonly RoutedUICommand AddPlaylist = new("Add Playlist", "AddPlaylist", typeof(EditCommands));
        public static readonly RoutedUICommand AddArtist = new("Add Artist", "AddArtist", typeof(EditCommands));
        public static readonly RoutedUICommand AddAlbum = new("Add Album", "AddAlbum", typeof(EditCommands));
        public static readonly RoutedUICommand AddTrack = new("Add Track", "AddTrack", typeof(EditCommands));
    }

    private async void AddPlaylist_Executed(object sender, ExecutedRoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Playlist, null);

    private async void AddArtist_Executed(object sender, ExecutedRoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Artist, null);

    private async void AddAlbum_Executed(object sender, ExecutedRoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Album, null);

    private async void AddTrack_Executed(object sender, ExecutedRoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Track, null);


    private async void AddPlaylist_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Playlist, null);

    private async void UpdatePlaylist_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Update, EntityType.Playlist, null);

    private async void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Delete, EntityType.Playlist, null);

    private async void AddArtist_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Artist, null);

    private async void UpdateArtist_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Update, EntityType.Artist, null);

    private async void DeleteArtist_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Delete, EntityType.Artist, null);

    private async void AddTrack_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Track, null);

    private async void UpdateTrack_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Update, EntityType.Track, null);

    private async void DeleteTrack_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Delete, EntityType.Track, null);
    private async void AddAlbum_Click(object sender, RoutedEventArgs e)
    => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Album, null);

    private async void UpdateAlbum_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Update, EntityType.Album, null);

    private async void DeleteAlbum_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Delete, EntityType.Album, null);

}
