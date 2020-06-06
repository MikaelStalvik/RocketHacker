using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Microsoft.Win32;
using Newtonsoft.Json;
using RocketModder.Annotations;

namespace RocketModder
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        public Action UpdateUiAction { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<RocketFile> SelectedRocketFiles = new ObservableCollection<RocketFile>();

        private TimeSpan _calculatedLength;
        public TimeSpan CalculatedLength
        {
            get => _calculatedLength;
            set
            {
                _calculatedLength = value;
                OnPropertyChanged();
            }
        }
        private int _calculateRowLength;
        public int CalculateRowLength
        {
            get => _calculateRowLength;
            set
            {
                _calculateRowLength = value;
                CalculatedLength = CalcOffsetInTime(_calculateRowLength);
                OnPropertyChanged();
            }
        }

        private RocketFile _selectedItem;
        public RocketFile SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
                if (SelectedItem != null) SelectedOffset = SelectedItem.Offset;
                OnPropertyChanged();
            }
        }

        private int _selectedOffset;
        public int SelectedOffset
        {
            get => _selectedOffset;
            set
            {
                _selectedOffset = value;
                SelectedItem.Offset = SelectedOffset;
                SelectedItem.OffsetInTime = CalcOffsetInTime(SelectedOffset);
                OnPropertyChanged();
                UpdateUiAction?.Invoke();
            }
        }

        private TracksHeader _tracksHeader;
        public TracksHeader TracksHeader
        {
            get => _tracksHeader;
            set
            {
                _tracksHeader = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand AddFilesCommand { get; set; }
        public RelayCommand SaveFileCommand { get; set; }
        public RelayCommand CalculateCommand { get; set; }
        public RelayCommand LoadProjectCommand { get; set; }
        public RelayCommand SaveProjectCommand { get; set; }
        public RelayCommand DeleteCommand { get; set; }
        public RelayCommand MoveUpCommand { get; set; }
        public RelayCommand MoveDownCommand { get; set; }

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
                    CalculateCommand.Execute(null);
                    OnPropertyChanged(nameof(SelectedRocketFiles));
                }
            });
            SaveFileCommand = new RelayCommand(o =>
            {
                var sfd = new SaveFileDialog {Filter = "Rocket files|*.rocket"};
                if (sfd.ShowDialog() == true)
                {
                    GenerateFile(sfd.FileName);
                }
            });
            CalculateCommand = new RelayCommand(ExecuteCalculate);
            LoadProjectCommand = new RelayCommand(o =>
            {
                var ofn = new OpenFileDialog {Filter = "Projects|*.json"};
                if (ofn.ShowDialog() == true)
                {
                    var json = File.ReadAllText(ofn.FileName);
                    var prj = JsonConvert.DeserializeObject<ProjectHolder>(json);
                    TracksHeader = prj.TracksHeader;
                    SelectedRocketFiles = new ObservableCollection<RocketFile>(prj.RocketFiles);
                    UpdateUiAction?.Invoke();
                }
            });
            SaveProjectCommand = new RelayCommand(o =>
            {
                var sfn = new SaveFileDialog { Filter = "Projects|*.json"};
                if (sfn.ShowDialog() == true)
                {
                    var prj = new ProjectHolder
                    {
                        TracksHeader = TracksHeader,
                        RocketFiles = SelectedRocketFiles.ToList()
                    };
                    var json = JsonConvert.SerializeObject(prj);
                    File.WriteAllText(sfn.FileName, json);
                }
            });
            DeleteCommand = new RelayCommand(o =>
            {
                if (SelectedItem != null)
                {
                    SelectedRocketFiles.Remove(SelectedItem);
                }
            });
            MoveUpCommand = new RelayCommand(o =>
            {
                if (SelectedItem != null)
                {
                    var idx = SelectedRocketFiles.IndexOf(SelectedItem);
                    if (idx > 0)
                    {
                        SelectedRocketFiles.Move(idx, idx - 1);
                    }
                }
            });
            MoveDownCommand = new RelayCommand(o =>
            {
                if (SelectedItem != null)
                {
                    var idx = SelectedRocketFiles.IndexOf(SelectedItem);
                    if (idx < SelectedRocketFiles.Count - 1)
                    {
                        SelectedRocketFiles.Move(idx, idx + 1);
                    }
                }
            });
            TracksHeader = new TracksHeader
            {
                Rows = 10000,
                StartRow = 0,
                EndRow = 10000,
                RowsPerBeat = 8,
                BeatsPerMin = 128
            };
            OnPropertyChanged(nameof(TracksHeader));
        }

        private TimeSpan CalcOffsetInTime(int offset)
        {
            var rowRate = (TracksHeader.BeatsPerMin / 60.0) * TracksHeader.RowsPerBeat;
            return TimeSpan.FromSeconds(offset / rowRate);
        }

        private void ExecuteCalculate(object obj)
        {
            var prevOffset = 0;
            var maxList = new List<int>();
            var itemMaxList = new List<int>();
            var i = 0;
            foreach (var rocketFile in SelectedRocketFiles)
            {
                var rocket = ReadRocketFile(rocketFile.Filename, i == 0);
                var highest = -999;
                foreach (var track in rocket)
                {
                    if (track.Keys.Any())
                    {
                        var keys = track.Keys.ToList();
                        var highestRow = keys.Max(x => x.Row);
                        if (highestRow > highest)
                        {
                            highest = highestRow;
                        }
                    }
                }
                maxList.Add(highest + prevOffset);
                itemMaxList.Add(highest);
                prevOffset = highest + prevOffset;
                i++;
            }

            for (i = 0; i < SelectedRocketFiles.Count; i++)
            {
                SelectedRocketFiles[i].MaxLength = itemMaxList[i];
                SelectedRocketFiles[i].LengthInTime = CalcOffsetInTime(SelectedRocketFiles[i].MaxLength);
            }
            for (i = 1; i < SelectedRocketFiles.Count; i++)
            {
                SelectedRocketFiles[i].Offset = maxList[i - 1];
                SelectedRocketFiles[i].OffsetInTime = CalcOffsetInTime(SelectedRocketFiles[i].Offset);
            }

            OnPropertyChanged(nameof(SelectedRocketFiles));
            UpdateUiAction?.Invoke();
        }

        private void ReadHeader(XDocument xml)
        {
            var hdr = from c in xml.Descendants("tracks")
                select new TracksHeader
                {
                    Rows = int.Parse(c.Attribute("rows")?.Value ?? "0"),
                    StartRow = int.Parse(c.Attribute("startRow")?.Value ?? "0"),
                    EndRow = int.Parse(c.Attribute("endRow")?.Value ?? "0"),
                    RowsPerBeat = int.Parse(c.Attribute("rowsPerBeat")?.Value ?? "0"),
                    BeatsPerMin = int.Parse(c.Attribute("beatsPerMin")?.Value ?? "0"),
                };
            TracksHeader = hdr.FirstOrDefault(); 
            OnPropertyChanged(nameof(TracksHeader));
        }

        private List<TrackItem> ReadRocketFile(string filename, bool readHeader = false)
        {
            var xml = XDocument.Load(filename);

            if (readHeader) ReadHeader(xml);

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

        private void CleanDuplicates(List<TrackItem> data)
        {
            foreach (var track in data)
            {
                var keys = track.Keys.ToList();
                var newKeys = new List<KeyItem>();
                foreach (var item in keys)
                {
                    var exists = newKeys.FirstOrDefault(x => x.Row == item.Row);
                    if (exists == null)
                    {
                        newKeys.Add(item);
                    }
                    else
                    {
                        exists.Value = item.Value;
                        exists.Interpolation = item.Interpolation;
                    }
                }
                track.Keys = newKeys;
            }
        }

        private void SaveFile(string filename, IEnumerable<TrackItem> data)
        {
            // all data aggregated, generate XML
            var xd = new XDocument(new XElement("rootElement",
                new XElement("tracks",
                    new XAttribute("rows", 10000),
                    new XAttribute("startRow", 0),
                    new XAttribute("endRow", 10000),
                    new XAttribute("rowsPerBeat", 8),
                    new XAttribute("beatsPerMin", 134),
                    from track in data
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

            xd.Save(filename);
        }

        private void GenerateFile(string filename)
        {
            var resdata = new List<TrackItem>();
            for (var i = 0; i < SelectedRocketFiles.Count; i++)
            {
                var rocket = ReadRocketFile(SelectedRocketFiles[i].Filename, i == 0);
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
                        var ofs = SelectedRocketFiles[i].Offset;
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
            CleanDuplicates(resdata);

            SaveFile(filename, resdata);
        }
    }
}
