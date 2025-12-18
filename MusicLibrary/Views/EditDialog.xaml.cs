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
}
