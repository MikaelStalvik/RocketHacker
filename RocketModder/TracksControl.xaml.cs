using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RocketModder
{
    /// <summary>
    /// Interaction logic for TracksControl.xaml
    /// </summary>
    public partial class TracksControl : UserControl
    {
        public TracksControl()
        {
            InitializeComponent();
        }

        private List<TrackItem> _activeTracks { get; set; }

        public void RenderControl(List<TrackItem> rocketData)
        {
            _activeTracks = rocketData;
            InternalRenderControl(_activeTracks);
        }

        private void InternalRenderControl(List<TrackItem> activeTracks)
        {
            GridControl.ColumnDefinitions.Clear();
            GridControl.RowDefinitions.Clear();
            GridControl.Children.Clear();
            var rd = new RowDefinition { Height = new GridLength(0, GridUnitType.Auto) };
            GridControl.RowDefinitions.Add(rd);
            rd = new RowDefinition { Height = new GridLength(1, GridUnitType.Star) };
            GridControl.RowDefinitions.Add(rd);
            var i = 0;
            foreach (var track in activeTracks)
            {
                var cd = new ColumnDefinition { Width = new GridLength(128) };
                GridControl.ColumnDefinitions.Add(cd);

                var sp = new StackPanel();
                var rowCount = 0;
                foreach (var keyItem in track.Keys)
                {
                    var fallbackColor = "dedede".ParseColor();
                    var backgroundColor = rowCount.IsOdd() ? new SolidColorBrush(fallbackColor) : new SolidColorBrush(Colors.White);
                    var dp = new DockPanel { LastChildFill = true, Margin = new Thickness(4, 0, 4, 0) };
                    var tbd = new TextBlock { Width = 40, Background = backgroundColor, Text = keyItem.Row.ToString() };
                    DockPanel.SetDock(tbd, Dock.Left);
                    dp.Children.Add(tbd);
                    tbd = new TextBlock
                    {
                        TextAlignment = TextAlignment.Right,
                        Text = keyItem.Value.ToString(),
                        Background = backgroundColor
                    };
                    DockPanel.SetDock(tbd, Dock.Right);
                    dp.Children.Add(tbd);
                    sp.Children.Add(dp);
                    rowCount++;
                }

                Grid.SetColumn(sp, i);
                Grid.SetRow(sp, 1);
                GridControl.Children.Add(sp);

                var tb = new TextBlock
                {
                    Text = track.Name,
                    Background = new SolidColorBrush(track.Color.ParseColor()),
                    Margin = new Thickness(4, 0, 4, 0),
                    ToolTip = track.Name
                };
                Grid.SetColumn(tb, i);
                Grid.SetRow(tb, 0);
                GridControl.Children.Add(tb);

                i++;
            }
        }

        private void TextBoxBase_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_activeTracks == null) return;
            var tb = (sender as TextBox);
            var filterText = tb.Text.Trim();
            if (string.IsNullOrEmpty(filterText))
            {
                InternalRenderControl(_activeTracks);
            }
            else
            {
                InternalRenderControl(_activeTracks.Where(x => x.Name.Contains(filterText, StringComparison.CurrentCultureIgnoreCase)).ToList());
            }
        }
    }
}
