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
    public partial class PopupWindow : Form
    {
        public PopupWindow()
        {
            InitializeComponent();
        }
        protected override bool ShowWithoutActivation
        {
            get
            {
                return true;
            }
        }
    }
}
