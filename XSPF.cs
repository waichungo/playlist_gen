using System;
using System.Collections.Generic;
using System.Xml;
using System.Diagnostics;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
            var command = $"-v quiet -print_format json -show_format -show_streams {file}";
            var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                Arguments = command,
                FileName = "ffprobe",
                CreateNoWindow = true,
                RedirectStandardOutput = true,
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
            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            var obj = JsonConvert.DeserializeObject<JObject>(result);
            var durString = (string)obj["format"]["duration"];
            var parsed = double.Parse(durString);
            duration = (long)parsed;


            return duration;
        }
        public List<PlayListItem> media { get; set; } = new();
        public static XSPF Generate(List<string> files)
        {

            return null;
        }
        private static XmlDocument GenetrateDoc(XSPF playlist)
        {
            var doc = new XmlDocument();

            return doc;
        }
        public static void Save(string filename, XSPF playlist)
        {

        }
    }

}