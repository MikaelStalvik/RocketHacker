using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.Win32;
using RocketModder.Annotations;

namespace RocketModder
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public Action UpdateUiAction { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<RocketFile> SelectedRocketFiles = new ObservableCollection<RocketFile>();

        private RocketFile _selectedItem;
        public RocketFile SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand AddFilesCommand { get; set; }
        public RelayCommand SaveFileCommand { get; set; }
        public RelayCommand CalculateCommand { get; set; }

        public MainViewModel()
        {
            AddFilesCommand = new RelayCommand(o =>
            {
                var ofd = new OpenFileDialog {Multiselect = true, Filter = "Rocket files|*.rocket"};
                if (ofd.ShowDialog() == true)
                {
                    foreach (var file in ofd.FileNames)
                    {
                        SelectedRocketFiles.Add(new RocketFile {Filename = file, Offset = 0});
                    }
                    OnPropertyChanged(nameof(SelectedRocketFiles));
                }
            });
            SaveFileCommand = new RelayCommand(o =>
            {
                var sfd = new SaveFileDialog {Filter = "Rocket files|*.rocket"};
                if (sfd.ShowDialog() == true)
                {

                }
            });
            CalculateCommand = new RelayCommand(ExecuteCalculate);
        }

        private void ExecuteCalculate(object obj)
        {
            var maxList = new List<int>();
            foreach (var rocketFile in SelectedRocketFiles)
            {
                var rocket = ReadRocketFile(rocketFile.Filename);
                var highest = -999;
                foreach (var track in rocket)
                {
                    if (track.Keys.Any())
                    {
                        var keys = track.Keys.ToList();
                        var highestRow = keys.Max(x => x.Row);
                        if (highestRow > highest) highest = highestRow;
                    }
                }
                maxList.Add(highest);
            }

            for (var i = 1; i < SelectedRocketFiles.Count; i++)
            {
                SelectedRocketFiles[i].Offset = maxList[i - 1];
            }
            OnPropertyChanged(nameof(SelectedRocketFiles));
            UpdateUiAction?.Invoke();
        }

        private List<TrackItem> ReadRocketFile(string filename)
        {
            var xml = XDocument.Load(filename);
            var qry = from c in xml.Descendants("track")
                select new TrackItem
                {
                    Name = c.Attribute("name").Value,
                    Folded = int.Parse(c.Attribute("folded").Value),
                    MuteKeyCount = c.Attribute("muteKeyCount").Value,
                    Color = c.Attribute("color").Value,
                    Keys = from k in c.Descendants("key")
                        select new KeyItem
                        {
                            Row = int.Parse(k.Attribute("row").Value),
                            Value = double.Parse(k.Attribute("value").Value, CultureInfo.InvariantCulture),
                            Interpolation = int.Parse(k.Attribute("interpolation").Value)
                        }
                };
            return qry.ToList();
        }

        private string GenerateFile(List<List<TrackItem>> rockets, List<int> offsets)
        {
            var resdata = new List<TrackItem>();
            for (var i = 0; i < rockets.Count; i++)
            {
                var rocket = rockets[i];
                for (var j = 0; j < rocket.Count; j++)
                {
                    var item = rocket[j];
                    var existing = resdata.FirstOrDefault(x => x.Name.Equals(item.Name));
                    if (existing == null)
                    {
                        resdata.Add(item);
                    }
                    else
                    {
                        // add all keys
                        var ofs = offsets[i];
                        var existingKeys = existing.Keys.ToList();
                        foreach (var key in item.Keys)
                        {
                            existingKeys.Add(new KeyItem
                            {
                                Row = key.Row + ofs,
                                Value = key.Value,
                                Interpolation = key.Interpolation
                            });
                        }

                        existing.Keys = existingKeys;
                    }
                }
            }

            // all data aggregated, generate XML
            var xd = new XDocument(new XElement("rootElement",
                new XElement("tracks",
                    new XAttribute("rows", 10000),
                    new XAttribute("startRow", 0),
                    new XAttribute("endRow", 10000),
                    new XAttribute("rowsPerBeat", 8),
                    new XAttribute("beatsPerMin", 134),
                    from track in resdata
                    select new XElement("track",
                        new XAttribute("name", track.Name),
                        new XAttribute("color", track.Color),
                        new XAttribute("folded", track.Folded),
                        from key in track.Keys
                        select new XElement("key",
                            new XAttribute("row", key.Row),
                            new XAttribute("value", key.Value),
                            new XAttribute("interpolation", key.Interpolation)
                        )
                    )
                )));


            return string.Empty;
        }
    }
}
