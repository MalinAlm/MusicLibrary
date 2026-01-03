using System.Windows;

namespace MusicLibrary.Views.Dialogs
{
    public partial class ConfirmDialog : Window
    {
        public ConfirmDialog(string dialogTitle, string messageText, string okButtonText = "OK", string cancelButtonText = "Cancel")
        {
            InitializeComponent();
            DataContext = new ConfirmDialogViewModel(dialogTitle, messageText, okButtonText, cancelButtonText);
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private sealed class ConfirmDialogViewModel
        {
            public string DialogTitle { get; }
            public string MessageText { get; }
            public string OkButtonText { get; }
            public string CancelButtonText { get; }

            public ConfirmDialogViewModel(string dialogTitle, string messageText, string okButtonText, string cancelButtonText)
            {
                DialogTitle = dialogTitle;
                MessageText = messageText;
                OkButtonText = okButtonText;
                CancelButtonText = cancelButtonText;
            }
        }
    }
}
