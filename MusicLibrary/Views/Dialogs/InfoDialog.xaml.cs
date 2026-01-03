using System.Windows;

namespace MusicLibrary.Views.Dialogs
{
    public partial class InfoDialog : Window
    {
        public InfoDialog(string dialogTitle, string messageText)
        {
            InitializeComponent();
            DataContext = new InfoDialogViewModel(dialogTitle, messageText);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private sealed class InfoDialogViewModel
        {
            public string DialogTitle { get; }
            public string MessageText { get; }

            public InfoDialogViewModel(string dialogTitle, string messageText)
            {
                DialogTitle = dialogTitle;
                MessageText = messageText;
            }
        }
    }
}
