using GO2FlashLauncher.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace GO2FlashLauncher.Service
{
    internal class ClientUpdatorService
    {
        private readonly string Host = "client.guerradenaves.lat";
        private readonly HttpClient httpClient = new HttpClient();
        public string RootPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GalaxyOrbit4", "Client");

        private ClientUpdatorService()
        {
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GalaxyOrbit4")))
            {
                _ = Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GalaxyOrbit4"));
            }
            if (!Directory.Exists(RootPath))
            {
                _ = Directory.CreateDirectory(RootPath);
            }
            if (Directory.GetFiles(RootPath, "*.swf", SearchOption.AllDirectories).Length < 1)
            {
                foreach (string f in Directory.GetFiles(Path.Combine(Environment.CurrentDirectory, "client"), "*.*", SearchOption.AllDirectories))
                {
                    string destination = f.Replace(Path.Combine(Environment.CurrentDirectory, "client"), RootPath);
                    if (!Directory.Exists(destination.Remove(destination.LastIndexOf("\\"))))
                    {
                        _ = Directory.CreateDirectory(destination.Remove(destination.LastIndexOf("\\")));
                    }
                    FileInfo file = new FileInfo(f);
                    _ = file.CopyTo(destination);
                }
            }
        }

        private static ClientUpdatorService _instance;
        public static ClientUpdatorService Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new ClientUpdatorService();
                }
                return _instance;
            }
        }

        public void UpdateFiles()
        {
            HttpResponseMessage xmlres = httpClient.GetAsync("https://" + Host + "/data/config.xml").ConfigureAwait(false).GetAwaiter().GetResult();
            Stream xmldat = xmlres.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
            XmlSerializer serializer = new XmlSerializer(typeof(GO2Xml));
            GO2Xml xml = serializer.Deserialize(xmldat) as GO2Xml;
            _ = xmldat.Seek(0, SeekOrigin.Begin);
            File.Delete(Path.Combine(RootPath, "data", "config.xml"));
            using (FileStream f = new FileStream(Path.Combine(RootPath, "data", "config.xml"), FileMode.CreateNew))
            {
                xmldat.CopyTo(f);
                xmldat.Close();
            }
            foreach (Resource resource in xml.Resources.Resource)
            {
                string fileName = Path.Combine(xml.Resources.Res, resource.Src);
                CheckUpdate(fileName);
            }
            string clientFile = xml.Resources.Client + "Client.swf";
            CheckUpdate(clientFile);
            string galaxyAssetFile = Path.Combine(xml.Resources.Res, xml.Resources.GalaxyAssetPath + ".swf");
            CheckUpdate(galaxyAssetFile);
            foreach (Audio music in xml.Music.Audio)
            {
                string fileName = Path.Combine(xml.Music.Res, music.Src);
                CheckUpdate(fileName);
            }
        }

        private void CheckUpdate(string fileName)
        {
            string[] files = Directory.GetFiles(RootPath, "*.*", SearchOption.AllDirectories);
            if (!files.Any(x => x.EndsWith(fileName.Remove(0, fileName.LastIndexOf("/") + 1))))
            {
                string url = "https://" + Host + "/" + fileName;
                HttpResponseMessage response = httpClient.GetAsync(url).ConfigureAwait(false).GetAwaiter().GetResult();
                if (!response.IsSuccessStatusCode)
                {
                    _ = MessageBox.Show("File update for " + fileName + " is failed! You can still continue the game but it might be outdated client!", "Warning!");
                    return;
                }
                DeleteOldFile(files, fileName);
                Stream s = response.Content.ReadAsStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
                string[] directories = fileName.Split('/');
                string lastDirect = RootPath;
                foreach (string direc in directories.Take(directories.Length - 1))
                {
                    lastDirect = Path.Combine(lastDirect, direc);
                    if (!Directory.Exists(Path.Combine(lastDirect, direc)))
                    {
                        _ = Directory.CreateDirectory(Path.Combine(lastDirect, direc));
                    }

                }
                using (FileStream f = new FileStream(Path.Combine(RootPath, fileName), FileMode.CreateNew))
                {
                    _ = s.Seek(0, SeekOrigin.Begin);
                    s.CopyTo(f);
                    s.Close();
                }
            }
        }

        private void DeleteOldFile(string[] files, string newFile)
        {
            string realName = Regex.Replace(newFile, @"[\d-]", string.Empty);
            realName = realName.Remove(0, realName.LastIndexOf("/") + 1);
            IEnumerable<string> oldFile = files.Where(x => x.EndsWith(realName));
            foreach (string file in oldFile)
            {
                File.Delete(file);
            }
        }
    }
}
