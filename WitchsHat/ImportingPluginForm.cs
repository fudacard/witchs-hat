using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WitchsHat
{
    public partial class ImportingPluginForm : Form
    {
        public delegate void OkEventHandler(Plugin plugin);
        public OkEventHandler OkClicked;
        Plugin[] plugins;
        public ImportingPluginForm(Form1 form1)
        {
            InitializeComponent();
            PluginImporter importer = new PluginImporter();
            plugins = importer.ReadPlugins(Path.Combine(Application.StartupPath, @"Data\Plugins"), form1.FileImportDirs);
            listBox1.Items.Clear();
            foreach (Plugin plugin in plugins)
            {
                listBox1.Items.Add(plugin.Name);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0)
            {
                OkClicked(plugins[listBox1.SelectedIndex]);
                Close();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex >= 0)
            {
                label1.Text = plugins[listBox1.SelectedIndex].Description;
            }
            else
            {
                label1.Text = "";
            }
        }
    }
}
