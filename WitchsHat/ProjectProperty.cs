using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WitchsHat
{
    public class ProjectProperty
    {
        public string Name { get; set; }
        public string Dir { get; set; }
        public string Encoding { get; set; }
        public string HtmlPath { get; set; }

        public static ProjectProperty ReadProjectProperty(string path)
        {
            ProjectProperty pp = new ProjectProperty();
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
                                pp.Name = projectName;
                                break;
                            case "Encoding":
                                string encoding = reader.ReadString();
                                Console.WriteLine(encoding);
                                pp.Encoding = encoding;
                                break;
                            case "MainHtml":
                                string html = reader.ReadString();
                                Console.WriteLine(html);
                                pp.HtmlPath = html;
                                break;
                        }
                    }
                }

            }
            pp.Dir = path.Substring(0, path.LastIndexOf('\\'));
            return pp;
        }

        public static void WriteProjectProperty(ProjectProperty pp)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            XmlWriter writer = XmlWriter.Create(Path.Combine(pp.Dir, pp.Name + ".whprj"), settings);
            writer.WriteStartElement("Project");
            writer.WriteElementString("Name", pp.Name);
            writer.WriteElementString("Encoding", pp.Encoding);
            writer.WriteElementString("MainHtml", pp.HtmlPath);
            writer.WriteEndElement();
            writer.Flush();
            writer.Close();
        }
    }
}
