using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
    }
}
