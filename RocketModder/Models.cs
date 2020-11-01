using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using RocketModder.Annotations;

namespace RocketModder
{
    public class TrackStruct
    {
        public string Name { get; set; }
        public bool Folded { get; set; }
        public string MuteKeyCount { get; set; }
        public string Color { get; set; }
    }

    public class TrackItem
    {
        public string Name { get; set; }
        public int Folded { get; set; }
        public string MuteKeyCount { get; set; }
        public string Color { get; set; }
        public IEnumerable<KeyItem> Keys { get; set; }
    }

    public class KeyItem
    {
        public int Row { get; set; }
        public double Value { get; set; }
        public int Interpolation { get; set; }
    }

    public class RocketFile
    {
        public string Filename { get; set; }
        public int Offset { get; set; }
        public TimeSpan OffsetInTime { get; set; }
        public int MaxLength { get; set; }
        public TimeSpan LengthInTime { get; set; }
        public string HighestKey { get; set; }
    }

    public class TracksHeader : INotifyPropertyChanged
    {
        public int Rows { get; set; }
        public int StartRow { get; set; }
        public int EndRow { get; set; }
        public int RowsPerBeat { get; set; }
        public int BeatsPerMin { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class ProjectHolder
    {
        public TracksHeader TracksHeader { get; set; }
        public List<RocketFile> RocketFiles { get; set; }
    }
}
