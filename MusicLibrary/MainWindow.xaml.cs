using Microsoft.EntityFrameworkCore;
using MusicLibrary;
using MusicLibrary.ViewModels;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MusicLibrary;

public partial class MainWindow : Window
{

    private MusicViewModel _vm = new MusicViewModel();

    public MainWindow()
    {
        InitializeComponent();

        DataContext = _vm;

        _vm.ArtistsChanged = ReloadArtists;

        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        await _vm.LoadDataAsync();
        LoadArtists();
       
    }

    private  void LoadArtists()
    {
        using var db = new MusicContext();

        var artists = db.Artists
            .Where(artist => artist.Albums.Count > 2)
            .Include(artist => artist.Albums)
            .ThenInclude(album => album.Tracks)
            .ToList();

         myTreeView.ItemsSource = new ObservableCollection<Artist>(artists);
    }

    public void ReloadArtists()
    {
        LoadArtists();
    }


    private void TreeView_SelectedItemChanged(
    object sender,
    RoutedPropertyChangedEventArgs<object> e)
{
    if (DataContext is MusicViewModel vm &&
        e.NewValue is Track track)
    {
        vm.SelectedLibraryTrack = track;
    }
}
    private async void DataGrid_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 20)
        {
            if (DataContext is MusicViewModel vm)
            {
                await vm.LoadMoreTracksAsync();
            }
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
            {
                vm.RemoveTrackFromPlaylistCommand.Execute(null);
            }
        }
    }


}