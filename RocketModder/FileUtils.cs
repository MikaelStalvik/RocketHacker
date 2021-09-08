using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xaml.Schema;
using System.Xml.Linq;

namespace RocketModder
{
    public static class FileUtils
    {
        public static TracksHeader TracksHeader { get; set; }
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
        private static TracksHeader ReadHeaderExtended(XDocument xml)
        {
            var header = new TracksHeader();
            var item = xml.Descendants("BeatsPerMin");
            header.BeatsPerMin = item != null ? int.Parse(item.First().Value) : 0;
            item = xml.Descendants("RowsPerBeat");
            header.RowsPerBeat = item != null ? int.Parse(item.First().Value) : 0;
            var attr = xml.Root.Attribute("rows");
            header.Rows = attr != null ? int.Parse(attr.Value) : 0;


            return header;
        }

        private class GroupHelper
        {
            public string Name { get; set; }
            public string Color { get; set; }
        }

        public static List<TrackItem> ReadRocketFile(string filename, bool readHeader = false)
        {
            if (!File.Exists(filename)) return new List<TrackItem>();
            var xml = XDocument.Load(filename);
            var isBazookaFormat = false;

            if (readHeader)
            {
                TracksHeader = ReadHeader(xml);
                if (TracksHeader.BeatsPerMin == 0)
                {
                    isBazookaFormat = true;
                    TracksHeader = ReadHeaderExtended(xml);
                }
            }

            try
            {
                if (isBazookaFormat)
                {
                    var groupsQry = from c in xml.Descendants("groupinfo")
                        select new GroupHelper
                        {
                            Name = c.Attribute("name").Value,
                            Color = c.Attribute("color").Value
                        };
                    var groups = new Dictionary<string, string>();
                    foreach (var item in groupsQry.ToList())
                    {
                        groups[item.Name] = item.Color;
                    }
                    
                    var qry = from c in xml.Descendants("track")
                        select new TrackItem
                        {
                            Name = c.Attribute("name").Value,
                            Keys = from k in c.Descendants("key")
                                select new KeyItem
                                {
                                    Row = int.Parse(k.Attribute("row")?.Value ?? string.Empty),
                                    Value = double.Parse(k.Attribute("value")?.Value ?? string.Empty,
                                        CultureInfo.InvariantCulture),
                                    Interpolation = int.Parse(k.Attribute("interpolation")?.Value ?? string.Empty)
                                }
                        };
                    var resolved = qry.ToList();
                    // process groups
                    foreach (var item in resolved)
                    {
                        var idx = item.Name.IndexOf(":");
                        var group = idx > -1 ? item.Name.Substring(0, idx) : string.Empty;
                        var color = "#8A8A8A";
                        if (idx > -1)
                        {   
                            color = groups.ContainsKey(group) ? groups[group] : "#8A8A8A";
                        }

                        item.Color = color;
                    }
                    return resolved;
                }
                else
                {
                    var qry = from c in xml.Descendants("track")
                        select new TrackItem
                        {
                            Name = c.Attribute("name")?.Value,
                            Folded = int.Parse(c.Attribute("folded")?.Value ?? string.Empty),
                            MuteKeyCount = c.Attribute("muteKeyCount")?.Value,
                            Color = c.Attribute("color")?.Value,
                            Keys = from k in c.Descendants("key")
                                select new KeyItem
                                {
                                    Row = int.Parse(k.Attribute("row")?.Value ?? string.Empty),
                                    Value = double.Parse(k.Attribute("value")?.Value ?? string.Empty,
                                        CultureInfo.InvariantCulture),
                                    Interpolation = int.Parse(k.Attribute("interpolation")?.Value ?? string.Empty)
                                }
                        };
                    return qry.ToList();
                }
            }
            catch (Exception ex)
            {
            }

            return null;
        }

        public static void SaveFile(string filename, IEnumerable<TrackItem> data, int bpm)
        {
            const bool UseNewFormat = true;
            if (UseNewFormat)
            {
                var groups = new Dictionary<string, string>();
                foreach (var item in data)
                {
                    var idx = item.Name.IndexOf(":");
                    if (idx != -1)
                    {
                        var grp = item.Name.Substring(0, idx);
                        var color = item.Color;
                        groups[grp] = color;
                    }
                }
                
                // all data aggregated, generate XML
                var xd = new XDocument(new XElement("rootElement",
                    new XElement("TimeFormat", "{row}"),
                    new XElement("BeatsPerMin", bpm),
                    new XElement("RowsPerBeat", 8),
                    
                    new XElement("groupinfos",
                        from grp in groups.Keys
                        select new XElement("groupinfo", 
                            new XAttribute("name", grp),
                            new XAttribute("color", groups[grp])
                            )
                        ),

                    new XElement("tracks",
                        from track in data
                        select new XElement("track",
                            new XAttribute("name", track.Name),
                            new XAttribute("visible", true),
                            from key in track.Keys
                            select new XElement("key",
                                new XAttribute("row", key.Row),
                                new XAttribute("value", key.Value),
                                new XAttribute("interpolation", key.Interpolation)
                            )
                        )
                    )));
                var root = xd.Descendants("rootElement").First();
                root.SetAttributeValue("rows", 5120);
                

                xd.Save(filename);
            }
            else
            {
                // all data aggregated, generate XML
                var xd = new XDocument(new XElement("rootElement",
                    new XElement("tracks",
                        new XAttribute("rows", 10000),
                        new XAttribute("startRow", 0),
                        new XAttribute("endRow", 10000),
                        new XAttribute("rowsPerBeat", 8),
                        new XAttribute("beatsPerMin", bpm),
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
}
