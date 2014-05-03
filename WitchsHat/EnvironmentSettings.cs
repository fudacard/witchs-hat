using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace WitchsHat
{
    public class EnvironmentSettings
    {
        public string ProjectsPath { get; set; }
        public bool TempProjectEnable { get; set; }
        public bool ServerEnable { get; set; }
        public int ServerPort { get; set; }
        public string EnchantjsUrl { get; set; }
        public string Browser { get; set; }
        public string Encoding { get; set; }
        public string FontName { get; set; }
        public float FontSize { get; set; }
        public bool EnchantjsDownload { get; set; }
        public bool SuggestEnable { get; set; }
        public bool IndentUseTab { get; set; }

        public static EnvironmentSettings ReadEnvironmentSettings(string path)
        {
            EnvironmentSettings settings = new EnvironmentSettings();
            using (XmlReader reader = XmlReader.Create(path))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.LocalName)
                        {
                            case "ProjectsPath":
                                string projectsPath = reader.ReadString();
                                Console.WriteLine("[" + projectsPath + "]");
                                settings.ProjectsPath = projectsPath;
                                break;
                            case "EnchantjsBuild":
                                string url = reader.ReadString();
                                Console.WriteLine("[" + url + "]");
                                settings.EnchantjsUrl = url;
                                break;
                            case "Server":
                                string server = reader.ReadString();
                                Console.WriteLine("[" + server + "]");
                                if (server == "on")
                                {
                                    settings.ServerEnable = true;
                                }
                                else
                                {
                                    settings.ServerEnable = false;
                                }
                                break;
                            case "ServerPort":
                                int port = int.Parse(reader.ReadString());
                                Console.WriteLine("[" + port + "]");
                                settings.ServerPort = port;
                                break;
                            case "TempProject":
                                string tempproject = reader.ReadString();
                                Console.WriteLine("[" + tempproject + "]");
                                if (tempproject == "on")
                                {
                                    settings.TempProjectEnable = true;
                                }
                                else
                                {
                                    settings.TempProjectEnable = false;
                                }
                                break;
                            case "Browser":
                                string b = reader.ReadString();
                                Console.WriteLine("[" + b + "]");
                                settings.Browser = b;
                                break;
                            case "RunBrowser":
                                string run = reader.ReadString();
                                Console.WriteLine("[" + run + "]");
                                settings.RunBrowser = run;
                                break;
                            case "Encoding":
                                string encoding = reader.ReadString();
                                Console.WriteLine("[" + encoding + "]");
                                settings.Encoding = encoding;
                                break;
                            case "FontName":
                                string fontName = reader.ReadString();
                                Console.WriteLine("[" + fontName + "]");
                                settings.FontName = fontName;
                                break;
                            case "FontSize":
                                string fontSize = reader.ReadString();
                                Console.WriteLine("[" + fontSize + "]");
                                settings.FontSize = float.Parse(fontSize);
                                break;
                            case "EnchantjsDownload":
                                string download = reader.ReadString();
                                Console.WriteLine("[" + download + "]");
                                if (download == "on")
                                {
                                    settings.EnchantjsDownload = true;
                                }
                                else
                                {
                                    settings.EnchantjsDownload = false;
                                }
                                break;
                            case "SuggestEnable":
                                string suggest = reader.ReadString();
                                Console.WriteLine("[" + suggest + "]");
                                if (suggest == "on")
                                {
                                    settings.SuggestEnable = true;
                                }
                                else
                                {
                                    settings.SuggestEnable = false;
                                }
                                break;
                            case "IndentUseTab":
                                string usetab = reader.ReadString();
                                Console.WriteLine("[" + usetab + "]");
                                if (usetab == "on")
                                {
                                    settings.IndentUseTab = true;
                                }
                                else
                                {
                                    settings.IndentUseTab = false;
                                }
                                break;
                        }
                    }
                }

            }
            return settings;
        }

        public void ReadUserSettings(string path)
        {

            using (XmlReader reader = XmlReader.Create(path))
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        switch (reader.LocalName)
                        {
                            case "ProjectsPath":
                                string projectsPath = reader.ReadString();
                                if (projectsPath != "")
                                {
                                    Console.WriteLine("[" + projectsPath + "]");
                                    this.ProjectsPath = projectsPath;
                                }
                                break;
                            case "EnchantjsBuild":
                                string url = reader.ReadString();
                                if (url != "")
                                {
                                    Console.WriteLine("[" + url + "]");
                                    this.EnchantjsUrl = url;
                                }
                                break;
                            case "Server":
                                string server = reader.ReadString();
                                Console.WriteLine("[" + server + "]");
                                if (server == "on")
                                {
                                    this.ServerEnable = true;
                                }
                                else if (server == "off")
                                {
                                    this.ServerEnable = false;
                                }
                                break;
                            case "ServerPort":
                                string port = reader.ReadString();
                                if (port != "")
                                {
                                    Console.WriteLine("[" + port + "]");
                                    this.ServerPort = int.Parse(port);
                                }
                                break;
                            case "Browser":
                                string b = reader.ReadString();
                                if (b != "")
                                {
                                    Console.WriteLine("[" + b + "]");
                                    this.Browser = b;
                                }
                                break;
                            case "RunBrowser":
                                string run = reader.ReadString();
                                if (run != "")
                                {
                                    Console.WriteLine("[" + run + "]");
                                    this.RunBrowser = run;
                                }
                                break;
                            case "TempProject":
                                string tempproject = reader.ReadString();
                                Console.WriteLine("[" + tempproject + "]");
                                if (tempproject != "")
                                {
                                    if (tempproject == "on")
                                    {
                                        this.TempProjectEnable = true;
                                    }
                                    else
                                    {
                                        this.TempProjectEnable = false;
                                    }
                                }
                                break;
                            case "Encoding":
                                string encoding = reader.ReadString();
                                if (encoding != "")
                                {
                                    Console.WriteLine("[" + encoding + "]");
                                    this.Encoding = encoding;

                                }
                                break;
                            case "FontName":
                                string fontName = reader.ReadString();
                                Console.WriteLine("[" + fontName + "]");
                                if (fontName != "")
                                {
                                    this.FontName = fontName;
                                }
                                break;
                            case "FontSize":
                                string fontSize = reader.ReadString();
                                Console.WriteLine("[" + fontSize + "]");
                                if (fontSize != "")
                                {
                                    this.FontSize = float.Parse(fontSize);
                                }
                                break;
                            case "EnchantjsDownload":
                                string download = reader.ReadString();
                                Console.WriteLine("[" + download + "]");
                                if (download != "")
                                {
                                    if (download == "on")
                                    {
                                        this.EnchantjsDownload = true;
                                    }
                                    else
                                    {
                                        this.EnchantjsDownload = false;
                                    }
                                }
                                break;
                            case "SuggestEnable":
                                string suggest = reader.ReadString();
                                Console.WriteLine("[" + suggest + "]");
                                if (suggest != "")
                                {
                                    if (suggest == "on")
                                    {
                                        this.SuggestEnable = true;
                                    }
                                    else
                                    {
                                        this.SuggestEnable = false;
                                    }
                                }
                                break;
                            case "IndentUseTab":
                                string usetab = reader.ReadString();
                                Console.WriteLine("[" + usetab + "]");
                                if (usetab != "")
                                {
                                    if (usetab == "on")
                                    {
                                        this.IndentUseTab = true;
                                    }
                                    else
                                    {
                                        this.IndentUseTab = false;
                                    }
                                }
                                break;
                        }
                    }
                }
            }
        }

        public void WriteEnvironmentSettings(string filePath)
        {
            XmlWriterSettings xmlsettings = new XmlWriterSettings();
            xmlsettings.Indent = true;
            XmlWriter writer = XmlWriter.Create(filePath, xmlsettings);
            writer.WriteStartElement("Settings");
            writer.WriteElementString("ProjectsPath", this.ProjectsPath);
            writer.WriteElementString("TempProject", this.TempProjectEnable ? "on" : "off");
            writer.WriteElementString("Server", this.ServerEnable ? "on" : "off");
            writer.WriteElementString("ServerPort", this.ServerPort.ToString());
            writer.WriteElementString("Encoding", this.Encoding);
            writer.WriteElementString("Browser", this.Browser);
            writer.WriteElementString("RunBrowser", this.RunBrowser);
            writer.WriteElementString("FontName", this.FontName);
            writer.WriteElementString("FontSize", this.FontSize.ToString());
            writer.WriteElementString("EnchantjsDownload", this.EnchantjsDownload ? "on" : "off");
            writer.WriteElementString("SuggestEnable", this.SuggestEnable ? "on" : "off");
            writer.WriteElementString("IndentUseTab", this.IndentUseTab ? "on" : "off");
            writer.WriteEndElement();
            writer.Flush();
            writer.Close();
        }


        public string RunBrowser { get; set; }
    }
}
