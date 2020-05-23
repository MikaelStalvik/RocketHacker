using System.Windows;

namespace RocketModder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _vm = new MainViewModel();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;
            SelectedRocketFilesLb.ItemsSource = _vm.SelectedRocketFiles;
            _vm.UpdateUiAction += () =>
            {
                SelectedRocketFilesLb.ItemsSource = null;
                SelectedRocketFilesLb.ItemsSource = _vm.SelectedRocketFiles;
            };
        }

    }
}