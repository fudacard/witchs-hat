using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WitchsHat;
using System.IO;
using System.Collections.Generic;

namespace WitchsHatTest
{
    [TestClass]
    public class PluginImporterTest
    {
        [TestMethod]
        public void ReadPluginsTest()
        {
            PluginImporter importer = new PluginImporter();
            string projectRoot = @"..\..\..\WitchsHat\bin\Release\";
            List<string> FileImportDirs = new List<string>();
            FileImportDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\build"));
            FileImportDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\build\plugins"));
            FileImportDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\images"));
            FileImportDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\images\monster"));
            FileImportDirs.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Witchs Hat\enchant.js\examples\plugins\widget\Alert_Confirm"));

            Plugin[] plugins = importer.ReadPlugins(Path.Combine(projectRoot, @"Data\Plugins"), FileImportDirs);
            Assert.AreEqual("ui.enchant.js", plugins[0].Name);
            Assert.IsNotNull(plugins[0].Files[0]);
            Assert.AreEqual("widget.enchant.js", plugins[1].Name);
        }
    }
}
