using System.Windows;
using MusicLibrary.ViewModels;
using System.Windows.Controls;
using System.Windows.Input;


namespace MusicLibrary.Views;

public partial class EditDialog : Window
{
    public EditDialog(CrudMode mode, EntityType entity)
    {
        InitializeComponent();
        DataContext = new EditDialogViewModel(mode, entity, this);
    }

    private void LibraryTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (DataContext is EditDialogViewModel vm)
        {
            // Om man klickar på en Track så sätt den som vald (för Add selected)
            vm.SelectedLibraryTrack = e.NewValue as Track;
        }
    }

    private double _previousScrollExtentHeight = 0;
    private double _previousVerticalOffset = 0;

    private async void TrackSelectorList_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (DataContext is not EditDialogViewModel viewModel)
            return;

        bool extentHeightChangedBecauseItemsWereAdded = e.ExtentHeightChange != 0;
        if (extentHeightChangedBecauseItemsWereAdded)
            return;

        bool userScrolledDown = e.VerticalChange > 0;
        if (!userScrolledDown)
            return;

        bool userIsNearBottom = e.VerticalOffset + e.ViewportHeight >= e.ExtentHeight - 20;
        if (!userIsNearBottom)
            return;

        await viewModel.LoadNextTracksPageForSelectorAsync();
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
            return;

        if (DataContext is EditDialogViewModel viewModel)
            await viewModel.ApplySearchAndReloadAsync();
    }

}
