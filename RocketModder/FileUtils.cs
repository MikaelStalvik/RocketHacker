using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace RocketModder
{
    public static class FileUtils
    {
        public static TracksHeader TracsHeader { get; set; }
        private static TracksHeader ReadHeader(XDocument xml)
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
            return hdr.FirstOrDefault();
        }

        public static List<TrackItem> ReadRocketFile(string filename, bool readHeader = false)
        {
            if (!File.Exists(filename)) return new List<TrackItem>();
            var xml = XDocument.Load(filename);

            if (readHeader)
            {
                TracsHeader = ReadHeader(xml);
            }

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

        public static void SaveFile(string filename, IEnumerable<TrackItem> data)
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

    }
}
