using System;
using System.Collections.Generic;
using System.Text;

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
    }

}
