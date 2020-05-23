using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Xml.Linq;
using Microsoft.Win32;

namespace RocketModder
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel _vm = new MainViewModel();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = _vm;
            SelectedRocketFilesLb.ItemsSource = _vm.SelectedRocketFiles;
        }


        public string BoolToStr(bool v)
        {
            return v ? "1" : "0";
        }

        private void parseFile(string filename)
        {
            var fullDict = new Dictionary<TrackStruct, List<KeyItem>>();

            var xml = XElement.Load(filename);

            var ritems = xml.Elements("rootElement");

            var tracks = xml.Elements("tracks").Elements();
            foreach (var track in tracks)
            {
                var name = track.Attributes("name").FirstOrDefault().Value;
                var folded = track.Attributes("folded").FirstOrDefault().Value;
                var muteKeyCount = track.Attributes("muteKeyCount").FirstOrDefault().Value;
                var color = track.Attributes("color").FirstOrDefault().Value;
                var ts = new TrackStruct
                    {Name = name, Color = color, Folded = int.Parse(folded) == 0, MuteKeyCount = muteKeyCount};

                var lst = new List<KeyItem>();
                var keys = track.Elements("key");
                foreach (var key in keys)
                {
                    var row = key.Attributes("row").FirstOrDefault().Value;
                    var value = key.Attributes("value").FirstOrDefault().Value;
                    var interpolation = key.Attributes("interpolation").FirstOrDefault().Value;
                    lst.Add(new KeyItem
                    {
                        Row = int.Parse(row),
                        Value = double.Parse(value, CultureInfo.InvariantCulture),
                        Interpolation = int.Parse(interpolation)
                    });
                }

                fullDict[ts] = lst;
                int k = 12;
            }

            //var fkey = fullDict.Keys.FirstOrDefault(x => x.Name.Equals("light_add"));
            //var items = fullDict[fkey];
            //foreach (var item in items)
            //{
            //    Debug.WriteLine(item.Row + " " + item.Value);
            //}

            var th = 1592;
            var ofs_add = 200;
            foreach (var key in fullDict.Keys)
            {
                var lst = fullDict[key];
                for (var i = 0; i < lst.Count; i++)
                {
                    var row = lst[i].Row;
                    if (row >= th)
                    {
                        lst[i].Row = row + ofs_add;
                        Debug.WriteLine("change: " + row + " " + key.Name);
                    }
                }
            }

            var sb = new StringBuilder();
            foreach (var key in fullDict.Keys)
            {
                sb.AppendLine(
                    $"<track name=\"{key.Name}\" folded=\"{BoolToStr(key.Folded)}\" muteKeyCount=\"{key.MuteKeyCount}\" color=\"{key.Color}\">");
                foreach (var item in fullDict[key])
                {
                    sb.AppendLine(
                        $"  <key row=\"{item.Row}\" value=\"{item.Value}\" iterpolation=\"{item.Interpolation}\" />");
                }

                sb.AppendLine("</track>");
            }

            Debug.WriteLine(sb.ToString());
            Clipboard.SetText(sb.ToString());
            //Debug.WriteLine("\n\n");
            //fkey = fullDict.Keys.FirstOrDefault(x => x.Name.Equals("light_add"));
            //items = fullDict[fkey];
            //foreach (var item in items)
            //{
            //    Debug.WriteLine(item.Row + " " + item.Value + " " + fkey.Name);
            //}

            int j = 12;
        }

        void parseFile2(string filename)
        {
            var th = 1592;
            var ofs_add = 200;
            var lines = File.ReadAllLines(filename);
            var sb = new StringBuilder();
            foreach (var line in lines)
            {
                var sline = line.Trim();
                if (sline.StartsWith("<key"))
                {
                    var idx = line.IndexOf("row=\"");
                    var rv = line.Substring(idx + 5);
                    idx = rv.IndexOf("\"");
                    rv = rv.Substring(0, idx);
                    var v = int.Parse(rv);

                    if (v >= th) v += ofs_add;

                    idx = line.IndexOf("row=\"");
                    var rest = line.Substring(idx + 5);
                    idx = rest.IndexOf("\"");
                    rest = rest.Substring(idx + 1);

                    rest = $"    <key row=\"{v}\"" + rest;

                    sb.AppendLine(rest);
                }
                else
                {
                    sb.AppendLine(line);
                }
            }

            Clipboard.SetText(sb.ToString());
        }

        void parseFile3(string filename)
        {
            //var doc = new XmlDocument();
            //doc.Load(filename);
            //var root = doc.DocumentElement.SelectSingleNode("/rootElement");
            //var tracks = doc.DocumentElement.SelectSingleNode("/rootElement/tracks");
            //int k = 12;
            var xml = XDocument.Load(filename);

            //var qry = from c in xml.Descendants("track")
            //    select new
            //    {
            //        Name = c.Attribute("name").Value,
            //        Folded = c.Attribute("folded").Value,
            //        MuteKeyCount = c.Attribute("muteKeyCount").Value,
            //        Color = c.Attribute("color").Value,
            //        Items = from k in c.Descendants("key")
            //            select new KeyItem
            //            {
            //                Row = int.Parse(k.Attribute("row").Value),
            //                Value = double.Parse(k.Attribute("value").Value, CultureInfo.InvariantCulture),
            //                Interpolation = int.Parse(k.Attribute("interpolation").Value)
            //            }
            //    };
            var tracksq = xml.Descendants("tracks");
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
            var qq = qry.ToList();

            var xd = new XDocument(new XElement("rootElement", new XElement("tracks",
                new XAttribute("rows", 10000),
                new XAttribute("startRow", 0),
                new XAttribute("endRow", 10000),
                from track in qq
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


            int k = 12;
        }

        List<TrackItem> readRocketFile(string filename)
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

        string generateFile(List<List<TrackItem>> rockets, List<int> offsets)
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


        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            if (ofd.ShowDialog() == true)
            {
                parseFile3(ofd.FileName);
            }
        }

        private void SaveButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
        }

        private void GenButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            var rockets = new List<List<TrackItem>>();
            var filename = @"C:\dev\bb06\build\bb06-1-intro.rocket";
            rockets.Add(readRocketFile(filename));
            filename = @"C:\dev\bb06\build\bb06-2-laserballs.rocket";
            rockets.Add(readRocketFile(filename));

            var offsets = new List<int> {0, 352};
            var q = generateFile(rockets, offsets);
            //XElement doc =
            //    new XElement("rootElement",
            //        new XElement("tracks",
            //            new XElement("track", "1")));
            //int k = 12;
        }
    }
}