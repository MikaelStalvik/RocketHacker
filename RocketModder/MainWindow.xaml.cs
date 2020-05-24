using System.Windows;

namespace RocketModder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm = new MainViewModel();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;
            ListView.ItemsSource = _vm.SelectedRocketFiles;
            _vm.UpdateUiAction += () =>
            {
                ListView.ItemsSource = null;
                ListView.ItemsSource = _vm.SelectedRocketFiles;
            };

        }

    }
}