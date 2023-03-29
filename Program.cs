using System;
using System.IO;
using System.Collections.Generic;

playlist_gen.Start.Main();
namespace playlist_gen
{
    public class Start
    {
        private static string[] MediaExtensions = { "mp4", "vob", "webm", "mp3", "m4a", "avi" };
        private static bool IsMedia(string file)
        {
            file = file.ToLower();
            if (File.Exists(file))
            {
                return MediaExtensions.Any(e => file.EndsWith(e));
            }
            return false;
        }
        public static void Main()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1 && Directory.Exists(args[0]))
            {
                var files = Directory.GetFiles(args[0], "*", new EnumerationOptions
                {
                    RecurseSubdirectories = true,
                    IgnoreInaccessible = true
                }).Where(f => IsMedia(f)).ToList();
                if (files.Count>0){
                    var playList=XSPF.Generate(files);
                    XSPF.Save(args[1],playList);
                }
            }
            else
            {
                Console.WriteLine("Error :Invalid arguments\n Reguired <directory <destinationfile>");
            }

        }
    }
}