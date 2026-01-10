using MusicLibrary.ViewModels;
using MusicLibrary.Views;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace MusicLibrary;

public partial class MainWindow : Window
{
    private MusicViewModel _vm;

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

    private async Task RefreshAllAsync()
    {
        var selectedPlaylistId = _vm.SelectedPlaylist?.PlaylistId;

        await _vm.LoadDataAsync();    // playlists
        await _vm.LoadLibraryAsync(); // library tracks
        await LoadArtistsAsync();     // treeview (NU async)

        if (selectedPlaylistId != null)
        {
            _vm.SelectedPlaylist = _vm.Playlists.FirstOrDefault(p => p.PlaylistId == selectedPlaylistId);
            if (_vm.SelectedPlaylist != null)
                await _vm.LoadMoreTracksAsync();
        }
    }


    private async Task LoadArtistsAsync()
    {
        var artists = await _vm.LoadArtistsTreeAsync();
        myTreeView.ItemsSource = new ObservableCollection<Artist>(artists);
    }


    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is MusicViewModel vm && e.NewValue is Track track)
        {
            vm.SelectedLibraryTrack = track;
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


    //Meny
    private async Task OpenDialogAndRefreshAsync(CrudMode mode, EntityType entity)
    {
        var dlg = new EditDialog(mode, entity) { Owner = this };
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
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Playlist);

    private async void AddArtist_Executed(object sender, ExecutedRoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Artist);

    private async void AddAlbum_Executed(object sender, ExecutedRoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Album);

    private async void AddTrack_Executed(object sender, ExecutedRoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Track);


    private async void AddPlaylist_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Playlist);

    private async void UpdatePlaylist_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Update, EntityType.Playlist);

    private async void DeletePlaylist_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Delete, EntityType.Playlist);

    private async void AddArtist_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Artist);

    private async void UpdateArtist_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Update, EntityType.Artist);

    private async void DeleteArtist_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Delete, EntityType.Artist);

    private async void AddTrack_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Track);

    private async void UpdateTrack_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Update, EntityType.Track);

    private async void DeleteTrack_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Delete, EntityType.Track);
    private async void AddAlbum_Click(object sender, RoutedEventArgs e)
    => await OpenDialogAndRefreshAsync(CrudMode.Add, EntityType.Album);

    private async void UpdateAlbum_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Update, EntityType.Album);

    private async void DeleteAlbum_Click(object sender, RoutedEventArgs e)
        => await OpenDialogAndRefreshAsync(CrudMode.Delete, EntityType.Album);

}
