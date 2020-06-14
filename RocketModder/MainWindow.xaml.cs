using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
            _vm.GridControl = TracksControl;
            DataContext = _vm;
            ListView.ItemsSource = _vm.SelectedRocketFiles;
            _vm.UpdateUiAction += () =>
            {
                ListView.ItemsSource = null;
                ListView.ItemsSource = _vm.SelectedRocketFiles;
            };
            _vm.RebindListviewAction += () =>
            {
                ICollectionView view = CollectionViewSource.GetDefaultView(_vm.SelectedRocketFiles);
                view.Refresh();
            };
        }

        private void ListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var lv = (ListView)sender;
            _vm.UpdateSelectedItem((RocketFile) lv.SelectedItem);
            e.Handled = true;
        }

    }
}