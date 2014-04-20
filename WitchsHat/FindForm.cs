using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WitchsHat
{
    public partial class FindForm : Form
    {

        public FindForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            string text = textBox1.Text;
            if (text == "")
            {
                return;
            }
            Sgry.Azuki.WinForms.AzukiControl azuki = MainForm.GetActiveAzuki();
            if (azuki == null)
            {
                return;
            }

            bool matchCase = checkBox1.Checked;
            int begin, end;
            azuki.GetSelection(out begin, out end);
            Sgry.Azuki.SearchResult result = null;
            Console.WriteLine("[" + azuki.GetSelectedText() + "]");
            if (begin == end)
            {
                result = azuki.Document.FindNext(text, azuki.CaretIndex, matchCase);
            }
            else if (azuki.GetSelectedText() == textBox1.Text)
            {
                result = azuki.Document.FindNext(text, end, matchCase);
            }
            else
            {
                result = azuki.Document.FindNext(text, begin, end, matchCase);
            }
            if (result != null)
            {
                int lineIndex, columnIndex;
                azuki.Document.GetLineColumnIndexFromCharIndex(result.Begin, out lineIndex, out columnIndex);
                azuki.Document.SetCaretIndex(lineIndex, columnIndex);
                azuki.SetSelection(result.End, result.Begin);
                azuki.ScrollToCaret();
            }
        }

        private void FindForm_Load(object sender, EventArgs e)
        {
        }

        private void FindForm_Shown(object sender, EventArgs e)
        {
            textBox1.Focus();
        }

        public Form1 MainForm { get; set; }
    }
}
