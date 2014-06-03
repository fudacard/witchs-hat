using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WitchsHat
{
    public class PluginImporter
    {
        public Plugin[] ReadPlugins(string pluginFolder, List<string> dirs)
        {
            string[] pluginDescripters = Directory.GetFiles(pluginFolder, "*.plugin");
            List<Plugin> plugins = new List<Plugin>();
            foreach (string pluginDescripter in pluginDescripters)
            {
                Plugin plugin = new Plugin();
                List<WHFile> files = new List<WHFile>();
                EnvironmentSettings settings = new EnvironmentSettings();
                bool filenotfound = false;
                using (XmlReader reader = XmlReader.Create(pluginDescripter))
                {
                    WHFile file = null;
                    while (reader.Read())
                    {
                        if (reader.NodeType == XmlNodeType.Element)
                        {
                            switch (reader.LocalName)
                            {
                                case "Name":
                                    plugin.Name = reader.ReadString();
                                    break;
                                case "Description":
                                    string Description = reader.ReadString();
                                    plugin.Description = Description;
                                    break;
                                case "File":
                                    file = new WHFile("","");
                                    break;
                                case "Src":
                                    string Src = reader.ReadString();
                                    file.Src = Src;
                                    if (CheckFiles(file, dirs))
                                    {
                                        files.Add(file);
                                    }
                                    else
                                    {
                                        filenotfound = true;
                                    }
                                    break;
                                case "Dest":
                                    string Dest = reader.ReadString();
                                    if (Dest != "")
                                    {
                                        file.DestDir = Dest;
                                    }
                                    break;
                            }
                        }
                    }
                }
                if (!filenotfound)
                {
                    plugin.Files = files.ToArray();
                    plugins.Add(plugin);
                }
            }
            return plugins.ToArray();
        }

        public bool CheckFiles(WHFile file, List<string> dirs)
        {
            bool found = false;
            foreach (string dir in dirs)
            {
                if (File.Exists(Path.Combine(dir, file.Src)))
                {
                    file.FullSrc = Path.Combine(dir, file.Src);
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Console.WriteLine("not found: " + file.Src);
            }

            return found;
        }
    }

    public class Plugin
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public WHFile[] Files { get; set; }
    }
}
