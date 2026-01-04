using Microsoft.EntityFrameworkCore;
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
    private MusicViewModel _vm = new MusicViewModel();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAllAsync();
    }

    private async Task RefreshAllAsync()
    {
        await _vm.LoadDataAsync();    // playlists
        await _vm.LoadLibraryAsync(); // library tracks
        LoadArtists();                // treeview
    }

    // --- TreeView load (som ni redan har) ---
    private void LoadArtists()
    {
        using var db = new MusicContext();

        var artists = db.Artists
            .Include(a => a.Albums)
                .ThenInclude(al => al.Tracks)
            .OrderBy(a => a.Name ?? "")
            .ToList();

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
//Meny
    private async Task OpenDialogAndRefreshAsync(CrudMode mode, EntityType entity)
    {
        var dlg = new EditDialog(mode, entity) { Owner = this };
        var result = dlg.ShowDialog();

        if (result == true)
            await RefreshAllAsync();
    }

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
}
