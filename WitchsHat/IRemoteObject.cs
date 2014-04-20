using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WitchsHat
{
    interface IRemoteObject
    {
        void StartupNextInstance(params object[] parameters);
    }
}
