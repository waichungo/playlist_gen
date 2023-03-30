using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

using System.Web;

namespace playlist_gen
{
    public class PlayListItem
    {
        public string FilePath { get; set; } = "";
        public long Duration { get; set; } = 0;
    }

    public class XSPF
    {
        public static long GetDuration(string file)
        {
            long duration = 0L;
            var result = "";
            var error = "";
            var command = $"-v quiet -print_format json -show_format -show_streams \"{file}\"";
            var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                Arguments = command,
                FileName = "ffprobe.exe",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden

            };
            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    result += e.Data;
                }
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data != null)
                {
                    error += e.Data;
                }
            };
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            var obj = JsonConvert.DeserializeObject<JObject>(result);
            if (obj != null)
            {
                var durString = (string)obj["format"]["duration"];
                var parsed = double.Parse(durString);
                duration = (long)parsed;

            }
            return duration;
        }
        public List<PlayListItem> media { get; set; } = new();
        public static XSPF Generate(List<string> files)
        {
            XSPF playlist = new();
            var threads = new List<Thread>();
            var mutex = new Mutex(false);
            int count = 0;
            var durations = new Dictionary<string, long>();
            for (int i = 0; i < files.Count; i++)
            {
                while (count > 3)
                {
                    Thread.Sleep(1000);
                }
                var file = files[i];
                {
                    mutex.WaitOne();
                    count++;
                    mutex.ReleaseMutex();
                }
                var thread = new Thread(() =>
                {
                    var dur = GetDuration(file);
                    {
                        mutex.WaitOne();
                        durations.Add(file, dur);
                        count--;
                        mutex.ReleaseMutex();
                    }

                });
                thread.Start();
                threads.Add(thread);

            }
            threads.ForEach(e => e.Join());
            for (int i = 0; i < files.Count; i++)
            {

                var file = files[i];
                playlist.media.Add(new PlayListItem
                {
                    Duration = durations[file],
                    FilePath = file
                });
            }

            return playlist;
        }
        private static XmlDocument GenetrateDoc(XSPF playlist)
        {
            var doc = new XmlDocument();
            var playlistElement = doc.CreateElement("playlist");
            playlistElement.SetAttribute("xmlns", "http://xspf.org/ns/0/");
            playlistElement.SetAttribute("xmlns:vlc", "http://www.videolan.org/vlc/playlist/ns/0/");
            playlistElement.SetAttribute("version", "1");
            doc.AppendChild(playlistElement);

            var title = doc.CreateElement("title");
            title.AppendChild(doc.CreateTextNode("Playlist"));
            playlistElement.AppendChild(title);

            var tracklist = doc.CreateElement("trackList");
            playlistElement.AppendChild(tracklist);
            var count = 0;
            foreach (var file in playlist.media)
            {
                var item = doc.CreateElement("track");

                var location = doc.CreateElement("location");
                location.AppendChild(doc.CreateTextNode(new Uri(file.FilePath).AbsoluteUri));
                item.AppendChild(location);

                var duration = doc.CreateElement("duration");
                duration.AppendChild(doc.CreateTextNode($"{file.Duration}"));
                item.AppendChild(duration);

                var extension = doc.CreateElement("extension");
                extension.SetAttribute("application", "http://www.videolan.org/vlc/playlist/0");
                var vlcId = doc.CreateElement("vlc", "id", "http://www.videolan.org/vlc/playlist/ns/0/");
                vlcId.AppendChild(doc.CreateTextNode($"{count}"));
                extension.AppendChild(vlcId);

                item.AppendChild(extension);

                tracklist.AppendChild(item);
                count++;

            }
            var extension2 = doc.CreateElement("extension");
            extension2.SetAttribute("application", "http://www.videolan.org/vlc/playlist/0");
            for (var i = 0; i < playlist.media.Count; i++)
            {
                var id = doc.CreateElement("vlc", "item", "http://www.videolan.org/vlc/playlist/ns/0/");
                id.Prefix = "vlc";
                id.SetAttribute("tid", $"{i}");
                extension2.AppendChild(id);
            }
            playlistElement.AppendChild(extension2);


            return doc;
        }
        public static void Save(string filename, XSPF playlist)
        {
            var doc = GenetrateDoc(playlist);
            if (File.Exists(filename))
            {
                File.Delete(filename);
            }
            var data = "";
            using (var mstream = new MemoryStream())
            {
                using (var writer = new XmlTextWriter(mstream, Encoding.UTF8))
                {
                    writer.Formatting = System.Xml.Formatting.Indented;
                    doc.WriteContentTo(writer);
                    writer.Flush();
                    mstream.Flush();
                    mstream.Position = 0;
                    using (var reader = new StreamReader(mstream))
                    {
                        data = reader.ReadToEnd();
                    }                    
                }
            }
            data = "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" + data;
            File.WriteAllText(filename, data);
        }
    }
}