using System;
using System.IO;
using System.Net;
using System.Xml;

namespace WordpressMediaLibraryBackup
{
    internal class Backup
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("-----------------------------------\n" +
                              "Wordpress media library backup tool\n" +
                              "-----------------------------------");
    
            if (args.Length < 2)
            {
                Console.WriteLine("Usage:\nWordpressMediaLibraryBackup.exe <source-file>.xml <destination-directory>");
                return;
            }

            var xmlFile = args[0];
            var destination = args[1];
            var urls = GetUrls(xmlFile);

            Console.WriteLine(DateTime.Now + " - Backup process started.\n");

            for (var i = 0; i < urls.Count; i++)
            {
                var url = new Uri(urls[i].InnerXml);

                Echo("DOWNLOADING", ConsoleColor.Yellow, url.OriginalString, false);

                try
                {
                    var client = WebRequest.Create(url) as HttpWebRequest;
                    client.Method = WebRequestMethods.Http.Get;

                    var response = client.GetResponse() as HttpWebResponse;
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        Echo("NOT FOUND", ConsoleColor.Red, url.OriginalString);
                        client.Abort();
                        continue;
                    }

                    var destDir = Path.Combine(destination, Path.GetDirectoryName(url.LocalPath.Substring(1)));
                    var destFile = Path.Combine(destDir, Path.GetFileName(url.LocalPath));
                    Directory.CreateDirectory(destDir);

                    var localMod = File.Exists(destFile) ? File.GetLastWriteTimeUtc(destFile) : new DateTime(1900, 1, 1);
                    if (response.LastModified < localMod)
                    {
                        Echo("SKIPPED", ConsoleColor.Cyan, url.OriginalString);
                        client.Abort();
                        continue;
                    }

                    var dataStream = response.GetResponseStream();
                    using (var destFileStream = new FileStream(destFile, FileMode.Create, FileAccess.ReadWrite))
                    {
                        dataStream.CopyTo(destFileStream);
                    }
                    Echo("DOWNLOADED", ConsoleColor.Green, url.OriginalString);
                }
                catch (Exception)
                {
                    Echo("NOT FOUND", ConsoleColor.Red, url.OriginalString);
                }
            }

            Console.WriteLine("\n" + DateTime.Now + " - Backup process finished.\n");
        }

        private static XmlNodeList GetUrls(string xmlFile)
        {
            var doc = new XmlDocument();
            doc.Load(xmlFile);

            var urls = doc.GetElementsByTagName("wp:attachment_url");
            if (urls.Count == 0)
            {
                Console.WriteLine("Error: No URLs found.");
                Environment.Exit(0);
            }

            return urls;
        }

        private static void Echo(string state, ConsoleColor color, string url, bool close = true)
        {
            Console.CursorLeft = 0;
            Console.ForegroundColor = color;
            Console.Write(state.PadRight(13));
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write(close ? url + "\n" : url);
        }
    }
}
