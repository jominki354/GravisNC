using System.Windows;
using GCode.Core.Services;

namespace GCode.App.WPF.Views
{
    public partial class ConfirmDialog : Window
    {
        public ConfirmResult Result { get; private set; } = ConfirmResult.Cancel;

        public ConfirmDialog(string message)
        {
            InitializeComponent();
            TxtMessage.Text = message;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            Result = ConfirmResult.Yes;
            Close();
        }

        private void BtnDontSave_Click(object sender, RoutedEventArgs e)
        {
            Result = ConfirmResult.No;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Result = ConfirmResult.Cancel;
            Close();
        }
    }
}
