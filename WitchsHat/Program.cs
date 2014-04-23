using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Ipc;
using System.Runtime.Remoting.Lifetime;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WitchsHat
{
    static class Program
    {
        public static Mutex mutex;
        /// <summary>
        /// アプリケーションのメイン エントリ ポイントです。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string APP_NAME = Application.ProductName;
            string OBJECT_NAME = "WitchsHat";
            string URL = "ipc://" + Application.ProductName + "/" + OBJECT_NAME;

            bool createdNew = false;
            mutex = new Mutex(true, APP_NAME, out createdNew);

            if (createdNew)
            {
                // 初回起動時
                Form1 f = new Form1();

                LifetimeServices.LeaseTime = TimeSpan.Zero;
                LifetimeServices.RenewOnCallTime = TimeSpan.Zero;
                IpcChannel ipc = new IpcChannel(APP_NAME);
                ChannelServices.RegisterChannel(ipc, false);
                RemotingServices.Marshal(f, OBJECT_NAME);

                Application.Run(f);

            }
            else
            {
                // 多重起動時
                IRemoteObject remoteObject = (IRemoteObject)RemotingServices.Connect(typeof(IRemoteObject), URL);

                remoteObject.StartupNextInstance(new object[] { Environment.GetCommandLineArgs() });
            }
        }
    }
}
