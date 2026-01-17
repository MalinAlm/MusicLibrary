using System.Windows;
using MusicLibrary.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;


namespace MusicLibrary.Views;

public partial class EditDialog : Window
{
    public EditDialog(CrudMode mode, EntityType entity, object? context = null)
    {
        InitializeComponent();
        DataContext = new EditDialogViewModel(mode, entity, this, context);
    }

    private void LibraryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is EditDialogViewModel vm)
        {
           
            vm.SelectedLibraryTrack = e.NewValue as Track;
        }
    }

    private double _previousScrollExtentHeight = 0;
    private double _previousVerticalOffset = 0;


    private async void SelectorList_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (DataContext is not EditDialogViewModel vm)
            return;

        if (e.ExtentHeightChange != 0)
            return;

        if (e.VerticalChange <= 0)
            return;

        bool userIsNearBottom =
            e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 20;

        if (!userIsNearBottom)
            return;

        if (vm.Entity == EntityType.Track)
            await vm.LoadNextTracksPageForSelectorAsync();
        else if (vm.Entity == EntityType.Artist)
            await vm.LoadNextArtistsPageForSelectorAsync();
    }


    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (DataContext is EditDialogViewModel viewModel)
            await viewModel.ApplySearchAndReloadAsync();
    }

    private async void ArtistSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;
        if (DataContext is EditDialogViewModel vm)
            await vm.ApplyArtistSearchAndReloadAsync();
    }

}
