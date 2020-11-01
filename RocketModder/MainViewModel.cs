using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Xps.Packaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using RocketModder.Annotations;

namespace RocketModder
{
    public sealed class MainViewModel : INotifyPropertyChanged
    {
        public Action UpdateUiAction { get; set; }
        public Action RebindListviewAction { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        public TracksControl GridControl { get; set; }

        [NotifyPropertyChangedInvocator]
        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public ObservableCollection<RocketFile> SelectedRocketFiles = new ObservableCollection<RocketFile>();

        private int _bpm;
        public int Bpm
        {
            get => _bpm;
            set
            {
                _bpm = value;
                OnPropertyChanged();
            }
        }

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
        private RocketFile SelectedItem
        {
            get => _selectedItem;
            set
            {
                _selectedItem = value;
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
                OnPropertyChanged(nameof(SelectedOffset));
                OnPropertyChanged(nameof(SelectedItem));
                RebindListviewAction?.Invoke();
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

        private string _totalLength;

        public string TotalLength
        {
            get => _totalLength;
            set
            {
                _totalLength = value;
                OnPropertyChanged();
            }
        }

        public void UpdateSelectedItem(RocketFile item)
        {
            _selectedItem = item;
            if (_selectedItem != null)
            {
                SelectedOffset = _selectedItem.Offset;
                if (_selectedItem != null)
                {
                    BuildGrid(_selectedItem.Filename);
                }
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
        public RelayCommand PreviewCommand { get; set; }
        public RelayCommand CutCommand { get; set; }

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
                    Bpm = TracksHeader.BeatsPerMin;
                    SelectedRocketFiles = new ObservableCollection<RocketFile>(prj.RocketFiles);
                    UpdateUiAction?.Invoke();
                }
                CalculateCommand.Execute(null);
            });
            SaveProjectCommand = new RelayCommand(o =>
            {
                var sfn = new SaveFileDialog { Filter = "Projects|*.json"};
                if (sfn.ShowDialog() == true)
                {
                    InternalSave(sfn.FileName);
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
            CutCommand = new RelayCommand(o =>
            {
                var (result, value) = InputWindow.PromptForValue();
                if (result)
                {
                    var filename = SelectedItem.Filename;
                    var rocketData = FileUtils.ReadRocketFile(filename);
                    foreach (var trackItem in rocketData)
                    {
                        var res = new List<KeyItem>();
                        foreach (var key in trackItem.Keys)
                        {
                            if (key.Row <= value) res.Add(new KeyItem
                            {
                                Interpolation = key.Interpolation,
                                Row = key.Row,
                                Value = key.Value
                            });
                        }
                        trackItem.Keys = res;
                    }

                    var sourcePath = Path.GetDirectoryName(SelectedItem.Filename);
                    var orgFilename = Path.GetFileName(SelectedItem.Filename);
                    var backupFile = $"org_{orgFilename}";
                    backupFile = Path.Combine(sourcePath, backupFile);
                    File.Copy(SelectedItem.Filename, backupFile);
                    FileUtils.SaveFile(SelectedItem.Filename, rocketData, Bpm);
                    CalculateCommand.Execute(null);
                }
            });
            PreviewCommand = new RelayCommand(ExecutePreview);
            TracksHeader = new TracksHeader
            {
                Rows = 10000,
                StartRow = 0,
                EndRow = 10000,
                RowsPerBeat = 8,
                BeatsPerMin = 128
            };
            OnPropertyChanged(nameof(TracksHeader));
            Bpm = 133;
        }

        private void InternalSave(string filename)
        {
            TracksHeader.BeatsPerMin = _bpm;
            var prj = new ProjectHolder
            {
                TracksHeader = TracksHeader,
                RocketFiles = SelectedRocketFiles.ToList()
            };
            var json = JsonConvert.SerializeObject(prj);
            File.WriteAllText(filename, json);
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
            var highestKey = string.Empty;
            foreach (var rocketFile in SelectedRocketFiles)
            {
                var rocket = FileUtils.ReadRocketFile(rocketFile.Filename, i == 0);
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
                            highestKey = track.Name;
                        }
                    }
                }

                rocketFile.HighestKey = highestKey;
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

            var sum = 0.0;
            foreach (var item in SelectedRocketFiles)
            {
                sum += item.LengthInTime.TotalSeconds;
            }
            var td = TimeSpan.FromSeconds(sum);
            TotalLength = $"{td.Minutes}:{td.Seconds}.{td.Milliseconds}";

            OnPropertyChanged(nameof(SelectedRocketFiles));
            UpdateUiAction?.Invoke();
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

        private void GenerateFile(string filename)
        {
            var resdata = new List<TrackItem>();
            for (var i = 0; i < SelectedRocketFiles.Count; i++)
            {
                var rocket = FileUtils.ReadRocketFile(SelectedRocketFiles[i].Filename, i == 0);
                for (var j = 0; j < rocket.Count; j++)
                {
                    var item = rocket[j];
                    var existing = resdata.FirstOrDefault(x => x.Name.Equals(item.Name));
                    var ofs = SelectedRocketFiles[i].Offset;
                    if (existing == null)
                    {
                        var newItem = new TrackItem
                        {
                            MuteKeyCount = item.MuteKeyCount,
                            Color = item.Color,
                            Name = item.Name,
                            Folded = item.Folded
                        };
                        var keys = new List<KeyItem>();
                        foreach (var key in item.Keys)
                        {
                            keys.Add(new KeyItem
                            {
                                Value = key.Value,
                                Interpolation = key.Interpolation,
                                Row = key.Row + ofs
                            });
                        }
                        newItem.Keys = keys;
                        resdata.Add(newItem);
                    }
                    else
                    {
                        // add all keys
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

            FileUtils.SaveFile(filename, resdata, _bpm);
        }

        private void BuildGrid(string filename)
        {
            var rocketData = FileUtils.ReadRocketFile(filename);
            GridControl.RenderControl(rocketData);
        }

        private void ExecutePreview(object obj)
        {
            var tmpFile = Path.GetTempFileName();
            GenerateFile(tmpFile);
            BuildGrid(tmpFile);
        }
    }
}
