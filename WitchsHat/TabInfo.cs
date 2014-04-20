using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitchsHat
{
    class TabInfo
    {
        public  const int TabTypeAzuki = 0;
        public  const int TabTypeImage = 1;
        public  const int TabTypeBrowser = 2;
        public int Type { get; set; }
        public string Uri { get; set; }
    }
}
