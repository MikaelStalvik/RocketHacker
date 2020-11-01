using System.Windows;

namespace RocketModder
{
    /// <summary>
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        public InputWindow()
        {
            InitializeComponent();
            Owner = Application.Current.MainWindow;
        }

        public static (bool, int) PromptForValue()
        {
            var dlg = new InputWindow();
            dlg.ShowDialog();
            if (dlg.DialogResult == true)
            {
                int.TryParse(dlg.InputTb.Text, out var res);
                return (true, res);
            }

            return (false, -1);
        }

        private void OkButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
