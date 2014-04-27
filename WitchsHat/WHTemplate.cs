using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WitchsHat
{
    public class WHTemplate
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<WHFile> Files { get; set; }

        public static WHTemplate ReadTemplate(string path)
        {
            WHTemplate temp = new WHTemplate();
            List<WHFile> files = new List<WHFile>();
            using (XmlReader reader = XmlReader.Create(path))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.LocalName)
                        {
                            case "Name":
                                string projectName = reader.ReadString();
                                Console.WriteLine(projectName);
                                temp.Name = projectName;
                                break;
                            case "Description":
                                string Description = reader.ReadString();
                                Console.WriteLine(Description);
                                temp.Description = Description;
                                break;
                            case "File":
                                files.Add(new WHFile("", ""));
                                break;
                            case "Src":
                                string Src = reader.ReadString();
                                Console.WriteLine(Src);
                                files[files.Count - 1].Src = Src;
                                break;
                            case "Dest":
                                string Dest = reader.ReadString();
                                if (Dest != "")
                                {
                                    Console.WriteLine(Dest);
                                    files[files.Count - 1].DestDir = Dest;
                                }
                                break;
                        }
                    }
                }
            }
            temp.Files = files;
            return temp;
        }

        public bool CheckFiles(ICollection<string> dirs)
        {
            bool fill = true;
            foreach (WHFile file in Files)
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
                if (found)
                {
                    Console.WriteLine(file.FullSrc);
                }
                else
                {
                    fill = false;
                    Console.WriteLine("not found: " + file.Src);
                }
            }
            return fill;
        }
    }

    public class WHFile
    {
        public string Src { get; set; }
        public string DestDir { get; set; }
        public string FullSrc { get; set; }
        public string FullDest { get; set; }
        public WHFile(string src, string dest)
        {
            Src = src;
            DestDir = dest;
        }
    }
}
