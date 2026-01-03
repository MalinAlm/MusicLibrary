using System.Windows;
using MusicLibrary.ViewModels;

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
}
